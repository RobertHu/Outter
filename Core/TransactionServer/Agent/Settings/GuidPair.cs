using Core.TransactionServer.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Settings
{
    public struct CurrencyIdPair : IEquatable<CurrencyIdPair>
    {
        private readonly Guid _sourceCurrencyId;
        private readonly Guid _targetCurrencyId;

        public CurrencyIdPair(Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            _sourceCurrencyId = sourceCurrencyId;
            _targetCurrencyId = targetCurrencyId;
        }

        internal Guid SourceCurrencyId { get { return _sourceCurrencyId; } }

        internal Guid TargetCurrencyId { get { return _targetCurrencyId; } }

        public override int GetHashCode()
        {
            return HashCodeGenerator.Calculate(_sourceCurrencyId.GetHashCode(), _targetCurrencyId.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return Equals((CurrencyIdPair)obj);
        }

        public bool Equals(CurrencyIdPair other)
        {
            return _sourceCurrencyId.Equals(other.SourceCurrencyId) &&
                  _targetCurrencyId.Equals(other.TargetCurrencyId);
        }
    }

    public struct QuotePolicyInstrumentIdPair : IEquatable<QuotePolicyInstrumentIdPair>
    {
        private readonly Guid _quotePolicyId;
        private readonly Guid _instrumentId;

        internal QuotePolicyInstrumentIdPair(Guid quotePolicyId, Guid instrumentId)
        {
            _quotePolicyId = quotePolicyId;
            _instrumentId = instrumentId;
        }

        internal Guid QuotePolicyId { get { return _quotePolicyId; } }

        internal Guid InstrumentId { get { return _instrumentId; } }

        public override int GetHashCode()
        {
            int result = 17;
            result = 31 * result + _quotePolicyId.GetHashCode();
            result = 31 * result + _instrumentId.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this.Equals((QuotePolicyInstrumentIdPair)obj);
        }


        public bool Equals(QuotePolicyInstrumentIdPair other)
        {
            return _quotePolicyId.Equals(other.QuotePolicyId) &&
                   _instrumentId.Equals(other.InstrumentId);
        }
    }
}
