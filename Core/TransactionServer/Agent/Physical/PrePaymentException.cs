using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical
{
    internal sealed class PrePaymentException : Exception
    {
        internal PrePaymentException(Guid instalmentPolicyId, int period, string msg = "")
            : base(msg)
        {
            this.InstalmentPolicyId = instalmentPolicyId;
            this.Period = period;
        }

        internal Guid InstalmentPolicyId { get; private set; }

        internal int Period { get; private set; }
    }

    internal sealed class InstalmentInfoNotFoundException : Exception
    {
        internal InstalmentInfoNotFoundException(Guid orderId, PhysicalType physicalType)
        {
            this.OrderId = orderId;
            this.PhysicalType = physicalType;
        }

        internal Guid OrderId { get; private set; }
        internal PhysicalType PhysicalType { get; private set; }
    }

    internal sealed class PhysicalOrderNotFoundException : Exception
    {
        internal PhysicalOrderNotFoundException(Guid orderId)
        {
            this.OrderId = orderId;
        }

        internal Guid OrderId { get; private set; }
    }

}
