using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class GetPointsRequest
    {
        public string ChannelCode { get; set; }
        public string Email { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemName { get; set; }
    }

    public class GetFulfillmentChannelLogRequest
    {
        public string FulfillmentChannel { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
    }
}
