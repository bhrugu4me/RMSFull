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
using System.Data.SqlClient;
using JE.RMS.Common.Model;
using Microsoft.ServiceBus.Messaging;
using JE.RMS.Common.Constants;
using Microsoft.ServiceBus;

namespace JE.RMS.Services
{
    public class ScheduledGetRewardsTrxReadyForFulfillment
    {

        public static void Run(TimerInfo getRewardsTrxReadyForFulfillmentTimer, ICollector<string> errormessage, TraceWriter log)
        {
            try
            {
                if (getRewardsTrxReadyForFulfillmentTimer.IsPastDue)
                {
                    log.Verbose("Timer is running late!", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");
                }

                //Get RewardsTrx Ready for fulfillment
                List<SqlParameter> objprm = new List<SqlParameter>();
                objprm.Add(new SqlParameter("@RewardTrxStatus", "Ready for fulfillment"));
                List<Common.Model.GetRewardsRequest> lstRewardsTrx = MSSQLConnection.ExecuteStoredProcedure<Common.Model.GetRewardsRequest>(Common.Constants.USPContstants.GetRewardsTrx, objprm).ToList();
                log.Verbose($"Get RewardsTrx Ready for fulfillment:={lstRewardsTrx.Count}", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");

                //Service bus queue names and connection strings
                var connectionString = ConfigurationManager.AppSettings["MyServiceBusReader"].ToString();
                var FulfillmentRequestTopicClient = TopicClient.CreateFromConnectionString(connectionString, "fulfillmentrequest");
                var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

                #region Create Subscrptions for the first time
                //// Create a "EnergyEarthSubscription" filtered subscription.
                if (!namespaceManager.SubscriptionExists("fulfillmentrequest", "EnergyEarthSubscription"))
                {
                    SqlFilter energyEarthFilter = new SqlFilter("channelType = 'EnergyEarth'");
                    namespaceManager.CreateSubscription("fulfillmentrequest", "EnergyEarthSubscription", energyEarthFilter);
                }

                //// Create a "GBASSSubscription" filtered subscription.
                if (!namespaceManager.SubscriptionExists("fulfillmentrequest", "GBASSCTLAdjSubscription"))
                {
                    SqlFilter GBASSFilter = new SqlFilter("channelType = 'GBASSCTLAdj'");
                    namespaceManager.CreateSubscription("fulfillmentrequest", "GBASSCTLAdjSubscription", GBASSFilter);
                }

                if (!namespaceManager.SubscriptionExists("fulfillmentrequest", "GBASSTariffSubscription"))
                {
                    SqlFilter GBASSFilter = new SqlFilter("channelType = 'GBASSTariff'");
                    namespaceManager.CreateSubscription("fulfillmentrequest", "GBASSTariffSubscription", GBASSFilter);
                }

                //// Create a "CRMSubscription" filtered subscription.
                if (!namespaceManager.SubscriptionExists("fulfillmentrequest", "CRMSubscription"))
                {
                    SqlFilter CRMFilter = new SqlFilter("channelType = 'CRM'");
                    namespaceManager.CreateSubscription("fulfillmentrequest", "CRMSubscription", CRMFilter);
                }

                //// Create a "SmartConnectSubscription" filtered subscription.
                if (!namespaceManager.SubscriptionExists("fulfillmentrequest", "SmartConnectSubscription"))
                {
                    SqlFilter SmartConnectFilter = new SqlFilter("channelType = 'SmartConnectCheque'");
                    namespaceManager.CreateSubscription("fulfillmentrequest", "SmartConnectSubscription", SmartConnectFilter);
                }
                #endregion

                RewardFulfillmentRequest RewardsRequestObj = new RewardFulfillmentRequest();
                foreach (Common.Model.GetRewardsRequest item in lstRewardsTrx)
                {
                    RewardFulfillmentRequestList rewardFulfillmentRequestList = new RewardFulfillmentRequestList();
                    rewardFulfillmentRequestList.RewardFulfillmentRequest = new RewardFulfillmentRequest();

                    RewardsRequestObj.RequestId = item.RequestId;
                    RewardsRequestObj.TransactionType = item.TransactionType;
                    RewardsRequestObj.RMSRewardID = item.RMSRewardID;

                    RewardsRequestObj.Reward = new Reward();
                    RewardsRequestObj.Reward.ProductCode = item.ProductCode;
                    RewardsRequestObj.Reward.ProductValue = item.ProductValue.ToString();
                    RewardsRequestObj.Reward.ProgramName = item.ProgramName;
                    RewardsRequestObj.Reward.EffectiveDate = item.EffectiveDate;
                    RewardsRequestObj.Reward.RewardType = item.RewardType;

                    RewardsRequestObj.Customer = new CustomerJSON();
                    RewardsRequestObj.Customer.CustomerID = item.CustomerID;
                    RewardsRequestObj.Customer.SourceSystemID = item.DBSourceSystemID.ToString();
                    RewardsRequestObj.Customer.SourceSystemName = item.SourceSystemShortName;
                    RewardsRequestObj.Customer.SourceSystemUniqueID = item.SourceSystemUniqueID;
                    RewardsRequestObj.Customer.SourceSystemUniqueIDType = item.SourceSystemUniqueIDType;
                    RewardsRequestObj.Customer.MasterID = item.MasterID;
                    RewardsRequestObj.Customer.Email = item.Email;
                    RewardsRequestObj.Customer.FirstName = item.FirstName;
                    RewardsRequestObj.Customer.LastName = item.LastName;
                    RewardsRequestObj.Customer.CompanyName = item.CompanyName;
                    RewardsRequestObj.Customer.AddressLine1 = item.AddressLine1;
                    RewardsRequestObj.Customer.AddressLine2 = item.AddressLine2;
                    RewardsRequestObj.Customer.City = item.City;
                    RewardsRequestObj.Customer.StateProvince = item.StateProvince;
                    RewardsRequestObj.Customer.ZipPostalCode = item.ZipPostalCode;
                    RewardsRequestObj.Customer.Phone1 = item.Phone1;
                    RewardsRequestObj.Customer.Product = item.Product;
                    RewardsRequestObj.Customer.Language = item.LanguageCode;
                    RewardsRequestObj.AdditionalData = JsonConvert.DeserializeObject<AdditionalData[]>(item.AdditionalData);
                    rewardFulfillmentRequestList.RewardFulfillmentRequest = RewardsRequestObj;
                    var reqObject = JsonConvert.SerializeObject(rewardFulfillmentRequestList);
                    if (item.FulfillmentChannelID != (int)Common.Constants.FulfillmentChannel.EnergyEarth)
                    {
                        var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(reqObject);
                        var serializedJson = (JObject)JsonConvert.DeserializeObject(reqObject);
                        foreach (var response in serializedJson["RewardFulfillmentRequest"]["Customer"])
                        {
                            if (response.Path.Contains("CustomerID"))
                            {
                                response.Remove();
                                break;
                            }
                        }
                        reqObject = JsonConvert.SerializeObject(serializedJson);
                    }

                    log.Verbose($"Reward fulfillment request push to different channel :={reqObject.ToString()}", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");
                    var message = new BrokeredMessage(reqObject);
                    if (item.FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.EnergyEarth)
                    {
                        message.Properties["channelType"] = FulfillmentChannel.EnergyEarth.ToString();
                        FulfillmentRequestTopicClient.Send(message);
                    }
                    else if (item.FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSTariff)
                    {
                        message.Properties["channelType"] = FulfillmentChannel.GBASSTariff.ToString();
                        FulfillmentRequestTopicClient.Send(message);
                    }
                    else if (item.FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.GBASSCTLAdj)
                    {
                        message.Properties["channelType"] = FulfillmentChannel.GBASSCTLAdj.ToString();
                        FulfillmentRequestTopicClient.Send(message);
                    }
                    else if (item.FulfillmentChannelID == (int)Common.Constants.FulfillmentChannel.Cheque)
                    {
                        message.Properties["channelType"] = "SmartConnectCheque";
                        FulfillmentRequestTopicClient.Send(message);
                    }

                    //Call stored procedure to Update RewardTrx
                    List<SqlParameter> RewardTrxParams = new List<SqlParameter>();
                    RewardTrxParams.Add(new SqlParameter("@RewardTrxID", item.RewardTrxID));
                    var RewardTrxID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.UpdateFulfillmentRequestTimestamp, RewardTrxParams);
                    log.Verbose($"RewardTrx updated successfully. RewardTrID={RewardTrxID[0]}", "JE.RMS.Services.EvaluateRewardsTrx");

                    //Call stored procedure to Save MessageLog
                    List<SqlParameter> MessageLogParams = new List<SqlParameter>();
                    MessageLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID));
                    MessageLogParams.Add(new SqlParameter("@MessageType", MessageType.RewardFulfillmentRequest.GetDescription()));
                    MessageLogParams.Add(new SqlParameter("@IPAddress", ""));
                    MessageLogParams.Add(new SqlParameter("@Message", reqObject));
                    MessageLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var MessageLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveMessageLog, MessageLogParams);
                    log.Verbose($"MessageLog stored successfully. MessageLogID={MessageLogID[0]}", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");


                    //Call stored procedure to Save RewardTrxChangeLog
                    List<SqlParameter> RewardTrxChangeLogParams = new List<SqlParameter>();
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardsTrxID", item.RewardTrxID.ToString()));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RewardTrxStatus", "Sent for fulfillment"));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@Comment", string.Empty));
                    RewardTrxChangeLogParams.Add(new SqlParameter("@RMSRewardID", null));
                    var RewardTrxChangeLogID = MSSQLConnection.ExecuteStoredProcedure<long>(USPContstants.SaveRewardTrxChangeLog, RewardTrxChangeLogParams);
                    log.Verbose($"RewardTrxChangeLog stored successfully. RewardTrxChangeLogID={RewardTrxChangeLogID[0]}", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");
                }

                log.Verbose($"C# Timer trigger function executed at: {DateTime.Now}", "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");

            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.GetRewardsTrxReadyForFulfillment");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }

    }
}