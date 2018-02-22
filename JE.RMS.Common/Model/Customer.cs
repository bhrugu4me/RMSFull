using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class Customer
    {
        public int RowNum { get; set; }
        public int CustomerID { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string ZipPostalCode { get; set; }
        public string Phone1 { get; set; }
        public string Product { get; set; }
        public string Language { get; set; }
        public string Country { get; set; }
        public int CreatedBy { get; set; } = 3;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public string CustomerStatus { get; set; }
        public int JurisdictionID { get; set; }
        [JsonIgnore]
        public long TotalRows { get; set; }
    }
    public class CustomerJSON
    {
        public int CustomerID { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string ZipPostalCode { get; set; }
        public string Phone1 { get; set; }
        public string Product { get; set; }
        public string Language { get; set; }
    }

    public class EECustomerList
    {
        public List<EECustomerModel> Users { get; set; }
    }

    public class CustomerList
    {
        public long TotalRows { get; set; }
        public List<Customer> Users { get; set; }
    }
    public class EECustomerModel : CustomerJSON
    {
        public int FulfillmentChannelID { get; set; } = 1;
        public string UserID { get; set; }
        public DateTime? AccountAcceptanceDate { get; set; }
        public string StartingPointBalance { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string AvailablePointBalance { get; set; }
        public string AvailablePointBalanceDollars { get; set; }
        public string NumberofTransactions { get; set; }
        public string AccountStatus { get; set; }
        public string NextRewardDueDate { get; set; }
        public string Program { get; set; }
    }
}
