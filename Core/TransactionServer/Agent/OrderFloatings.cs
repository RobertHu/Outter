using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.Quotations;

namespace Core.TransactionServer.Agent
{

    [Flags]
    public enum ChangedItem
    {
        None = 0,
        Quotation = (1 << 0),
        CurrecnyRate = (1 << 1),
        MarginFormula = (1 << 2),
        TradePLFormula = (1 << 3),
        LotBalance = (1 << 4),
        IsPayoff = (1 << 5),
        DiscountOfOdd = (1 << 6),
        ValueDiscountAsMargin = (1 << 7),
        All = ~(-1 << 8)
    }


    internal static class ChangedItemHelper
    {
        internal static bool Include(this ChangedItem source, ChangedItem item)
        {
            return (source & item) != 0;
        }
    }



    internal class CalculateParams
    {
        protected Order _owner;
        private Price _ask;
        private Price _bid;
        private MarginFormula _marginFormula;
        private TradePLFormula _tradePLFormula;
        private decimal _lotBlance;
        private decimal _currencyRateIn;
        private decimal _currencyRateOut;

        internal CalculateParams(Order order)
        {
            this._owner = order;
            this.IsCalculated = false;
        }

        internal MarginFormula MarginFormula
        {
            get { return this._marginFormula; }
        }

        internal bool IsCalculated { get; set; }
        public ChangedItem ChangedItem { get; protected set; }

        internal virtual ChangedItem Update(Quotation qutation)
        {
            ChangedItem changedItems = ChangedItem.None;
            if (_ask != qutation.Ask || _bid != qutation.Bid) changedItems |= ChangedItem.Quotation;

            var currencyRate = _owner.Owner.CurrencyRate(null);
            var settingInstrument = _owner.Owner.SettingInstrument();

            if (_currencyRateIn != currencyRate.RateIn || _currencyRateOut != currencyRate.RateOut) changedItems |= ChangedItem.CurrecnyRate;
            if (_tradePLFormula != settingInstrument.TradePLFormula) changedItems |= ChangedItem.TradePLFormula;
            if (_marginFormula != settingInstrument.MarginFormula) changedItems |= ChangedItem.MarginFormula;
            if (_lotBlance != _owner.LotBalance) changedItems |= ChangedItem.LotBalance;
            _ask = qutation.Ask;
            _bid = qutation.Bid;
            _marginFormula = settingInstrument.MarginFormula;
            _tradePLFormula = settingInstrument.TradePLFormula;
            _lotBlance = _owner.LotBalance;
            _currencyRateIn = currencyRate.RateIn;
            _currencyRateOut = currencyRate.RateOut;
            this.ChangedItem = changedItems;
            return changedItems;
        }
    }
}