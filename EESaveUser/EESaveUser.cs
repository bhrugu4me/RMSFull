using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using Newtonsoft.Json.Linq;
using JE.RMS.Common.Model;
using System.Text;
using System;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;

namespace JE.RMS.Services
{
    public class EESaveUser
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EESaveUser");

            // Get request body
            try
            {
                EEUser user = await req.Content.ReadAsAsync<EEUser>();

                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                    var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                    string GetUsersUrl = ConfigurationManager.AppSettings["SaveUsersUrl"].ToString();

                    HttpResponseMessage response = new HttpResponseMessage();

                    if (user != null && user.UserID != Guid.Empty)
                    {
                        log.Verbose($"Called Save User (Existing User) with UserId = {user.UserID}", "JE.RMS.Services.EESaveUser");
                        GetUsersUrl = GetUsersUrl + '/' + user.UserID.ToString();
                        //Call Update User API [HTTPPUT]
                        response = await httpClient.PutAsJsonAsync(GetUsersUrl, user);

                        log.Verbose($"Success : Update User for UserId = {user.UserID}", "JE.RMS.Services.EESaveUser");
                    }
                    else
                    {
                        log.Verbose($"Called Save User (New User)", "JE.RMS.Services.EESaveUser");
                        //Call Add User API [HTTPPOST]
                        response = await httpClient.PostAsJsonAsync(GetUsersUrl, user);

                        log.Verbose($"Success : Add User", "JE.RMS.Services.EESaveUser");
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EESaveUser", ex, "JE.RMS.Services.EESaveUser");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}