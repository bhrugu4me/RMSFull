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
using JE.RMS.Common.Constants;

namespace JE.RMS.Services
{
    public class OnRewardsRequestRecieved
    {
        #region valriables
        public static IList<string> messages;
        public static bool valid;
        public static string saverewardsobj;
        #endregion

        public static async void Run(string inputmessage, ICollector<string> errormessage, TraceWriter log)
        {
            log.Verbose($"C# trigger queue function processed a request. inputmessage={inputmessage}", "JE.RMS.Services.OnRewardsRequestRecieved");

            try
            {
                JObject inputJSON = JObject.Parse(inputmessage);

                JSchema objectschema = new JSchema();
                objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RequestSchema);

                if (inputJSON["TransactionType"] != null)
                {
                    if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Qualify.GetDescription()
                                || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Terminate.GetDescription()
                                || inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reactivate.GetDescription())
                    {
                        objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.Terminate_Reactivate_Qualify_Schema);
                    }

                    if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.Reward.GetDescription())
                    {
                        objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.RewardSchema);
                    }

                    if (inputJSON["TransactionType"].ToString() == TransactionTypeEnum.ProgramUpdateSourceSystem.GetDescription())
                    {
                        objectschema = JSchema.Parse(Common.Constants.RewardRequestSchema.ProgramUpdateSchema);
                    }
                }

                //Message schema validation
                valid = inputJSON.IsValid(objectschema, out messages);
                log.Verbose($"Valid ={valid}", "JE.RMS.Services.OnRewardsRequestRecieved");
                inputJSON.Add("IsValid", valid);
                var messageJSON = "";
                if (messages.Count > 0)
                    messageJSON = JsonConvert.SerializeObject(messages);
                inputJSON.Add("ValidationMessage", messageJSON);
                log.Verbose($"Validation message = {messageJSON}", "JE.RMS.Services.OnRewardsRequestRecieved");

                saverewardsobj = inputJSON.ToString();
                log.Verbose($"Published message ={saverewardsobj}", "JE.RMS.Services.OnRewardsRequestRecieved");

                // Call SaveRewardsTrx to save rewards transaction.
                using (HttpClient client = new HttpClient())
                {
                    var SaveRewardsTrxEndpoint = ConfigurationManager.AppSettings["SaveRewardsTrxEndpoint"].ToString();
                    var accept = "application/json";
                    client.DefaultRequestHeaders.Add("Accept", accept);

                    using (var response = await client.PostAsync(SaveRewardsTrxEndpoint, new StringContent(saverewardsobj, Encoding.UTF8, "application/x-www-form-urlencoded")))
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        log.Verbose($"Response ={result}", "JE.RMS.Services.OnRewardsRequestRecieved");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception ={ex}", ex, "JE.RMS.Services.OnRewardsRequestRecieved");
                errormessage.Add(JsonConvert.SerializeObject(ex).ToString());
            }
        }
    }
}