using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;
using System;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using System.Linq;
using JE.RMS.Common.Model;
using JE.RMS.Common;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using Dapper;
using Newtonsoft.Json.Linq;

namespace JE.RMS.Services
{
    public class ScheduledGetPointTransactionLog
    {
        public static string startDate = string.Empty;
        public static string endDate = string.Empty;
        public static async void Run(TimerInfo getPointTransactionTimer, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# Timer trigger function processed a request", "JE.RMS.Services.ScheduledGetPointTransactionLog");

            try
            {
                List<SqlParameter> objParams = new List<SqlParameter>();
                objParams.Add(new SqlParameter("@ProcessName", "GetPointTransactionLog"));
                ProcessLog DateLog = MSSQLConnection.ExecuteStoredProcedure<ProcessLog>(Common.Constants.USPContstants.GetLastDateOfTransactionLog, objParams).FirstOrDefault();

                log.Verbose($"Get Points transaction called with start date = {DateLog.StartDate.Date.ToString()}, end date = {DateLog.EndDate.Date.ToString()}", "JE.RMS.Services.GetTransactionLogFromEnergyEarth");

                //Check if start date & end date is provided
                if (DateLog.StartDate != null && DateLog.EndDate != null)
                {

                    if (DateLog.StartDate > DateLog.EndDate)
                    {
                        log.Verbose($"Function seems to be already executed before. Provided startdate : {DateLog.StartDate} and EndDate : {DateLog.EndDate}");
                    }
                    using (HttpClient httpClient = new HttpClient())
                    {
                        //Add Basic Authentication header
                        httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["EnergyEarthBaseUrl"].ToString());
                        var auth = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["EEUserName"].ToString() + ":" + ConfigurationManager.AppSettings["EEPassword"].ToString());
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));
                        startDate = DateLog.StartDate.ToString("MM/dd/yyyy");
                        endDate = DateLog.EndDate.ToString("MM/dd/yyyy");
                        string GetPointTransactionsUrl = ConfigurationManager.AppSettings["GetTransactionLogFromEnergyEarthUrl"].ToString() + "?startDate=" + startDate + "&endDate=" + endDate;

                        var response = await httpClient.GetStringAsync(GetPointTransactionsUrl);

                        log.Verbose($"Success : Get Points transaction start date={DateLog.StartDate.Date.ToString()}, end date = {DateLog.EndDate.Date.ToString()}.", "JE.RMS.Services.GetTransactionLogFromEnergyEarth");

                        //Convert Users from Energy earth to Customer List Model
                        using (var conn = MSSQLConnection.CreateConnection())
                        {
                            log.Verbose("Started migration of EE users to Customer Table", "JE.RMS.Services.DMEEUserToCustomer");

                            conn.Open();
                            string deleteQuery = "DELETE FROM [dbo].[FulfillmentChannelTransactionLog] WHERE RecordDate BetWeen @StartDate AND @EndDate";
                            conn.Execute(deleteQuery, new { StartDate = DateLog.StartDate, EndDate = DateLog.EndDate });

                            string getFulfillmentChannelIDQuery = "SELECT FulfillmentChannelID FROM [dbo].[FulfillmentChannel] WHERE ChannelName = 'EnergyEarth'";
                            int FID = conn.Query<int>(getFulfillmentChannelIDQuery).FirstOrDefault();

                            //Convert Users from Energy earth to Customer List Model
                            var transactionList = JObject.Parse(response);
                            List<FulfillmentChannelTransactionLog> transactionLogList = new List<FulfillmentChannelTransactionLog>();
                            foreach (JObject x in transactionList["Transactions"])
                            { // Where 'obj' and 'obj["otherObject"]' are both JObjects
                                FulfillmentChannelTransactionLog transactionLog = new FulfillmentChannelTransactionLog();
                                transactionLog.CreatedDate = DateTime.Now;
                                transactionLog.TransactionData = x.ToString();
                                transactionLog.FulfillmentChannelID = FID;
                                string date = x.SelectToken("OrderDate").ToString();
                                string date1 = x.SelectToken("ShipDate").ToString();
                                transactionLog.RecordDate = string.IsNullOrEmpty(date) ? DateTime.Now : Convert.ToDateTime(date);
                                transactionLogList.Add(transactionLog);
                            }

                            string processQuery = "INSERT INTO [dbo].[FulfillmentChannelTransactionLog] (FulfillmentChannelID,TransactionData,RecordDate,CreatedDate) VALUES (@FulfillmentChannelID, @TransactionData, @RecordDate, @CreatedDate)";
                            conn.Execute(processQuery, transactionLogList);

                            var Log = MSSQLConnection.ExecuteStoredProcedure<ProcessLog>(Common.Constants.USPContstants.UpdateProcessLog, objParams).FirstOrDefault();

                            log.Verbose("Completed migration of EE users to Customer Table", "JE.RMS.Services.ScheduledGetPointTransactionLog");
                        }

                    }
                }
                else //Bad request : Start Date or End Date is not provided.
                {
                    log.Error("Missing Parameter for GetTransactionLogFromEnergyEarth : startDate/endDate", null, "JE.RMS.Services.ScheduledGetPointTransactionLog");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while GetTransactionLogFromEnergyEarth", ex, "JE.RMS.Services.ScheduledGetPointTransactionLog");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}