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
    public class EECreateOrder
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EECreateOrder");

            // Get request body
            try
            {
                EEOrder order = await req.Content.ReadAsAsync<EEOrder>();

                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                    var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                    string CreateOrderUrl = ConfigurationManager.AppSettings["EERewardsUrl"].ToString();

                    HttpResponseMessage response = new HttpResponseMessage();

                    if (order != null)
                    {
                        log.Verbose($"Called Create Order: {order.BatchID}", "JE.RMS.Services.EECreateOrder");

                        response = await httpClient.PostAsJsonAsync(CreateOrderUrl, order);
                        if (response.IsSuccessStatusCode == true)
                        {
                            log.Verbose($"Success : Create order for BatchID = {order.BatchID}", "JE.RMS.Services.EECreateOrder");
                        }
                        else
                        {
                            log.Error($"Error : Create order for BatchID = {order.BatchID}", null, "JE.RMS.Services.EECreateOrder");
                            log.Error($"Response : = {response.Content.ReadAsStringAsync().Result}", null, "JE.RMS.Services.EECreateOrder");
                        }
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EECreateOrder", ex, "JE.RMS.Services.EECreateOrder");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}