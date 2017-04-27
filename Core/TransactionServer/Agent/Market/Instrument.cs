using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Framework;
using System.Diagnostics;
using Protocal.TradingInstrument;

namespace Core.TransactionServer.Agent.Market
{
    public sealed class Instrument
    {
        private QuotationBulk _bulk;

        public Instrument(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; private set; }

        internal QuotationBulk Quotation
        {
            get { return _bulk; }
        }

        public void UpdateQuotation(QuotationBulk bulk)
        {
            _bulk = bulk;
        }

        internal Quotation GetQuotation(IQuotePolicyProvider provider)
        {
            if (_bulk == null) return null;
            Quotation result;
            _bulk.TryGetQuotation(this.Id, provider, out result);
            return result;
        }

    }


    internal static class InstrumentStatusHelper
    {
        internal static bool AllowPlacing(this  Protocal.TradingInstrument.InstrumentStatus status)
        {
            return status == Protocal.TradingInstrument.InstrumentStatus.SessionOpen;
        }

        internal static bool IsTradeDayClose(this Protocal.TradingInstrument.InstrumentStatus status)
        {
            return status == Protocal.TradingInstrument.InstrumentStatus.DayClose;
        }
    }

}