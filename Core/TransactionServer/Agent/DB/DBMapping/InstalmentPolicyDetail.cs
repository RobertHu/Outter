using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class InstalmentPolicyDetail
    {
        internal Guid InstalmentPolicyId { get; set; }

        internal bool IsActive { get; set; }

        internal int Period { get; set; }

        internal int Frequence { get; set; }

        internal decimal InterestRate { get; set; }

        internal int AdministrationFeeBase { get; set; }

        internal decimal AdministrationFee { get; set; }

        internal int ContractTerminateType { get; set; }

        internal decimal ContractTerminateFee { get; set; }

        internal int DownPaymentBasis { get; set; }

        internal int LatePaymentAutoCutDay { get; set; }

        internal int AutoCutPenaltyBase { get; set; }

        internal decimal AutoCutPenaltyValue { get; set; }

        internal int ClosePenaltyBase { get; set; }

        internal decimal ClosePenaltyValue { get; set; }

        internal decimal DebitInterestRatio { get; set; }

        internal int DebitFreeDays { get; set; }

        internal int DebitInterestType { get; set; }
    }
}
