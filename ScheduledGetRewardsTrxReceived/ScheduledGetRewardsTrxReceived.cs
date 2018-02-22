using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using Newtonsoft.Json.Linq;
using System.Web;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using System.Collections.Generic;
using Dapper;
using System.Data;
using JE.RMS.Common.Model;
using System.Data.SqlClient;
using JE.RMS.Common.Constants;

namespace JE.RMS.Services
{
    public class ScheduledGetRewardsTrxReceived
    {

        public static void Run(TimerInfo evaluateRewardsTrxTimer, ICollector<string> errormessage, TraceWriter log)
        {
            if (evaluateRewardsTrxTimer.IsPastDue)
            {
                log.Verbose("Timer is running late!", "JE.RMS.Services.GetRewardsTrxReceived");
            }

            try
            {
                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@RewardTrxStatus", "Received"));
                List<GetRewardsRequest> lstRewardsTrx = MSSQLConnection.ExecuteStoredProcedure<GetRewardsRequest>(USPContstants.GetRewardsTrx, objprm).ToList();
                log.Verbose($"Get RewardsTrx Received:={lstRewardsTrx.Count}", "JE.RMS.Services.GetRewardsTrxReceived");

                // Retrieve storage account from connection string
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["jermsstorage_STORAGE"].ToString());
                // Create the queue client
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                // Retrieve a reference to a queue
                CloudQueue queue = queueClient.GetQueueReference("rewardfulfillmentrequestqueue");
                // Create the queue if it doesn't already exist.
                queue.CreateIfNotExists();

                foreach (GetRewardsRequest item in lstRewardsTrx)
                {
                    var reqObject = JsonConvert.SerializeObject(item);
                    log.Verbose($"Push message in a queue:= {reqObject}", "JE.RMS.Services.GetRewardsTrxReceived");
                    // Create a message and add it to the queue.
                    CloudQueueMessage message = new CloudQueueMessage(reqObject.ToString());
                    queue.AddMessage(message);

                    //Call stored procedure to Save RewardTrxChangeLog
                    List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID.ToString()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", "Received - Ready for fulfillment"));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", string.Empty));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                }

                log.Verbose($"C# Timer trigger function executed at: {DateTime.Now}", "JE.RMS.Services.GetRewardsTrxReceived");
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.GetRewardsTrxReceived");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }

    }
}