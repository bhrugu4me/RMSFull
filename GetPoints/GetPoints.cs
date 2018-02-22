using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using System;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using System.Linq;
using JE.RMS.Common.Model;
using JE.RMS.Common;
using System.Collections.Generic;
using System.Data.SqlClient;
using JE.RMS.Common.Constants;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;

namespace JE.RMS.Services
{
    public class GetPoints
    {
        #region variables
        public static JObject reqMessage;

        #endregion
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.GetPoints");
            bool isValid = true;
            string validationMessage = string.Empty;
            int SourceSystemID = 0;
            int SSID = 0;
            try
            {
                string reqString = await req.Content.ReadAsStringAsync();
                var GetPointsRequest = JsonConvert.DeserializeObject<GetPointsRequest>(reqString);

                if (!string.IsNullOrEmpty(GetPointsRequest.SourceSystemID))
                {
                    int.TryParse(GetPointsRequest.SourceSystemID, out SourceSystemID);
                    if (SourceSystemID <= 0)
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : SourceSystemID must be numeric;";
                    }
                }

                if (string.IsNullOrEmpty(GetPointsRequest.ChannelCode))
                {
                    isValid = false;
                    validationMessage = validationMessage + "Validation Failed : Channel Code is required;";
                }
                else if (GetPointsRequest.ChannelCode != FulfillmentChannel.EnergyEarth.GetDescription())
                {
                    isValid = false;
                    validationMessage = validationMessage + "Validation Failed : Channel code not supported;";
                }

                if ((!string.IsNullOrEmpty(GetPointsRequest.SourceSystemUniqueID) && string.IsNullOrEmpty(GetPointsRequest.SourceSystemUniqueIDType)) || (string.IsNullOrEmpty(GetPointsRequest.SourceSystemUniqueID) && !string.IsNullOrEmpty(GetPointsRequest.SourceSystemUniqueIDType)))
                {
                    isValid = false;
                    validationMessage = validationMessage + "Validation Failed : SourceSystemUniqueID & SourceSystemUniqueIDType both are required;";
                }

                if (SourceSystemID > 0 || !string.IsNullOrEmpty(GetPointsRequest.SourceSystemName))
                {
                    List<SqlParameter> SSIDprm = new List<SqlParameter>();
                    SSIDprm.Add(new SqlParameter("@SourceSystemShortName", GetPointsRequest.SourceSystemName));
                    SSIDprm.Add(new SqlParameter("@SourceSystemID", SourceSystemID));

                    SSID = MSSQLConnection.ExecuteStoredProcedure<int>(USPContstants.GetSourceSystemID, SSIDprm).FirstOrDefault();
                    if (SSID == 0)
                    {
                        isValid = false;
                        validationMessage = validationMessage + "Validation Failed : Please provide valid SourceSystemID or SourceSystemName (if both are provided they must match);";
                    }
                }


                if (!string.IsNullOrEmpty(GetPointsRequest.Email))
                {
                    GetPointsRequest.SourceSystemUniqueID = string.Empty;
                    GetPointsRequest.SourceSystemUniqueIDType = string.Empty;
                    GetPointsRequest.MasterID = string.Empty;
                }

                if (!string.IsNullOrEmpty(GetPointsRequest.MasterID))
                {
                    GetPointsRequest.SourceSystemUniqueID = string.Empty;
                    GetPointsRequest.SourceSystemUniqueIDType = string.Empty;
                }

                if (isValid == false)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, validationMessage);
                }

                //Get All Customer Unique IDs
                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@email", GetPointsRequest.Email));
                objprm.Add(new SqlParameter("@masterID", GetPointsRequest.MasterID));
                objprm.Add(new SqlParameter("@sourceSystemUniqueID", GetPointsRequest.SourceSystemUniqueID));
                objprm.Add(new SqlParameter("@sourceSystemUniqueIDType", GetPointsRequest.SourceSystemUniqueIDType));
                objprm.Add(new SqlParameter("@channelCode", GetPointsRequest.ChannelCode));
                objprm.Add(new SqlParameter("@SourceSystemID", SSID));

                var ListCustomers = MSSQLConnection.ExecuteStoredProcedure<CustomerPointsResponse>(Common.Constants.USPContstants.GetCustomerPoints, objprm).ToList();

                //Fetch Customers and update customer extended
                foreach (var item in ListCustomers)
                {
                    if (!string.IsNullOrEmpty(item.UniqueID))
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["GetPointsFunctionUrl"].ToString());

                            string GetPointsFunctionCode = ConfigurationManager.AppSettings["GetPointsFunctionCode"].ToString();

                            string GetPointsFunctionUrl = GetPointsFunctionCode + "&UserId=" + item.UniqueID;
                            var response = await httpClient.GetAsync(GetPointsFunctionUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                item.Points = Math.Round(Convert.ToDouble(response.Content.ReadAsStringAsync().Result) * 100, 2);
                                item.SourceSystemUniqueID = GetPointsRequest.SourceSystemUniqueID;
                                item.SourceSystemUniqueIDType = GetPointsRequest.SourceSystemUniqueIDType;
                                item.MasterID = GetPointsRequest.MasterID;
                                item.ChannelCode = GetPointsRequest.ChannelCode;
                                item.SourceSystemID = GetPointsRequest.SourceSystemID;
                                item.SourceSystemName = GetPointsRequest.SourceSystemName;
                            }
                            else
                            {
                                errormessage.Add(response.Content.ReadAsStringAsync().Result);
                            }
                        }
                    }
                }

                log.Verbose($"Update Customer Extended completed on : {DateTime.Now.ToString()}", "JE.RMS.Services.GetPoints");

                var PointResponse = req.CreateResponse(HttpStatusCode.OK);
                PointResponse.Content = new StringContent(JsonConvert.SerializeObject(ListCustomers), System.Text.Encoding.UTF8, "application/json");
                return PointResponse;
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while UpdateCustomerExtended", ex, "JE.RMS.Services.GetPoints");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}