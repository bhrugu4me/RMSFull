using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class RewardsTrxChangeLog
    {
        public long RewardTrxLogID { get; set; }
        public long RewardTrxID { get; set; }
        public int RewardTrxStatusID { get; set; }
        public string Comment { get; set; }
        public int UserID { get; set; }
        public DateTime LogDate { get; set; }
        public string StatusName { get; set; }
        public string UserName { get; set; }

    }
}
