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
    public class EEGetUser
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EEGetUser");

            try
            {
                //Read UserId from request URL
                string UserId = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "UserId", true) == 0).Value;

                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                    var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                    string GetUsersUrl = ConfigurationManager.AppSettings["GetUsersUrl"].ToString();
                    if (!string.IsNullOrEmpty(UserId))
                    {
                        //if User ID is provided, result would be based on UserID, else API will return all users
                        GetUsersUrl = GetUsersUrl + '/' + UserId;
                    }

                    log.Verbose($"Called Get User API with UserId : {UserId}", "JE.RMS.Services.EEGetUser");
                    var response = await httpClient.GetAsync(GetUsersUrl);
                    log.Verbose($"Success : Get User API with UserId : {UserId}", "JE.RMS.Services.EEGetUser");

                    return response;
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EEGetUser", ex, "JE.RMS.Services.EEGetUser");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}