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
using System.Text;

namespace JE.RMS.Services
{
    public class ReadGetRewardsTrxReceived
    {
        public static async void Run(TimerInfo evaluateRewardsTrxTimer, ICollector<string> errormessage, TraceWriter log)
        {
            try
            {
                // Retrieve storage account from connection string
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["jermsstorage_STORAGE"].ToString());
                // Create the queue client
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

                // Retrieve a reference to a queue
                CloudQueue queue = queueClient.GetQueueReference("rewardfulfillmentrequestqueue");
                // Get the next message
                IEnumerable<CloudQueueMessage> retrievedMessage = queue.GetMessages(Convert.ToInt32(ConfigurationManager.AppSettings["ReadMessageCountFromQueue"]));
                log.Verbose($"After the reading of queue message = {retrievedMessage.Count()}", "JE.RMS.Services.ReadGetRewardsTrxReceived");
                string rewardfulfillmentrequest = "[";
                bool IsQueueEmpty = true;
                foreach (var item in retrievedMessage)
                {
                    rewardfulfillmentrequest += item.AsString + ",";
                    //Process the message in less than 30 seconds, and then delete the message
                    queue.DeleteMessage(item);
                    IsQueueEmpty = false;
                }
                rewardfulfillmentrequest += "]";

                // Call SaveRewardsTrx to save rewards transaction.
                if (!IsQueueEmpty)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var SaveRewardsTrxEndpoint = ConfigurationManager.AppSettings["EvaluateRewardsTrx"].ToString();
                        var accept = "application/json";
                        client.DefaultRequestHeaders.Add("Accept", accept);

                        using (var response = await client.PostAsync(SaveRewardsTrxEndpoint, new StringContent(rewardfulfillmentrequest, Encoding.UTF8, "application/x-www-form-urlencoded")))
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            log.Verbose($"Response ={result}", "JE.RMS.Services.ReadGetRewardsTrxReceived");
                        }
                    }
                }

                log.Verbose($"C# trigger queue function processed a request. inputmessage= {rewardfulfillmentrequest}", "JE.RMS.Services.ReadGetRewardsTrxReceived");
            }
            catch(Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.ReadGetRewardsTrxReceived");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}