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
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Web;
using System.Net.Http.Formatting;

namespace JE.RMS.Services
{
    public class SubmitRewardFulfillmentResponse
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.SubmitRewardFulfillmentResponse");

            try
            {
                string clientIP = ((HttpContextWrapper)req.Properties["MS_HttpContext"]).Request.UserHostAddress;
                log.Verbose($"clientIP:={clientIP}", "JE.RMS.Services.SubmitRewardFulfillmentResponse");

                string Message = await req.Content.ReadAsStringAsync();
                var RewardFulfillmentResponse = JsonConvert.DeserializeObject<FulfillmentResponse>(Message);

                if (RewardFulfillmentResponse == null)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Null Request object.");
                }

                JSchema schema = JSchema.Parse(RewardRequestSchema.FulfillmentResponseSchema);
                IList<string> messages;
                JObject inputJSON = JObject.Parse(Message);

                bool valid = inputJSON.IsValid(schema, out messages);
                log.Info($"Valid ={valid}");
                log.Info($"Validation message ={messages}");

                var messageJSON = "";
                if (messages.Count > 0)
                    messageJSON = JsonConvert.SerializeObject(messages);

                List<SqlParameter> validateParam = new List<SqlParameter>();
                validateParam.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentResponse.RMSRewardID));

                var RMSRewardIDCount = MSSQLConnection.ExecuteStoredProcedure<int>(USPContstants.ValidateRMSRewardID, validateParam).FirstOrDefault();
                if (RMSRewardIDCount == 0)
                {
                    messageJSON = messageJSON + RewardFulfillmentResponse.RMSRewardID + " does not exist or fulfillment already processed. Path: RMSRewardID;";
                }

                if (!string.IsNullOrEmpty(messageJSON))
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, messageJSON);
                }

                var connectionString = ConfigurationManager.AppSettings["MyServiceBusReader"].ToString();

                var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

                #region Create Subscrptions for the first time
                //// Create a "AllFulfillmentResponseSubscription" for all subscription.
                if (!namespaceManager.SubscriptionExists("fulfillmentresponse", "AllFulfillmentResponseSubscription"))
                {
                    namespaceManager.CreateSubscription("fulfillmentresponse", "AllFulfillmentResponseSubscription");
                }
                #endregion

                RewardFulfillmentResponse.ClientIP = clientIP;

                var FulfillmentResponseTopicClient = TopicClient.CreateFromConnectionString(connectionString, "fulfillmentresponse");

                BrokeredMessage message = new BrokeredMessage(RewardFulfillmentResponse);

                //Send message to Topic
                FulfillmentResponseTopicClient.Send(message);

                #region Update Audit Fields
                //Update timestapm in Audit fields
                List<SqlParameter> AuditParam = new List<SqlParameter>();
                AuditParam.Add(new SqlParameter("@RMSRewardID", RewardFulfillmentResponse.RMSRewardID));

                var RMSRewardID = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParam);
                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSRewardID[0]}", "JE.RMS.Services.SubmitRewardFulfillmentResponse");

                #endregion

                var Status = "Success";
                return req.CreateResponse(HttpStatusCode.OK, new { Status }, JsonMediaTypeFormatter.DefaultMediaType);

            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.SubmitRewardFulfillmentResponse");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Fail", ex);
            }
        }
    }
}