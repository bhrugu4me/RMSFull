using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class TransactionType
    {
        public int TransactionTypeID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
