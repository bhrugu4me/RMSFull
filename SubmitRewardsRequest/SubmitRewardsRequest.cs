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
using JE.RMS.Common.Model;
using System.Net.Http.Formatting;

namespace JE.RMS.Services
{
    public class SubmitRewardsRequest
    {
        #region variables
        public static string clientIP = "";
        public static JObject reqMessage;
        #endregion

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.SubmitRewardsRequest");
            try
            {
                //To obtain client IP Address
                clientIP = ((HttpContextWrapper)req.Properties["MS_HttpContext"]).Request.UserHostAddress;
                log.Verbose($"clientIP:={clientIP}", "JE.RMS.Services.SubmitRewardsRequest");

                //Read request object as string
                string reqString = await req.Content.ReadAsStringAsync();
                reqMessage = JObject.Parse(reqString);

                // Retrieve storage account from connection string
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["jermsstorage_STORAGE"].ToString());
                // Create the queue client
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                // Retrieve a reference to a queue
                CloudQueue queue = queueClient.GetQueueReference("submitrewardsrequestqueue");
                // Create the queue if it doesn't already exist.
                queue.CreateIfNotExists();


                foreach (JObject x in reqMessage["RewardsRequest"])
                {
                    //Added Audit fields in request object
                    x.Add("SourceIP", clientIP);
                    x.Add("RMSRewardID", Guid.NewGuid().ToString());
                    x.Add("RewardsRequestReceiveTimestamp", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                    CloudQueueMessage message = new CloudQueueMessage(x.ToString());
                    queue.AddMessage(message);
                    log.Verbose($"Message was published={x}", "JE.RMS.Services.SubmitRewardsRequest");
                }

                var Status = "Success";
                return req.CreateResponse(HttpStatusCode.OK, new { Status }, JsonMediaTypeFormatter.DefaultMediaType);
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.SubmitRewardsRequest");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError,ex.Message, ex);
            }
        }
    }
}