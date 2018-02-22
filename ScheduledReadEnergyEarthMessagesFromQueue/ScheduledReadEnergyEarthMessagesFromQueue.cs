using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Configuration;
using System;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Collections.Generic;
using JE.RMS.Common.Model;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;

namespace JE.RMS.Services
{
    public class ScheduledReadEnergyEarthMessagesFromQueue
    {

        public static async Task Run(TimerInfo getScheduledReadEnergyEarthMessagesTimer, ICollector<string> errormessage, TraceWriter log)
        {

            if (getScheduledReadEnergyEarthMessagesTimer.IsPastDue)
            {
                log.Verbose("Timer is running late!", "JE.RMS.Services.ScheduledReadEnergyEarthMessagesFromQueue");
            }

            try
            {
                var connectionString = ConfigurationManager.AppSettings["MyServiceBusReader"].ToString();

                SubscriptionClient client = SubscriptionClient.CreateFromConnectionString(connectionString, "fulfillmentrequest", "EnergyEarthSubscription");

                int batchSize = Convert.ToInt32(ConfigurationManager.AppSettings["ReadEnergyEarthMessageBatchSize"].ToString());
                var brokeredMessagesList = client.ReceiveBatch(1);

                List<Guid> messageLockTokenList = new List<System.Guid>();

                if (brokeredMessagesList.Count() > 0)
                {
                    foreach (BrokeredMessage message in brokeredMessagesList)
                    {
                        var raw = message.GetBody<string>();
                        var RewardFulfillmentRequestList = JsonConvert.DeserializeObject<RewardFulfillmentRequestList>(raw);

                        using (HttpClient httpClient = new HttpClient())
                        {
                            //Add Basic Authentication header
                            httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["ProcessFulfillmentUrl"].ToString());

                            //log.Verbose($"Calling Update point status API, UserId : {CustomerUniqueId}, with Status : {PointStatus.Status}", "JE.RMS.Services.ProcessFulfillmentForEnergyEarth");
                            var response = await httpClient.PostAsJsonAsync(string.Empty, RewardFulfillmentRequestList);

                            if (response.IsSuccessStatusCode)
                            {
                                log.Verbose($"Success : Process fulfillment for EE", "JE.RMS.Services.ScheduledReadEnergyEarthMessagesFromQueue");
                            }
                            else
                            {
                                log.Error($"Error : Process fulfillment for EE", null, "JE.RMS.Services.ScheduledReadEnergyEarthMessagesFromQueue");
                            }
                        }
                        messageLockTokenList.Add(message.LockToken);
                    }

                    client.CompleteBatch(messageLockTokenList);
                    log.Verbose($"Completed : Process fulfillment for EE batch", "JE.RMS.Services.ScheduledReadEnergyEarthMessagesFromQueue");

                }
            }
            catch (Exception ex)
            {
                log.Error("Something went wrong while ScheduledReadEnergyEarthMessagesFromQueue", ex, "JE.RMS.Services.ScheduledReadEnergyEarthMessagesFromQueue");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}