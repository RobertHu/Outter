using Core.TransactionServer.Agent.Util;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class ResetOverridedQuotation
    {
        private int? hashCode = null;

        internal Guid QuotePolicyId { get; set; }
        internal Guid InstrumentId { get; set; }
        internal Price Origin { get; set; }
        internal Price Ask { get; set; }
        internal Price Bid { get; set; }


        public override int GetHashCode()
        {
            if (hashCode == null)
            {
                hashCode = HashCodeGenerator.Calculate(this.QuotePolicyId.GetHashCode(), this.InstrumentId.GetHashCode());
            }
            return hashCode.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            ResetOverridedQuotation other = obj as ResetOverridedQuotation;
            if (other == null) return false;
            return this.QuotePolicyId.Equals(other.QuotePolicyId) && this.InstrumentId.Equals(other.InstrumentId);
        }

    }
}
