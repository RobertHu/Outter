using Core.TransactionServer.Agent.DB.DBMapping;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class OpenOrderCurrentDayPLCalculator
    {
        internal static Dictionary<Guid, OpenOrderPLOfCurrentDay> Calculate(TradeDayInfo tradeDayInfo, Dictionary<Guid, TradeDayCommonResult> resetCommonResultDict, Exchanger exchanger)
        {
            if (tradeDayInfo.Settings.ValueDate == null && !tradeDayInfo.Settings.UseCompatibleMode) return null;
            Dictionary<Guid, OpenOrderPLOfCurrentDay> result = new Dictionary<Guid, OpenOrderPLOfCurrentDay>();
            foreach (var eachPair in resetCommonResultDict)
            {
                var orderId = eachPair.Key;
                var commonResetResult = eachPair.Value;
                if (!orderId.ShouldCalculate(tradeDayInfo.AffectedOrders)) continue;
                var eachOrder = tradeDayInfo.GetOrder(orderId);
                if (!IsOpenOrderOfCurrentDay(eachOrder, tradeDayInfo)) continue;
                OpenOrderPLOfCurrentDay openOrderPLOfCurrentDay = new OpenOrderPLOfCurrentDay();
                int decimals = tradeDayInfo.Settings.IsInterestUseAccountCurrency ? tradeDayInfo.RateSetting.RoundDecimals.Max : tradeDayInfo.RateSetting.RoundDecimals.Instrument;
                var storage = Math.Round(eachOrder.LotBalance * commonResetResult.StoragePerLot, decimals, MidpointRounding.AwayFromZero);
                var interest = Math.Round(eachOrder.LotBalance * commonResetResult.InterestPerLot, decimals, MidpointRounding.AwayFromZero);
                openOrderPLOfCurrentDay.DayNotValued += tradeDayInfo.Settings.ShouldValueCurrentDayPL ? InterestStorage.Empty : new InterestStorage(interest, storage);

                var exchangeStorage = exchanger.ExchangeByCommonDecimals(storage);
                var exchangeInterest = exchanger.ExchangeByCommonDecimals(interest);
                openOrderPLOfCurrentDay.NotValued += tradeDayInfo.Settings.ShouldValueCurrentDayPL ? InterestStorage.Empty : new InterestStorage(exchangeInterest, exchangeStorage);

                bool isInterestValued = (eachOrder.InterestValueDate ?? tradeDayInfo.TradeDay) <= tradeDayInfo.TradeDay;
                decimal interestPLValued = tradeDayInfo.Settings.ShouldValueCurrentDayPL && isInterestValued ? exchangeInterest : 0;
                decimal storagePLValued = tradeDayInfo.Settings.ShouldValueCurrentDayPL ? exchangeStorage : 0;
                openOrderPLOfCurrentDay.Valued += new InterestStorage(interestPLValued, storagePLValued);

                openOrderPLOfCurrentDay.IsInterestValued = isInterestValued;
                openOrderPLOfCurrentDay.IsStorageValued = true;
                result.Add(orderId, openOrderPLOfCurrentDay);
            }
            return result;
        }

        private static bool IsOpenOrderOfCurrentDay(Order order, TradeDayInfo tradeDayInfo)
        {
            var tradePolicyDetail = Settings.Setting.Default.GetTradePolicy(tradeDayInfo.Account.TradePolicyId, tradeDayInfo.TradeDay)[tradeDayInfo.Instrument.Id, null];
            return order.IsOpen && (tradePolicyDetail.InterestCut == tradeDayInfo.Settings.WeekDay || tradePolicyDetail.InterestCut == 11 ||
                   (tradePolicyDetail.InterestCut == 12 && tradeDayInfo.Settings.IsMonthLastDay));
        }

    }


    internal sealed class NotValuedOpenOrderPLCalculator : OpenOrderPLCalculatorBase
    {
        internal static readonly NotValuedOpenOrderPLCalculator Default = new NotValuedOpenOrderPLCalculator();

        static NotValuedOpenOrderPLCalculator() { }
        private NotValuedOpenOrderPLCalculator() { }

        protected override bool ShouldCalculate(Guid instrumentId, DateTime tradeDay)
        {
            return true;
        }

        internal override bool ShouldAddInstrumentDayOpenCloseHistory(InstrumentDayOpenCloseHistory item, DateTime tradeDay)
        {
            return item.RealValueDate == null || item.RealValueDate.Value > tradeDay;
        }
    }


    internal sealed class ValuedOpenOrderPLCalculator : OpenOrderPLCalculatorBase
    {
        internal static readonly ValuedOpenOrderPLCalculator Default = new ValuedOpenOrderPLCalculator();

        static ValuedOpenOrderPLCalculator() { }
        private ValuedOpenOrderPLCalculator() { }

        protected override bool ShouldAddOrderDayHistoryToPL(Order order, OrderDayHistory orderDayHistory, InstrumentDayOpenCloseHistory instrumentDayOpenCloseHistory, DateTime tradeDay)
        {
            return base.ShouldAddOrderDayHistoryToPL(order, orderDayHistory, instrumentDayOpenCloseHistory, tradeDay) && orderDayHistory.TradeDay < tradeDay;
        }

        internal override bool ShouldAddInstrumentDayOpenCloseHistory(InstrumentDayOpenCloseHistory item, DateTime tradeDay)
        {
            return item.RealValueDate == tradeDay;
        }

        protected override bool ShouldCalculate(Guid instrumentId, DateTime tradeDay)
        {
            return ResetManager.Default.ExistsInstrumentTradeDay(instrumentId, tradeDay);
        }
    }



    internal abstract class OpenOrderPLCalculatorBase
    {
        internal Dictionary<Guid, InterestStorage> Calculate(Guid accountId, Guid instrumentId, IEnumerable<Order> allExecuteOrders, DateTime tradeDay, Exchanger exchanger, List<Guid> affectedOrders)
        {
            if (!this.ShouldCalculate(instrumentId, tradeDay)) return null;
            Dictionary<Guid, InterestStorage> result = new Dictionary<Guid, InterestStorage>();
            var instrumentOpenCloseHistorys = this.GetInstrumentDayOpenCloseHistorys(instrumentId, tradeDay);
            var orderKeys = GetOrderKeys(allExecuteOrders, instrumentOpenCloseHistorys);
            var historyOrders = ResetManager.Default.GetOrderDayHistorys(orderKeys);
            if (historyOrders == null || historyOrders.Count == 0) return result;
            foreach (var eachOrder in allExecuteOrders)
            {
                if (!eachOrder.IsOpen || !eachOrder.ShouldCalculate(affectedOrders)) continue;
                foreach (var eachInstrument in instrumentOpenCloseHistorys)
                {
                    foreach (var eachOrderDayHistory in historyOrders)
                    {
                        if (this.ShouldAddOrderDayHistoryToPL(eachOrder, eachOrderDayHistory, eachInstrument, tradeDay))
                        {
                            CalculatePL(eachOrderDayHistory, result, exchanger);
                        }
                    }
                }
            }
            return result;
        }

        private void CalculatePL(OrderDayHistory orderDayHistory, Dictionary<Guid, InterestStorage> dict, Exchanger exchanger)
        {
            var interestPL = exchanger.ExchangeByCommonDecimals(orderDayHistory.DayInterestPLNotValued);
            var storagePL = exchanger.ExchangeByCommonDecimals(orderDayHistory.DayStoragePLNotValued);
            InterestStorage interestStoragePL;
            if (!dict.TryGetValue(orderDayHistory.OrderID, out interestStoragePL))
            {
                dict.Add(orderDayHistory.OrderID, new InterestStorage(interestPL, storagePL));
            }
            else
            {
                dict[orderDayHistory.OrderID] = new InterestStorage(interestStoragePL.Interest + interestPL, interestStoragePL.Storage + storagePL);
            }
        }


        protected abstract bool ShouldCalculate(Guid instrumentId, DateTime tradeDay);


        protected virtual bool ShouldAddOrderDayHistoryToPL(Order order, OrderDayHistory orderDayHistory, InstrumentDayOpenCloseHistory instrumentDayOpenCloseHistory, DateTime tradeDay)
        {
            return order.Id == orderDayHistory.OrderID && orderDayHistory.TradeDay == instrumentDayOpenCloseHistory.TradeDay && orderDayHistory.LotBalance > 0;
        }

        internal abstract bool ShouldAddInstrumentDayOpenCloseHistory(InstrumentDayOpenCloseHistory item, DateTime tradeDay);

        protected List<InstrumentDayOpenCloseHistory> GetInstrumentDayOpenCloseHistorys(Guid instrumentId, DateTime tradeDay)
        {
            var instrumentDayOpenCloseHistorys = ResetManager.Default.GetInstrumentDayOpenCloseHistory(instrumentId);
            var result = new List<InstrumentDayOpenCloseHistory>();
            if (instrumentDayOpenCloseHistorys == null) return result;
            foreach (var eachInstrument in instrumentDayOpenCloseHistorys)
            {
                if (this.ShouldAddInstrumentDayOpenCloseHistory(eachInstrument, tradeDay))
                {
                    result.Add(eachInstrument);
                }
            }
            return result;
        }

        protected List<KeyValuePair<Guid, DateTime?>> GetOrderKeys(IEnumerable<Order> orders, List<InstrumentDayOpenCloseHistory> instrumentOpenCloseHistorys)
        {
            List<KeyValuePair<Guid, DateTime?>> result = new List<KeyValuePair<Guid, DateTime?>>(orders.Count() * instrumentOpenCloseHistorys.Count);
            foreach (var eachOrder in orders)
            {
                foreach (var eachInstrument in instrumentOpenCloseHistorys)
                {
                    result.Add(new KeyValuePair<Guid, DateTime?>(eachOrder.Id, eachInstrument.TradeDay));
                }
            }
            return result;
        }
    }


}
