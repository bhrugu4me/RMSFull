using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using JE.RMS.Common.Model;
using System.Data.SqlClient;
using JE.RMS.Common.Constants;

namespace JE.RMS.Services
{
    public class ProcessFulfillmentForEnergyEarth
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

            try
            {
                string Message = await req.Content.ReadAsStringAsync();
                long changeLogID = 0;
                var RewardFulfillmentRequest = JsonConvert.DeserializeObject<RewardFulfillmentRequestList>(Message).RewardFulfillmentRequest;
                string energyEarthResponseMessage = string.Empty;

                #region Order API
                if (RewardFulfillmentRequest != null && RewardFulfillmentRequest.Reward != null)
                {
                    List<SqlParameter> GetFulfillmentRuleParams = new List<SqlParameter>();
                    GetFulfillmentRuleParams.Add(new SqlParameter("@Product", RewardFulfillmentRequest.Reward.ProductCode.ToString()));
                    GetFulfillmentRuleParams.Add(new SqlParameter("@Program", RewardFulfillmentRequest.Reward.ProgramName.ToString()));
                    var ApiName = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetFulfillmentRule, GetFulfillmentRuleParams).FirstOrDefault();

                    if (!string.IsNullOrEmpty(ApiName) && ApiName == "Order")
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["CreateOrderFunctionUrl"].ToString());

                            string CreateOrderFunctionUrl = "";
                            log.Verbose($"Calling CreateOrder API", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            var EEOrder = new EEOrder();
                            EEOrder.BatchID = RewardFulfillmentRequest.RequestId;
                            EEOrder.Programs = new List<EEProgram>();
                            EEProgram eeProgam = new EEProgram();
                            var GetEEProgramName = new List<SqlParameter>();
                            GetEEProgramName.Add(new SqlParameter("@ProgramCode", RewardFulfillmentRequest.Reward.ProgramName));
                            var EnergyEarthProgramName = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetEnergyEarthProgramName, GetEEProgramName).FirstOrDefault();
                            if (!string.IsNullOrEmpty(EnergyEarthProgramName))
                            {
                                eeProgam.ProgramName = EnergyEarthProgramName;
                            }
                            else
                            {
                                Message += "Cannot find energy earth program name associated with program : " + RewardFulfillmentRequest.Reward.ProgramName;
                            }
                            eeProgam.Recipients = new List<Recipient>();
                            eeProgam.Recipients.Add(new Recipient()
                            {
                                Address1 = RewardFulfillmentRequest.Customer.AddressLine1,
                                Address2 = RewardFulfillmentRequest.Customer.AddressLine2,
                                CompanyName = RewardFulfillmentRequest.Customer.CompanyName,
                                City = RewardFulfillmentRequest.Customer.City,
                                Email = RewardFulfillmentRequest.Customer.Email,
                                FirstName = RewardFulfillmentRequest.Customer.FirstName,
                                Identifier = RewardFulfillmentRequest.RequestId,
                                LastName = RewardFulfillmentRequest.Customer.LastName,
                                Phone = RewardFulfillmentRequest.Customer.Phone1,
                                PostalCode = RewardFulfillmentRequest.Customer.ZipPostalCode,
                                StateProvinceCode = RewardFulfillmentRequest.Customer.StateProvince,
                                Value = Convert.ToDouble(RewardFulfillmentRequest.Reward.ProductValue)
                            });
                            EEOrder.Programs.Add(eeProgam);

                            ////Call stored procedure to Save MessageLog
                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(EEOrder).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            var response = await httpClient.PostAsJsonAsync(CreateOrderFunctionUrl, EEOrder);

                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };

                            if (response.IsSuccessStatusCode)
                            {
                                fulfillmentResponse.Status = "Success";
                                log.Verbose($"Success : Create Order API", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";

                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while Register Points Energy earth: " + energyEarthResponseMessage, null);
                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;
                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                ////Call stored procedure to Save MessageLog
                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }

                            ////Call stored procedure to Save MessageLog
                            MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            #region Update Audit Fields
                            //Update timestapm in Audit fields
                            List<SqlParameter> AuditParameters = new List<SqlParameter>();
                            AuditParameters.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                            var RMSID = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParameters);
                            log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            ////Call stored procedure to Save RewardTrxChangeLog
                            changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.FulfillmentCompleted.GetDescription(), string.Empty, null);
                            log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            changeLogID = 0;
                            #endregion

                            return req.CreateResponse(HttpStatusCode.OK);
                        }
                    }
                }
                #endregion

                #region Customer Unique ID

                //Check if user exists, if not, create new user
                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@CustomerID", RewardFulfillmentRequest.Customer.CustomerID));

                string CustomerUniqueId = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetCustomerUniqueID, objprm).FirstOrDefault();

                RewardPointForUser rewardPoint = new RewardPointForUser();
                rewardPoint.Amount = Convert.ToDecimal(RewardFulfillmentRequest.Reward.ProductValue) / 100;
                rewardPoint.Description = RewardFulfillmentRequest.Reward.RewardType;
                rewardPoint.PointAmount = Convert.ToInt32(Math.Round(Convert.ToDouble(RewardFulfillmentRequest.Reward.ProductValue)));

                objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@ProgramCode", RewardFulfillmentRequest.Reward.ProgramName));
                var EnergyEarthProgram = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetEnergyEarthProgramName, objprm).FirstOrDefault();
                if (!string.IsNullOrEmpty(EnergyEarthProgram))
                {
                    RewardFulfillmentRequest.Reward.ProgramName = EnergyEarthProgram;
                }
                else if (RewardFulfillmentRequest.TransactionType == TransactionTypeEnum.Reward.GetDescription() || RewardFulfillmentRequest.TransactionType == TransactionTypeEnum.ProgramUpdateSourceSystem.GetDescription())
                {
                    //Invalid Program Name

                    FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                    {
                        RequestID = RewardFulfillmentRequest.RequestId,
                        RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                        Message = "Couldn't find energy earth program name for given Program : " + RewardFulfillmentRequest.Reward.ProgramName,
                        Status = "Fail"
                    };

                    changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while Register Points Energy earth: " + energyEarthResponseMessage, null);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                    changeLogID = 0;
                    List<SqlParameter> AuditParams = new List<SqlParameter>();
                    AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                    var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                    log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    ////Call stored procedure to Save MessageLog
                    List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                    MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                    MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                    MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                    MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                    MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                    var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                    log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Invalid ProgramName for Energy Earth");
                }

                if (!string.IsNullOrEmpty(CustomerUniqueId))
                {
                    rewardPoint.UserID = CustomerUniqueId.ToString();

                    List<SqlParameter> customerIDParam = new List<SqlParameter>();
                    customerIDParam.Add(new SqlParameter("@customerID", RewardFulfillmentRequest.Customer.CustomerID));
                    log.Verbose($"calling sp", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    CustomerExtended CustomerExtObj = MSSQLConnection.ExecuteStoredProcedure<CustomerExtended>(USPContstants.GetCustomerExtendedOnCustomer, customerIDParam).FirstOrDefault();

                    var NeedsProgramUpdate = MSSQLConnection.ExecuteStoredProcedure<bool>(USPContstants.CheckReactivation, customerIDParam).FirstOrDefault();

                    log.Info($"Needs ProgramUpdate : {NeedsProgramUpdate}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    if (NeedsProgramUpdate == true && RewardFulfillmentRequest.TransactionType == TransactionTypeEnum.Reward.GetDescription() && CustomerExtObj != null
                        && !string.IsNullOrEmpty(EnergyEarthProgram) && CustomerExtObj.Program != EnergyEarthProgram)
                    {
                        //As Program Name is different in Energy Earth, update the same before proceeding with applying reward
                        using (HttpClient httpClient = new HttpClient())
                        {
                            EEUser user = new EEUser()
                            {
                                Program = EnergyEarthProgram,
                                UserID = Guid.Parse(CustomerUniqueId)
                            };

                            //Add Basic Authentication header
                            httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["SaveUsersFunctionUrl"].ToString());

                            log.Verbose($"Called Update User", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(user).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            //Call Add User API [HTTPPOST]
                            var response = await httpClient.PostAsJsonAsync("", user);

                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };

                            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Created)
                            {
                                log.Verbose($"Success : Update User", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";
                                //Error while creating user 
                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while update Energy earth User." + energyEarthResponseMessage, null);
                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;

                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }
                        }
                    }
                }
                else if (RewardFulfillmentRequest.TransactionType == TransactionTypeEnum.Reward.GetDescription())
                {
                    //Create User & Add points     
                    EEUser user = new EEUser()
                    {
                        Address1 = RewardFulfillmentRequest.Customer.AddressLine1,
                        Address2 = RewardFulfillmentRequest.Customer.AddressLine2,
                        City = RewardFulfillmentRequest.Customer.City,
                        CompanyName = RewardFulfillmentRequest.Customer.CompanyName,
                        Email = RewardFulfillmentRequest.Customer.Email,
                        FirstName = RewardFulfillmentRequest.Customer.FirstName,
                        LanguageCode = RewardFulfillmentRequest.Customer.Language,
                        LastName = RewardFulfillmentRequest.Customer.LastName,
                        Phone = RewardFulfillmentRequest.Customer.Phone1,
                        PostalCode = RewardFulfillmentRequest.Customer.ZipPostalCode,
                        StateProvinceCode = RewardFulfillmentRequest.Customer.StateProvince,
                        Program = RewardFulfillmentRequest.Reward.ProgramName,
                        //ClientID = RewardFulfillmentRequest.Customer.CustomerID
                    };

                    HttpResponseMessage response = new HttpResponseMessage();

                    //Create EE User 
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["SaveUsersFunctionUrl"].ToString());

                        log.Verbose($"Called Save User (New User)", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                        MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                        MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                        MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                        MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(user).ToString()));
                        MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                        var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                        log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        //Call Add User API [HTTPPOST]
                        response = await httpClient.PostAsJsonAsync("", user);
                        energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result.Replace(@"\", string.Empty).Replace("\"", string.Empty);

                        FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                        {
                            RequestID = RewardFulfillmentRequest.RequestId,
                            RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                            Message = energyEarthResponseMessage
                        };

                        log.Verbose("StatusCode :" + response.StatusCode);
                        log.Verbose("IsSuccessStatusCode :" + response.IsSuccessStatusCode.ToString());

                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Created)
                        {
                            //fetch customerExtended for created user
                            rewardPoint.UserID = response.Content.ReadAsStringAsync().Result.Replace(@"\", string.Empty).Replace("\"", string.Empty);
                            CustomerUniqueId = rewardPoint.UserID;
                            log.Verbose($"Success : Add User", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        }
                        else
                        {
                            fulfillmentResponse.Status = "Fail";

                            //Error while creating user 
                            changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while creating Energy earth User : " + energyEarthResponseMessage, null);
                            log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            changeLogID = 0;

                            List<SqlParameter> AuditParams = new List<SqlParameter>();
                            AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                            var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                            log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                            MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            return response;
                        }
                    }
                }
                else
                {
                    FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                    {
                        RequestID = RewardFulfillmentRequest.RequestId,
                        RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                        Message = "Cannot find Energy Earth user associated with Email :" + RewardFulfillmentRequest.Customer.Email,
                        Status = "Fail"
                    };

                    changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), fulfillmentResponse.Message, null);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                    changeLogID = 0;

                    List<SqlParameter> AuditParams = new List<SqlParameter>();
                    AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                    var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                    log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                    MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                    MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                    MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                    MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                    MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                    var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                    log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                    return req.CreateErrorResponse(HttpStatusCode.InternalServerError, fulfillmentResponse.Message);
                }
                #endregion

                switch (RewardFulfillmentRequest.TransactionType)
                {
                    case "Reward":
                        #region Add Points
                        //Add points

                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["RegisterPointFunctionUrl"].ToString());

                            string RegisterPointsUrl = "";
                            log.Verbose($"Calling Register points API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(rewardPoint).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            var response = await httpClient.PostAsJsonAsync(RegisterPointsUrl, rewardPoint);
                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };

                            if (response.IsSuccessStatusCode)
                            {
                                fulfillmentResponse.Status = "Success";
                                log.Verbose($"Success : Register points API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";

                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while Register Points Energy earth: " + energyEarthResponseMessage, null);
                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;
                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                ////Call stored procedure to Save MessageLog
                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }

                            ////Call stored procedure to Save MessageLog
                            MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        }


                        #endregion
                        break;
                    case "Terminate":
                    case "Reactivate":
                        #region Update point Status : Suspend/Unsuspend
                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["UpdatePointStatusFunctionUrl"].ToString());

                            string UpdatePointStatusFunctionUrl = "";
                            EEPointStatus PointStatus = new EEPointStatus();
                            PointStatus.UserID = CustomerUniqueId;
                            if (RewardFulfillmentRequest.TransactionType == "Terminate")
                            {
                                PointStatus.Status = "Suspend";
                            }
                            else if (RewardFulfillmentRequest.TransactionType == "Reactivate")
                            {
                                PointStatus.Status = "Unsuspend";
                            }

                            log.Verbose($"Calling Update point status API, UserId : {CustomerUniqueId}, with Status : {PointStatus.Status}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(PointStatus).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            var response = await httpClient.PostAsJsonAsync(UpdatePointStatusFunctionUrl, PointStatus);

                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };


                            if (response.IsSuccessStatusCode)
                            {
                                fulfillmentResponse.Status = "Success";
                                log.Verbose($"Success : Update point status API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";

                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while Update point status API Energy earth" + energyEarthResponseMessage, null);
                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;

                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                ////Call stored procedure to Save MessageLog
                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }

                            ////Call stored procedure to Save MessageLog
                            MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        }
                        #endregion
                        break;
                    case "Qualify":
                        #region Update point Status : Qualify
                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["UpdatePointStatusFunctionUrl"].ToString());

                            string UpdatePointStatusFunctionUrl = "";
                            EEPointStatus PointStatus = new EEPointStatus();
                            PointStatus.UserID = CustomerUniqueId;
                            PointStatus.Status = "Qualify";

                            log.Verbose($"Calling Update point status API, UserId : {CustomerUniqueId}, with Status : {PointStatus.Status}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(PointStatus).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            var response = await httpClient.PostAsJsonAsync(UpdatePointStatusFunctionUrl, PointStatus);

                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };

                            if (response.IsSuccessStatusCode)
                            {
                                fulfillmentResponse.Status = "Success";
                                log.Verbose($"Success : Update point status API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";
                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while Update point status Energy earth." + energyEarthResponseMessage, null);

                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;
                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                ////Call stored procedure to Save MessageLog
                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }

                            ////Call stored procedure to Save MessageLog
                            MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        }
                        #endregion

                        //#region Add points if Product value >0
                        //if (RewardFulfillmentRequest.Reward != null && !string.IsNullOrEmpty(RewardFulfillmentRequest.Reward.ProductValue) && Convert.ToDecimal(RewardFulfillmentRequest.Reward.ProductValue) > 0)
                        //{
                        //    using (HttpClient httpClient = new HttpClient())
                        //    {
                        //        //Add Basic Authentication header
                        //        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["RegisterPointFunctionUrl"].ToString());

                        //        string RegisterPointsUrl = "";
                        //        log.Verbose($"Calling Register points API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        //        List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                        //        MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                        //        MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                        //        MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                        //        MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(rewardPoint).ToString()));
                        //        MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                        //        var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                        //        log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        //        var response = await httpClient.PostAsJsonAsync(RegisterPointsUrl, rewardPoint);

                        //        energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                        //        FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                        //        {
                        //            RequestID = RewardFulfillmentRequest.RequestId,
                        //            RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                        //            Message = energyEarthResponseMessage
                        //        };

                        //        if (response.IsSuccessStatusCode)
                        //        {
                        //            fulfillmentResponse.Status = "Success";
                        //            log.Verbose($"Success : Register points API, UserId : {CustomerUniqueId}, Reward Amount : {RewardFulfillmentRequest.Reward.ProductValue}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        //        }
                        //        else
                        //        {
                        //            fulfillmentResponse.Status = "Fail";

                        //            changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), energyEarthResponseMessage, null);
                        //            log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        //            changeLogID = 0;

                        //            List<SqlParameter> AuditParams = new List<SqlParameter>();
                        //            AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                        //            var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                        //            log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        //            ////Call stored procedure to Save MessageLog
                        //            List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                        //            MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                        //            MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                        //            MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                        //            MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                        //            MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                        //            var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                        //            log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        //            return response;
                        //        }

                        //        ////Call stored procedure to Save MessageLog
                        //        MessageLogParams = new List<SqlParameter>();
                        //        MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                        //        MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                        //        MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                        //        MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                        //        MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                        //        MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                        //        log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        //    }
                        //}
                        //#endregion
                        break;
                    case "ProgramUpdateSourceSystem":
                        #region Update Customer data

                        //Create EE User 
                        using (HttpClient httpClient = new HttpClient())
                        {
                            EEUser user = new EEUser()
                            {
                                Address1 = RewardFulfillmentRequest.Customer.AddressLine1,
                                Address2 = RewardFulfillmentRequest.Customer.AddressLine2,
                                City = RewardFulfillmentRequest.Customer.City,
                                CompanyName = RewardFulfillmentRequest.Customer.CompanyName,
                                Email = RewardFulfillmentRequest.Customer.Email,
                                FirstName = RewardFulfillmentRequest.Customer.FirstName,
                                LanguageCode = RewardFulfillmentRequest.Customer.Language,
                                LastName = RewardFulfillmentRequest.Customer.LastName,
                                Phone = RewardFulfillmentRequest.Customer.Phone1,
                                PostalCode = RewardFulfillmentRequest.Customer.ZipPostalCode,
                                StateProvinceCode = RewardFulfillmentRequest.Customer.StateProvince,
                                Program = RewardFulfillmentRequest.Reward.ProgramName,
                                UserID = Guid.Parse(CustomerUniqueId)
                            };

                            //Add Basic Authentication header
                            httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["SaveUsersFunctionUrl"].ToString());

                            log.Verbose($"Called Update User", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.EnergyEarthRequest.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(user).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                            //Call Add User API [HTTPPOST]
                            var response = await httpClient.PostAsJsonAsync("", user);

                            energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                            FulfillmentResponse fulfillmentResponse = new FulfillmentResponse()
                            {
                                RequestID = RewardFulfillmentRequest.RequestId,
                                RMSRewardID = RewardFulfillmentRequest.RMSRewardID,
                                Message = energyEarthResponseMessage
                            };

                            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Created)
                            {
                                fulfillmentResponse.Status = "Success";
                                log.Verbose($"Success : Update User", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            }
                            else
                            {
                                fulfillmentResponse.Status = "Fail";
                                //Error while creating user 
                                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.Error.GetDescription(), "Error while update Energy earth User." + energyEarthResponseMessage, null);
                                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                                changeLogID = 0;

                                List<SqlParameter> AuditParams = new List<SqlParameter>();
                                AuditParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                                var RMSReward = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParams);
                                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSReward[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                List<SqlParameter> MessageLogFailParams = new List<SqlParameter>();
                                MessageLogFailParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                                MessageLogFailParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                                MessageLogFailParams.Add(new SqlParameter("@IPAddress", string.Empty));
                                MessageLogFailParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                                MessageLogFailParams.Add(new SqlParameter("@RewardsTrxID", null));
                                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogFailParams);
                                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                                return response;
                            }

                            MessageLogParams = new List<SqlParameter>();
                            MessageLogParams.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));
                            MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                            MessageLogParams.Add(new SqlParameter("@IPAddress", string.Empty));
                            MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(fulfillmentResponse).ToString()));
                            MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                            MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                            log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        }
                        #endregion
                        break;
                    default:
                        break;
                }

                #region Update Customer Extended
                //Get Customer Extended
                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["GetUserFunctionUrl"].ToString());

                    string GetUserFunctionUrl = ConfigurationManager.AppSettings["GetUserFunctionCode"].ToString() + CustomerUniqueId;
                    log.Verbose($"Calling Get EEUser API, UserId : {CustomerUniqueId}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                    var response = await httpClient.GetAsync(GetUserFunctionUrl);

                    energyEarthResponseMessage = energyEarthResponseMessage + response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        log.Verbose($"Success : Get EEUser API, UserId : {CustomerUniqueId}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                        var responseUser = JsonConvert.DeserializeObject<CustomerExtendedList>(await response.Content.ReadAsStringAsync());
                        var userData = responseUser.Users.FirstOrDefault();
                        userData.CustomerID = RewardFulfillmentRequest.Customer.CustomerID;
                        userData.UniqueID = CustomerUniqueId;

                        var CustomerExtendedID = SaveCustomerExtended(userData);
                        log.Verbose($"Customer extended stored successfully. CustomerExtendedID={CustomerExtendedID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                    }
                    else
                    {
                        log.Verbose($"Error : Get EEUser API, UserId : {CustomerUniqueId}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                        return response;
                    }
                }
                #endregion

                #region Update Audit Fields
                //Update timestapm in Audit fields
                List<SqlParameter> AuditParam = new List<SqlParameter>();
                AuditParam.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentRequest.RMSRewardID));

                var RMSRewardID = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParam);
                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSRewardID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");



                ////Call stored procedure to Save RewardTrxChangeLog
                changeLogID = SaveRewardTrxChangeLog(RewardFulfillmentRequest.RMSRewardID, RewardTrxStatusEnum.FulfillmentCompleted.GetDescription(), string.Empty, null);
                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={changeLogID}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                changeLogID = 0;
                #endregion

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        private static long SaveCustomerExtended(CustomerExtended userData)
        {
            List<SqlParameter> CustomerExtendedParams = new List<SqlParameter>();
            CustomerExtendedParams.Add(new SqlParameter("@CustomerID", userData.CustomerID));
            CustomerExtendedParams.Add(new SqlParameter("@UniqueID", userData.UniqueID));
            CustomerExtendedParams.Add(new SqlParameter("@AccountAcceptanceDate", userData.AccountAcceptanceDate));
            CustomerExtendedParams.Add(new SqlParameter("@StartingPointBalance", userData.StartingPointBalance));
            CustomerExtendedParams.Add(new SqlParameter("@AvailablePointBalance", userData.AvailablePointBalance));
            CustomerExtendedParams.Add(new SqlParameter("@AvailablePointBalanceDollars", userData.AvailablePointBalanceDollars));
            CustomerExtendedParams.Add(new SqlParameter("@NumberofTransactions", userData.NumberofTransactions));
            CustomerExtendedParams.Add(new SqlParameter("@AccountStatus", userData.AccountStatus));
            CustomerExtendedParams.Add(new SqlParameter("@NextRewardDueDate", userData.NextRewardDueDate));
            CustomerExtendedParams.Add(new SqlParameter("@ProgramName", userData.Program));
            var customerExtendedID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveCustomerExtended, CustomerExtendedParams);
            return customerExtendedID[0];
        }

        private static long SaveRewardTrxChangeLog(string RMSRewardID, string RewardTrxStatus, string Comment, string RewardTrxID)
        {
            List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
            RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", RMSRewardID));
            RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", RewardTrxStatus));
            RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", Comment));
            RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", RewardTrxID));

            var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
            return RewardTrxChangeLogID[0];
        }
    }
}