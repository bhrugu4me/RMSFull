using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using Newtonsoft.Json.Linq;
using System.Web;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using JE.RMS.Common.Constants;
using JE.RMS.Common.Model;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;

namespace JE.RMS.Services
{
    public class EvaluateRewardsTrx
    {
        public static EvaluateRuleResponse Response = new EvaluateRuleResponse();

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            try
            {
                log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EvaluateRewardsTrx");
                string rewardfulfillmentrequest = await req.Content.ReadAsStringAsync();
                var evaluationResult = EvaluateRewardsTrxMessage(rewardfulfillmentrequest, log);
                log.Verbose($"C# HTTP function processed a request. inputmessage= {rewardfulfillmentrequest}", "JE.RMS.Services.EvaluateRewardsTrx");
                return req.CreateResponse(HttpStatusCode.OK, evaluationResult);
            }
            catch (Exception ex)
            {
                log.Error($"Exception={ex}", ex, "JE.RMS.Services.EvaluateRewardsTrx");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        static void BasicAction(TraceWriter log)
        {
            log.Verbose($"Method=alpha, Thread={Thread.CurrentThread.ManagedThreadId}");
        }

        private static EvaluateRuleResponse EvaluateRewardsTrxMessage(string rewardfulfillmentrequest, TraceWriter log)
        {
            //Get FulFillmentRule from DB
            var FulfillmentRulesList = MSSQLConnection.ExecuteStoredProcedure<FulfillmentRules>(USPContstants.GetFulfillmentRules, null);

            var lstRewardsTrx = JsonConvert.DeserializeObject<GetRewardsRequest[]>(rewardfulfillmentrequest);
            foreach (GetRewardsRequest item in lstRewardsTrx)
            {

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@Email", item.Email));
                objprm.Add(new SqlParameter("@ProductID", item.ProductID));
                objprm.Add(new SqlParameter("@ProgramID", item.ProgramID));
                objprm.Add(new SqlParameter("@RewardTrxID", item.RewardTrxID));
                var FulfillmentDataPerCust = MSSQLConnection.ExecuteStoredProcedure<FulfillmentRules>(USPContstants.GetFulFillmentDataPerCustomer, objprm);

                item.RewardTrxStatus = string.Empty;
                string Comment = string.Empty;
                if (item.TransactionType == TransactionTypeEnum.ProgramUpdateSourceSystem.GetDescription()
                    || item.TransactionType == TransactionTypeEnum.Qualify.GetDescription()
                    || item.TransactionType == TransactionTypeEnum.Reactivate.GetDescription()
                    || item.TransactionType == TransactionTypeEnum.Terminate.GetDescription())
                {
                    objprm = new List<SqlParameter>();
                    objprm.Add(new SqlParameter("@FulfillmentChannel", FulfillmentChannel.EnergyEarth.GetDescription()));
                    var FulfillmentChannelID = MSSQLConnection.ExecuteStoredProcedure<int>(USPContstants.GetFulfillmentChannelID, objprm).FirstOrDefault();
                    item.FulfillmentChannelID = FulfillmentChannelID;
                    item.RewardTrxStatus = "Ready for fulfillment";
                }
                else
                {
                    foreach (FulfillmentRules fulfillmentRule in FulfillmentRulesList)
                    {
                        if (fulfillmentRule.ProductID == item.ProductID && fulfillmentRule.ProgramID == item.ProgramID)
                        {
                            item.RewardTrxStatus = "Ready for fulfillment";
                            if (item.IsFulfillImmediate == true)
                            {
                                item.RewardTrxStatus = RewardTrxStatusEnum.ReadyForFulfillmentImmediate.GetDescription();
                            }
                            if (fulfillmentRule.RequireApproval)
                            {
                                item.RewardTrxStatus = "Waiting for approval";
                                Comment = "Reward Trx is Waiting for approval.";
                            }
                            else if (fulfillmentRule.MaxOccurrencePerYear > 0 && FulfillmentDataPerCust[0].MaxOccurrencePerYear >= fulfillmentRule.MaxOccurrencePerYear)
                            {
                                item.RewardTrxStatus = "Rejected - System";
                                Comment = "Max occurrence per year is exceeds.";
                            }
                            else if (fulfillmentRule.MaxOccurrencePerCustomer > 0 && FulfillmentDataPerCust[0].MaxOccurrencePerCustomer >= fulfillmentRule.MaxOccurrencePerCustomer)
                            {
                                item.RewardTrxStatus = "Rejected - System";
                                Comment = "Max occurrence per customer is exceeds.";
                            }
                            else if (fulfillmentRule.MaxRewardValue > 0 && item.ProductValue > fulfillmentRule.MaxRewardValue)
                            {
                                item.RewardTrxStatus = "Rejected - System";
                                Comment = "Max reward value is exceeds.";
                            }
                            else if (fulfillmentRule.MaxCumulativeRewardValuePerYear > 0 && FulfillmentDataPerCust[0].MaxCumulativeRewardValuePerYear + item.ProductValue > fulfillmentRule.MaxCumulativeRewardValuePerYear)
                            {
                                item.RewardTrxStatus = "Rejected - System";
                                Comment = "Max cumulative reward value per year is exceeds.";
                            }
                            if (fulfillmentRule.FulfillmentChannelID > 0)
                            {
                                item.FulfillmentChannelID = fulfillmentRule.FulfillmentChannelID;
                            }
                            else
                            {
                                item.RewardTrxStatus = string.Empty;
                            }
                            break;
                        }
                    }
                }

                if (item.RewardTrxStatus != string.Empty)
                {
                    //Call stored procedure to Update RewardTrx
                    List<SqlParameter> RewardTrxParams = new List<SqlParameter>();
                    RewardTrxParams.Add(new SqlParameter("@RewardTrxID", item.RewardTrxID));
                    RewardTrxParams.Add(new SqlParameter("@FulfillmentChannelID", item.FulfillmentChannelID));
                    log.Verbose($"item.FulfillmentChannelID={item.FulfillmentChannelID}", "JE.RMS.Services.EvaluateRewardsTrx");
                    var RewardTrxID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.UpdateFulfillmentChannelIDInRewardsTrx, RewardTrxParams);
                    log.Verbose($"RewardTrx updated successfully. RewardTrID={RewardTrxID[0]}", "JE.RMS.Services.EvaluateRewardsTrx");

                    //Call stored procedure to Save RewardTrxChangeLog
                    List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID.ToString()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", item.RewardTrxStatus));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", Comment));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={RewardTrxChangeLogID[0]}", "JE.RMS.Services.EvaluateRewardsTrx");

                }
                else
                {
                    item.RewardTrxStatus = "Validation Error";
                    Comment = $"No fulfillment rule found for Program Code: { item.ProgramName } and Product Code: { item.ProductCode}. ";
                    //Call stored procedure to Save RewardTrxChangeLog
                    List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID.ToString()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", RewardTrxStatusEnum.ValidationError.GetDescription()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", Comment));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={RewardTrxChangeLogID[0]}", "JE.RMS.Services.EvaluateRewardsTrx");
                }

                Response.Comment = Comment;
                Response.RequestID = item.RequestId;
                Response.Status = item.RewardTrxStatus;
            }
            return Response;
        }
    }
}