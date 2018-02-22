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
    public class GetCustomers
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.GetCustomers");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@pageNumber", Convert.ToInt32(data.SelectToken("pageNumber").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@pageSize", Convert.ToInt32(data.SelectToken("pageSize").ToString().TrimStart('{').TrimEnd('}'))));
                objprm.Add(new SqlParameter("@searchText", data.SelectToken("searchText")==null?"" : data.SelectToken("searchText").ToString().TrimStart('{').TrimEnd('}')));

                log.Verbose($"calling sp", "JE.RMS.Services.GetCustomers");

                List<Common.Model.Customer> retobj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.Customer>(Common.Constants.USPContstants.GetCustomers, objprm);

                Common.Model.CustomerList obj = new Common.Model.CustomerList();
                if (retobj.Count > 0)
                {

                    obj.TotalRows = retobj.FirstOrDefault().TotalRows;
                }
                else
                {
                    obj.TotalRows = 0;
                }
                    obj.Users = new List<Common.Model.Customer>();
                    obj.Users = retobj;
                

                log.Verbose($"received response:={obj}", "JE.RMS.Services.GetCustomers");

                var CustomerResponse = req.CreateResponse(HttpStatusCode.OK);
                CustomerResponse.Content = new StringContent(JsonConvert.SerializeObject(obj), System.Text.Encoding.UTF8, "application/json");
                return CustomerResponse;
                //return req.CreateResponse(HttpStatusCode.OK, obj);
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
 