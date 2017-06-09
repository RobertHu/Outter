using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Reset.PhysicalReset;
using System.Diagnostics;
using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.BLL.OrderBusiness;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class TradeDayCommonResult
    {
        internal decimal InterestPerLot { get; set; }
        internal decimal StoragePerLot { get; set; }
        internal int InterestYearDays { get; set; }
        internal DateTime? InterestValueDate { get; set; }
        internal decimal InstalmentInterest { get; set; }
    }

    internal struct InterestStorage : IEquatable<InterestStorage>
    {
        internal static readonly InterestStorage Empty = new InterestStorage(0m, 0m);

        private decimal _interest;
        private decimal _storagePL;

        internal InterestStorage(decimal interest, decimal storage)
        {
            _interest = interest;
            _storagePL = storage;
        }

        internal InterestStorage(InterestStorage other)
        {
            _interest = other.Interest;
            _storagePL = other.Storage;
        }

        internal decimal Interest
        {
            get { return _interest; }
        }

        internal decimal Storage
        {
            get { return _storagePL; }
        }

        public static InterestStorage operator +(InterestStorage left, InterestStorage right)
        {
            return new InterestStorage(left.Interest + right.Interest, left.Storage + right.Storage);
        }


        public bool Equals(InterestStorage other)
        {
            return this.Interest == other.Interest && this.Storage == other.Storage;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((InterestStorage)obj);
        }

        public override int GetHashCode()
        {
            return this.Interest.GetHashCode() ^ this.Storage.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("InterestPl = {0}, StoragePL = {1}", this.Interest, this.Storage);
        }

    }

    internal sealed class Exchanger
    {
        private RateSetting _rateSetting;

        internal Exchanger(RateSetting rateSetting)
        {
            _rateSetting = rateSetting;
        }

        internal decimal ExchangeByCommonDecimals(decimal value)
        {
            return value.Exchange(_rateSetting.RateIn, _rateSetting.RateOut, _rateSetting.RoundDecimals.Common, null);
        }

        internal decimal ExchangeByCommonWithInstrumentSourceDecimals(decimal value)
        {
            return value.Exchange(_rateSetting.RateIn, _rateSetting.RateOut, _rateSetting.RoundDecimals.Common, _rateSetting.RoundDecimals.Instrument);
        }

        internal decimal ExchangeByInstrumentDecimals(decimal value)
        {
            return value.Exchange(_rateSetting.RateIn, _rateSetting.RateOut, _rateSetting.RoundDecimals.Common, null);
        }

        internal decimal Exchange(decimal value, decimal rateIn, decimal rateOut, int decimals, int? sourceDecimals = null)
        {
            return value.Exchange(rateIn, rateOut, decimals, sourceDecimals);
        }

    }

    internal sealed class OpenOrderPLOfCurrentDay
    {
        internal InterestStorage DayNotValued { get; set; }
        internal InterestStorage NotValued { get; set; }
        internal InterestStorage Valued { get; set; }

        internal OpenOrderPLOfCurrentDay()
        {
            this.DayNotValued = InterestStorage.Empty;
            this.NotValued = InterestStorage.Empty;
            this.Valued = InterestStorage.Empty;
        }

        internal bool IsInterestValued { get; set; }
        internal bool IsStorageValued { get; set; }
    }

    internal static class TradeDayCalculatorFactory
    {
        internal static TradeDayCalculator CreateForReset(TradeDayInfo tradeDayInfo, InstrumentCloseQuotation closeQuotation)
        {
            return CreateCommon(tradeDayInfo, closeQuotation, true);
        }

        internal static TradeDayCalculator CreateForHistoryOrder(TradeDayInfo tradeDayInfo, InstrumentCloseQuotation closeQuotation)
        {
            return CreateCommon(tradeDayInfo, closeQuotation, false);
        }

        private static TradeDayCalculator CreateCommon(TradeDayInfo tradeDayInfo, InstrumentCloseQuotation closeQuotation, bool isReset)
        {
            if (closeQuotation != null)
            {
                tradeDayInfo.UpdateInstrumentDayClosePrice(closeQuotation.BuyPrice, closeQuotation.SellPrice);
            }
            return new TradeDayCalculator(tradeDayInfo, isReset);
        }
    }

    internal sealed class TradeDayCalculator
    {
        private TradeDayInfo _info;
        private Dictionary<Guid, OrderResetResult> _resetResultDict = new Dictionary<Guid, OrderResetResult>();
        private OrderPhaseUpdater _orderPhaseUpdater;
        private Exchanger _exchanger;
        private bool _isReset;
        private decimal _valuedPLForBook = 0m;

        internal TradeDayCalculator(TradeDayInfo data, bool isReset)
        {
            _info = data;
            _isReset = isReset;
            _orderPhaseUpdater = new OrderPhaseUpdater(data.CloseOrders, _info.Settings.ResetTime, _info.Instrument.Id, _info.TradeDay);
            this.Balance = 0m;
            _exchanger = new Exchanger(data.RateSetting);
        }

        internal decimal Balance { get; private set; }

        internal DateTime ResetTime { get { return _info.Settings.ResetTime; } }

        internal decimal ValuedPLForBook { get { return _valuedPLForBook; } }

        internal Dictionary<Guid, OrderResetResult> ResetResults
        {
            get { return _resetResultDict; }
        }

        internal void Calculate()
        {
            if (_isReset)
            {
                ResetManager.Default.ProcessForReset(_info.Instrument.Id, _info.TradeDay);
            }
            Dictionary<Guid, TradeDayCommonResult> commonDict = this.CalculateCommon();
            Dictionary<Guid, OpenOrderPLOfCurrentDay> openOrderPLOfCurrentDayDict = OpenOrderCurrentDayPLCalculator.Calculate(_info, commonDict, _exchanger);
            this.UpdateInterestPerLotAndStoragePerLot(openOrderPLOfCurrentDayDict, commonDict);
            List<OrderRelation> valuedOrderRelations = OrderRelationResetter.Default.UpdateOrderRelationValueDate(_info);
            Dictionary<Guid, InterestStorage> notValuedPLDict = NotValuedOpenOrderPLCalculator.Default.Calculate(_info.Account.Id, _info.Instrument.Id, _info.Orders, _info.TradeDay, _exchanger, _info.AffectedOrders);
            Dictionary<Guid, InterestStorage> valuedPLDict = ValuedOpenOrderPLCalculator.Default.Calculate(_info.Account.Id, _info.Instrument.Id, _info.OrdersAllExecute, _info.TradeDay, _exchanger, _info.AffectedOrders);
            this.FillResetResult(commonDict, openOrderPLOfCurrentDayDict, notValuedPLDict, valuedPLDict);
            this.CalculateFloatPL();
            List<PhysicalOrderRelation> matureOrderRelations = OrderRelationResetter.Default.UpdatePhysicalOrderRelationValueMatureDate(_info);
            CloseOrderPLCalculator.Calculate(_info, _exchanger, _resetResultDict, _info.AffectedOrders);
            foreach (var eachResetResult in _resetResultDict.Values)
            {
                eachResetResult.ValuedPL += new InterestStorage(eachResetResult.InstalmentInterest, 0m);
            }
            this.CalculateValuedPLForBook();
            this.CalculateFullPaymentCostAndPledgeCost();
            this.UpdateTradeFloatPLAndTradePLValuedForPhysicalOrder();
            this.Balance += this.CalculatePLValuedAtResetTime(valuedOrderRelations);
            _orderPhaseUpdater.Update();
            this.Balance += this.CalculateMatureFund(matureOrderRelations);
        }

        private void CalculateValuedPLForBook()
        {
            if (_isReset) return;
            decimal result = 0m;
            foreach (var eachResetResult in _resetResultDict.Values)
            {
                result += eachResetResult.ValuedPL.Interest + eachResetResult.ValuedPL.Storage + eachResetResult.TradePLValued;
            }
            _valuedPLForBook = result;
        }


        private void UpdateInterestPerLotAndStoragePerLot(Dictionary<Guid, OpenOrderPLOfCurrentDay> openOrderPLOfCurrentDayDict, Dictionary<Guid, TradeDayCommonResult> commonDict)
        {
            try
            {
                if (openOrderPLOfCurrentDayDict == null || openOrderPLOfCurrentDayDict.Count == 0) return;
                foreach (var eachPair in openOrderPLOfCurrentDayDict)
                {
                    var orderId = eachPair.Key;
                    var openOrderPLOfCurrentDay = eachPair.Value;
                    var commonResult = commonDict[orderId];
                    commonResult.InterestPerLot = openOrderPLOfCurrentDay.IsInterestValued ? 0m : commonResult.InterestPerLot;
                    commonResult.StoragePerLot = openOrderPLOfCurrentDay.IsStorageValued ? 0m : commonResult.StoragePerLot;
                }
            }
            catch
            {
                throw;
            }
        }

        private void CalculateFullPaymentCostAndPledgeCost()
        {
            foreach (var eachOrder in _info.OrdersAllExecute)
            {
                if (!eachOrder.ShouldCalculate(_info.AffectedOrders)) continue;
                if (eachOrder.Owner.ExecuteTime > _info.TradeDayBeginTime && eachOrder.Owner.ExecuteTime <= _info.TradeDayEndTime)
                {
                    PhysicalOrder physicalOrder = eachOrder as PhysicalOrder;
                    if (physicalOrder == null) continue;
                    OrderResetResult resetResult;
                    if (!_resetResultDict.TryGetValue(eachOrder.Id, out resetResult)) continue;
                    resetResult.FullPaymentCost = FullPaymentCostCalculator.Calculate(physicalOrder);
                    resetResult.PledgeCost = PledgeCostCalculator.Calculate(physicalOrder);
                }
            }
        }


        private void UpdateTradeFloatPLAndTradePLValuedForPhysicalOrder()
        {
            foreach (var eachOrder in _info.OrdersAllExecute)
            {
                if (!eachOrder.ShouldCalculate(_info.AffectedOrders)) continue;
                PhysicalOrder physicalOrder = eachOrder as PhysicalOrder;
                if (physicalOrder == null) continue;
                this.UpdateTradePLFloatForPhysicalOrder(physicalOrder);
                this.UpdateTradePLValuedForPhysicalOrder(physicalOrder);
            }
        }


        private void UpdateTradePLValuedForPhysicalOrder(PhysicalOrder physicalOrder)
        {
            OrderResetResult resetResult;
            if (!_resetResultDict.TryGetValue(physicalOrder.Id, out resetResult)) return;
            if (!physicalOrder.IsOpen)
            {
                resetResult.TradePLValued = 0m;
                foreach (PhysicalOrderRelation orderRelation in physicalOrder.OrderRelations)
                {
                    if ((!orderRelation.IsFullPayment || physicalOrder.PhysicalTradeSide != PhysicalTradeSide.Sell))
                    {
                        resetResult.TradePLValued += orderRelation.TradePL;
                    }
                }
            }
            else
            {
                if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery)
                {
                    resetResult.TradePLValued = 0m;
                }
            }
        }


        private void UpdateTradePLFloatForPhysicalOrder(PhysicalOrder physicalOrder)
        {
            if (physicalOrder.IsOpen && physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Deposit || (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Buy && physicalOrder.IsPayoff))
            {
                OrderResetResult resetResult;
                if (!_resetResultDict.TryGetValue(physicalOrder.Id, out resetResult)) return;
                resetResult.TradePLFloat = 0m;
            }
        }



        private decimal CalculateMatureFund(List<PhysicalOrderRelation> matureOrderRelations)
        {
            decimal result = 0m;
            foreach (var eachOrderRelation in matureOrderRelations)
            {
                result += eachOrderRelation.PhysicalValue;
            }
            return result;
        }

        private decimal CalculatePLValuedAtResetTime(List<OrderRelation> valuedOrderRelations)
        {
            return this.CalculateValuedOpenOrderPL() + OrderRelationResetter.Default.CalculateValuedCloseOrderPL(valuedOrderRelations, _exchanger);
        }

        private decimal CalculateValuedOpenOrderPL()
        {
            decimal result = 0m;
            foreach (var eachOrderResetResult in _resetResultDict.Values)
            {
                var eachOrder = _info.GetOrder(eachOrderResetResult.OrderId);
                if (!eachOrder.IsOpen) continue;
                result += eachOrderResetResult.ValuedPL.Interest + eachOrderResetResult.ValuedPL.Storage;
            }
            return result;
        }


        private void CalculateFloatPL()
        {
            foreach (var eachResetResult in _resetResultDict.Values)
            {
                var eachOrder = _info.GetOrder(eachResetResult.OrderId);

#if INSTRUMENTRESETTEST
                if (eachOrder.Id != Guid.Parse("12557828-7227-4DF3-A9BA-E6DB740D2182")) continue;
#else
#endif

                if (eachOrder.ShouldCalculate(_info.AffectedOrders) && this.ShouldCalculateOrderFloatPL(eachOrder))
                {
                    this.CalculateOrderFloatPL(eachOrder, eachResetResult);
                }
            }
        }

        private bool ShouldCalculateOrderFloatPL(Order order)
        {
            return order.Phase == OrderPhase.Executed && order.IsOpen && order.LotBalance > 0 && order.ExecuteTime <= _info.Settings.ResetTime;
        }

        private void CalculateOrderFloatPL(Order order, OrderResetResult resetResult)
        {
            if (_info.Settings.BuyPrice != null && _info.Settings.SellPrice != null)
            {
                Price dayClosePrice = order.IsBuy ? _info.Settings.SellPrice : _info.Settings.BuyPrice;
                Price buyPrice = order.IsBuy ? order.ExecutePrice : dayClosePrice;
                Price sellPrice = !order.IsBuy ? order.ExecutePrice : dayClosePrice;
                decimal tradePLFloat = TradePLCalculator.Calculate(_info.Instrument.TradePLFormula, order.LotBalance * order.Owner.ContractSize(_info.TradeDay), (decimal)buyPrice, (decimal)sellPrice, (decimal)dayClosePrice);
                resetResult.DayClosePrice = dayClosePrice;
                resetResult.TradePLFloat = _exchanger.Exchange(tradePLFloat, _info.RateSetting.RateIn, _info.RateSetting.RateOut, _info.RateSetting.RoundDecimals.Common);
            }
        }

        private void FillResetResult(Dictionary<Guid, TradeDayCommonResult> commonDict, Dictionary<Guid, OpenOrderPLOfCurrentDay> openOrderPLOfCurrentDayDict, Dictionary<Guid, InterestStorage> notValuedPLDict, Dictionary<Guid, InterestStorage> valuedPLDict)
        {
            foreach (var eachOrder in _info.Orders)
            {
                if (!eachOrder.ShouldCalculate(_info.AffectedOrders)) continue;
                if (eachOrder.Phase == OrderPhase.Executed && eachOrder.IsOpen && eachOrder.Owner.ExecuteTime <= _info.Settings.ResetTime)
                {
                    _resetResultDict.Add(eachOrder.Id, this.CreateResetResult(eachOrder, commonDict, openOrderPLOfCurrentDayDict, notValuedPLDict, valuedPLDict));
                }
            }
        }

        private OrderResetResult CreateResetResult(Order order, Dictionary<Guid, TradeDayCommonResult> commonDict, Dictionary<Guid, OpenOrderPLOfCurrentDay> openOrderPLOfCurrentDayDict, Dictionary<Guid, InterestStorage> notValuedPLDict, Dictionary<Guid, InterestStorage> valuedPLDict)
        {
            OrderResetResult resetResult = new OrderResetResult();
            TradeDayCommonResult commonResult = null;
            if (commonDict != null)
            {
                commonDict.TryGetValue(order.Id, out commonResult);
            }

            OpenOrderPLOfCurrentDay openOrderPLOfCurrentDay = null;
            if (openOrderPLOfCurrentDayDict != null)
            {
                openOrderPLOfCurrentDayDict.TryGetValue(order.Id, out openOrderPLOfCurrentDay);
            }

            InterestStorage interestStoragePLNotValued = InterestStorage.Empty;
            if (notValuedPLDict != null)
            {
                notValuedPLDict.TryGetValue(order.Id, out interestStoragePLNotValued);
            }

            InterestStorage interestStoragePLValued = InterestStorage.Empty;
            if (valuedPLDict != null)
            {
                valuedPLDict.TryGetValue(order.Id, out interestStoragePLValued);
            }
            resetResult.TradeDay = _info.TradeDay;
            resetResult.OrderId = order.Id;
            resetResult.LotBalance = order.LotBalance;
            resetResult.CurrencyId = _info.RateSetting.CurrencyId;
            Price livePrice = order.IsBuy ? _info.Settings.SellPrice : _info.Settings.BuyPrice;
            resetResult.Margin = Calculator.MarginCalculator.CalculateMargin((int)_info.Instrument.MarginFormula, order.LotBalance, order.Owner.ContractSize(_info.TradeDay), order.ExecutePrice, livePrice, _info.RateSetting.RateIn, _info.RateSetting.RateOut, _info.RateSetting.RoundDecimals.Common, _info.RateSetting.RoundDecimals.Instrument);

            resetResult.PerLot = commonResult == null ? InterestStorage.Empty : new InterestStorage(commonResult.InterestPerLot, commonResult.StoragePerLot);

            resetResult.FloatPL = commonResult == null ? InterestStorage.Empty : new InterestStorage(_exchanger.ExchangeByCommonWithInstrumentSourceDecimals(order.LotBalance * commonResult.InterestPerLot), _exchanger.ExchangeByCommonWithInstrumentSourceDecimals(order.LotBalance * commonResult.StoragePerLot));

            resetResult.DayNotValuedPL = openOrderPLOfCurrentDay == null ? InterestStorage.Empty : new InterestStorage(openOrderPLOfCurrentDay.DayNotValued);

            resetResult.NotValuedPL = interestStoragePLNotValued + (openOrderPLOfCurrentDay == null ? InterestStorage.Empty : openOrderPLOfCurrentDay.NotValued);

            resetResult.ValuedPL = interestStoragePLValued + (openOrderPLOfCurrentDay == null ? InterestStorage.Empty : openOrderPLOfCurrentDay.Valued);

            if (_info.Instrument.Category == InstrumentCategory.Physical)
            {
                Physical.PhysicalOrder physicalOrder = (Physical.PhysicalOrder)order;
                resetResult.PhysicalPaidAmount = physicalOrder.PaidAmount;
                resetResult.PaidPledgeBalance = physicalOrder.PaidPledgeBalance;
                resetResult.PhysicalOriginValueBalance = physicalOrder.PhysicalOriginValueBalance;
                resetResult.InstalmentInterest = commonResult == null ? 0m : commonResult.InstalmentInterest;
            }
            return resetResult;
        }





        private Dictionary<Guid, TradeDayCommonResult> CalculateCommon()
        {
            Dictionary<Guid, TradeDayCommonResult> result = new Dictionary<Guid, TradeDayCommonResult>();
            foreach (var eachOrder in _info.OpenOrders)
            {
#if TEST
                Debug.Assert(eachOrder.Id != Guid.Parse("853603af-e6b3-4d39-b0e7-aadfc4c6b894"));
#endif
                if (!eachOrder.ShouldCalculate(_info.AffectedOrders)) continue;
                TradeDayCommonResult commonResult = this.CalculateCommonForOrder(eachOrder, _info.TradeDay);
                if (!_info.Settings.IsUseSettlementPriceForInterest && commonResult.InterestPerLot == 0m && commonResult.StoragePerLot == 0m && commonResult.InstalmentInterest == 0m) continue;
                Debug.Assert(!result.ContainsKey(eachOrder.Id));
                result.Add(eachOrder.Id, commonResult);
            }
            return result;
        }


        private TradeDayCommonResult CalculateCommonForOrder(Order order, DateTime tradeDay)
        {
            TradeDayCommonResult result = new TradeDayCommonResult();
            OrderDayHistory orderDayHistory = ResetManager.Default.GetOrderDayHistory(order.Id, tradeDay.AddDays(-1));
            result.StoragePerLot = InterestAndStorageCalculater.CalculateStoragePerLot(order, orderDayHistory == null ? 0m : orderDayHistory.StoragePerLot, _info);
            result.InterestPerLot = InterestAndStorageCalculater.CalculateInterestPerLot(order, orderDayHistory == null ? 0m : orderDayHistory.InterestPerLot, _info);
            result.InterestYearDays = _info.Instrument.InterestYearDays;
            result.InterestValueDate = order.InterestValueDate;
            if (order.IsPhysical)
            {
                this.CalculateCommonForPhysicalOrder((PhysicalOrder)order, result);
            }
            return result;
        }

        private void CalculateCommonForPhysicalOrder(PhysicalOrder order, TradeDayCommonResult result)
        {
            if (order.Instalment != null && order.Instalment.Period > 1)
            {
                decimal interestRate, remainsAmount;
                result.InstalmentInterest = PhysicalReset.InstalmentCalculator.CalculateInstalmentInterest(_info, order, out interestRate, out remainsAmount);
            }
        }

    }

    internal static class InterestAndStorageCalculater
    {

#if RESETTEST

        internal static decimal CalculateInterestPerLot(Order order, decimal historyInterestPerLot, TradeDayInfo dayInfo)
        {
            decimal result = historyInterestPerLot;
            if (dayInfo.Settings.IsInterestUseAccountCurrency)
            {
                if (order.InterestValueDate == null || order.InterestValueDate <= dayInfo.TradeDay)
                {
                    if (dayInfo.Instrument.InterestFormula > 0)
                    {
                        result += (decimal)InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Instrument);
                    }
                    else
                    {
                        decimal interestPerLot = (decimal)InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Max);
                        result += interestPerLot.Exchange(1 / dayInfo.RateSetting.RateIn, 1 / dayInfo.RateSetting.RateOut, dayInfo.RateSetting.RoundDecimals.Max, null);
                    }
                }
            }
            else
            {
                if (order.InterestValueDate == null || order.InterestValueDate <= dayInfo.TradeDay)
                {
                    result += (decimal)InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Instrument);
                }
            }
            return result;
        }


        private static float CalculateInterestPerLot(int interestFormula, Price executePrice, Price buyPrice, Price sellPrice, bool useSettlementPriceForInterest, int interestMultiple, int yearDays, decimal interestRateBuy, decimal interestRateSell, bool isBuy, decimal contractSize, int decimals)
        {
            float result = 0f;
            if (useSettlementPriceForInterest && (buyPrice == null || sellPrice == null))
            {
                return result;
            }
            Price price = executePrice;
            if (useSettlementPriceForInterest)
            {
                price = isBuy ? sellPrice : buyPrice;
            }
            decimal interestRate = (isBuy ? interestRateBuy : interestRateSell) / 100;
            switch (interestFormula)
            {
                case 0:
                    result = (float)interestRate * 100;
                    break;
                case 1:
                    result = (float)((interestRate / yearDays) * contractSize);
                    break;
                case 2:
                    result = (float)((interestRate / yearDays) * (contractSize / (decimal)price));
                    break;
                case 3:
                    result = (float)((interestRate / yearDays) * (contractSize * (decimal)price));
                    break;
                default:
                    throw new ArgumentException(string.Format("Illegal interestFormula = {0}", interestFormula));
            }
            return (float)(interestMultiple * Math.Round(result, decimals, MidpointRounding.AwayFromZero));
        }
#else

        internal static decimal CalculateInterestPerLot(Order order, decimal historyInterestPerLot, TradeDayInfo dayInfo)
        {
            decimal result = historyInterestPerLot;
            if (dayInfo.Settings.IsInterestUseAccountCurrency)
            {
                if (order.InterestValueDate == null || order.InterestValueDate <= dayInfo.TradeDay)
                {
                    if (dayInfo.Instrument.InterestFormula > 0)
                    {
                        result += InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Instrument);
                    }
                    else
                    {
                        decimal interestPerLot = InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Max);
                        result += interestPerLot.Exchange(1 / dayInfo.RateSetting.RateIn, 1 / dayInfo.RateSetting.RateOut, dayInfo.RateSetting.RoundDecimals.Max, null);
                    }
                }
            }
            else
            {
                if (order.InterestValueDate == null || order.InterestValueDate <= dayInfo.TradeDay)
                {
                    result += InterestAndStorageCalculater.CalculateInterestPerLot((int)dayInfo.Instrument.InterestFormula, order.ExecutePrice, dayInfo.Settings.BuyPrice, dayInfo.Settings.SellPrice, dayInfo.Settings.IsUseSettlementPriceForInterest, dayInfo.Settings.InterestMultiple, dayInfo.Instrument.InterestYearDays, dayInfo.Settings.InterestRateBuy, dayInfo.Settings.InterestRateSell, order.IsBuy, order.Owner.ContractSize(dayInfo.TradeDay), dayInfo.RateSetting.RoundDecimals.Instrument);
                }
            }
            return result;
        }

        private static decimal CalculateInterestPerLot(int interestFormula, Price executePrice, Price buyPrice, Price sellPrice, bool useSettlementPriceForInterest, int interestMultiple, int yearDays, decimal interestRateBuy, decimal interestRateSell, bool isBuy, decimal contractSize, int decimals)
        {
            decimal result = 0m;
            if (useSettlementPriceForInterest && (buyPrice == null || sellPrice == null))
            {
                return result;
            }
            Price price = executePrice;
            if (useSettlementPriceForInterest)
            {
                price = isBuy ? sellPrice : buyPrice;
            }
            decimal interestRate = (isBuy ? interestRateBuy : interestRateSell) / 100;
            switch (interestFormula)
            {
                case 0:
                    result = interestRate * 100;
                    break;
                case 1:
                    result = (interestRate / yearDays) * contractSize;
                    break;
                case 2:
                    result = (interestRate / yearDays) * (contractSize / (decimal)price);
                    break;
                case 3:
                    result = (interestRate / yearDays) * (contractSize * (decimal)price);
                    break;
                default:
                    throw new ArgumentException(string.Format("Illegal interestFormula = {0}", interestFormula));
            }
            return interestMultiple * Math.Round(result, decimals, MidpointRounding.AwayFromZero);
        }

#endif
        internal static decimal CalculateStoragePerLot(Order order, decimal historyStoragePerLot, TradeDayInfo data)
        {
            decimal result = historyStoragePerLot;
            if (data.Settings.IsInterestUseAccountCurrency)
            {
                decimal storagePerLot = InterestAndStorageCalculater.CalculateStoragePerLot(data.Settings.InterestMultiple, data.Settings.StoragePerLotInterestRateBuy, data.Settings.StoragePerLotInterestRateSell, order.IsBuy);
                result += storagePerLot.Exchange(1 / data.RateSetting.RateIn, 1 / data.RateSetting.RateOut, data.RateSetting.RoundDecimals.Max, null);
            }
            else
            {
                var storagePerLot = InterestAndStorageCalculater.CalculateStoragePerLot(data.Settings.InterestMultiple, data.Settings.StoragePerLotInterestRateBuy, data.Settings.StoragePerLotInterestRateSell, order.IsBuy);
                result += Math.Round(storagePerLot, data.RateSetting.RoundDecimals.Instrument, MidpointRounding.AwayFromZero);

            }
            return result;
        }

        private static decimal CalculateStoragePerLot(int interestMultiple, decimal storagePerLotBuyInterestRate, decimal storagePerLotSellInterestRate, bool isBuy)
        {
            decimal interestRate = isBuy ? storagePerLotBuyInterestRate : storagePerLotSellInterestRate;
            return interestMultiple * interestRate;
        }

    }

    internal sealed class OrderPhaseUpdater
    {
        private IEnumerable<Order> _orders;
        private DateTime _resetTime;
        private Guid _instrumentId;
        private DateTime _tradeDay;

        internal OrderPhaseUpdater(IEnumerable<Order> orders, DateTime resetTime, Guid instrumentId, DateTime tradeDay)
        {
            _orders = orders;
            _resetTime = resetTime;
            _instrumentId = instrumentId;
            _tradeDay = tradeDay;
        }

        internal void Update()
        {
            foreach (var eachOrder in _orders)
            {
                eachOrder.UpdateCloseOrderPhase(_tradeDay, _instrumentId, _resetTime);
            }
        }


    }



}
