using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.TypeExtensions;
using Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions;
using log4net;
using Protocal;
using Protocal.CommonSetting;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    internal abstract class OrderPlaceVerifierBase : OrderVerifierBase
    {
        protected virtual OrderNetVerifier OrderNetVerifier
        {
            get { return OrderNetVerifier.Default; }
        }

        protected override void InnerVerify(Order order, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            this.VerifyInstrumentAcceptableOrderType(order, context);
            Order amendedOrder = order.Owner.AmendedOrder;
            if (amendedOrder != null && this.IsTradeOptionAndSetPriceEqual(order, amendedOrder)) return;
            base.InnerVerify(order, isPlaceByRiskMonitor, appType, context);
            this.VerifyCommon(order, isPlaceByRiskMonitor, appType);
        }

        protected abstract void VerifySettings(Order order);

        private void VerifyCommon(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            var tran = order.Owner;
            if (order.Lot <= 0) throw new TransactionServerException(TransactionError.InvalidLotBalance, "The lot is <= 0");

            if (tran.Type == TransactionType.Pair && order.IsOpen)
            {
                throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable, "Pair order must be close orde");
            }
            this.VerifySettings(order);
            this.VerifyDealingPolicyDetail(order, isPlaceByRiskMonitor);
            this.VerifyPrice(order, appType);
        }

        protected override bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return base.ShouldVerifyBPointAndMaxDQLot(order, isPlaceByRiskMonitor, appType) && order.IsOpen && InstrumentPriceStatusManager.Default.IsOnBPoint(order.Owner.SettingInstrument().OriginCode);
        }

        protected override void VerifyBPointAndMaxDQLot(Order order, PlaceContext context)
        {
            this.VerifyNetLot(order);
            this.VerifyMaxDQLot(order, context);
        }

        private void VerifyNetLot(Order order)
        {
            decimal netLot = 0;
            var instrument = order.Owner.AccountInstrument;
            if (order.IsBuy)
                netLot = instrument.TotalSellLotBalance - instrument.TotalBuyLotBalance;
            else
                netLot = instrument.TotalBuyLotBalance - instrument.TotalSellLotBalance;
            if (order.Lot > netLot) throw new TransactionServerException(TransactionError.OpenOrderIsNotAllowedAtBPoint);
        }


        private void VerifyDealingPolicyDetail(Order order, bool isPlaceByRiskMonitor)
        {
            var tran = order.Owner;
            var dealingPolicyDetail = tran.DealingPolicyPayload();
            if (dealingPolicyDetail.PlaceSptMktTimeSpan > TimeSpan.Zero && !isPlaceByRiskMonitor
                && (tran.OrderType == OrderType.SpotTrade || tran.OrderType == OrderType.Market))
            {
                DateTime lastExecuteTime = tran.Owner.GetLastExecuteTimeForPlacing(tran.InstrumentId);
                if ((this.BaseTime - lastExecuteTime) < dealingPolicyDetail.PlaceSptMktTimeSpan)
                {
                    string errorInfo = string.Format("Order cancelled, order requires {0} seconds from last trade", dealingPolicyDetail.PlaceSptMktTimeSpan.TotalSeconds);
                    throw new TransactionServerException(TransactionError.TimeSpanBetweenOrders, errorInfo);
                }
            }
        }


        private bool IsTradeOptionAndSetPriceEqual(Order order, Order amendedOrder)
        {
            if (amendedOrder.TradeOption == order.TradeOption && amendedOrder.SetPrice == order.SetPrice)
            {
                return true;
            }

            if (order.Owner.Type == TransactionType.OneCancelOther && amendedOrder.Owner.OrderCount == 2)
            {
                Order theOtherOrder = amendedOrder == amendedOrder.Owner.FirstOrder ? amendedOrder.Owner.SecondOrder : amendedOrder.Owner.FirstOrder;
                if (theOtherOrder.TradeOption == order.TradeOption && theOtherOrder.SetPrice == order.SetPrice)
                {
                    return true;
                }
            }
            return false;
        }


        protected virtual void VerifyInstrumentAcceptableOrderType(Order order, PlaceContext context)
        {
            var tran = order.Owner;
            if (tran.Type == TransactionType.OneCancelOther && order.IsOpen)//OCO Open order, check with tradepolicy
            {
                if (!tran.TradePolicyDetail(context.TradeDay).AllowNewOCO)
                {
                    throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
                }
            }
            else if (!tran.SettingInstrument(context.TradeDay).IsTypeAcceptable(tran.Type, tran.OrderType) ||
                (tran.Owner.Setting(context.TradeDay).Type == AccountType.Agent && tran.OrderType != OrderType.SpotTrade))
            {
                throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
            }
        }

        protected void VerifyPlaceSettings(Order order, PlaceContext context)
        {
            Transaction tran = order.Owner;
            Settings.Instrument instrument = order.Instrument(context.TradeDay);
            DateTime baseTime = Market.MarketManager.Now;

            if (tran.Type == TransactionType.OneCancelOther && tran.OrderCount != 2)
            {
                throw new TransactionServerException(TransactionError.InvalidRelation, "OCO transaction should have 2 orders");
            }

            if (!order.IsOpen)
            {
                CloseOrderVerifier.Default.Verify(order);
            }

            this.VerifyAmendOrder(order);

            if (tran.Type == TransactionType.OneCancelOther && order.IsOpen)
            {
                if (!tran.TradePolicyDetail(context.TradeDay).AllowNewOCO)
                {
                    throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable, "New OCO order is not allowed by TradePolicy");
                }
            }
            else
            {

                if (!instrument.IsTypeAcceptable(tran.Type, order.OrderType) || (tran.Owner.Setting(context.TradeDay).Type == AccountType.Agent && tran.OrderType != OrderType.SpotTrade))
                {
                    throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable, "Order type is not allowd by the instrument");
                }
            }

            if ((tran.OrderType == OrderType.SpotTrade || order.TradeOption != TradeOption.Invalid) && order.SetPrice == null)
            {
                throw new TransactionServerException(TransactionError.InvalidPrice, "SetPrice is null");
            }
        }

        private void VerifyAmendOrder(Order order)
        {
            var tran = order.Owner;
            if (tran.AmendedOrder != null && !tran.AmendedOrder.Owner.CanAmend)
            {
                throw new TransactionServerException(TransactionError.InitialOrderCanNotBeAmended, string.Format("The amended order is {0}", tran.AmendedOrder.Owner.Phase));
            }

            if (tran.SubType == TransactionSubType.Amend && tran.AmendedOrder == null)
            {
                throw new TransactionServerException(TransactionError.AmendedOrderNotFound, "Can't found the amended order");
            }
        }


        protected override bool IsTimingAcceptable(DateTime baseTime, Transaction tran, PlaceContext context)
        {
            bool isTimingAcceptable = tran.EndTime > baseTime;

            switch (tran.OrderType)
            {
                case OrderType.MarketOnOpen:
                    isTimingAcceptable = true;
                    break;
                case OrderType.MarketOnClose:
                    string errorDetail;
                    isTimingAcceptable = tran.TradingInstrument.CanPlace(tran.SubmitTime, (OrderType.MarketOnClose).IsPendingType(), tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider), context, out errorDetail);
                    if (!isTimingAcceptable)
                    {
                        var tradeDay = Settings.Setting.Default.GetTradeDay(context.TradeDay);
                        var systemParameter = Settings.Setting.Default.SystemParameter;
                        isTimingAcceptable = (tran.SubmitTime >= tran.SettingInstrument(context.TradeDay).DayOpenTime &&
                            tran.SubmitTime <= tran.SettingInstrument(context.TradeDay).DayCloseTime.AddMinutes(0 - systemParameter.MooMocAcceptDuration));
                    }
                    break;
            }
            return isTimingAcceptable;
        }

        protected override void VerifyInstrumentCanPlaceAndTrade(Transaction tran, PlaceContext context)
        {
            string errorDetail;
            if ((!tran.TradingInstrument.CanPlace(this.BaseTime, tran.IsPending, tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider), context, out errorDetail) || (tran.OrderType != OrderType.Market && !tran.AccountInstrument.HasTradePrice(tran.SubmitorQuotePolicyProvider))))
            {
                var instrument = tran.SettingInstrument(context.TradeDay);
                this.Logger.WarnFormat("TranId {0} is canceled because instrument is not placing , isActive = {1}", tran.Id, instrument.IsActive);
                throw new TransactionServerException(TransactionError.InstrumentIsNotAccepting, errorDetail);
            }
        }

        private void VerifyPrice(Order order, AppType appType)
        {
            Transaction tran = order.Owner;
            if (!this.ShouldVerifyPrice(tran, appType)) return;
            Price buy, sell, comparePrice;
            DateTime priceTimestamp;
            IQuotePolicyProvider quotePolicyProvider = order.Owner.SubmitorQuotePolicyProvider;
            Logger.InfoFormat("quotePolicyId = {0}, accountId = {1}, submitorId = {2}, accountQuotePolicyId = {3}, customerQuotePolicyId = {4}", quotePolicyProvider.PrivateQuotePolicyId , order.AccountId, order.Owner.SubmitorId, order.Account.Setting().QuotePolicyID, order.Account.Customer.PrivateQuotePolicyId);
            var quotation = tran.AccountInstrument.GetQuotation(quotePolicyProvider);
            buy = quotation.BuyPrice;
            sell = quotation.SellPrice;
            priceTimestamp = quotation.Timestamp;
            comparePrice = order.IsBuy ? sell : buy;
            this.InnerVerifyPrice(order, buy, sell, comparePrice, priceTimestamp, appType);
        }

        protected virtual void InnerVerifyPrice(Order order, Price buy, Price sell, Price comparePrice, DateTime priceTimestamp, AppType appType)
        {
            var tran = order.Owner;
            if (tran.OrderType == OrderType.SpotTrade)
            {
                order.VerifySportOrderPrice(buy, sell, comparePrice, priceTimestamp);
            }
            else if (tran.OrderType == OrderType.Limit || tran.OrderType == OrderType.OneCancelOther)
            {
                order.VerifyLimitAndOCOOrderPrice(buy, sell, comparePrice, priceTimestamp, appType, this.OrderNetVerifier);
            }
        }



        private bool ShouldVerifyPrice(Transaction tran, AppType appType)
        {
            return tran.SubType != TransactionSubType.Match && !(appType == AppType.RiskMonitor || tran.IsFreeOfPriceCheck(false));
        }
    }

    internal sealed class OrderPlaceVerifier : OrderPlaceVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(OrderPlaceVerifier));

        public static readonly OrderPlaceVerifier Default = new OrderPlaceVerifier();
        private OrderPlaceVerifier() { }
        static OrderPlaceVerifier() { }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return TradeSidePlaceVerifier.Default;
        }

        protected override void InnerVerify(Order order, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            base.InnerVerify(order, isPlaceByRiskMonitor, appType, context);
            this.VerifyPlaceSettings(order, context);
        }


        protected override void VerifySettings(Order order)
        {
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal sealed class BOOrderPlaceVerifier : OrderPlaceVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(BOOrderPlaceVerifier));

        internal static readonly BOOrderPlaceVerifier Default = new BOOrderPlaceVerifier();
        private BOOrderPlaceVerifier() { }

        protected override OrderNetVerifier OrderNetVerifier
        {
            get
            {
                return BOOrderNetVerifier.Default;
            }
        }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return BOOrderTradeSideVerifier.Default;
        }

        protected override void VerifyInstrumentAcceptableOrderType(Order order, PlaceContext context)
        {
        }

        protected override bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return false;
        }

        protected override void VerifySettings(Order order)
        {
            BinaryOption.OrderPlaceValidator.Default.Validate((BinaryOption.Order)order);
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        protected override bool ShouldVerifyInstrumentCanPlaceAndTrade(Transaction tran)
        {
            return false;
        }

        protected override void InnerVerifyPrice(Order order, Price buy, Price sell, Price comparePrice, DateTime priceTimestamp, AppType appType)
        {
            comparePrice = buy;
            var dealingPolicyDetail = order.Owner.DealingPolicyPayload();
            if (order.SetPrice != null && Math.Abs(order.SetPrice - comparePrice) > dealingPolicyDetail.AcceptDQVariation)
            {
                order.JudgePrice = comparePrice;
                order.JudgePriceTimestamp = priceTimestamp;

                string errorDetail = string.Format("comparePrice={0}, setPrice={1}, acceptDQVariation={2}, comparePriceTime={3}",
                    comparePrice, order.SetPrice, dealingPolicyDetail.AcceptDQVariation, priceTimestamp);
                throw new TransactionServerException(TransactionError.OutOfAcceptDQVariation, errorDetail);
            }
        }

    }

    internal sealed class PhysicalOrderPlaceVerifier : OrderPlaceVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(PhysicalOrderPlaceVerifier));

        public static readonly PhysicalOrderPlaceVerifier Default = new PhysicalOrderPlaceVerifier();

        private PhysicalOrderPlaceVerifier() { }
        static PhysicalOrderPlaceVerifier() { }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return PhysicalOrderTradeSideExecuteVerifier.Default;
        }

        protected override void InnerVerify(Order order, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            base.InnerVerify(order, isPlaceByRiskMonitor, appType, context);
            this.VerifyPlaceSettings(order, context);
        }

        protected override bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return base.ShouldVerifyBPointAndMaxDQLot(order, isPlaceByRiskMonitor, appType) && PhysicalVerifier.ShouldVerifyBPointAndMaxDQLot(order);
        }

        protected override void VerifySettings(Order order)
        {
            PhysicalVerifier.VerifyInstalmentPolicy((Physical.PhysicalOrder)order);
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }


    internal static class PhysicalVerifier
    {
        internal static void VerifyInstalmentPolicy(Physical.PhysicalOrder order)
        {
            if (order.Instalment != null)
            {
                InstalmentPolicyDetail instalmentPolicyDetail = order.Instalment.InstalmentPolicyDetail(null);
                if (!instalmentPolicyDetail.IsActive)
                {
                    throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable, "Instalment policy is not active");
                }
            }
        }

        internal static bool ShouldVerifyBPointAndMaxDQLot(Order order)
        {
            var physicalOrder = (Physical.PhysicalOrder)order;
            return physicalOrder.PhysicalTradeSide != PhysicalTradeSide.Deposit && physicalOrder.PhysicalTradeSide != PhysicalTradeSide.Delivery;
        }

    }

    internal sealed class CloseOrderVerifier
    {
        public static readonly CloseOrderVerifier Default = new CloseOrderVerifier();
        private CloseOrderVerifier() { }
        static CloseOrderVerifier() { }

        internal void Verify(Order order)
        {
            if (!this.ShouldVerify(order)) return;
            if (order.OrderRelations == null || order.OrderRelations.Count() == 0)
            {
                throw new TransactionServerException(TransactionError.InvalidOrderRelation, "Close order should have more than 1 order realtion at least");
            }
            this.ValidCloseLot(order);
        }

        private bool ShouldVerify(Order order)
        {
            var tran = order.Owner;
            if (tran.SubType == TransactionSubType.Amend || tran.SubType == TransactionSubType.Assign) return false;
            if (tran.OrderType == OrderType.Limit || tran.OrderType == OrderType.OneCancelOther) return true;
            return false;
        }


        private void ValidCloseLot(Order order)
        {
            foreach (OrderRelation orderRelation in order.OrderRelations)
            {
                Order openOrder = orderRelation.OpenOrder;
                if (openOrder == null)
                {
                    throw new TransactionServerException(TransactionError.OpenOrderNotExists);
                }
                this.ValidOpenOrderCanBeClosedLot(order, openOrder, orderRelation.ClosedLot);
            }
        }

        private void ValidOpenOrderCanBeClosedLot(Order originCloseOrder, Order openOrder, decimal closedLot)
        {
            decimal canBeClosedLot = openOrder.CanBeClosedLot;
            foreach (OrderRelation orderRelation in openOrder.OrderRelations)
            {
                canBeClosedLot -= GetClosedLot(orderRelation, originCloseOrder);
            }
            if (closedLot > canBeClosedLot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
            }
        }

        private decimal GetClosedLot(OrderRelation orderRelation, Order originCloseOrder)
        {
            return this.ShouldCalculateClosedLot(orderRelation, originCloseOrder) ? orderRelation.ClosedLot : 0m;
        }

        private bool ShouldCalculateClosedLot(OrderRelation orderRelation, Order originCloseOrder)
        {
            var closeOrder = orderRelation.CloseOrder;
            return this.IsOrderPhaseIsOk(closeOrder, originCloseOrder) && this.IsTheSameTradeOption(closeOrder, originCloseOrder);
        }

        private bool IsOrderPhaseIsOk(Order closeOrder, Order originCloseOrder)
        {
            return closeOrder != originCloseOrder
               && closeOrder.Phase != OrderPhase.Canceled
               && closeOrder.Phase != OrderPhase.Executed
               && closeOrder.Phase != OrderPhase.Completed
               && closeOrder.Phase != OrderPhase.Deleted;
        }


        private bool IsTheSameTradeOption(Order closeOrder, Order originCloseOrder)
        {
            return (closeOrder.Owner.OrderType == OrderType.Limit || closeOrder.Owner.OrderType == OrderType.OneCancelOther)
                  && closeOrder.TradeOption == originCloseOrder.TradeOption;
        }


    }

}
