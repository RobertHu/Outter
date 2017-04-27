using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class HistoryOrderFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(HistoryOrderFactory));

        internal static void Process(Transaction tran, Settings.Setting setting, DateTime tradeDay)
        {
            var account = tran.Owner;
            var historyOrderRecover = new HistoryOrderRecover(account, tran.InstrumentId, GetAffectedOrders(tran), new List<Guid>(), setting);
            DateTime beginResetTradeDay = tradeDay;
            DateTime? lastResetTradeDay = account.LastResetDay;

            if (lastResetTradeDay == null || beginResetTradeDay > lastResetTradeDay)
            {
                RecoverBalance(new Dictionary<DateTime, decimal>(), tran, CalculateNotRecoverOrderCost(tran));
            }
            else
            {
                var deltaAmountPerTradeDayDict = historyOrderRecover.RecoverDay(beginResetTradeDay, lastResetTradeDay.Value);
                decimal orderExecuteFee = CalculateCost(tran);
                RecoverAccountBalanceDayHistory(deltaAmountPerTradeDayDict, orderExecuteFee, tran);
                RecoverBalance(deltaAmountPerTradeDayDict, tran, orderExecuteFee);
                UpdateInstalmentOrderDebitInterest(tran, setting, beginResetTradeDay);
                OrderUpdater.Default.UpdateOrderInterestPerLotAndStoragePerLot(tran, lastResetTradeDay.Value);
            }
            RemoveCompletedOrdersFromCache(tran);
            string content;
            account.SaveAndBroadcastResetContent(Agent.Caching.CacheType.HistoryOrder, out content);
            Logger.InfoFormat("HistoryOrderFactory, save Content = {0}", content);
        }

        private static void RemoveCompletedOrdersFromCache(Transaction tran)
        {
            foreach (var eachOrder in tran.Orders)
            {
                if (eachOrder.Phase == OrderPhase.Completed)
                {
                    tran.Owner.RemoveOrderFromCache(eachOrder);
                    if (!eachOrder.IsOpen)
                    {
                        foreach (var eachOrderRelation in eachOrder.OrderRelations)
                        {
                            if (eachOrderRelation.OpenOrder.Phase == OrderPhase.Completed)
                            {
                                tran.Owner.RemoveOrderFromCache(eachOrderRelation.OpenOrder);
                            }
                        }
                    }
                }
            }
        }


        private static decimal CalculateNotRecoverOrderCost(Transaction tran)
        {
            decimal result = 0m;
            foreach (var eachOrder in tran.Orders)
            {
                result += eachOrder.SumBillsForBalance();
            }
            return result;
        }



        private static void RecoverBalance(Dictionary<DateTime, decimal> deltaAmountPerTradeDayDict, Transaction tran, decimal orderFee)
        {
            decimal balance = 0m;
            foreach (var eachAmount in deltaAmountPerTradeDayDict.Values)
            {
                balance += eachAmount;
            }
            tran.Owner.AddBalance(tran.CurrencyId, balance + orderFee, null);
        }


        /// <summary>
        /// each trade day balance equal the sum of the trade day balances that less than it
        /// </summary>
        /// <param name="deltaAmountPerTradeDayDict"></param>
        /// <param name="orderExecuteFee"></param>
        /// <param name="tran"></param>
        private static void RecoverAccountBalanceDayHistory(Dictionary<DateTime, decimal> deltaAmountPerTradeDayDict, decimal orderExecuteFee, Transaction tran)
        {
            DateTime beginTradeDay = deltaAmountPerTradeDayDict.Keys.Min();
            DateTime endTradeDay = deltaAmountPerTradeDayDict.Keys.Max();
            Guid currencyId = tran.Owner.IsMultiCurrency ? tran.CurrencyId : tran.Owner.Setting().CurrencyId;
            var historyBalancePerTradeDay = DBRepository.Default.GetAccountBalanceDayHistory(tran.Owner.Id, currencyId, beginTradeDay, endTradeDay);
            foreach (var eachTradeDay in deltaAmountPerTradeDayDict.Keys)
            {
                decimal balance = orderExecuteFee;
                foreach (var tradeDay in deltaAmountPerTradeDayDict.Keys)
                {
                    if (tradeDay <= eachTradeDay)
                    {
                        balance += deltaAmountPerTradeDayDict[tradeDay];
                    }
                }
                decimal historyBalance = 0m;
                historyBalancePerTradeDay.TryGetValue(eachTradeDay, out historyBalance);
                tran.Owner.AddHistoryBalanceOnly(currencyId, eachTradeDay, historyBalance + balance);
            }
        }


        private static void UpdateInstalmentOrderDebitInterest(Transaction tran, Settings.Setting setting, DateTime? tradeDay)
        {
            if (!tran.IsPhysical) return;
            DateTime lastTradeDay = DateTime.Now.Date;
            int currencyDecimals;
            if (tran.Owner.IsMultiCurrency)
            {
                var currency = setting.GetCurrency(tran.CurrencyId);
                currencyDecimals = currency.Decimals;
            }
            else
            {
                currencyDecimals = tran.Owner.Setting(tradeDay).Currency(tradeDay).Decimals;
            }
            foreach (var eachOrder in tran.Orders)
            {
                Physical.PhysicalOrder physicalOrder = (Physical.PhysicalOrder)eachOrder;
                if (physicalOrder.Instalment == null) return;
                var instalmentPolicyDetail = physicalOrder.Instalment.InstalmentPolicyDetail(null);
                foreach (var eachInstalmentDetail in physicalOrder.Instalment.InstalmentDetails)
                {
                    if (eachInstalmentDetail.PaymentDateTimeOnPlan >= lastTradeDay) continue;
                    eachInstalmentDetail.DebitInterest = InstalmentManager.CalculateDebitInterest(eachInstalmentDetail.Principal,
                        eachInstalmentDetail.Interest, (lastTradeDay - eachInstalmentDetail.PaymentDateTimeOnPlan.Value).Days,
                        instalmentPolicyDetail.InterestRate, instalmentPolicyDetail.DebitInterestType,
                        instalmentPolicyDetail.DebitInterestRatio, instalmentPolicyDetail.DebitFreeDays, tran.SettingInstrument().InterestYearDays, currencyDecimals);
                }
            }
        }

        private static decimal CalculateCost(Transaction tran)
        {
            decimal result = 0m;
            foreach (var eachOrder in tran.Orders)
            {
                result += CalculateOrderCost(eachOrder);
            }
            return result;
        }

        private static decimal CalculateOrderCost(Order order)
        {
            decimal result = 0m;
            result += order.CommissionSum + order.LevySum + order.OtherFeeSum;
            var physicalOrder = order as Physical.PhysicalOrder;
            if (physicalOrder != null)
            {
                result += physicalOrder.InstalmentAdministrationFee - Math.Abs(physicalOrder.PaidPledge) + physicalOrder.PhysicalPaymentDiscount;
                if (!physicalOrder.IsOpen)
                {
                    foreach (Physical.PhysicalOrderRelation eachOrderRelation in physicalOrder.OrderRelations)
                    {
                        result += eachOrderRelation.ClosePenalty + eachOrderRelation.OverdueCutPenalty + eachOrderRelation.PayBackPledge;
                    }
                }
            }
            return result;
        }

        private static List<Guid> GetAffectedOrders(Transaction tran)
        {
            List<Guid> result = new List<Guid>();
            foreach (var eachOrder in tran.Orders)
            {
                result.Add(eachOrder.Id);
                if (!eachOrder.IsOpen)
                {
                    foreach (var eachOrderRelation in eachOrder.OrderRelations)
                    {
                        if (!result.Contains(eachOrderRelation.OpenOrder.Id))
                        {
                            result.Add(eachOrderRelation.OpenOrder.Id);
                        }
                    }
                }
            }
            return result;
        }


    }



    internal sealed class HistoryOrderRecover
    {
        private Account _account;
        private Guid _instrumentId;
        private List<Guid> _affectedOrders;
        private List<Guid> _deletedOrders;
        private Settings.Setting _setting;

        internal HistoryOrderRecover(Account account, Guid instrumentId, List<Guid> affectedOrders, List<Guid> deletedOrders, Settings.Setting setting)
        {
            _account = account;
            _instrumentId = instrumentId;
            _affectedOrders = affectedOrders;
            _deletedOrders = deletedOrders;
            _setting = setting;
        }

        /// <summary>
        /// each trade day balance equal the sum of the trade day balances that less than it
        /// </summary>
        /// <param name="deltaAmountPerTradeDayDict"></param>
        /// <param name="orderExecuteFee"></param>
        /// <param name="tran"></param>

        internal Dictionary<DateTime, decimal> RecoverDay(DateTime beginResetTradeDay, DateTime lastResetTradeDay)
        {
            var deltaAmountPerTradeDayDict = new Dictionary<DateTime, decimal>();
            for (DateTime eachTradeDay = beginResetTradeDay; eachTradeDay <= lastResetTradeDay; eachTradeDay = eachTradeDay.AddDays(1))
            {
                decimal deltaAmount = this.RecoverDay(eachTradeDay);
                deltaAmountPerTradeDayDict.Add(eachTradeDay, deltaAmount);
            }
            return deltaAmountPerTradeDayDict;
        }

        private decimal RecoverDay(DateTime tradeDay)
        {
            List<Guid> orders = new List<Guid>(_affectedOrders.Count + _deletedOrders.Count);
            orders.AddRange(_affectedOrders);
            orders.AddRange(_deletedOrders);
            decimal preAmount = this.CalculateRecoverValuedAmount(orders, tradeDay);
            this.DeleteOrderDayHistory(_affectedOrders, tradeDay);
            decimal postAmount = 0m;
            if (_affectedOrders.Count != 0)
            {
                postAmount = this.RecoverOrderDayHistory(tradeDay, _affectedOrders);
            }
            return postAmount - preAmount;
        }

        private void DeleteOrderDayHistory(List<Guid> orders, DateTime tradeDay)
        {
            ResetManager.Default.RemoveOrderDayHistorys(orders, tradeDay);
        }


        private decimal RecoverOrderDayHistory(DateTime tradeDay, List<Guid> affectedOrders)
        {
            var instrumentTradeDaySetting = ResetManager.Default.LoadInstrumentHistorySettingAndData(_account.Id, _instrumentId, tradeDay);
            TradeDayInfo tradeDayInfo = new TradeDayInfo(_account, _instrumentId, tradeDay, instrumentTradeDaySetting, affectedOrders, _setting);
            TradeDayCalculator tradeDayCalculator = new TradeDayCalculator(tradeDayInfo, false);
            tradeDayCalculator.Calculate();
            var resetResults = tradeDayCalculator.ResetResults;
            if (resetResults == null) return 0m;
            var settingInstrument = _setting.GetInstrument(_instrumentId, tradeDay);
            OrderDayHistorySaver.Save(_account, _account.GetInstrument(_instrumentId), tradeDayCalculator, tradeDayInfo, settingInstrument);
            return tradeDayCalculator.ValuedPLForBook;
        }

        private decimal CalculateRecoverValuedAmount(List<Guid> affectedOrders, DateTime tradeDay)
        {
            List<OrderDayHistory> orderDayHistorys = ResetManager.Default.GetOrderDayHistorys(affectedOrders, _account.Id, _instrumentId, tradeDay);
            if (orderDayHistorys == null || orderDayHistorys.Count == 0) return 0m;
            decimal result = 0m;
            foreach (var eachOrderDayHistory in orderDayHistorys)
            {
                result += eachOrderDayHistory.InterestPLValued + eachOrderDayHistory.StoragePLValued + eachOrderDayHistory.TradePLValued;
            }
            return result;
        }

    }


}
