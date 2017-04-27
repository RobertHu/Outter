using Core.TransactionServer.Agent.BroadcastBLL;
using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;


namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class HistoryOrderDeleter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(HistoryOrderDeleter));

        private sealed class AffectedAmountManager
        {
            private sealed class AffectedAmount
            {
                internal AffectedAmount(Bill bill, DateTime tradeDay)
                {
                    this.TradeDay = tradeDay;
                    this.Value = bill.Value;
                    this.IsPL = (bill.Type == BillType.InterestPL || bill.Type == BillType.StoragePL || bill.Type == BillType.TradePL) ? true : false;
                }

                internal AffectedAmount(DateTime tradeDay, decimal value)
                {
                    this.TradeDay = tradeDay;
                    this.Value = value;
                    this.IsPL = false;
                }

                internal DateTime TradeDay { get; private set; }
                internal decimal Value { get; private set; }
                internal bool IsPL { get; private set; }
            }

            private Dictionary<DateTime, List<AffectedAmount>> _tradeDayAmountsDict = new Dictionary<DateTime, List<AffectedAmount>>();

            internal void Add(Bill bill)
            {
                DateTime tradeDay = DB.DBRepository.Default.GetTradeDay(bill.UpdateTime);
                List<AffectedAmount> amounts = this.GetAmounts(tradeDay);
                amounts.Add(new AffectedAmount(bill,tradeDay));
            }

            internal void Add(DateTime tradeDay, decimal value)
            {
                List<AffectedAmount> amounts = this.GetAmounts(tradeDay);
                amounts.Add(new AffectedAmount(tradeDay, value));
            }

            private List<AffectedAmount> GetAmounts(DateTime tradeDay)
            {
                List<AffectedAmount> amounts;
                if (!_tradeDayAmountsDict.TryGetValue(tradeDay, out amounts))
                {
                    amounts = new List<AffectedAmount>();
                    _tradeDayAmountsDict.Add(tradeDay, amounts);
                }
                return amounts;
            }

            internal Dictionary<DateTime, decimal> CalculateAffectedAmountPerTradeDay()
            {
                Dictionary<DateTime, decimal> result = new Dictionary<DateTime, decimal>();
                foreach (var eachPair in _tradeDayAmountsDict)
                {
                    decimal amount = 0m;
                    foreach (var eachAmount in eachPair.Value)
                    {
                        amount += eachAmount.Value;
                    }
                    result.Add(eachPair.Key, amount);
                }
                return result;
            }

        }

        private static class AffectedOpenOrderRecover
        {
            internal static void Recove(Order order, DateTime closeTime, bool isPayForInstalmentDebitInterest, DateTime tradeDay, decimal paybackPledge = 0m, decimal closedPhysicalValue = 0m)
            {
                PhysicalOrder physicalOrder = order as PhysicalOrder;
                if (physicalOrder != null)
                {
                    RecovePaidPledgeBalanceAndOriginValueBalance((PhysicalOrder)order, paybackPledge, closedPhysicalValue);
                    RecoveInstalmentInfo(physicalOrder, isPayForInstalmentDebitInterest, closeTime, tradeDay);
                }
            }

            private static void RecovePaidPledgeBalanceAndOriginValueBalance(PhysicalOrder order, decimal paybackPledge, decimal closedPhysicalValue)
            {
                order.PaidPledgeBalance += paybackPledge;
                order.PhysicalOriginValueBalance += closedPhysicalValue;
            }

            private static void RecoveInstalmentInfo(PhysicalOrder order, bool isPayForInstalmentDebitInterest, DateTime closeTime, DateTime tradeDay)
            {
                if (order.Instalment != null)
                {
                    int remainPeriod, minSequence;
                    RemoveNotPayoffInstalmentDetails(order, closeTime, out remainPeriod, out minSequence);
                    GenerateInstalmentDetails(order, remainPeriod, minSequence, isPayForInstalmentDebitInterest, tradeDay);
                }
            }

            private static void GenerateInstalmentDetails(PhysicalOrder order, int remainPeriod, int minSequence, bool isPayForInstalmentDebitInterest, DateTime tradeDay)
            {
                var lastInstalmentPolicyDetailModel = DBRepository.Default.GetLastInstalmentPolicyDetail(order.Owner.ExecuteTime.Value);
                var instalmentPolicyDetail = lastInstalmentPolicyDetailModel == null ? order.Instalment.InstalmentPolicyDetail(null) : new Settings.InstalmentPolicyDetail(lastInstalmentPolicyDetailModel);
                decimal instalmentAmount = order.PhysicalOriginValueBalance - Math.Abs(order.PaidPledgeBalance);
                order.GenerateInstalmentDetails(instalmentPolicyDetail, instalmentAmount, remainPeriod, tradeDay, minSequence);
                var instrument = order.Owner.SettingInstrument();
                var account = order.Owner.Owner;
                int currencyDecimals = account.IsMultiCurrency ? Settings.Setting.Default.GetCurrency(instrument.CurrencyId, tradeDay).Decimals : account.Setting(tradeDay).Currency(tradeDay).Decimals;
                foreach (var eachInstalmentDetail in order.InstalmentDetails)
                {
                    if (isPayForInstalmentDebitInterest)
                    {
                        eachInstalmentDetail.DebitInterest = InstalmentManager.CalculateDebitInterest(eachInstalmentDetail.Principal, eachInstalmentDetail.Interest, DateTime.Now.Date.GetDateDiff(eachInstalmentDetail.PaymentDateTimeOnPlan.Value), instalmentPolicyDetail.InterestRate,
                            instalmentPolicyDetail.DebitInterestType, instalmentPolicyDetail.DebitInterestRatio, instalmentPolicyDetail.DebitFreeDays, instrument.InterestYearDays, currencyDecimals);
                    }
                }
            }


            private static void RemoveNotPayoffInstalmentDetails(PhysicalOrder order, DateTime closeTime, out int remainPeriod, out int minSequence)
            {
                remainPeriod = 0;
                minSequence = 0;
                var notPayoffDetails = GetNotPayoffInstalmentDetails(order, closeTime);
                if (notPayoffDetails == null || notPayoffDetails.Count() == 0) return;
                remainPeriod = notPayoffDetails.Count;
                minSequence = notPayoffDetails.Min(m => m.Period);
                foreach (var eachNotPayoffDetail in notPayoffDetails)
                {
                    order.DeleteInstalmentDetail(eachNotPayoffDetail);
                }
            }

            private static List<Physical.InstalmentBusiness.InstalmentDetail> GetNotPayoffInstalmentDetails(PhysicalOrder order, DateTime closeTime)
            {
                var result = new List<Physical.InstalmentBusiness.InstalmentDetail>();
                foreach (var eachInstalmentDetail in order.Instalment.InstalmentDetails)
                {
                    if (eachInstalmentDetail.PaidDateTime == null || eachInstalmentDetail.PaidDateTime >= closeTime)
                    {
                        result.Add(eachInstalmentDetail);
                    }
                }
                return result;
            }


        }

        private static class OrderHelper
        {
            internal static List<Guid> GetAffectedOrders(Order order)
            {
                List<Guid> result = new List<Guid>();
                if (!order.IsOpen)
                {
                    foreach (var eachOrderRelation in order.OrderRelations)
                    {
                        result.Add(eachOrderRelation.OpenOrder.Id);
                    }
                }
                return result;
            }
        }

        private static class CloseOrderRecover
        {

            internal static void RecoveAffectedOpenOrders(Order order, bool isPayForInstalmentDebitInterest, DateTime tradeDay)
            {
                foreach (var eachOrderRelation in order.OrderRelations)
                {
                    Order openOrder = eachOrderRelation.OpenOrder;
                    openOrder.LotBalance += eachOrderRelation.ClosedLot;
                    var context = Engine.ExecuteContext.CreateExecuteDirectly(order.AccountId, openOrder.Id, Engine.ExecuteStatus.None);
                    context.BookInfo = new Engine.BookInfo(tradeDay, tradeDay);
                    openOrder.RecalculateEstimateFee(context);
                    if (order.Owner.IsPhysical)
                    {
                        PhysicalOrderRelation physicalOrderRelation = (PhysicalOrderRelation)eachOrderRelation;
                        AffectedOpenOrderRecover.Recove(order, eachOrderRelation.CloseTime.Value, isPayForInstalmentDebitInterest, tradeDay, physicalOrderRelation.PayBackPledge, physicalOrderRelation.ClosedPhysicalValue);
                    }
                }
            }


            private static void VerifyPhysicalOrder(Order order)
            {
                PhysicalOrder closeOrder = order as PhysicalOrder;
                if (closeOrder != null)
                {
                    VerifyExistOpenPhysicalOrderWithSameDirection(order);
                }
            }

            /// <summary>
            /// 检查是否存在和closeOrder同方向的开仓单并且affectedOrders不包含此开仓单
            /// </summary>
            /// <param name="closeOrder"></param>
            /// <param name="affectedOrders"></param>
            private static void VerifyExistOpenPhysicalOrderWithSameDirection(Order closeOrder)
            {
                List<Guid> affectedOrders = OrderHelper.GetAffectedOrders(closeOrder);
                var account = closeOrder.Owner.Owner;
                foreach (var eachTran in account.Transactions)
                {
                    if (!eachTran.IsPhysical) continue;
                    foreach (var eachOrder in eachTran.Orders)
                    {
                        if (eachOrder.IsOpen && eachOrder.IsBuy == closeOrder.IsBuy && eachOrder.LotBalance > 0 && !affectedOrders.Contains(eachOrder.Id))
                        {
                            throw new TransactionServerException(TransactionError.OrderCannotBeDeleted, string.Format("Exist some open order ID = {0}, with same direction and not in closeOrder ID = {1}'s open orders", eachOrder.Id, closeOrder.Id));
                        }
                    }
                }
            }

        }

        internal Agent.Account Account { get; private set; }
        private HistoryOrderRecover _historyOrderRecover;
        private Order _order;
        private PhysicalOrder _physicalOrder;
        private AffectedAmountManager _affectedAmountManager = new AffectedAmountManager();
        private AffectedAmountManager _deletedOrderAffectedAmountManager = new AffectedAmountManager();
        private AffectedAmountManager _deletedOrderAmountManager = new AffectedAmountManager();
        private bool _isPayForInstalmentDebitInterest;
        private Guid _currencyId;

        internal HistoryOrderDeleter(Order order, Settings.Setting setting, bool isPayForInstalmentDebitInterest)
        {
            this.Account = order.Owner.Owner;
            _order = order;
            _isPayForInstalmentDebitInterest = isPayForInstalmentDebitInterest;
            _physicalOrder = _order as PhysicalOrder;
            _currencyId = DBResetRepository.GetCurrencyId(this.Account.Id, order.Instrument().Id, _order.Owner.ExecuteTime.Value);
            _historyOrderRecover = new HistoryOrderRecover(this.Account, _order.Instrument().Id, OrderHelper.GetAffectedOrders(_order), new List<Guid> { order.Id }, setting);
        }

        internal void Delete()
        {
            DateTime tradeDay = DB.DBRepository.Default.GetTradeDay(_order.Owner.ExecuteTime.Value);
            if (!this.CanBeDeleted()) return;
            this.RecoverAffectedOrder(tradeDay);
            this.Account.RemoveOrder(_order);
            this.RecoverTradeDay(tradeDay);
            this.RecoveBalanceAndBalanceHistory(tradeDay);
            string content;
            this.Account.SaveAndBroadcastResetContent(Agent.Caching.CacheType.HistoryOrder, out content);
            Logger.InfoFormat("delete order content = {0}", content);
        }


        private void RecoveBalanceAndBalanceHistory(DateTime tradeDay)
        {
            Dictionary<DateTime, decimal> resetAmountPerTradeDayDict = _affectedAmountManager.CalculateAffectedAmountPerTradeDay();
            Dictionary<DateTime, decimal> deletedOrderAmountPerTradeDayDict = _deletedOrderAffectedAmountManager.CalculateAffectedAmountPerTradeDay();
            this.RecoveBalanceHistory(tradeDay,resetAmountPerTradeDayDict, deletedOrderAmountPerTradeDayDict);
            this.RecoveBalance(resetAmountPerTradeDayDict, _deletedOrderAmountManager.CalculateAffectedAmountPerTradeDay());
        }

        private void RecoveBalance(Dictionary<DateTime, decimal> resetAmountPerTradeDayDict, Dictionary<DateTime, decimal> deletedOrderAmountPerTradeDayDict)
        {
            decimal deltaBalance = resetAmountPerTradeDayDict.Sum(m => m.Value);
            decimal deletedOrderCost = deletedOrderAmountPerTradeDayDict.Sum(m => m.Value);
            this.Account.AddBalance(_currencyId, deltaBalance + deletedOrderCost, null);
        }

        private void RecoveBalanceHistory(DateTime tradeDay,Dictionary<DateTime, decimal> resetAmountPerTradeDayDict, Dictionary<DateTime, decimal> deletedOrderAmountPerTradeDayDict)
        {
            DateTime beginTradeDay = tradeDay;
            DateTime? endTradeDay = _order.Owner.Owner.LastResetDay;
            if (endTradeDay != null && beginTradeDay <= endTradeDay)
            {
                var historyBalancePerTradeDay = DBRepository.Default.GetAccountBalanceDayHistory(_order.AccountId, _currencyId, beginTradeDay, endTradeDay.Value);
                for (DateTime eachTradeDay = beginTradeDay; eachTradeDay <= endTradeDay; eachTradeDay = eachTradeDay.AddDays(1))
                {
                    decimal deltaResetBalance = this.CalculateDeltaHistoryBalance(eachTradeDay, resetAmountPerTradeDayDict);
                    decimal deletedOrderCost = this.GetDeletedOrderCost(deletedOrderAmountPerTradeDayDict, eachTradeDay);
                    decimal historyBalance = 0m;
                    historyBalancePerTradeDay.TryGetValue(eachTradeDay, out historyBalance);
                    this.Account.AddHistoryBalanceOnly(_currencyId, eachTradeDay, historyBalance + deltaResetBalance + deletedOrderCost);
                }
            }
        }

        private decimal GetDeletedOrderCost(Dictionary<DateTime, decimal> deletedOrderAmountPerTradeDayDict, DateTime tradeDay)
        {
            decimal cost = 0m;
            foreach (var eachPair in deletedOrderAmountPerTradeDayDict)
            {
                DateTime eachTradeDay = eachPair.Key;
                decimal amount = eachPair.Value;
                if (eachTradeDay <= tradeDay)
                {
                    cost += amount;
                }
            }
            return cost;
        }


        private decimal CalculateDeltaHistoryBalance(DateTime resetTradeDay, Dictionary<DateTime, decimal> resetAmountPerTradeDayDict)
        {
            decimal result = 0m;
            foreach (var eachResetAmountPerTradeDay in resetAmountPerTradeDayDict)
            {
                DateTime eachTradeDay = eachResetAmountPerTradeDay.Key;
                decimal resetAmount = eachResetAmountPerTradeDay.Value;
                if (eachTradeDay <= resetTradeDay)
                {
                    result += resetAmount;
                }
            }
            return result;
        }


        private bool CanBeDeleted()
        {
            Logger.InfoFormat("CanBeDeleted, orderId ={0}, isOpen = {1}, lot = {2}, lotBalance = {3}", _order.Id, _order.IsOpen, _order.Lot, _order.LotBalance);
            return (_order.IsOpen && _order.Lot == _order.LotBalance) ||
                    (!_order.IsOpen && _order.LotBalance == 0);
        }

        private void RecoverTradeDay(DateTime tradeDay)
        {
            var account = _order.Owner.Owner;
            DateTime? endTradeDay;
            if (this.ShouldRecoverTradeDay(tradeDay, out endTradeDay))
            {
                this.DoRecoverTradeDay(tradeDay, endTradeDay.Value);
            }
        }

        private void DoRecoverTradeDay(DateTime beginTradeDay, DateTime endTradeDay)
        {
            Dictionary<DateTime, decimal> tradeDayPerResetAmountDict = _historyOrderRecover.RecoverDay(beginTradeDay, endTradeDay);
            foreach (var eachPair in tradeDayPerResetAmountDict)
            {
                DateTime tradeDay = eachPair.Key;
                decimal deltaResetAmount = eachPair.Value;
                this.AddResetAmount(deltaResetAmount, tradeDay);
            }
        }

        private bool ShouldRecoverTradeDay(DateTime beginTradeDay, out DateTime? endTradeDay)
        {
            var account = _order.Owner.Owner;
            endTradeDay = null;
            if (account.LastResetDay == null) return false;
            endTradeDay = account.LastResetDay.Value;
            if (beginTradeDay > endTradeDay) return false;
            return true;
        }


        private void AddResetAmount(decimal deltaResetAmount, DateTime tradeDay)
        {
            if (deltaResetAmount != 0)
            {
                _affectedAmountManager.Add(tradeDay, deltaResetAmount);
            }
            else
            {
                Logger.InfoFormat("AddResetAmount tradeDay = {0}, deltaResetAmount = 0, deletedOrderId = {1}, accountId = {2}", tradeDay, _order.Id, this.Account.Id);
            }
        }


        private void RecoverAffectedOrder(DateTime tradeDay)
        {
            if (_order.IsOpen)
            {
                _order.LotBalance = 0m;
            }
            else
            {
                ResetManager.Default.LoadHistorySetting(tradeDay, "HistoryorderDeleted.RecoverAffectedOrder");
                CloseOrderRecover.RecoveAffectedOpenOrders(_order, _isPayForInstalmentDebitInterest, tradeDay);
            }
            this.RecoveBills(_order);
            this.DeleteOrderInstalmentDetails();
        }

        private void DeleteOrderInstalmentDetails()
        {
            if (!_order.Owner.IsPhysical || !_order.IsOpen) return;
            PhysicalOrder physicalOrder = (PhysicalOrder)_order;
            physicalOrder.DeleteAllInstalmentDetail();
        }


        private void RecoveBills(Order order)
        {
            foreach (var eachBill in order.Bills)
            {
                if (this.ShouldPassPaidPledge(order, eachBill.Type)) continue;

                if (eachBill.Type != BillType.PayBackPledge && this.ShouldRecoverBill(eachBill))
                {
                    _deletedOrderAffectedAmountManager.Add(-eachBill);
                }
            }

            foreach (var eachBill in order.Bills)
            {
                if (this.ShouldPassPaidPledge(order, eachBill.Type)) continue;

                if (this.ShouldRecoverBill(eachBill))
                {
                    _deletedOrderAmountManager.Add(-eachBill);
                }
            }
        }

        private bool ShouldRecoverBill(Bill eachBill)
        {
            return eachBill.Type != BillType.InterestPL && eachBill.Type != BillType.StoragePL;
        }

        private bool ShouldPassPaidPledge(Order order, BillType billType)
        {
            return !order.IsOpen && billType == BillType.PaidPledge;
        }

    }

}
