using System;
using System.Collections.Generic;

namespace JE.RMS.Common.Model
{
    public class EEUser
    {
        public Guid UserID { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid? ClientID { get; set; }
        public string Program { get; set; }
        public string CompanyName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string StateProvinceCode { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string LanguageCode { get; set; }
    }

    public class EEUsers
    {
        public List<EEUser> Users { get; set; }
    }
}
