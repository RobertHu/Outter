using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderInstalment
    {
        internal OrderInstalment(DataRow dr)
        {
            this.OrderId = dr.GetColumn<Guid>("OrderId");
            this.Sequence = dr.GetColumn<int>("Sequence");
            this.InterestRate = dr.GetColumn<decimal?>("InterestRate");
            this.Principal = dr.GetColumn<decimal>("Principal");
            this.Interest = dr.GetColumn<decimal>("Interest");
            this.DebitInterest = dr.GetColumn<decimal>("DebitInterest");
            this.PaymentDateTimeOnPlan = dr.GetColumn<DateTime?>("PaymentDateTimeOnPlan");
            this.PaidDateTime = dr.GetColumn<DateTime?>("PaidDateTime");
            this.UpdatePersonId = dr.GetColumn<Guid?>("UpdatePersonId");
            this.UpdateTime = dr.GetColumn<DateTime?>("UpdateTime");
        }

        internal Guid OrderId { get; private set; }

        internal int Sequence { get; private set; }

        internal decimal? InterestRate { get; private set; }

        internal decimal Principal { get; private set; }

        internal decimal Interest { get; private set; }

        internal decimal DebitInterest { get; private set; }

        internal DateTime? PaymentDateTimeOnPlan { get; private set; }

        internal DateTime? PaidDateTime { get; private set; }

        internal Guid? UpdatePersonId { get; private set; }

        internal DateTime? UpdateTime { get; private set; }
    }
}
