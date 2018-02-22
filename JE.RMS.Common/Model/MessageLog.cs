using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class MessageLog
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
    }
}
