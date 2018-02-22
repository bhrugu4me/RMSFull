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
using System.Linq;

namespace JE.RMS.Services
{
    public class EEGetPoints
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EEGetPoints");

            try
            {
                //Read UserId from request URL
                string UserId = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "UserId", true) == 0).Value;

                if (!string.IsNullOrEmpty(UserId))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                        var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                        log.Verbose($"Get Points called with UserId={UserId}", "JE.RMS.Services.EEGetPoints");

                        string GetPointsUrl = ConfigurationManager.AppSettings["GetPointsUrl"].ToString() + '/' + UserId;

                        var response = await httpClient.GetAsync(GetPointsUrl);

                        log.Verbose($"Success : Get points for UserId={UserId}", "JE.RMS.Services.EEGetPoints");

                        return response;
                    }
                }
                else //Bad request, UserId was not provided in request URL
                {
                    log.Error("Missing Parameter for EEGetPoints : UserID", null, "JE.RMS.Services.EEGetPoints");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing parametre : UserID");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EEGetPoints", ex, "JE.RMS.Services.EEGetPoints");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}