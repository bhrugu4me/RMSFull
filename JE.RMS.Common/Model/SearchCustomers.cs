using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JE.RMS.Common.Model
{
    public class SearchCustomersList
    {
        public long TotalRows { get; set; }

        public List<SearchCustomers> Customers { get; set; }
    }

    public class SearchCustomers
    {
        public string MasterID { get; set; }
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
        public string CustomerStatus { get; set; }
        public string Jurisdiction { get; set; }

        public List<SearchCustomerExtended> CustomerExtended { get; set; }

    }

    public class SearchCustomerExtended
    {
        public string UniqueID { get; set; }
        public DateTime? AccountAcceptanceDate { get; set; }
        public string StartingPointBalance { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string AvailablePointBalance { get; set; }
        public string AvailablePointBalanceDollars { get; set; }
        public string NumberofTransactions { get; set; }
        public string AccountStatus { get; set; }
        public string NextRewardDueDate { get; set; }
    }

    public class CustomerCustomerExtendedList
    {
        public int CustomerID { get; set; }
        public string MasterID { get; set; }
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
        public string CustomerStatus { get; set; }
        public string Jurisdiction { get; set; }


        public int? CustomerExtendedID { get; set; }
        public string UniqueID { get; set; }
        public DateTime? AccountAcceptanceDate { get; set; }
        public string StartingPointBalance { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string AvailablePointBalance { get; set; }
        public string AvailablePointBalanceDollars { get; set; }
        public string NumberofTransactions { get; set; }
        public string AccountStatus { get; set; }
        public string NextRewardDueDate { get; set; }

        public long TotalRows { get; set; }
    }

    public class SearchCustomerRequest
    {
        public string Email { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemName { get; set; }
        public string PageNumber { get; set; }
        public string PageSize { get; set; }
    }
}
