using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class RewardTrxStatus
    {
        public int RewardsTrxStatusID {get;set;}
        public string StatusName { get; set; }
        public bool IsActive { get; set; }
        
    }
}
