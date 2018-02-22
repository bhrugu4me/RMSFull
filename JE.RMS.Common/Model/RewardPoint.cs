using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class RewardPoint
    {
        /// <summary>
        /// Amount in Dollars
        /// </summary>
        public decimal Amount { get; set; }
        public int PointAmount { get; set; }
        public string Description { get; set; }
    }

    public class RewardPointForUser : RewardPoint
    {
        public string UserID { get; set; }
    }
}
