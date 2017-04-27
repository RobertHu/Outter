using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Data;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    internal class DayQuotation
    {
        private Instrument instrument;
        private DateTime tradeDay;
        private Price ask;
        private Price bid;

        internal DayQuotation(IDBRow  dayQuotationRow, Instrument instrument)
        {
            this.instrument = instrument;
            this.tradeDay = (DateTime)dayQuotationRow["TradeDay"];
            this.ask = Price.CreateInstance((string)dayQuotationRow["Ask"], instrument.NumeratorUnit, instrument.Denominator);
            this.bid = Price.CreateInstance((string)dayQuotationRow["Bid"], instrument.NumeratorUnit, instrument.Denominator);
        }

        internal DateTime TradeDay
        {
            get { return this.tradeDay; }
        }
        internal Price Ask
        {
            get { return this.ask; }
        }
        internal Price Bid
        {
            get { return this.bid; }
        }

        /// <summary>
        /// NOTE: Buy and Sell is on the side of company
        /// </summary>
        internal Price Buy
        {
            get { return (this.instrument.IsNormal ? this.bid : this.ask); }
        }

        internal Price Sell
        {
            get { return (this.instrument.IsNormal ? this.ask : this.bid); }
        }
    }
}