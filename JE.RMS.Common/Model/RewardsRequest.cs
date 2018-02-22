using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Model
{
    public class RewardsRequest
    {
        public string RequestId { get; set; }
        public string TransactionType { get; set; }
        public CustomerJSON Customer { get; set; }
        public Reward Reward { get; set; }
        public AdditionalData[] AdditionalData { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
        public string SourceIP { get; set; }
        public string RewardsRequestReceiveTimestamp { get; set; }
        public string EvaluateFulfillmentRuleTimestamp { get; set; }
        public string RMSRewardID { get; set; }
        public bool IsOrder { get; set; } = false;
    }

    public class RewardFulfillmentRequest
    {
        public string RequestId { get; set; }
        public string TransactionType { get; set; }
        public CustomerJSON Customer { get; set; }
        public Reward Reward { get; set; }
        public AdditionalData[] AdditionalData { get; set; }
        public string RMSRewardID { get; set; }
    }

    public class GetRewardsRequest
    {
        public string DBRequestID { get; set; }
        public long RewardTrxID { get; set; }
        public string RequestId { get; set; }
        public string TransactionType { get; set; }
        public int TransactionTypeID { get; set; }
        public string AdditionalData { get; set; }
        public string SourceIP { get; set; }
        public DateTime RewardsRequestReceiveTimestamp { get; set; }
        public DateTime EvaluateFulfillmentRuleTimestamp { get; set; }
        public string RMSRewardID { get; set; }


        public int ProductID { get; set; }
        public int ProgramID { get; set; }
        public string ProductCode { get; set; }
        public decimal ProductValue { get; set; }
        public string ProgramName { get; set; }
        public string RewardType { get; set; }
        public DateTime? EffectiveDate { get; set; }

        public int CustomerID { get; set; }
        public int FulfillmentChannelID { get; set; }
        public int DBSourceSystemID { get; set; }
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
        public string LanguageCode { get; set; }
        public string SourceSystemShortName { get; set; }
        public string RewardTrxStatus { get; set; }

        public int CreatedBy { get; set; } = 3;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool IsFulfillImmediate { get; set; } = false;
    }
    public class AdditionalData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class AdditionalDataArray
    {
        public AdditionalData[] AdditionalDataArrayData { get; set; }
    }

    public class rewardList
    {
        public List<RewardsRequest> RewardsRequest { get; set; }
        public string SourceIP { get; set; }
        public string RewardsRequestReceiveTimestamp { get; set; }
        public string EvaluateFulfillmentRuleTimestamp { get; set; }
        public string RMSRewardID { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

    public class RewardFulfillmentRequestList
    {
        public RewardFulfillmentRequest RewardFulfillmentRequest { get; set; }
    }

    public class RewardFulfillmentResponseList
    {
        public List<RewardFulfillmentRequest> RewardFulfillmentRequest { get; set; }
        public bool HasMoreMessages { get; set; }
        public long TotalRecord { get; set; }
    }

    public class GetRewardsRequestList
    {
        public List<GetRewardsRequest> GetRewardsRequest { get; set; }
    }
}
