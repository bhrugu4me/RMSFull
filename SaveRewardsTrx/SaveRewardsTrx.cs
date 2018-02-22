using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using System;
using JE.RMS.Common.Model;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using Microsoft.Azure.WebJobs;
using JE.RMS.Common.Constants;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace JE.RMS.Services
{
    public class SaveRewardsTrx
    {
        #region variables
        public static decimal ProductValue = 0;
        public static string RewardTrxStatus = "Validation error";
        public static int SourceSystemID = 0;
        #endregion

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.SaveRewardsTrx");
            try
            {
                string Message = await req.Content.ReadAsStringAsync();
                var item = JsonConvert.DeserializeObject<RewardsRequest>(await req.Content.ReadAsStringAsync());

                using (var connection = MSSQLConnection.CreateConnection())
                {
                    connection.Open();

                    // Call stored procedure to Save Customer
                    Customer Customers = new Customer();
                    GetRewardsRequest SaveRewardTrx = new GetRewardsRequest();
                    log.Verbose($"In SaveRewardTrx, Message={Message}", "JE.RMS.Services.SaveRewardsTrx");
                    log.Verbose($"In SaveRewardTrx, IsValid={item.IsValid}", "JE.RMS.Services.SaveRewardsTrx");

                    if (item.IsValid == true && item.IsValid.ToString().ToLower() == "true")
                    {
                        RewardTrxStatus = "Received";
                    }
                    else
                    {
                        RewardTrxStatus = "Validation error";
                    }

                    log.Verbose($"In SaveRewardTrx, RewardTrxStatus={RewardTrxStatus}", "JE.RMS.Services.SaveRewardsTrx");

                    if (item.Reward != null && !string.IsNullOrEmpty(item.Reward.ProductValue) && !Decimal.TryParse(item.Reward.ProductValue, out ProductValue))
                    {
                        RewardTrxStatus = "Validation error";
                        item.ValidationMessage += " Required numeric value for ProductValue field.";
                    }

                    if (!item.IsOrder)
                    {
                        if (string.IsNullOrEmpty(item.Customer.SourceSystemUniqueIDType))
                        {
                            RewardTrxStatus = "Validation error";
                            item.ValidationMessage += " Source system is not supported.";
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
                        else
                        {
                            item.IsValid = false;
                            RewardTrxStatus = RewardTrxStatusEnum.ValidationError.GetDescription();
                            item.ValidationMessage += "Validation Failed : Required value for at least one of 'SourceSystemID' or 'SourceSystemName'. Path: SourceSystemID or SourceSystemName;";
                        }
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
                    if (item.AdditionalData != null && item.AdditionalData.Where(p => p.Name == "OldEmailAddress") !=null && item.AdditionalData.Where(p => p.Name == "OldEmailAddress").Count() > 0)
                    {
                        CustomerTrxParams.Add("@OldEmailAddress", item.AdditionalData.Where(p => p.Name == "OldEmailAddress").FirstOrDefault().Value);
                    }
                    CustomerTrxParams.Add("@IsOrder", item.IsOrder);
                    log.Verbose($"Before calling SaveRewards stored proc, RewardTrxStatus={RewardTrxStatus}", "JE.RMS.Services.SaveRewardsTrx");

                    SaveRewardTrx = connection.Query<GetRewardsRequest>(Common.Constants.USPContstants.SaveRewards, CustomerTrxParams, null, true, null, CommandType.StoredProcedure).FirstOrDefault();
                    log.Verbose($"SaveRewardTrx sucessfully ={SaveRewardTrx.RewardTrxID}", "JE.RMS.Services.SaveRewardsTrx");

                }
                return req.CreateResponse(HttpStatusCode.OK, true);
            }
            catch (Exception ex)
            {
                log.Error($"Exception={ex}", ex, "JE.RMS.Services.SaveRewardsTrx");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}