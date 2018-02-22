using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class SearchRewardsTrx
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

            try
            {
                // Get request body
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.SearchRewardsTrx");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@startDate", data.SelectToken("startDate").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@endDate", data.SelectToken("endDate").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@ssUniqueID", data.SelectToken("ssUniqueID").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@email", data.SelectToken("email").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@sourceSystem", data.SelectToken("sourceSystem").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@countryids", data.SelectToken("countryids").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@jurisdiction", data.SelectToken("jurisdiction").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@transType", data.SelectToken("transType").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@transStatus", data.SelectToken("transStatus").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@pageNumber", Convert.ToInt32(data.SelectToken("pageNumber").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@pageSize", Convert.ToInt32(data.SelectToken("pageSize").ToString().TrimStart('{').TrimEnd('}'))));
                log.Verbose($"calling sp", "JE.RMS.Services.SearchRewardsTrx");

                var obj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.SearchRewardTrx>(Common.Constants.USPContstants.SearchRewardsTrx, objprm);

                log.Verbose($"received response:={obj}", "JE.RMS.Services.SearchRewardsTrx");

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