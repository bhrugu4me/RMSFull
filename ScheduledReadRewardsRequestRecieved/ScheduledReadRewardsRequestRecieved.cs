using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Text;
using JE.RMS.Common.Model;
using JE.RMS.Common.Constants;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Data.SqlClient;

namespace JE.RMS.Services
{
    public class ScheduledReadRewardsRequestRecieved
    {
        #region valriables
        public static IList<string> messages;
        public static bool valid;
        public static string saverewardsobj;
        #endregion

        public static async void Run(TimerInfo rewardsRequestTrxTimer, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# timer function processed a request.", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");

            try
            {
                // Retrieve storage account from connection string
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["jermsstorage_STORAGE"].ToString());
                // Create the queue client
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

                // Retrieve a reference to a queue
                CloudQueue queue = queueClient.GetQueueReference("submitrewardsrequestqueue");
                // Get the next message
                IEnumerable<CloudQueueMessage> retrievedMessage = queue.GetMessages(Convert.ToInt32(ConfigurationManager.AppSettings["ReadRewardsRequestReceivedFromQueueBatchSize"]));
                log.Verbose($"After the reading of queue message = {retrievedMessage.Count()}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                foreach (var item in retrievedMessage)
                {
                    string inputmessage = item.AsString;
                    //Process the message in less than 30 seconds, and then delete the message
                    queue.DeleteMessage(item.Id, item.PopReceipt);

                    JObject inputJSON = JObject.Parse(inputmessage);

                    JSchema objectschema = new JSchema();
                    objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RequestSchema);

                    if (inputJSON["TransactionType"] != null)
                    {
                        if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Qualify.GetDescription()
                                    || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Terminate.GetDescription()
                                    || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reactivate.GetDescription())
                        {
                            objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.Terminate_Reactivate_Qualify_Schema);
                        }

                        if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reward.GetDescription())
                        {
                            string product = inputJSON.SelectToken("Reward.ProductCode").ToString().Replace("{", "").Replace("}", "");
                            string program = inputJSON.SelectToken("Reward.ProgramName").ToString().Replace("{", "").Replace("}", "");
                            if (product != null && program != null)
                            {
                                List<SqlParameter> GetFulfillmentRuleParams = new List<SqlParameter>();
                                GetFulfillmentRuleParams.Add(new SqlParameter("@Product", product.ToString()));
                                GetFulfillmentRuleParams.Add(new SqlParameter("@Program", program.ToString()));
                                var ApiName = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.GetFulfillmentRule, GetFulfillmentRuleParams).FirstOrDefault();
                                log.Verbose($"APIName ={ApiName}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                                log.Verbose($"Program ={program}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                                log.Verbose($"Product ={product}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");

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

                        if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.ProgramUpdateSourceSystem.GetDescription())
                        {
                            objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.ProgramUpdateSchema);
                        }
                    }

                    //Message schema validation
                    valid = inputJSON.IsValid(objectschema, out messages);
                    log.Verbose($"Valid ={valid}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                    inputJSON.Add("IsValid", valid);
                    var messageJSON = "";
                    if (messages.Count > 0)
                        messageJSON = JsonConvert.SerializeObject(messages);
                    inputJSON.Add("ValidationMessage", messageJSON);
                    log.Verbose($"Validation message = {messageJSON}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");

                    saverewardsobj = inputJSON.ToString();
                    log.Verbose($"Published message ={saverewardsobj}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");

                    // Call SaveRewardsTrx to save rewards transaction.
                    using (HttpClient client = new HttpClient())
                    {
                        var SaveRewardsTrxEndpoint = ConfigurationManager.AppSettings["SaveRewardsTrxEndpoint"].ToString();
                        var accept = "application/json";
                        client.DefaultRequestHeaders.Add("Accept", accept);

                        using (var response = await client.PostAsync(SaveRewardsTrxEndpoint, new StringContent(saverewardsobj, Encoding.UTF8, "application/x-www-form-urlencoded")))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                var result = response.Content.ReadAsStringAsync().Result;
                                log.Verbose($"Response ={result}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                            }
                            else
                            {
                                var result = response.Content.ReadAsStringAsync().Result;
                                log.Verbose($"StatusCode ={response.StatusCode} ReasonPhrase ={response.ReasonPhrase}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                                log.Verbose($"Response ={result}", "JE.RMS.Services.ScheduledReadRewardsRequestRecieved");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.OnRewardsRequestRecieved");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}