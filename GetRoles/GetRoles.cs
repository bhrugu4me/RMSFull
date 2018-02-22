using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using JE.RMS.Common.Model;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class GetRoles
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger GetRoles function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.GetRoles");

            try
            {
                var roles = MSSQLConnection.ExecuteStoredProcedure<Role>(Common.Constants.USPContstants.GetRoles);
                log.Verbose($"received response:={roles}", "JE.RMS.Services.GetRoles");

                return req.CreateResponse(HttpStatusCode.OK, roles);
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while GetRoles", ex, "JE.RMS.Services.GetRoles");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}