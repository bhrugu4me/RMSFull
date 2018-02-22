using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class EEOrder
    {
        public string BatchID { get; set; }
        public bool OverrideValidation { get; set; }
        public List<EEProgram> Programs { get; set; }
    }

    public class EEProgram
    {
        public string ProgramName { get; set; }
        public List<Recipient> Recipients { get; set; }
    }

    public class Recipient
    {
        public string Identifier { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Address1 { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Address2 { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string City { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StateProvinceCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string PostalCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Phone { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double Value { get; set; }
    }
}
