using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class GetSystemLog
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.GetSystemLog");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@startDate", data.SelectToken("startDate").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@endDate", data.SelectToken("endDate").ToString().TrimStart('{').TrimEnd('}')));

                log.Verbose($"calling sp", "JE.RMS.Services.GetSystemLog");

                List<Common.Model.SystemLog> obj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.SystemLog>(Common.Constants.USPContstants.GetSystemLogs, objprm);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.GetSystemLog");

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
 