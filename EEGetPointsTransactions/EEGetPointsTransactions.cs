using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;
using System;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using System.Linq;

namespace JE.RMS.Services
{
    public class EEGetPointsTransactions
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.EEGetPointsTransactions");

            try
            {
                // Get request body
                string startDate = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "startDate", true) == 0).Value;
                string endDate = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "endDate", true) == 0).Value;

                log.Verbose($"Get Points transaction called with start date = {startDate}, end date = {endDate}", "JE.RMS.Services.EEGetPointsTransactions");

                //Check if start date & end date is provided
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                        var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                        string GetPointTransactionsUrl = ConfigurationManager.AppSettings["EEGetPointsTransactionsUrl"].ToString() + "?startDate=" + startDate + "&endDate=" + endDate;

                        var response = await httpClient.GetAsync(GetPointTransactionsUrl);

                        log.Verbose($"Success : Get Points transaction start date={startDate}, end date = {endDate}.", "JE.RMS.Services.EEGetPointsTransactions");

                        return response;
                    }
                }
                else //Bad request : Start Date or End Date is not provided.
                {
                    log.Error("Missing Parameter for EEGetPointsTransactions : startDate/endDate", null, "JE.RMS.Services.EEGetPointsTransactions");
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing parametre : startDate/endDate");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while EEGetPointsTransactions", ex, "JE.RMS.Services.EEGetPointsTransactions");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}