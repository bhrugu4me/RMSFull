using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;
using System;
using Newtonsoft.Json;
using JE.RMS.Common.Model;
using Newtonsoft.Json.Linq;
using JE.RMS.Common;
using Dapper;
using System.Linq;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Data.SqlClient;
using JE.RMS.Common.Constants;

namespace JE.RMS.Services
{
    public class DMEEUserToCustomer
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}", "JE.RMS.Services.DMEEUserToCustomer");

            // Get request body
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                    var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                    string GetUsersUrl = ConfigurationManager.AppSettings["GetUsersUrl"].ToString();

                    //Get Users from Energy Earth API
                    var response = await httpClient.GetAsync(GetUsersUrl);

                    //Convert Users from Energy earth to Customer List Model
                    var customerList = JsonConvert.DeserializeObject<EECustomerList>(await response.Content.ReadAsStringAsync());

                    log.Verbose("Started migration of EE users to Customer Table", "JE.RMS.Services.DMEEUserToCustomer");

                    foreach (var customer in customerList.Users)
                    {
                        List<SqlParameter> CustomerParams = new List<SqlParameter>();
                        CustomerParams.Add(new SqlParameter("@Email", customer.Email));
                        CustomerParams.Add(new SqlParameter("@FirstName", customer.FirstName));
                        CustomerParams.Add(new SqlParameter("@LastName", customer.LastName));
                        CustomerParams.Add(new SqlParameter("@CompanyName", customer.CompanyName));
                        CustomerParams.Add(new SqlParameter("@UniqueID", customer.UserID));
                        CustomerParams.Add(new SqlParameter("@AccountAcceptanceDate", customer.AccountAcceptanceDate));
                        CustomerParams.Add(new SqlParameter("@StartingPointBalance", customer.StartingPointBalance));
                        CustomerParams.Add(new SqlParameter("@AvailablePointBalance", customer.AvailablePointBalance));
                        CustomerParams.Add(new SqlParameter("@AvailablePointBalanceDollars", customer.AvailablePointBalanceDollars));
                        CustomerParams.Add(new SqlParameter("@NumberofTransactions", customer.NumberofTransactions));
                        CustomerParams.Add(new SqlParameter("@AccountStatus", customer.AccountStatus));
                        CustomerParams.Add(new SqlParameter("@NextRewardDueDate", customer.NextRewardDueDate));
                        CustomerParams.Add(new SqlParameter("@ProgramName", customer.Program));

                        var RMSRewardID = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.SaveCustomerFromEnergyEarth, CustomerParams);
                    }

                    log.Verbose("Completed migration of EE users to Customer Table", "JE.RMS.Services.DMEEUserToCustomer");

                    return response;
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while DMEEUserToCustomer", ex, "JE.RMS.Services.DMEEUserToCustomer");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
                return req.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}