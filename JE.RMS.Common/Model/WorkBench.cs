using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class WorkBench
    {
        public long MessageLogID { get; set; }
        public long RewardsTrxID { get; set; }
        public int SystemID { get; set; }
        public int FulfillmentChannelID { get; set; }
        public int MessageTypeID { get; set; }
        public string IPAddress { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public string MessageType { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string TransactionTypeName { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string ProductName { get; set; }
        public string StatusName { get; set; }
        public double? ProductValue { get; set; }
        public string Comment { get; set; }
    }
}
