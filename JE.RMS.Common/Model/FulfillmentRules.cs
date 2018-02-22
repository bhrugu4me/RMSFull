using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class FulfillmentRules
    {
        public int RuleID { get; set; }
        public string RuleName { get; set; }
        public int ProgramID { get; set; }
        public int ProductID { get; set; }
        public int FulfillmentChannelID { get; set; }
        public bool RequireApproval { get; set; }
        public int MaxOccurrencePerYear { get; set; }
        public int MaxOccurrencePerCustomer { get; set; }
        public decimal MaxRewardValue { get; set; }
        public decimal MaxCumulativeRewardValuePerYear { get; set; }
        public bool IsActive { get; set; }
    }
}
