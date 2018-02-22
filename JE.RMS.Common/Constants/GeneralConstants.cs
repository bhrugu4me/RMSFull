using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JE.RMS.Common.Constants
{
    public class GeneralConstants
    {

    }

    public enum FulfillmentChannel
    {
        [Description("EnergyEarth")]
        EnergyEarth = 1,
        [Description("GBASSTariff")]
        GBASSTariff = 2,
        [Description("GBASSCTLAdj")]
        GBASSCTLAdj = 3,
        [Description("Cheque")]
        Cheque = 4
    }

    public enum TransactionTypeEnum
    {
        [Description("Reward")]
        Reward,
        [Description("Terminate")]
        Terminate,
        [Description("Reactivate")]
        Reactivate,
        [Description("Qualify")]
        Qualify,
        [Description("ProgramUpdateSourceSystem")]
        ProgramUpdateSourceSystem,
        [Description("Unknown")]
        Unknown
    }

    public enum ProgramNames
    {
        [Description("casouthledperks")]
        casouthledperks,
        [Description("jeperkssingleload")]
        jeperkssingleload,
        [Description("jeperks")]
        jeperks,
        [Description("jeperksretention")]
        jeperksretention,
        [Description("jeledperks")]
        jeledperks,
        [Description("jeledperksretention")]
        jeledperksretention,
        [Description("jeperksonrenewal")]
        jeperksonrenewal,
        [Description("jecanadaledperks")]
        jecanadaledperks,
        [Description("jeperksonrenewal(test)")]
        jeperksonrenewaltest,
        [Description("jeperksonrenewalclone")]
        jeperksonrenewalclone,
        [Description("gseperks")]
        gseperks,
        [Description("jeeastledperks")]
        jeeastledperks,
        [Description("celedperks")]
        celedperks
    }

    public enum MessageType
    {
        [Description("Reward Request")]
        RewardRequest,
        [Description("Reward Fulfillment Request")]
        RewardFulfillmentRequest,
        [Description("Reward Fulfillment Response")]
        RewardFulfillmentResponse,
        [Description("Energy Earth Request")]
        EnergyEarthRequest
    }

    public enum RewardTrxStatusEnum
    {
        [Description("Received")]
        Received,
        [Description("Waiting for approval")]
        WaitingForApproval,
        [Description("Ready for fulfillment")]
        ReadyForFulfillment,
        [Description("Sent for fulfillment")]
        SentForFulfillment,
        [Description("Fulfillment completed")]
        FulfillmentCompleted,
        [Description("Rejected - System")]
        RejectedSystem,
        [Description("Rejected - User")]
        RejectedUser,
        [Description("Validation error")]
        ValidationError,
        [Description("Error")]
        Error,
        [Description("Canceled")]
        Canceled,
        [Description("Received - Ready for fulfillment")]
        ReceivedReadyForFulfillment,
        [Description("Ready for fulfillment - Immediate")]
        ReadyForFulfillmentImmediate
    }
    //Get String Description 
    public static class AttributesHelperExtension
    {
        public static string GetDescription(this Enum value)
        {
            var da = (DescriptionAttribute[])(value.GetType().GetField(value.ToString())).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return da.Length > 0 ? da[0].Description : value.ToString();
        }
    }
}
