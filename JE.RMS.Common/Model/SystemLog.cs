using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class SystemLog
    {
        public long LogID { get; set; }

        public string Message { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
