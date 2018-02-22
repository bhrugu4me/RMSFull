using JE.RMS.Common;
using JE.RMS.Common.Constants;
using JE.RMS.Common.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;

namespace JE.RMS.Services
{
    public class ScheduledUpdateEnergyEarthUserInfo
    {
        public static async void Run(TimerInfo updateCustomerExtendedTimer, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# Timer trigger function processed a request", "JE.RMS.Services.ScheduledUpdateEnergyEarthUserInfo");

            try
            {

                log.Verbose($"Update Customer Extended started on : {DateTime.Now.ToString()}", "JE.RMS.Services.ScheduledUpdateEnergyEarthUserInfo");

                using (HttpClient httpClient = new HttpClient())
                {
                    //Add Basic Authentication header
                    httpClient.BaseAddress = new System.Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                    var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

                    string GetUsersUrl = ConfigurationManager.AppSettings["GetUsersUrl"].ToString();

                    //Get Users from Energy Earth API
                    var response = await httpClient.GetAsync(GetUsersUrl);

                    if (response.IsSuccessStatusCode == true)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        //Convert Users from Energy earth to Customer List Model
                        var customerList = JsonConvert.DeserializeObject<EECustomerList>(responseContent);

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
                    }

                    log.Verbose("Completed migration of EE users to Customer Table", "JE.RMS.Services.DMEEUserToCustomer");

                }

                log.Verbose($"Update Customer Extended completed on : {DateTime.Now.ToString()}", "JE.RMS.Services.ScheduledUpdateEnergyEarthUserInfo");

            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while UpdateCustomerExtended", ex, "JE.RMS.Services.ScheduledUpdateEnergyEarthUserInfo");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}