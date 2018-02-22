using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using System.Collections.Generic;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class GetTransactionTypeList
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

            try
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.GetTransactionTypeList");

                var obj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.TransactionType>(Common.Constants.USPContstants.GetTransactionTypeList);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.GetTransactionTypeList");

                return req.CreateResponse(HttpStatusCode.OK, obj);

            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

        }
    }
}