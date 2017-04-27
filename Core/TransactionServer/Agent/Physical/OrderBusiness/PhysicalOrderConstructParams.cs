using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalOrderConstructParams : OrderConstructParams
    {
        internal InstalmentConstructParams Instalment { get; set; }
        internal PhysicalConstructParams PhysicalSettings { get; set; }

        protected override OrderConstructParams Create()
        {
            return new PhysicalOrderConstructParams();
        }
        internal override OrderConstructParams Copy()
        {
            var result = (PhysicalOrderConstructParams)base.Copy();
            if (this.Instalment != null)
            {
                result.Instalment = this.Instalment.Copy();
            }
            result.PhysicalSettings = this.PhysicalSettings.Copy();
            return result;
        }

    }


    internal sealed class InstalmentConstructParams
    {
        internal Guid? InstalmentPolicyId { get; set; }
        internal decimal DownPayment { get; set; }
        internal InstalmentType InstalmentType { get; set; }
        internal RecalculateRateType RecalculateRateType { get; set; }
        internal InstalmentPeriod InstalmentPeriod { get; set; }
        internal bool IsInstalmentOverdue { get; set; }
        internal int InstalmentOverdueDay { get; set; }
        internal DownPaymentBasis DownPaymentBasis { get; set; }

        internal InstalmentConstructParams Copy()
        {
            return new InstalmentConstructParams
            {
                InstalmentPolicyId = this.InstalmentPolicyId,
                DownPayment = this.DownPayment,
                InstalmentType = this.InstalmentType,
                RecalculateRateType = this.RecalculateRateType,
                InstalmentPeriod = this.InstalmentPeriod,
                IsInstalmentOverdue = this.IsInstalmentOverdue,
                InstalmentOverdueDay = this.InstalmentOverdueDay,
                DownPaymentBasis = this.DownPaymentBasis
            };
        }

    }

    internal sealed class PhysicalConstructParams
    {
        internal PhysicalTradeSide PhysicalTradeSide { get; set; }
        internal decimal PhysicalOriginValue { get; set; }
        internal decimal PhysicalOriginValueBalance { get; set; }
        internal decimal PaidPledgeBalance { get; set; }
        internal int PhysicalValueMatureDay { get; set; }
        internal Guid? PhysicalRequestId { get; set; }
        internal Protocal.Physical.PhysicalType PhysicalType { get; set; }

        internal PhysicalConstructParams Copy()
        {
            return new PhysicalConstructParams
            {
                PhysicalTradeSide = this.PhysicalTradeSide,
                PhysicalOriginValue = this.PhysicalOriginValue,
                PhysicalOriginValueBalance= this.PhysicalOriginValueBalance,
                PaidPledgeBalance = this.PaidPledgeBalance,
                PhysicalValueMatureDay = this.PhysicalValueMatureDay,
                PhysicalRequestId = this.PhysicalRequestId,
                PhysicalType = this.PhysicalType
            };
        }

    }
}
