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
using System.Data.SqlClient;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using JE.RMS.Common.Constants;
using JE.RMS.Common.Model;
using Microsoft.ServiceBus;

namespace JE.RMS.Services
{
    public class GetRewardFulfillmentRequest
    {
        #region variables
        public static string clientIP = "";
        public static JObject reqMessage;
        //Service bus queue names and connection strings
        public static string connectionString = ConfigurationManager.AppSettings["MyServiceBusReader"].ToString();
        public static NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
        public static int BatchSize = Convert.ToInt32(ConfigurationManager.AppSettings["RewardFulfillmentRequestBatchSize"]);
        public static int RepeatCallSize = Convert.ToInt32(ConfigurationManager.AppSettings["RewardFulfillmentRequestRepeatCallSize"]);
        public static SubscriptionClient CRMSubscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, "fulfillmentrequest", "CRMSubscription");
        public static SubscriptionClient GBASSTariffSubscription = SubscriptionClient.CreateFromConnectionString(connectionString, "fulfillmentrequest", "GBASSTariffSubscription");
        public static SubscriptionClient GBASSCTLAdjSubscription = SubscriptionClient.CreateFromConnectionString(connectionString, "fulfillmentrequest", "GBASSCTLAdjSubscription");
        public static SubscriptionClient SmartConnectSubscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, "fulfillmentrequest", "SmartConnectSubscription");

        #endregion

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.SubmitRewardsRequest");
            try
            {
                // Get request body
                /*JObject data = await req.Content.ReadAsAsync<JObject>();
                string FulfillmentChannel = (string)data.SelectToken("FulfillmentChannel");*/

                string ChannelCode = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "ChannelCode", true) == 0).Value;

                if (!string.IsNullOrEmpty(ChannelCode))
                {
                    //Call stored procedure to Update RewardTrx
                    List<SqlParameter> FulfillmentChanneParams = new List<SqlParameter>();
                    FulfillmentChanneParams.Add(new SqlParameter("@FulfillmentChannel", ChannelCode));
                    var FulfillmentChannelID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.GetFulfillmentChannelID, FulfillmentChanneParams);
                    if (FulfillmentChannelID.Count == 0)
                    {
                        return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid channel code.");
                    }
                    log.Verbose($"FulfillmentChannelID={FulfillmentChannelID[0]}", "JE.RMS.Services.GetRewardFulfillmentRequest");
                    RewardFulfillmentResponseList res = new RewardFulfillmentResponseList();
                    RewardFulfillmentResponseList resp = GetMessagesFromSubscription(FulfillmentChannelID[0],res,log);
                    
                    return req.CreateResponse(HttpStatusCode.OK, resp);
                }
                else
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Required ChannelCode as request parameters.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.SubmitRewardsRequest");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        public static RewardFulfillmentResponseList GetMessagesFromSubscription(long FulfillmentChannelID, RewardFulfillmentResponseList resp, TraceWriter log)
        {
            IEnumerable<BrokeredMessage> RecievedMessage = null;
            long MessageCount = 0;
            if(resp.RewardFulfillmentRequest == null)
                resp.RewardFulfillmentRequest = new List<RewardFulfillmentRequest>();
            BatchSize = BatchSize - resp.RewardFulfillmentRequest.Count;
            if (FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSTariff)
            {
                MessageCount = namespaceManager.GetSubscription("fulfillmentrequest", "GBASSTariffSubscription").MessageCount;
                if (MessageCount > 0)
                    RecievedMessage = GBASSTariffSubscription.ReceiveBatch(BatchSize, new TimeSpan(0, 0, 0));
            }
            else if(FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSCTLAdj)
            {
                MessageCount = namespaceManager.GetSubscription("fulfillmentrequest", "GBASSCTLAdjSubscription").MessageCount;
                if (MessageCount > 0)
                    RecievedMessage = GBASSCTLAdjSubscription.ReceiveBatch(BatchSize, new TimeSpan(0,0,0));
            }
            else if (FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.Cheque)
            {
                MessageCount = namespaceManager.GetSubscription("fulfillmentrequest", "SmartConnectSubscription").MessageCount;
                if (MessageCount > 0)
                    RecievedMessage = SmartConnectSubscriptionClient.ReceiveBatch(BatchSize, new TimeSpan(0, 0, 0));
            }
            List<Guid> messageLockTokenList = new List<System.Guid>();
            
            resp.HasMoreMessages = false;
            if (RecievedMessage != null && RecievedMessage.Count() > 0)
            {
                foreach (BrokeredMessage message in RecievedMessage)
                {
                    var raw = message.GetBody<string>();
                    var RewardFulfillmentRequestObj = JsonConvert.DeserializeObject<RewardFulfillmentRequestList>(raw);
                    string RMSRewardID = RewardFulfillmentRequestObj.RewardFulfillmentRequest.RMSRewardID;
                    ////Call stored procedure to Update RewardTrx
                    List<SqlParameter> RewardTrxParams = new List<SqlParameter>();
                    RewardTrxParams.Add(new SqlParameter("@RMSRewardID", RMSRewardID));
                    var UpdatedRewardTrxID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.UpdateProcessFulfillmentTimestamp, RewardTrxParams).FirstOrDefault();
                    log.Verbose($"RewardTrx updated successfully. RewardTrID={UpdatedRewardTrxID}", "JE.RMS.Services.GetRewardFulfillmentRequest");
                    resp.RewardFulfillmentRequest.Add(RewardFulfillmentRequestObj.RewardFulfillmentRequest);
                    messageLockTokenList.Add(message.LockToken);
                }

                if (FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSTariff)
                {
                    GBASSTariffSubscription.CompleteBatch(messageLockTokenList);
                }
                else if (FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSCTLAdj)
                {
                    GBASSCTLAdjSubscription.CompleteBatch(messageLockTokenList);
                }
                else if (FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.Cheque)
                {
                    SmartConnectSubscriptionClient.CompleteBatch(messageLockTokenList);
                }
            }

            resp.HasMoreMessages = MessageCount > resp.RewardFulfillmentRequest.Count ? true : false;
            if (resp.HasMoreMessages && resp.RewardFulfillmentRequest.Count < RepeatCallSize)
                GetMessagesFromSubscription(FulfillmentChannelID, resp, log);
            resp.TotalRecord = MessageCount;
            return resp;
        }
    }
}