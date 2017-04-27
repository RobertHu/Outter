using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class TradeDayInfo
    {
        private IEnumerable<Order> _resetOrders;
        private IEnumerable<OrderRelation> _resetOrderRelations;
        private InstrumentTradeDaySetting _instrumentTradeDaySetting;
        private Setting _setting;

        internal TradeDayInfo(Agent.Account account, Guid instrumentId, DateTime tradeDay, InstrumentTradeDaySetting instrumentTradeDaySetting, List<Guid> affectedOrders, Setting setting)
        {
            _setting = setting;
            _instrumentTradeDaySetting = instrumentTradeDaySetting;
            this.Account = _setting.GetAccount(account.Id, tradeDay);
            this.Instrument = _setting.GetInstrument(instrumentId, tradeDay);
            _resetOrders = account.GetResetOrders(instrumentId);
            _resetOrderRelations = account.GetResetOrderRelations(instrumentId);
            this.TradeDay = tradeDay;
            _instrumentTradeDaySetting.UseCompatibleMode = _instrumentTradeDaySetting.ValueDate == null && this.Instrument.PLValueDay < 1 ? true : false;
            this.RateSetting = new RateSetting(this.Account.IsMultiCurrency, this.Account.CurrencyId, this.Instrument.CurrencyId, tradeDay, setting);
            this.AffectedOrders = affectedOrders;
        }

        internal DateTime TradeDay { get; private set; }

        internal DateTime TradeDayBeginTime
        {
            get { return _setting.GetTradeDay(this.TradeDay).BeginTime; }
        }

        internal DateTime TradeDayEndTime
        {
            get { return _setting.GetTradeDay(this.TradeDay).EndTime; }
        }

        internal Instrument Instrument { get; private set; }

        internal Settings.Account Account { get; private set; }

        internal IEnumerable<Order> Orders
        {
            get { return _resetOrders; }
        }

        internal IEnumerable<Order> OpenOrders
        {
            get
            {
                foreach (var eachOrder in _resetOrders)
                {
                    if (eachOrder.IsOpen && eachOrder.Phase == OrderPhase.Executed && eachOrder.Owner.ExecuteTime <= _instrumentTradeDaySetting.ResetTime && eachOrder.LotBalance > 0)
                    {
                        yield return eachOrder;
                    }
                }
            }
        }

        internal IEnumerable<Order> CloseOrders
        {
            get
            {
                foreach (var eachOrder in _resetOrders)
                {
                    if (!eachOrder.IsOpen && eachOrder.Phase == OrderPhase.Executed && eachOrder.Owner.ExecuteTime <= _instrumentTradeDaySetting.ResetTime)
                    {
                        yield return eachOrder;
                    }
                }

            }
        }

        internal IEnumerable<Order> OrdersAllExecute
        {
            get
            {
                foreach (var eachOrder in _resetOrders)
                {
                    if (eachOrder.Phase == OrderPhase.Executed || eachOrder.Phase == OrderPhase.Completed)
                    {
                        yield return eachOrder;
                    }
                }
            }
        }

        internal IEnumerable<OrderRelation> OrderRelations { get { return _resetOrderRelations; } }

        internal InstrumentTradeDaySetting Settings { get { return _instrumentTradeDaySetting; } }

        internal RateSetting RateSetting { get; private set; }

        internal List<Guid> AffectedOrders { get; private set; }

        internal void UpdateInstrumentDayClosePrice(Price buyPrice, Price sellPrice)
        {
            this.Settings.UpdateInstrumentDayClosePrice(buyPrice, sellPrice);
        }

        internal Order GetOrder(Guid orderId)
        {
            foreach (var eachOrder in _resetOrders)
            {
                if (eachOrder.Id == orderId)
                {
                    return eachOrder;
                }
            }
            return null;
        }


    }

    internal sealed class RateSetting
    {
        internal RateSetting(bool isMultiCurrency, Guid accountCurrencyId, Guid instrumentCurrencyId, DateTime tradeDay, Setting setting)
        {
            var accountCurrency = setting.GetCurrency(accountCurrencyId, tradeDay);
            var instrumentCurrency = setting.GetCurrency(instrumentCurrencyId, tradeDay);
            int decimals;
            if (isMultiCurrency)
            {
                this.RateIn = 1;
                this.RateOut = 1;
                decimals = instrumentCurrency.Decimals;
                this.CurrencyId = instrumentCurrency.Id;
            }
            else
            {
                var interestRate = setting.GetCurrencyRate(instrumentCurrencyId, accountCurrencyId, tradeDay);
                this.RateIn = interestRate.RateIn;
                this.RateOut = interestRate.RateOut;
                decimals = accountCurrency.Decimals;
                this.CurrencyId = accountCurrency.Id;
            }
            this.RoundDecimals = new RoundDecimals(4, instrumentCurrency.Decimals, accountCurrency.Decimals, decimals);
        }

        internal decimal RateIn { get; private set; }
        internal decimal RateOut { get; private set; }
        internal Guid CurrencyId { get; private set; }
        internal RoundDecimals RoundDecimals { get; private set; }
    }

    internal sealed class RoundDecimals
    {
        internal RoundDecimals(int max, int instrument, int account, int common)
        {
            this.Max = max;
            this.Instrument = instrument;
            this.Account = account;
            this.Common = common;
        }

        internal int Max { get; private set; }
        internal int Instrument { get; private set; }
        internal int Account { get; private set; }
        internal int Common { get; private set; }
    }
}
