using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class SourceSystem
    {
        public int SourceSystemID { get; set; }
        public string sourceSystemShortName { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public bool IsActive { get; set; }
    }
}
