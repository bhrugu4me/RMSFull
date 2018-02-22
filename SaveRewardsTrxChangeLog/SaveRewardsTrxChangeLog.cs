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
    public class SaveRewardsTrxChangeLog
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.SaveRewardsTrxChangeLog");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@RewardsTrxID", data.SelectToken("RewardsTrxID").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@RewardTrxStatus", data.SelectToken("RewardTrxStatus").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@Comment", data.SelectToken("Comment").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@UserID", Convert.ToInt32(data.SelectToken("UserID").ToString().TrimStart('{').TrimEnd('}'))));
                log.Verbose($"calling sp", "JE.RMS.Services.SaveRewardsTrxChangeLog");

                var obj = MSSQLConnection.ExecuteStoredProcedure<object>(Common.Constants.USPContstants.SaveRewardTrxChangeLog, objprm);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.SaveRewardsTrxChangeLog");

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
 