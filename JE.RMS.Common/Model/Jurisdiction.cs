using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class Jurisdiction
    {
        public int JurisdictionID { get; set; }
        public int CountryID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public bool IsActive { get; set; }
    }
}
