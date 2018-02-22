using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;

namespace JE.RMS.Services
{
    public class SubmitRewardsRequest
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
          
            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            //name = name ?? data?.RewardsRequest[0].RequestID;

            return req.CreateResponse(HttpStatusCode.OK, "Request was received successully.");
        }
    }
}