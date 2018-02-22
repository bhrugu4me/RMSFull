using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class Product
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ProgramCode { get; set; }
        public string ProgramName { get; set; }
        public string Description { get; set; }
        public string RuleName { get; set; }
        public bool RequireApproval { get; set; }
        public int MaxOccurrencePerYear { get; set; }
        public int MaxOccurrencePerCustomer { get; set; }
        public decimal MaxRewardValue { get; set; }
        public decimal MaxCumulativeRewardValuePerYear { get; set; }
    }

    
}
