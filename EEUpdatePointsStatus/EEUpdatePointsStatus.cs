using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class EEUpdatePointsStatus
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EEUpdatePointsStatus");

            try
            {
                string UserId = string.Empty;
                string Status = string.Empty;
                // Get request body
                JObject jObject = JObject.Parse(await req.Content.ReadAsStringAsync());

                if (jObject != null && jObject.SelectToken("UserID") != null && jObject.SelectToken("Status") != null)
                {
                    UserId = (string)jObject.SelectToken("UserID");
                    Status = (string)jObject.SelectToken("Status");
                }

                if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(Status))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                        var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                        string UpdatePointsStatusUrl = ConfigurationManager.AppSettings["EEUpdatePointsStatusUrl"].ToString() + '/' + UserId;
                        log.Verbose($"Update Point status called with UserId: {UserId} with Status : {Status}", "JE.RMS.Services.EEUpdatePointsStatus");

                        var response = await httpClient.PostAsJsonAsync(UpdatePointsStatusUrl, Status);

                        log.Verbose($"Success : Update Point status for User: {UserId} with Status : {Status}", "JE.RMS.Services.EEUpdatePointsStatus");

                        return response;
                    }
                }
                else
                {
                    log.Error($"Missing Parametres while calling Update point status UserId = {UserId}, Status = {Status}.", null, "JE.RMS.Services.EEUpdatePointsStatus");
                    errormessage.Add($"Missing Parametres while calling Update point status UserId = {UserId}, Status = {Status}.");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing parametre : UserID, Status");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EEUpdatePointsStatus", ex, "JE.RMS.Services.EEUpdatePointsStatus");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}