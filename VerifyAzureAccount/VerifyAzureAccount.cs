using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using System.Data;
using JE.RMS.Common;
using Newtonsoft.Json.Linq;
using System.Text;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Configuration;

namespace JE.RMS.Services
{
    public class VerifyAzureAccount
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage,TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                // Get request body
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.VerifyAzureAccount");

                string token = "";
                string mail = (string)data.SelectToken("mail");
                JObject userJsonresult;
                using (HttpClient client = new HttpClient())
                {
                    var tokenEndpoint = ConfigurationManager.AppSettings["ADLoginURI"].ToString();
                    var accept = "application/json";
                    log.Verbose($"creating auth headers", "JE.RMS.Services.VerifyAzureAccount");
                    client.DefaultRequestHeaders.Add("Accept", accept);
                    string postBody = @"resource=https://graph.microsoft.com&client_id=" + ConfigurationManager.AppSettings["ADClientID"].ToString()+ "&grant_type=password&username=" + ConfigurationManager.AppSettings["ADUserName"].ToString()
                      +"&password=" + ConfigurationManager.AppSettings["ADPassword"].ToString() +"&scope=User.Read.All";
                      
                    using (var response = await client.PostAsync(tokenEndpoint, new StringContent(postBody, Encoding.UTF8, "application/x-www-form-urlencoded")))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            log.Verbose($"creating end point", "JE.RMS.Services.VerifyAzureAccount");
                            var jsonresult = JObject.Parse(await response.Content.ReadAsStringAsync());
                            token = (string)jsonresult["access_token"];
                            tokenEndpoint = @"https://graph.microsoft.com/v1.0/users/?$filter=(mail+eq+%27" + mail + "%27 or startswith(userPrincipalName,'" + mail + "'))";
                            //tokenEndpoint = @"https://graph.microsoft.com/v1.0/users/?$filter=(mail+eq+%27" + mail + "%27 or userPrincipalName + eq +" + mail + " %27)";

                            client.DefaultRequestHeaders.Add("Accept", accept);
                            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                            using (var userResponse = await client.GetAsync(tokenEndpoint))
                            {
                                if (userResponse.IsSuccessStatusCode)
                                {
                                    userJsonresult = JObject.Parse(await userResponse.Content.ReadAsStringAsync());
                                    log.Verbose($"received response from end point - graph api", "JE.RMS.Services.VerifyAzureAccount");
                                    var res = req.CreateResponse(HttpStatusCode.OK);
                                    res.Content = new StringContent(userJsonresult.ToString(), Encoding.UTF8, "application/json");
                                    return res;
                                }
                                else
                                {
                                    userJsonresult = JObject.Parse(await userResponse.Content.ReadAsStringAsync());
                                    return req.CreateErrorResponse(HttpStatusCode.NoContent, "User Not Found");
                                }
                            }
                        }
                        else
                        {
                            userJsonresult = JObject.Parse(await response.Content.ReadAsStringAsync());
                            return req.CreateErrorResponse(HttpStatusCode.BadRequest, response.ReasonPhrase.ToString());
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                log.Error($"Exception ={ex}");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        
    }

}