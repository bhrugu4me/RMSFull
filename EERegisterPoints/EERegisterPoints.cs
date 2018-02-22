using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;
using System;
using Newtonsoft.Json;
using JE.RMS.Common.Model;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;

namespace JE.RMS.Services
{
    public class EERegisterPoints
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EERegisterPoints");

            try
            {
                string UserId = string.Empty;
                // Get request body
                var requestData = await req.Content.ReadAsStringAsync();
                JObject jObject = JObject.Parse(requestData);
                RewardPoint rewardPoint = JsonConvert.DeserializeObject<RewardPoint>(requestData);

                if (jObject != null && jObject.SelectToken("UserID") != null)
                {
                    UserId = (string)jObject.SelectToken("UserID");
                }

                if (!string.IsNullOrEmpty(UserId))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                        var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                        string RegisterPointsUrl = ConfigurationManager.AppSettings["EERegisterPointsUrl"].ToString() + '/' + UserId;
                        log.Verbose($"Calling Register points API, UserId : {UserId}, Reward Amount : {rewardPoint.Amount}", "JE.RMS.Services.EERegisterPoints");

                        var response = await httpClient.PostAsJsonAsync(RegisterPointsUrl, rewardPoint);
                        log.Verbose($"Success : Register points API, UserId : {UserId}, Reward Amount : {rewardPoint.Amount}", "JE.RMS.Services.EERegisterPoints");

                        return response;
                    }
                }
                else
                {
                    log.Error("Missing Parameter for EERegisterPoints : UserID", null, "JE.RMS.Services.EERegisterPoints");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing parametre : UserID");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EERegisterPoints", ex, "JE.RMS.Services.EERegisterPoints");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}