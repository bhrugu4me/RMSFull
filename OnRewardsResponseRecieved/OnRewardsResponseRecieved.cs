using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Configuration;
using JE.RMS.Common;
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Text;
using JE.RMS.Common.Model;
using Microsoft.ServiceBus;
using System.Data.SqlClient;
using JE.RMS.Common.Constants;
using Microsoft.ServiceBus.Messaging;

namespace JE.RMS.Services
{
    public class OnRewardsResponseRecieved
    {
        public static void Run(FulfillmentResponse responsemessage, ICollector<string> errormessage, TraceWriter log)
        {
            try
            {
                log.Verbose($"C# trigger queue function processed a request. inputmessage={responsemessage}", "JE.RMS.Services.OnRewardsResponseRecieved");
                #region Update Audit Fields
                //Update timestapm in Audit fields
                List<SqlParameter> AuditParam = new List<SqlParameter>();
                AuditParam.Add(new SqlParameter("@RMSRewardID", responsemessage.RMSRewardID));

                var RMSRewardID = MSSQLConnection.ExecuteStoredProcedure<string>(USPContstants.UpdateAuditFieldsInRewardsTrx, AuditParam);
                log.Verbose($"FulfillmentResponseTimestamp updated successfully. RMSRewardID={RMSRewardID[0]}", "JE.RMS.Services.OnRewardsResponseRecieved");
                string RewardTrxStatus = "Fulfillment completed";
                if (responsemessage.Status == "Fail")
                    RewardTrxStatus = "Error";

                List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                MessageLogParams.Add(new SqlParameter("@RMSRewardID", responsemessage.RMSRewardID));
                MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentResponse.GetDescription()));
                MessageLogParams.Add(new SqlParameter("@IPAddress", responsemessage.ClientIP));
                MessageLogParams.Add(new SqlParameter("@Message", JsonConvert.SerializeObject(responsemessage)));
                MessageLogParams.Add(new SqlParameter("@RewardsTrxID", null));
                var ErrorMessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                log.Verbose($"MessageLog stored successfully. MessageLogID={ErrorMessageLogID[0]}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");

                ////Call stored procedure to Save RewardTrxChangeLog
                List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", responsemessage.RMSRewardID));
                RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", RewardTrxStatus));
                RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", string.Empty));
                RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", null));

                var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={RewardTrxChangeLogID[0]}", "JE.RMS.Services.OnRewardsResponseRecieved");
                #endregion
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.OnRewardsRequestRecieved");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}