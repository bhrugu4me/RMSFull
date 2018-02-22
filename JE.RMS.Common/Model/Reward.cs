using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class Reward
    {
        public string ProductCode { get; set; }
        public string ProductValue { get; set; }
        public string ProgramName { get; set; }
        public string RewardType { get; set; }
        public DateTime? EffectiveDate { get; set; }
    }
}
