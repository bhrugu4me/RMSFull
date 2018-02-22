﻿using System.Linq;
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
    public class GetCustomerExtendedOnCustomer
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            try
            {
                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.GetCustomerExtendedOnCustomer");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@customerID", Convert.ToInt32(data.SelectToken("customerID").ToString().TrimStart('{').TrimEnd('}'))));
                log.Verbose($"calling sp", "JE.RMS.Services.GetCustomerExtendedOnCustomer");

                List<Common.Model.CustomerExtended> obj = MSSQLConnection.ExecuteStoredProcedure<Common.Model.CustomerExtended>(Common.Constants.USPContstants.GetCustomerExtendedOnCustomer, objprm);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.GetCustomerExtendedOnCustomer");

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
 