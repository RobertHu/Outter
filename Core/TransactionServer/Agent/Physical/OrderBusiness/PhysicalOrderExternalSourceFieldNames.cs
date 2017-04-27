using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal static class PhysicalOrderExternalSourceFieldNames
    {
        internal const string DeliveryLockLot = "DeliveryLockLot";
        internal const string PhysicalRequestId = "PhysicalRequestId";
        internal const string OverdueCutPenalty = "OverdueCutPenalty";
        internal const string ClosePenalty = "ClosePenalty";
        internal const string FrozenFund = "FrozenFund";
        internal const string PayBackPledge = "PayBackPledge";
        internal const string PhysicalOriginValue = "PhysicalOriginValue";
        internal const string PhysicalOriginValueBalance = "PhysicalOriginValueBalance";
        internal const string PaidPledge = "PaidPledge";
        internal const string PaidPledgeBalance = "PaidPledgeBalance";
        internal const string PhysicalTradeSide = "PhysicalTradeSide";
        internal const string PhysicalValueMatureDay = "PhysicalValueMatureDay";
        internal const string InstalmentPolicyId = "InstalmentPolicyId";
        internal const string DownPayment = "DownPayment";
        internal const string PhysicalInstalmentType = "PhysicalInstalmentType";
        internal const string RecalculateRateType = "RecalculateRateType";
        internal const string Period = "Period";
        internal const string InstalmentFrequence = "InstalmentFrequence";
        internal const string IsInstalmentOverdue = "IsInstalmentOverdue";
        internal const string InstalmentOverdueDay = "InstalmentOverdueDay";
    }
}
