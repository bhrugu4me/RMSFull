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

namespace JE.RMS.Services
{
    public class ScheduledUpdateCustomerExtended
    {
        public static async void Run(TimerInfo updateCustomerExtendedTimer, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# Timer trigger function processed a request", "JE.RMS.Services.ScheduledUpdateCustomerExtended");

            try
            {

                log.Verbose($"Update Customer Extended started on : {DateTime.Now.ToString()}", "JE.RMS.Services.ScheduledUpdateCustomerExtended");

                //Get All Customer Unique IDs
                List<string> ListCustomerUniqueIDs = MSSQLConnection.ExecuteStoredProcedure<string>(Common.Constants.USPContstants.GetAllCustomerUniqueID).ToList();

                //Fetch Customers and update customer extended
                foreach (var uniqueId in ListCustomerUniqueIDs)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["GetUserFunctionUrl"].ToString());

                        string GetUserFunctionUrl = ConfigurationManager.AppSettings["GetUserFunctionCode"].ToString() + uniqueId;
                        var response = await httpClient.GetAsync(GetUserFunctionUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseUser = JsonConvert.DeserializeObject<CustomerExtendedList>(await response.Content.ReadAsStringAsync());
                            var userData = responseUser.Users.FirstOrDefault();

                            List<SqlParameter> CustomerExtendedParams = new List<SqlParameter>();
                            CustomerExtendedParams.Add(new SqlParameter("@CustomerID", string.Empty));
                            CustomerExtendedParams.Add(new SqlParameter("@UniqueID", uniqueId));
                            CustomerExtendedParams.Add(new SqlParameter("@AccountAcceptanceDate", userData.AccountAcceptanceDate));
                            CustomerExtendedParams.Add(new SqlParameter("@StartingPointBalance", userData.StartingPointBalance));
                            CustomerExtendedParams.Add(new SqlParameter("@AvailablePointBalance", userData.AvailablePointBalance));
                            CustomerExtendedParams.Add(new SqlParameter("@AvailablePointBalanceDollars", userData.AvailablePointBalanceDollars));
                            CustomerExtendedParams.Add(new SqlParameter("@NumberofTransactions", userData.NumberofTransactions));
                            CustomerExtendedParams.Add(new SqlParameter("@AccountStatus", userData.AccountStatus));
                            CustomerExtendedParams.Add(new SqlParameter("@NextRewardDueDate", userData.NextRewardDueDate));
                            var customerExtendedID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveCustomerExtended, CustomerExtendedParams);
                        }
                        else
                        {
                            errormessage.Add(response.Content.ReadAsStringAsync().Result);
                        }
                    }
                }

                log.Verbose($"Update Customer Extended completed on : {DateTime.Now.ToString()}", "JE.RMS.Services.ScheduledUpdateCustomerExtended");

            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while UpdateCustomerExtended", ex, "JE.RMS.Services.ScheduledUpdateCustomerExtended");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}