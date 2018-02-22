using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using JE.RMS.Common.Model;
using System.Net.Http.Formatting;
using System.Collections.Generic;
using JE.RMS.Common.Constants;
using System.Data;
using Dapper;
using System.Text;
using System.Data.SqlClient;
using System.Web;

namespace JE.RMS.Services
{
    public class ApplyReward
    {
        #region variables
        public static string clientIP = "";
        public static JObject reqMessage;
        public static IList<string> messages;
        public static bool valid;
        public static string saverewardsobj;
        public static decimal ProductValue = 0;
        public static string RewardTrxStatus = RewardTrxStatusEnum.ValidationError.GetDescription();
        public static string Status = string.Empty;
        public static string Message = string.Empty;
        public static string ChannelCode = string.Empty;
        public static int SourceSystemID = 0;
        #endregion

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.ApplyReward");
            try
            {
                //To obtain client IP Address
                clientIP = ((HttpContextWrapper)req.Properties["MS_HttpContext"]).Request.UserHostAddress;
                log.Verbose($"clientIP:={clientIP}", "JE.RMS.Services.ApplyReward");

                //Read request object as string
                string reqString = await req.Content.ReadAsStringAsync();
                reqMessage = JObject.Parse(reqString);
                var RewardCount = reqMessage["RewardsRequest"].Count();
                if (reqMessage["ChannelCode"] != null)
                {
                    ChannelCode = reqMessage["ChannelCode"].ToString();
                    if (string.IsNullOrEmpty(ChannelCode) || ChannelCode != FulfillmentChannel.EnergyEarth.GetDescription())
                    {
                        return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Only Energy Earth transactions are allowed. You provided ChannelCode:" + ChannelCode);
                    }
                }
                else
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Required value for ChannelCode. Path: ChannelCode;");
                }

                if (RewardCount == 1)
                {
                    foreach (JObject x in reqMessage["RewardsRequest"])
                    {
                        //Added Audit fields in request object
                        x.Add("SourceIP", clientIP);
                        x.Add("RMSRewardID", Guid.NewGuid().ToString());
                        x.Add("RewardsRequestReceiveTimestamp", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));

                        JObject inputJSON = JObject.Parse(JsonConvert.SerializeObject(x));

                        JSchema objectschema = new JSchema();
                        objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RequestSchema);

                        if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Qualify.GetDescription()
                            || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Terminate.GetDescription()
                            || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reactivate.GetDescription())
                        {
                            objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.Terminate_Reactivate_Qualify_Schema);
                        }
                        else if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reward.GetDescription())
                        {
                            //objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RewardSchema);
                            string product = inputJSON.SelectToken("Reward.ProductCode").ToString().Replace("{", "").Replace("}", "");
                            string program = inputJSON.SelectToken("Reward.ProgramName").ToString().Replace("{", "").Replace("}", "");
                            if (product != null && program != null)
                            {
                                List<SqlParameter> GetFulfillmentRuleParams = new List<SqlParameter>();
                                GetFulfillmentRuleParams.Add(new SqlParameter("@Product", product.ToString()));
                                GetFulfillmentRuleParams.Add(new SqlParameter("@Program", program.ToString()));
                                var ApiName = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetFulfillmentRule, GetFulfillmentRuleParams).FirstOrDefault();

                                if (ApiName == "Order")
                                {
                                    objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.OrderRewardSchema);
                                    inputJSON.Add("IsOrder", true);
                                }
                                else
                                {
                                    objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RewardSchema);
                                }
                            }
                        }
                        else if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.ProgramUpdateSourceSystem.GetDescription())
                        {
                            objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.ProgramUpdateSchema);
                        }
                        else
                        {
                            return req.CreateErrorResponse(HttpStatusCode.BadRequest, inputJSON["TransactionType"].ToString() + " not supported.Path: TransactionType; Only Energy Earth transactions are allowed.");
                        }

                        //Message schema validation
                        valid = inputJSON.IsValid(objectschema, out messages);
                        log.Verbose($"Valid ={valid}", "JE.RMS.Services.ApplyReward");
                        inputJSON.Add("IsValid", valid);
                        var messageJSON = "";
                        if (messages.Count > 0)
                            messageJSON = JsonConvert.SerializeObject(messages);
                        inputJSON.Add("ValidationMessage", messageJSON);
                        log.Verbose($"Validation message = {messageJSON}", "JE.RMS.Services.ApplyReward");

                        saverewardsobj = inputJSON.ToString();
                        log.Verbose($"Published message ={saverewardsobj}", "JE.RMS.Services.ApplyReward");
                        var reqObject = JsonConvert.DeserializeObject<RewardsRequest>(saverewardsobj);

                        // Call SaveRewardsTrx to save rewards transaction.
                        var SaveRewardTrxResponse = SaveRewardTrx(reqObject, errormessage, log);
                        //RewardTrxID > 0 means reward request is saved successfully & we can process further
                        if (SaveRewardTrxResponse.RewardTrxID > 0 && SaveRewardTrxResponse.IsValid == true && SaveRewardTrxResponse.Status != RewardTrxStatusEnum.ValidationError.GetDescription())
                        {
                            //Fetch from database with State 'Ready for fulfillment - Immediate'
                            List<SqlParameter> objprm = new List<SqlParameter>();
                            objprm.Add(new SqlParameter("@RewardTrxStatus", RewardTrxStatusEnum.ReadyForFulfillmentImmediate.GetDescription()));
                            List<GetRewardsRequest> lstRewardsTrx = MSSQLConnection.ExecuteStoredProcedure<GetRewardsRequest>(USPContstants.GetRewardsTrx, objprm).ToList();
                            log.Verbose($"Get RewardsTrx Received:={lstRewardsTrx.Count}", "JE.RMS.Services.ApplyReward");

                            //Now create RewardFulfillmentRequest
                            string rewardfulfillmentrequest = "[";
                            foreach (var item in lstRewardsTrx)
                            {
                                item.IsFulfillImmediate = true;
                                rewardfulfillmentrequest += JsonConvert.SerializeObject(item) + ",";
                            }
                            rewardfulfillmentrequest += "]";

                            //Evaluate Reward Trx (HTTP)
                            using (HttpClient client = new HttpClient())
                            {
                                var EvaluateRewardsTrxEndpoint = ConfigurationManager.AppSettings["EvaluateRewardsTrx"].ToString();
                                var accept = "application/json";
                                client.DefaultRequestHeaders.Add("Accept", accept);

                                using (var response = await client.PostAsync(EvaluateRewardsTrxEndpoint, new StringContent(rewardfulfillmentrequest, Encoding.UTF8, "application/x-www-form-urlencoded")))
                                {
                                    var result = await response.Content.ReadAsStringAsync();

                                    var evaluateResponse = JsonConvert.DeserializeObject<EvaluateRuleResponse>(result);

                                    if (response.IsSuccessStatusCode && (evaluateResponse.Status == RewardTrxStatusEnum.ReadyForFulfillmentImmediate.GetDescription() || evaluateResponse.Status == RewardTrxStatusEnum.WaitingForApproval.GetDescription() || evaluateResponse.Status == RewardTrxStatusEnum.SentForFulfillment.GetDescription()))
                                    {
                                        if (evaluateResponse.Status == RewardTrxStatusEnum.WaitingForApproval.GetDescription())
                                        {
                                            Message = "Reward submitted successfully, waiting for approval.";
                                        }
                                        if (evaluateResponse.Status == RewardTrxStatusEnum.SentForFulfillment.GetDescription())
                                        {
                                            Message = "Reward submitted successfully, Sent for fulfillment.";
                                        }

                                        List<SqlParameter> RewardTrxParams = new List<SqlParameter>();
                                        RewardTrxParams.Add(new SqlParameter("@RewardTrxID", SaveRewardTrxResponse.RewardTrxID));
                                        var RewardTrxID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.UpdateFulfillmentRequestTimestamp, RewardTrxParams);

                                        //Now again fetch from database to have updated record post evaluation.
                                        List<SqlParameter> objprm1 = new List<SqlParameter>();
                                        objprm1.Add(new SqlParameter("@RewardTrxStatus", RewardTrxStatusEnum.ReadyForFulfillmentImmediate.GetDescription()));
                                        List<GetRewardsRequest> lstRewardsTrx1 = MSSQLConnection.ExecuteStoredProcedure<GetRewardsRequest>(USPContstants.GetRewardsTrx, objprm1).ToList();
                                        if (lstRewardsTrx1.Count > 0)
                                        {
                                            lstRewardsTrx1 = lstRewardsTrx1.Where(p => p.RewardTrxID == SaveRewardTrxResponse.RewardTrxID).ToList();
                                            //Process Fulfillment for Energy Earth 
                                            var ProcessResult = ProcessRequest(lstRewardsTrx1, errormessage, log);
                                            if (ProcessResult != "Success")
                                            {
                                                return CreateErrorResponse(req, "Error while processing Reward : " + ProcessResult);
                                            }
                                        }
                                        log.Verbose($"Response ={result}", "JE.RMS.Services.ApplyReward");
                                    }
                                    else
                                    {

                                        if (evaluateResponse.Status == RewardTrxStatusEnum.Error.GetDescription()
                                            || evaluateResponse.Status == RewardTrxStatusEnum.RejectedSystem.GetDescription()
                                            || evaluateResponse.Status == RewardTrxStatusEnum.ValidationError.GetDescription())
                                        {
                                            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, result);
                                        }
                                        else
                                        {
                                            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, new Exception(result));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            return req.CreateErrorResponse(HttpStatusCode.BadRequest, SaveRewardTrxResponse.ValidationMessage);
                        }
                    }
                }
                else
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Only 1 reward request is allowed. You provided :" + RewardCount.ToString());
                }

                Status = "Success";
                if (Message == string.Empty)
                {
                    Message = "Reward applied successfully.";
                }
                return req.CreateResponse(HttpStatusCode.OK, new { Status, Message }, JsonMediaTypeFormatter.DefaultMediaType);
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.ApplyReward");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message, ex);
            }
        }

        private static SaveRewardResponse SaveRewardTrx(RewardsRequest item, ICollector<string> errormessage, TraceWriter log)
        {
            SaveRewardResponse saveRewardResponse = new SaveRewardResponse();
            saveRewardResponse.RewardTrxID = 0;
            try
            {
                using (var connection = MSSQLConnection.CreateConnection())
                {
                    connection.Open();

                    // Call stored procedure to Save Customer
                    Customer Customers = new Customer();
                    GetRewardsRequest SaveRewardTrx = new GetRewardsRequest();

                    if (item.IsValid)
                        RewardTrxStatus = RewardTrxStatusEnum.ReadyForFulfillmentImmediate.GetDescription();

                    if (string.IsNullOrEmpty(item.Customer.SourceSystemUniqueIDType))
                    {
                        RewardTrxStatus = "Validation error";
                        item.ValidationMessage += " Source system is not supported.";
                    }

                    if (item.Reward != null && !string.IsNullOrEmpty(item.Reward.ProductValue) && !Decimal.TryParse(item.Reward.ProductValue, out ProductValue))
                    {
                        RewardTrxStatus = "Validation error";
                        item.ValidationMessage += " Required numeric value for ProductValue field.";
                    }

                    if (!string.IsNullOrEmpty(item.Customer.SourceSystemID))
                    {
                        int.TryParse(item.Customer.SourceSystemID, out SourceSystemID);
                        if (SourceSystemID <= 0)
                        {
                            item.IsValid = false;
                            RewardTrxStatus = RewardTrxStatusEnum.ValidationError.GetDescription();
                            item.ValidationMessage += "Validation Failed : SourceSystemID must be numeric;";
                        }
                    }

                    if (SourceSystemID > 0 || !string.IsNullOrEmpty(item.Customer.SourceSystemName))
                    {
                        List<SqlParameter> SSIDprm = new List<SqlParameter>();
                        SSIDprm.Add(new SqlParameter("@SourceSystemShortName", item.Customer.SourceSystemName));
                        SSIDprm.Add(new SqlParameter("@SourceSystemID", item.Customer.SourceSystemID));

                        SourceSystemID = MSSQLConnection.ExecuteStoredProcedure<int>(USPContstants.GetSourceSystemID, SSIDprm).FirstOrDefault();
                        if (SourceSystemID == 0)
                        {
                            item.IsValid = false;
                            RewardTrxStatus = RewardTrxStatusEnum.ValidationError.GetDescription();
                            item.ValidationMessage += "Validation Failed : Please provide valid SourceSystemID or SourceSystemName (if both are provided they must match);";
                        }
                    }
                    else if (item.IsOrder == false)
                    {
                        item.IsValid = false;
                        RewardTrxStatus = RewardTrxStatusEnum.ValidationError.GetDescription();
                        item.ValidationMessage += "Validation Failed : Required value for at least one of 'SourceSystemID' or 'SourceSystemName'. Path: SourceSystemID or SourceSystemName;";
                    }

                    item.ValidationMessage = item.ValidationMessage.TrimStart(';');

                    DynamicParameters CustomerTrxParams = new DynamicParameters();
                    CustomerTrxParams.Add("@SourceSystemID", SourceSystemID.ToString());
                    CustomerTrxParams.Add("@SourceSystemUniqueID", item.Customer.SourceSystemUniqueID);
                    CustomerTrxParams.Add("@SourceSystemUniqueIDType", item.Customer.SourceSystemUniqueIDType);
                    CustomerTrxParams.Add("@MasterID", item.Customer.MasterID);
                    CustomerTrxParams.Add("@Email", item.Customer.Email);
                    CustomerTrxParams.Add("@FirstName", item.Customer.FirstName);
                    CustomerTrxParams.Add("@LastName", item.Customer.LastName);
                    CustomerTrxParams.Add("@CompanyName", item.Customer.CompanyName);
                    CustomerTrxParams.Add("@AddressLine1", item.Customer.AddressLine1);
                    CustomerTrxParams.Add("@AddressLine2", item.Customer.AddressLine2);
                    CustomerTrxParams.Add("@City", item.Customer.City);
                    CustomerTrxParams.Add("@StateProvince", item.Customer.StateProvince);
                    CustomerTrxParams.Add("@ZipPostalCode", item.Customer.ZipPostalCode);
                    CustomerTrxParams.Add("@Phone1", item.Customer.Phone1);
                    CustomerTrxParams.Add("@Product", item.Customer.Product);
                    CustomerTrxParams.Add("@Language", item.Customer.Language);

                    CustomerTrxParams.Add("@RequestID", item.RequestId);
                    CustomerTrxParams.Add("@TransactionType", item.TransactionType);
                    CustomerTrxParams.Add("@ProductCode", (item.Reward != null && !string.IsNullOrEmpty(item.Reward.ProductCode)) ? item.Reward.ProductCode : string.Empty);
                    CustomerTrxParams.Add("@ProductValue", ProductValue);
                    CustomerTrxParams.Add("@ProgramName", (item.Reward != null && !string.IsNullOrEmpty(item.Reward.ProgramName)) ? item.Reward.ProgramName : string.Empty);
                    if (item.Reward != null)
                        CustomerTrxParams.Add("@EffectiveDate", item.Reward.EffectiveDate);
                    else
                        CustomerTrxParams.Add("@EffectiveDate", string.Empty);
                    CustomerTrxParams.Add("@SourceIP", item.SourceIP);
                    CustomerTrxParams.Add("@RewardsRequestReceiveTimestamp", item.RewardsRequestReceiveTimestamp);
                    CustomerTrxParams.Add("@RMSRewardID", item.RMSRewardID);
                    var additionalDataJSON = JsonConvert.SerializeObject(item.AdditionalData);
                    CustomerTrxParams.Add("@AdditionalData", additionalDataJSON);
                    CustomerTrxParams.Add("@RewardType", (!string.IsNullOrEmpty(item.Reward.RewardType)) ? item.Reward.RewardType : string.Empty);

                    CustomerTrxParams.Add("@MessageType", MessageType.RewardRequest.GetDescription());
                    CustomerTrxParams.Add("@Message", Message);

                    CustomerTrxParams.Add("@RewardTrxStatus", RewardTrxStatus);
                    CustomerTrxParams.Add("@Comment", item.ValidationMessage);

                    SaveRewardTrx = connection.Query<GetRewardsRequest>(Common.Constants.USPContstants.SaveRewards, CustomerTrxParams, null, true, null, CommandType.StoredProcedure).FirstOrDefault();

                    log.Verbose($"SaveRewardTrx sucessfully ={SaveRewardTrx.RewardTrxID}", "JE.RMS.Services.SaveRewardsTrx");

                    saveRewardResponse.Status = SaveRewardTrx.RewardTrxStatus;
                    saveRewardResponse.RewardTrxID = SaveRewardTrx.RewardTrxID;
                    saveRewardResponse.IsValid = item.IsValid;
                    //Hack : SP returns validation message in Reward type field (this is internal)
                    saveRewardResponse.ValidationMessage = item.ValidationMessage + SaveRewardTrx.RewardType;
                }


                return saveRewardResponse;
            }
            catch (Exception ex)
            {
                log.Error($"Exception={ex}", ex, "JE.RMS.Services.ApplyReward");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                saveRewardResponse.IsValid = false;
                saveRewardResponse.ValidationMessage = ex.Message;
                saveRewardResponse.Status = RewardTrxStatusEnum.Error.GetDescription();
                return saveRewardResponse;
            }
        }

        private static string ProcessRequest(List<GetRewardsRequest> lstRewardsTrx, ICollector<string> errormessage, TraceWriter log)
        {
            try
            {
                RewardFulfillmentRequest RewardsRequestObj = new RewardFulfillmentRequest();
                foreach (GetRewardsRequest item in lstRewardsTrx)
                {
                    RewardFulfillmentRequestList rewardFulfillmentRequestList = new RewardFulfillmentRequestList();
                    rewardFulfillmentRequestList.RewardFulfillmentRequest = new RewardFulfillmentRequest();

                    RewardsRequestObj.RequestId = item.RequestId;
                    RewardsRequestObj.TransactionType = item.TransactionType;
                    RewardsRequestObj.RMSRewardID = item.RMSRewardID;

                    RewardsRequestObj.Reward = new Reward();
                    RewardsRequestObj.Reward.ProductCode = item.ProductCode;
                    RewardsRequestObj.Reward.ProductValue = item.ProductValue.ToString();
                    RewardsRequestObj.Reward.ProgramName = item.ProgramName;
                    RewardsRequestObj.Reward.EffectiveDate = item.EffectiveDate;
                    RewardsRequestObj.Reward.RewardType = item.RewardType;

                    RewardsRequestObj.Customer = new CustomerJSON();
                    RewardsRequestObj.Customer.CustomerID = item.CustomerID;
                    RewardsRequestObj.Customer.SourceSystemID = item.DBSourceSystemID.ToString();
                    RewardsRequestObj.Customer.SourceSystemName = item.SourceSystemShortName;
                    RewardsRequestObj.Customer.SourceSystemUniqueID = item.SourceSystemUniqueID;
                    RewardsRequestObj.Customer.SourceSystemUniqueIDType = item.SourceSystemUniqueIDType;
                    RewardsRequestObj.Customer.MasterID = item.MasterID;
                    RewardsRequestObj.Customer.Email = item.Email;
                    RewardsRequestObj.Customer.FirstName = item.FirstName;
                    RewardsRequestObj.Customer.LastName = item.LastName;
                    RewardsRequestObj.Customer.CompanyName = item.CompanyName;
                    RewardsRequestObj.Customer.AddressLine1 = item.AddressLine1;
                    RewardsRequestObj.Customer.AddressLine2 = item.AddressLine2;
                    RewardsRequestObj.Customer.City = item.City;
                    RewardsRequestObj.Customer.StateProvince = item.StateProvince;
                    RewardsRequestObj.Customer.ZipPostalCode = item.ZipPostalCode;
                    RewardsRequestObj.Customer.Phone1 = item.Phone1;
                    RewardsRequestObj.Customer.Product = item.Product;
                    RewardsRequestObj.Customer.Language = item.LanguageCode;
                    RewardsRequestObj.AdditionalData = JsonConvert.DeserializeObject<AdditionalData[]>(item.AdditionalData);
                    rewardFulfillmentRequestList.RewardFulfillmentRequest = RewardsRequestObj;
                    var reqObject = JsonConvert.SerializeObject(rewardFulfillmentRequestList);

                    //Call stored procedure to Update RewardTrx
                    List<SqlParameter> RewardTrxParams = new List<SqlParameter>();
                    RewardTrxParams.Add(new SqlParameter("@RewardTrxID", item.RewardTrxID));
                    var RewardTrxID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.UpdateFulfillmentRequestTimestamp, RewardTrxParams);
                    log.Verbose($"RewardTrx updated successfully. RewardTrID={RewardTrxID[0]}", "JE.RMS.Services.ApplyReward");

                    //Call stored procedure to Save MessageLog
                    List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                    MessageLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID));
                    MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentRequest.GetDescription()));
                    MessageLogParams.Add(new SqlParameter("@IPAddress", ""));
                    MessageLogParams.Add(new SqlParameter("@Message", reqObject));
                    MessageLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                    log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ApplyReward");

                    //Call stored procedure to Save RewardTrxChangeLog
                    List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID.ToString()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", "Sent for fulfillment"));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", string.Empty));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={RewardTrxChangeLogID[0]}", "JE.RMS.Services.ApplyReward");

                    if (item.FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.EnergyEarth)
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["ProcessFulfillmentUrl"].ToString());

                            var response = httpClient.PostAsJsonAsync(string.Empty, rewardFulfillmentRequestList).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                log.Verbose($"Success : Process fulfillment for EE", "JE.RMS.Services.ApplyReward");
                            }
                            else
                            {
                                log.Error($"Error : Process fulfillment for EE", null, "JE.RMS.Services.ApplyReward");
                                return response.Content.ReadAsStringAsync().Result;
                            }
                        }
                    }
                }
                return "Success";
            }
            catch (Exception ex)
            {
                log.Error($"Exception={ex}", ex, "JE.RMS.Services.ApplyReward");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return "Fail";
            }
        }

        private static HttpResponseMessage CreateErrorResponse(HttpRequestMessage req, string Message, Exception ex = null)
        {
            if (ex != null)
            {
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, Message, ex);
            }
            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, Message);
        }

        private class SaveRewardResponse
        {
            public long RewardTrxID { get; set; }
            public bool IsValid { get; set; }
            public string ValidationMessage { get; set; }
            public string Status { get; set; }
        }
    }

}