using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class FulfillmentResponse
    {
        public string RequestID { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string RMSRewardID { get; set; }
        public string ClientIP { get; set; }
    }
}
