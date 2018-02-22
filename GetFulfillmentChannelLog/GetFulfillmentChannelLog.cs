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
using JE.RMS.Common.Model;
using System.Text;
using System.Globalization;

namespace JE.RMS.Services
{
    public class GetFulfillmentChannelLog
    {

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
            bool isValid = true, isSourceSystemUniqueID = true;
            string validationMessage = string.Empty;
            int SourceSystemID = 0;
            int SSID = 0;
            try
            {
                string reqString = await req.Content.ReadAsStringAsync();
                var getFulfillmentChannelLogRequest = JsonConvert.DeserializeObject<GetFulfillmentChannelLogRequest>(reqString);

                if (!string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemID))
                {
                    int.TryParse(getFulfillmentChannelLogRequest.SourceSystemID, out SourceSystemID);
                    if (SourceSystemID <= 0)
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : SourceSystemID must be numeric;";
                    }
                }

                if (string.IsNullOrEmpty(getFulfillmentChannelLogRequest.FulfillmentChannel))
                {
                    isValid = false;
                    validationMessage = validationMessage + "Validation Failed : Fulfillment Channel is required;";
                }

                if ((!string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueID) && string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueIDType)) || (string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueID) && !string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueIDType)))
                {
                    isValid = false;
                    validationMessage = validationMessage + "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType both are required;";
                }
                else if (string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueID) || string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemUniqueIDType))
                {
                    isSourceSystemUniqueID = false;
                }


                DateTime StartDate, EndDate;
                if (!isSourceSystemUniqueID)
                {
                    if (string.IsNullOrEmpty(getFulfillmentChannelLogRequest.StartDate) || string.IsNullOrEmpty(getFulfillmentChannelLogRequest.EndDate))
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : StartDate & EndDate are required;";
                    }
                    else if (!DateTime.TryParseExact(getFulfillmentChannelLogRequest.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out StartDate) || !DateTime.TryParseExact(getFulfillmentChannelLogRequest.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out EndDate))
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : StartDate & EndDate required in YYYY-MM-DD format;";
                    }
                    else if (Convert.ToDateTime(getFulfillmentChannelLogRequest.StartDate) > Convert.ToDateTime(getFulfillmentChannelLogRequest.EndDate))
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : StartDate must be less then EndDate;";
                    }
                }

                if (!string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemID) || !string.IsNullOrEmpty(getFulfillmentChannelLogRequest.SourceSystemName))
                {
                    List<SqlParameter> SSIDprm = new List<SqlParameter>();
                    SSIDprm.Add(new SqlParameter("@SourceSystemShortName", getFulfillmentChannelLogRequest.SourceSystemName));
                    SSIDprm.Add(new SqlParameter("@SourceSystemID", SourceSystemID));

                    SSID = MSSQLConnection.ExecuteStoredProcedure<int>(Common.Constants.USPContstants.GetSourceSystemID, SSIDprm).FirstOrDefault();
                    if (SSID == 0)
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : Please provide valid SourceSystemID or SourceSystemName (if both are provided they would match a SourceSystem);";
                    }
                }


                if (isValid == false)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, validationMessage);
                }

                JObject data = await req.Content.ReadAsAsync<JObject>();
                log.Verbose($"received data :={data}", "JE.RMS.Services.GetFulfillmentChannelLog");

                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@startDate", getFulfillmentChannelLogRequest.StartDate == string.Empty ? null : getFulfillmentChannelLogRequest.StartDate));
                objprm.Add(new SqlParameter("@endDate", getFulfillmentChannelLogRequest.EndDate == string.Empty ? null : getFulfillmentChannelLogRequest.EndDate));
                objprm.Add(new SqlParameter("@fulfillmentChannel", getFulfillmentChannelLogRequest.FulfillmentChannel));
                objprm.Add(new SqlParameter("@sourceSystemID", SSID));
                objprm.Add(new SqlParameter("@sourceSystemUniqueID", getFulfillmentChannelLogRequest.SourceSystemUniqueID));
                objprm.Add(new SqlParameter("@sourceSystemUniqueIDType", getFulfillmentChannelLogRequest.SourceSystemUniqueIDType));
                log.Verbose($"calling sp", "JE.RMS.Services.GetFulfillmentChannelLog");

                List<FulfillmentChannelTransactionLog> obj = MSSQLConnection.ExecuteStoredProcedure<FulfillmentChannelTransactionLog>(Common.Constants.USPContstants.GetFulfillmentChannelTransactionLog, objprm);
                log.Verbose($"received response:={obj}", "JE.RMS.Services.GetFulfillmentChannelLog");

                if (obj.Count > 0 && obj[0].FulfillmentChannelID == 0)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Validation Failed : Fulfillment Channel not supported;");
                }

                string transactionString = "{\"Transactions\":[";
                JObject Transactions = new JObject();
                foreach (var item in obj)
                {
                    transactionString += item.TransactionData + ",";
                }
                transactionString = transactionString.TrimEnd(',');
                transactionString = transactionString + "]}";

                var jObject = JObject.Parse(transactionString);
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
                return response;
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
