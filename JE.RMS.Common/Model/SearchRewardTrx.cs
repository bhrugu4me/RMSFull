using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class SearchRewardTrx
    {
        public long TotalRows { get; set; }

        public List<RewardTrx> RewardTransactions { get; set; }
    }

    public class RewardTrxTemp
    {
        public long TotalRows { get; set; }
        public long RowNum { get; set; }
        public long RewardTrxID { get; set; }
        public string RMSRewardID { get; set; }
        public string RequestID { get; set; }
        public int TransactionTypeID { get; set; }
        public int? SourceSystemID { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public int? CustomerID { get; set; }
        public int? ProductID { get; set; }
        public string ProductCode { get; set; }
        public double? ProductValue { get; set; }
        public string ProgramCode { get; set; }
        public string ProgramName { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string AdditionalData { get; set; }
        public int? FulfillmentChannelID { get; set; }
        public string SourceIP { get; set; }
        public DateTime? RewardsRequestReceiveTimestamp { get; set; }
        public DateTime? EvaluateFulfillmentRuleTimeStamp { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? LastModifiedBy { get; set; }
        public bool? IsActive { get; set; }
        public string TransactionTypeName { get; set; }
        public string SourceSystemName { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string ProductName { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string StatusName { get; set; }
        public string RewardType { get; set; }
        public DateTime? FulfillmentResponseTimestamp { get; set; }
    }

    public class RewardTrx
    {
        public long RowNum { get; set; }
        public long RewardTrxID { get; set; }
        public string RMSRewardID { get; set; }
        public string RequestID { get; set; }
        public int TransactionTypeID { get; set; }
        public int? SourceSystemID { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public int? CustomerID { get; set; }
        public int? ProductID { get; set; }
        public string ProductCode { get; set; }
        public double? ProductValue { get; set; }
        public string ProgramCode { get; set; }
        public string ProgramName { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string AdditionalData { get; set; }
        public int? FulfillmentChannelID { get; set; }
        public string SourceIP { get; set; }
        public DateTime? RewardsRequestReceiveTimestamp { get; set; }
        public DateTime? EvaluateFulfillmentRuleTimeStamp { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedBy { get; set; }
        public int? LastModifiedBy { get; set; }
        public bool? IsActive { get; set; }
        public string TransactionTypeName { get; set; }
        public string SourceSystemName { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string ProductName { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string StatusName { get; set; }
        public string RewardType { get; set; }
        public DateTime? FulfillmentResponseTimestamp { get; set; }
        [JsonIgnore]
        public long TotalRows { get; set; }
    }

    public class SearchRewardTrxRequest
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Email { get; set; }
        public string SourceSystemIDs { get; set; }
        public string CountryIDs { get; set; }
        public string JurisdictionIDs { get; set; }
        public string TransactionTypeIDs { get; set; }
        public string TransactionStatusIDs { get; set; }
        public string SourceSystemNames { get; set; }
        public string TransactionTypes { get; set; }
        public string TransactionStatuses { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string SourceSystemUniqueIDTypes { get; set; }
        public string PageNumber { get; set; }
        public string PageSize { get; set; }        
    }
}
