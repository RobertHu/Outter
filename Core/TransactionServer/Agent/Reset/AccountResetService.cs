using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.Reset.Exceptions;
using Core.TransactionServer.Agent.Test.Reset;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using iExchange.Common;
using iExchange.Common.Caching.Transaction;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class AccountResetService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountResetService));
        private Account _account;
        private InstrumentManager _instrumentManager;
        private SystemResetter _systemResetter;
        private InstrumentResetter _instrumentResetter;

        internal AccountResetService(Account account, InstrumentManager instrumentManager)
        {
            _account = account;
            _instrumentManager = instrumentManager;
            _systemResetter = new SystemResetter(account, instrumentManager);
            _instrumentResetter = new InstrumentResetter(account, instrumentManager);
        }

        internal DateTime? LastResetDay
        {
            get { return _systemResetter.LastResetDay; }
            set
            {
                _systemResetter.LastResetDay = value;
            }
        }

        internal DateTime? GetInstrumentLastResetDay(Guid instrumentId)
        {
            return _instrumentManager.GetLastResetDay(instrumentId);
        }


        internal IEnumerable<Order> GetOrders(Guid instrumentId)
        {
            foreach (var eachTran in _account.Transactions)
            {
                if (eachTran.InstrumentId != instrumentId) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    yield return eachOrder;
                }
            }
        }

        internal IEnumerable<OrderRelation> GetOrderRelations(Guid instrumentId)
        {
            foreach (var eachTran in _account.Transactions)
            {
                if (eachTran.InstrumentId != instrumentId) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsOpen) continue;
                    foreach (var eachOrderRelation in eachOrder.OrderRelations)
                    {
                        yield return eachOrderRelation;
                    }
                }
            }
        }


        internal void DoSystemReset(DateTime tradeDay)
        {
            _systemResetter.DoSystemReset(tradeDay);
        }


        internal void DoInstrumentReset(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            _instrumentResetter.DoInstrumentReset(instrumentId, tradeDay, closeQuotations);
        }

        internal void DoInstrumentReset(DateTime tradeDay)
        {
            _instrumentResetter.DoInstrumentReset(tradeDay);
        }

        internal bool IsInstrumentsReseted(DateTime tradeDay)
        {
            if (_account.InstrumentCount == 0) return true;
            foreach (var eachInstrument in _instrumentManager.Instruments)
            {
                if (eachInstrument.LastResetDay != null && eachInstrument.LastResetDay.Value < tradeDay)
                {
                    return false;
                }
            }
            return true;
        }

        internal DateTime? GetPositionDay()
        {
            DateTime lastTradeDay = DateTime.MaxValue;
            foreach (var eachInstrument in _instrumentManager.Instruments)
            {
                var positionDay = eachInstrument.LastPositionDay ?? DateTime.MaxValue;
                if (positionDay < lastTradeDay)
                {
                    lastTradeDay = positionDay;
                }
            }
            return lastTradeDay == DateTime.MaxValue ? (DateTime?)null : lastTradeDay;
        }

    }

    internal struct TradeDayPrice
    {
        private Price _buyPrice;
        private Price _sellPrice;

        internal TradeDayPrice(Price ask, Price bid, Settings.Instrument instrument)
        {
            _buyPrice = bid;
            _sellPrice = ask;
        }

        internal Price BuyPrice
        {
            get { return _buyPrice; }
        }

        internal Price SellPrice
        {
            get { return _sellPrice; }
        }

    }
}
