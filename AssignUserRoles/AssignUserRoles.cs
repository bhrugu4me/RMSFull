using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using JE.RMS.Common;
using System.Collections.Generic;
using System;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace JE.RMS.Services
{
    public class AssignUserRoles
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.AssignUserRoles");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@userID", Convert.ToInt32(data.SelectToken("UserID").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@userName", data.SelectToken("UserName").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@firstName", data.SelectToken("FirstName").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@lastName", data.SelectToken("LastName").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@email", data.SelectToken("Email").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@createdBy", Convert.ToInt32(data.SelectToken("CreatedBy").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@updatedBy", Convert.ToInt32(data.SelectToken("UpdatedBy").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@isActive", data.SelectToken("IsActive").ToString().TrimStart('{').TrimEnd('}')));
                objprm.Add(new SqlParameter("@roleID", Convert.ToInt32(data.SelectToken("RoleID").ToString().TrimStart('{').TrimEnd('}'))));

                log.Verbose($"calling sp", "JE.RMS.Services.AssignUserRoles");

                var obj = MSSQLConnection.ExecuteStoredProcedure<string>(Common.Constants.USPContstants.AssignUserRoles, objprm);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.AssignUserRoles");
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