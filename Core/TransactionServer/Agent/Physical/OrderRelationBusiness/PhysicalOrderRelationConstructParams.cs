using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderRelationBusiness
{
    public sealed class PhysicalOrderRelationConstructParams : OrderRelationConstructParams
    {
        internal decimal PhysicalValue { get; set; }
        internal decimal OverdueCutPenalty { get; set; }
        internal decimal ClosePenalty { get; set; }
        internal decimal PayBackPledge { get; set; }
        internal decimal ClosedPhysicalValue { get; set; }

        internal DateTime? PhysicalValueMatureDay { get; set; }
        internal DateTime? RealPhysicalValueMatureDate { get; set; }

    }
}
