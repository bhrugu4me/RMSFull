using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class CustomerExtended
    {
        public int CustomerExtendedID { get; set; }
        public int CustomerID { get; set; }
        public int FulfillmentChannelID { get; set; }
        public string UniqueID { get; set; }
        public DateTime? AccountAcceptanceDate { get; set; }
        public string StartingPointBalance { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string AvailablePointBalance { get; set; }
        public string AvailablePointBalanceDollars { get; set; }
        public string NumberofTransactions { get; set; }
        public string AccountStatus { get; set; }
        public string NextRewardDueDate { get; set; }
        public string Program { get; set; }
    }

    public class CustomerExtendedList
    {
        public List<CustomerExtended> Users { get; set; }
    }
}
