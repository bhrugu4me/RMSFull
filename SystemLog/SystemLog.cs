using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class SystemLog
    {

        public static void Run(string errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request");

            try
            {
                //JObject data = await req.Content.ReadAsAsync<JObject>();
                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@message", errormessage));
                List<Common.Model.SystemLog> obj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.SystemLog>(Common.Constants.USPContstants.SystemLogs,objprm);
                log.Verbose($"Save systemlog successfully.");
            }
            catch (System.Exception ex)
            {
                log.Error($"Exception ={ex}");
            }
        }
    }
}