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
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;

namespace JE.RMS.Services
{
    public class ReadRewardsTrxFromQueue
    {

        public static void Run(string myQueueItem, TraceWriter log)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"\RewardsRequest-schema.json");
            JSchema schema = JSchema.Parse(path);
            IList<string> messages;

            bool valid = data.IsValid(schema, out messages);

        }
    }
}