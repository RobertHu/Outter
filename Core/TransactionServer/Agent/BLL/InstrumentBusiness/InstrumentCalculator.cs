using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Util;
using iExchange.Common;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Quotations;
using System.Data;
using log4net;

namespace Core.TransactionServer.Agent.BLL.InstrumentBusiness
{
    public struct NecessaryAndQuantity
    {
        internal decimal buyNecessarySum;
        internal decimal sellNecessarySum;
        internal decimal buyQuantitySum;
        internal decimal sellQuantitySum;
        internal decimal partialPhysicalNecessarySum;
        internal bool needCalculateNecessary;

        internal bool IsEmpty
        {
            get
            {
                return this.buyNecessarySum == 0 && this.sellNecessarySum == 0 && this.buyQuantitySum == 0 && this.sellQuantitySum == 0;
            }
        }


    }

    internal enum CalculateType
    {
        CheckRiskForQuotation,
        CheckRisk,
        CheckRiskForInit
    }

    internal abstract class InstrumentCalculator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentCalculator));

        protected AccountClass.Instrument _owner;
        protected RiskData _riskData;

        protected InstrumentCalculator(AccountClass.Instrument owner)
        {
            _owner = owner;
            _riskData = _owner.RiskRawData;
        }

        public virtual void Calculate(DateTime baseTime, CalculateType calculateType, Quotation quotation)
        {
            if (_owner.ExecutedAndHasPositionOrders.Count == 0)
            {
                _riskData.Clear();
                return;
            }
            _riskData.ClearFloatingPL();
            NecessaryAndQuantity necessaryAndQuantity = new NecessaryAndQuantity();
            this.CalculateFloatPL(ref necessaryAndQuantity, quotation);
            if (calculateType != CalculateType.CheckRiskForQuotation || necessaryAndQuantity.needCalculateNecessary)
            {
                _riskData.ClearNecessary();
                this.CalculateNecessary(ref necessaryAndQuantity, baseTime);
            }

            if (this.ShouldCalculateLockOrderTradePLFloat(_owner.Owner.Setting(), necessaryAndQuantity.buyQuantitySum, necessaryAndQuantity.sellQuantitySum))
            {
                _riskData.LockOrderTradePLFloat = this.CalculateLockOrderTradePLFloat(necessaryAndQuantity.buyQuantitySum, necessaryAndQuantity.sellQuantitySum);
            }
            _riskData.RiskCredit = this.CalculateRiskCredit();
        }



        internal void CalculateFeeForCutting()
        {
            _riskData.FeeForCutting = _owner.CuttingFee.Calculate();
        }


        protected abstract bool ShouldCalculateLockOrderTradePLFloat(Settings.Account account, decimal buyQuantitySum, decimal sellQuantitySum);

        private void CalculateNecessary(ref NecessaryAndQuantity necessaryAndQuantity, DateTime baseTime)
        {
            decimal netNecessary = 0, hedgeNecessary = 0;
            this.CalculateNetAndHedgeNecessary(necessaryAndQuantity.buyNecessarySum, necessaryAndQuantity.sellNecessarySum, necessaryAndQuantity.buyQuantitySum, necessaryAndQuantity.sellQuantitySum,
                ref necessaryAndQuantity.partialPhysicalNecessarySum, out netNecessary, out hedgeNecessary);
            _riskData.NetNecessary = netNecessary;
            _riskData.HedgeNecessary = hedgeNecessary;
            TradePolicy tradePolicy = _owner.Owner.Setting().TradePolicy();
            int decimals = Math.Min(_owner.Currency(null).Decimals, this.GetTradePolicyDetail().NecessaryRound);
            _riskData.Necessary = Math.Round(netNecessary + hedgeNecessary, decimals, MidpointRounding.AwayFromZero);
            this.CalculateMinEquitities(netNecessary, hedgeNecessary, baseTime, decimals);
            _riskData.NecessaryFillingOpenOrder = Math.Round(tradePolicy.OpenNecessaryPolicy.Calculate(netNecessary, hedgeNecessary), decimals, MidpointRounding.AwayFromZero);
            _riskData.NecessaryFillingCloseOrder = Math.Round(tradePolicy.CloseNecessaryPolicy.Calculate(netNecessary, hedgeNecessary), decimals, MidpointRounding.AwayFromZero);
            _riskData.PartialPaymentPhysicalNecessary = necessaryAndQuantity.partialPhysicalNecessarySum;
        }

        private TradePolicyDetail GetTradePolicyDetail()
        {
            return MissedTradePolicyDetailManager.Get(_owner);
        }


        private void CalculateMinEquitities(decimal netNecessary, decimal hedgeNecessary, DateTime baseTime, int decimals)
        {
            TradePolicyDetail tradePolicyDetail = this.GetTradePolicyDetail();
            bool useNightNecessaryWhenBreak = Settings.Setting.Default.SystemParameter.UseNightNecessaryWhenBreak;
            DateTime tradeDayBeginTime = Settings.Setting.Default.SystemParameter.TradeDayBeginTime;
            bool useAlertLevel4 = this.ShouldUseAlertLevel4(baseTime, useNightNecessaryWhenBreak, tradeDayBeginTime);
            decimal alertLevel3 = useAlertLevel4 && tradePolicyDetail.AlertLevel4 > 0 ? tradePolicyDetail.AlertLevel4 : tradePolicyDetail.AlertLevel3;
            decimal alertLevel3Lock = useAlertLevel4 && tradePolicyDetail.AlertLevel4Lock > 0 ? tradePolicyDetail.AlertLevel4Lock : tradePolicyDetail.AlertLevel3Lock;
            _riskData.MinEquityAvoidRiskLevel1 = this.CalculateMinEquity(netNecessary, hedgeNecessary, decimals, tradePolicyDetail.AlertLevel1, tradePolicyDetail.AlertLevel1Lock);
            _riskData.MinEquityAvoidRiskLevel2 = this.CalculateMinEquity(netNecessary, hedgeNecessary, decimals, tradePolicyDetail.AlertLevel2, tradePolicyDetail.AlertLevel2Lock);
            _riskData.MinEquityAvoidRiskLevel3 = this.CalculateMinEquity(netNecessary, hedgeNecessary, decimals, tradePolicyDetail.AlertLevel3, tradePolicyDetail.AlertLevel3Lock);
        }

        private decimal CalculateMinEquity(decimal netNecessary, decimal hedgeNecessary, int decimals, decimal alertLevelCost, decimal alertLevelLockCost)
        {
            var equity = netNecessary * alertLevelCost + hedgeNecessary * alertLevelLockCost;
            return Math.Round(equity, decimals, MidpointRounding.AwayFromZero);
        }

        private bool ShouldUseAlertLevel4(DateTime baseTime, bool useNightNecessaryWhenBreak, DateTime tradeDayBeginDateTime)
        {
            if (_owner.Setting.IsExpired || baseTime < _owner.Setting.DayCloseTime)
            {
                return false;
            }
            else
            {
                return baseTime >= _owner.Setting.DayCloseTime && _owner.Setting.UseAlertLevel4WhenClosed;
            }
        }


        private void CalculateFloatPL(ref NecessaryAndQuantity necessaryAndQuantity, Quotation quotation)
        {
            if (!this.DoCalculateFloatPL(ref necessaryAndQuantity, quotation))
            {
                _owner.InvalidateCache();
                this.DoCalculateFloatPL(ref necessaryAndQuantity, quotation);
            }
        }


        private bool DoCalculateFloatPL(ref NecessaryAndQuantity necessaryAndQuantity, Quotation quotation)
        {
            foreach (Order order in _owner.ExecutedAndHasPositionOrders)
            {
                if (order.Phase != OrderPhase.Executed || order.ExecutePrice == null)
                {
                    Logger.ErrorFormat("order.Id = {0}, tranId ={1}, accountId = {2} order.Phase = {3}, order.ExecutePrice = {4} invalid order", order.Id, order.Owner.Id, order.Owner.AccountId,
                        order.Phase, order.ExecutePrice);
                    return false;
                }
                this.CalculateOrderFloatPL(order, ref necessaryAndQuantity, quotation);
            }
            return true;
        }


        protected virtual void CalculateOrderFloatPL(Order order, ref NecessaryAndQuantity necessaryAndQuantity, Quotation quotation)
        {
            var oldNecessary = order.Necessary;
            order.CalculateFloatPL(quotation);
            necessaryAndQuantity.needCalculateNecessary |= this.IsNecessaryChanged(oldNecessary, order);
            this.AddUpNecessaryAndQuantity(order, ref necessaryAndQuantity);
            this.AddUpCalculatedFloatPL(order);
        }

        protected abstract void AddUpNecessaryAndQuantity(Order order, ref NecessaryAndQuantity necessaryAndQuantity);

        private bool IsNecessaryChanged(decimal oldNecessary, Order order)
        {
            return order.Necessary != oldNecessary;
        }

        protected virtual void AddUpCalculatedFloatPL(Order order)
        {
            _riskData.TradePLFloat += order.TradePLFloat;
            _riskData.InterestPLFloat += order.InterestPLFloat;
            _riskData.StoragePLFloat += order.StoragePLFloat;
        }

        public void CalculateNetAndHedgeNecessary(decimal buyNecessarySum, decimal sellNecessarySum,
            decimal buyQuantitySum, decimal sellQuantitySum, ref decimal partialPhysicalNecessarySum,
            out decimal netNecessary, out decimal hedgeNecessary)
        {
            bool shouldUseDayNecessary = _owner.Trading.ShouldUseDayNecessary;

            partialPhysicalNecessarySum = partialPhysicalNecessarySum * this.GetTradePolicyDetail().PartPaidPhysicalNecessary
                * (shouldUseDayNecessary ? _owner.Owner.Setting().RateMarginD : _owner.Owner.Setting().RateMarginO);

            netNecessary = hedgeNecessary = 0;

            MarginFormula marginFormula = _owner.Setting.MarginFormula;

            if (marginFormula == MarginFormula.CSiPrice || marginFormula == MarginFormula.CSxPrice
                || marginFormula == MarginFormula.CSiMarketPrice || marginFormula == MarginFormula.CSxMarketPrice)
            {
                decimal buyNecessaryAvarage = buyQuantitySum == 0 ? 0 : buyNecessarySum / buyQuantitySum;
                decimal sellNecessaryAvarage = sellQuantitySum == 0 ? 0 : sellNecessarySum / sellQuantitySum;
                decimal hedgeQuantity = Math.Min(buyQuantitySum, sellQuantitySum);

                decimal netQuantity = Math.Abs(buyQuantitySum - sellQuantitySum);
                decimal netNecessaryAvarage = buyQuantitySum > sellQuantitySum ? buyNecessaryAvarage : sellNecessaryAvarage;

                if (shouldUseDayNecessary)
                {
                    netNecessary = _owner.Owner.Setting().RateMarginD * this.GetTradePolicyDetail().MarginD * netQuantity * netNecessaryAvarage;
                    hedgeNecessary = _owner.Owner.Setting().RateMarginLockD * this.GetTradePolicyDetail().MarginLockedD * hedgeQuantity * (buyNecessaryAvarage + sellNecessaryAvarage);
                }
                else
                {
                    netNecessary = _owner.Owner.Setting().RateMarginO * this.GetTradePolicyDetail().MarginO * netQuantity * netNecessaryAvarage;
                    hedgeNecessary = _owner.Owner.Setting().RateMarginLockO * this.GetTradePolicyDetail().MarginLockedO * hedgeQuantity * (buyNecessaryAvarage + sellNecessaryAvarage);
                }
            }
            else if (marginFormula == MarginFormula.FixedAmount || marginFormula == MarginFormula.CS)
            {
                if (shouldUseDayNecessary)
                {
                    if (marginFormula == 0 && this.GetTradePolicyDetail().VolumeNecessaryId != null)
                    {
                        netNecessary = this.GetTradePolicyDetail().VolumeNecessary.CalculateNecessary(_owner.Owner.Setting().RateMarginD, this.GetTradePolicyDetail().MarginD, Math.Abs(buyNecessarySum - sellNecessarySum), true);
                    }
                    else
                    {
                        netNecessary = _owner.Owner.Setting().RateMarginD * this.GetTradePolicyDetail().MarginD * Math.Abs(buyNecessarySum - sellNecessarySum);
                    }
                    hedgeNecessary = _owner.Owner.Setting().RateMarginLockD * this.GetTradePolicyDetail().MarginLockedD * Math.Min(buyNecessarySum, sellNecessarySum);
                }
                else
                {
                    if (marginFormula == 0 && this.GetTradePolicyDetail().VolumeNecessaryId != null)
                    {
                        netNecessary = this.GetTradePolicyDetail().VolumeNecessary.CalculateNecessary(_owner.Owner.Setting().RateMarginO, this.GetTradePolicyDetail().MarginO, Math.Abs(buyNecessarySum - sellNecessarySum), false);
                    }
                    else
                    {
                        netNecessary = _owner.Owner.Setting().RateMarginO * this.GetTradePolicyDetail().MarginO * Math.Abs(buyNecessarySum - sellNecessarySum);
                    }
                    hedgeNecessary = _owner.Owner.Setting().RateMarginLockO * this.GetTradePolicyDetail().MarginLockedO * Math.Min(buyNecessarySum, sellNecessarySum);
                }
            }
            else
            {
                throw new NotSupportedException(string.Format("Invalid Instrument MarginFormula found, Instrument Id:{0}, MarginFormula{1}", _owner.Id, marginFormula));
            }

            netNecessary += partialPhysicalNecessarySum;
        }

        /// <summary>
        /// 计算锁仓单成交产生的盈亏
        /// </summary>
        private decimal CalculateLockOrderTradePLFloat(decimal buyQuantitySum, decimal sellQuantitySum)
        {
            bool isBuyForLockOrder = (buyQuantitySum < sellQuantitySum);
            decimal quantityForLockOrder = Math.Abs(buyQuantitySum - sellQuantitySum);
            Quotation quotation = _owner.GetQuotation();
            Price buy = quotation.BuyPrice, sell = quotation.SellPrice;

            Price close, executePrice;
            if (isBuyForLockOrder)
            {
                executePrice = sell;
                close = buy;

                buy = executePrice;
                sell = close;
            }
            else
            {
                executePrice = buy;
                close = sell;

                buy = close;
                sell = executePrice;
            }

            return TradePLCalculator.Calculate(_owner.Setting.TradePLFormula, quantityForLockOrder, (decimal)buy, (decimal)sell, (decimal)close, _owner.CurrencyRate());
        }

        private decimal CalculateRiskCredit()
        {
            decimal netLot = Math.Abs(_owner.TotalBuyLotBalance - _owner.TotalSellLotBalance);
            return netLot * this.GetTradePolicyDetail().RiskCredit;
        }


        public virtual decimal CalculateBuyMargin()
        {
            return this.CalculateTotalMarginCommon(m => this.ShouldCalculateMargin(true, m));
        }

        public virtual decimal CalculateSellMargin()
        {
            return this.CalculateTotalMarginCommon(m => this.ShouldCalculateMargin(false, m));
        }

        protected decimal CalculateTotalMarginCommon(Predicate<Order> predicate)
        {
            decimal totalMargin = 0m;
            foreach (Order order in _owner.ExecutedAndHasPositionOrders)
            {
                if (predicate(order))
                {
                    totalMargin += NecessaryCalculator.CalculateNecessary(order, null, order.LotBalance, null);
                }
            }
            return totalMargin;
        }

        protected virtual bool ShouldCalculateMargin(bool isBuy, Order order)
        {
            return order.IsBuy == isBuy;
        }

    }

    internal class GeneralInstrumentCalculator : InstrumentCalculator
    {
        internal GeneralInstrumentCalculator(AccountClass.Instrument owner)
            : base(owner)
        {
        }

        protected override bool ShouldCalculateLockOrderTradePLFloat(Settings.Account account, decimal buyQuantitySum, decimal sellQuantitySum)
        {
            return buyQuantitySum != sellQuantitySum && account.IsAutoCut && account.RiskLevelAction == RiskLevelAction.HedgeExact;
        }

        protected override void AddUpNecessaryAndQuantity(Order order, ref NecessaryAndQuantity necessaryAndQuantity)
        {
            if (order.IsBuy)
            {
                necessaryAndQuantity.buyNecessarySum += order.Necessary;
                necessaryAndQuantity.buyQuantitySum += order.QuantityBalance;
            }
            else
            {
                necessaryAndQuantity.sellNecessarySum += order.Necessary;
                necessaryAndQuantity.sellQuantitySum += order.QuantityBalance;
            }
        }
    }

}
