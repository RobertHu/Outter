using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryOption = Core.TransactionServer.Agent.BinaryOption;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal abstract class TradeSideVerifierBase
    {
        internal void Verify(Order order, PlaceContext context)
        {
            this.InnerVerify(order, context);
        }

        protected abstract void InnerVerify(Order order, PlaceContext context);

        protected void VerifyCommon(Order order, PlaceContext context)
        {
            if (order.IsSpotOrder)
            {
                this.VerifySpotOrderTradeSide(order, context);
            }
            if (order.IsOpen)
            {
                this.VerifyOpenOrderTradeSide(order, context);
            }
        }

        private void VerifySpotOrderTradeSide(Order order, PlaceContext context)
        {
            var instrument = order.Instrument(context.TradeDay);
            var buySideNotAllowed = order.IsBuy && (instrument.AllowedSpotTradeOrderSides & AllowedOrderSides.Buy) != AllowedOrderSides.Buy;
            var sellSideNotAllowed = !order.IsBuy && (instrument.AllowedSpotTradeOrderSides & AllowedOrderSides.Sell) != AllowedOrderSides.Sell;
            if (buySideNotAllowed || sellSideNotAllowed)
            {
                throw new TransactionServerException(TransactionError.PriceChangedSincePlace, this.GetErrorMsg(order, context));
            }
        }

        private string GetErrorMsg(Order order, PlaceContext context)
        {
            return string.Format("Order side is not allowed, AllowedSpotTradeOrderSides={0}, AllowedNewTradeSides={1}", order.Instrument(context.TradeDay).AllowedSpotTradeOrderSides, order.Owner.DealingPolicyPayload(context.TradeDay).AllowedNewTradeSides);
        }


        private void VerifyOpenOrderTradeSide(Order order, PlaceContext context)
        {
            var buySideNotAllowed = order.IsBuy && (order.Owner.DealingPolicyPayload(context.TradeDay).AllowedNewTradeSides & AllowedOrderSides.Buy) != AllowedOrderSides.Buy;
            var sellSideNotAllowed = !order.IsBuy && (order.Owner.DealingPolicyPayload(context.TradeDay).AllowedNewTradeSides & AllowedOrderSides.Sell) != AllowedOrderSides.Sell;
            if (buySideNotAllowed || sellSideNotAllowed)
            {
                var error = order.IsSpotOrder ? TransactionError.PriceChangedSincePlace : TransactionError.OrderTypeIsNotAcceptable;
                throw new TransactionServerException(error, this.GetErrorMsg(order, context));
            }
        }

    }


    internal sealed class TradeSidePlaceVerifier : TradeSideVerifierBase
    {
        internal static readonly TradeSidePlaceVerifier Default = new TradeSidePlaceVerifier();

        static TradeSidePlaceVerifier() { }
        private TradeSidePlaceVerifier() { }

        protected override void InnerVerify(Order order, PlaceContext context)
        {
            if (!order.Owner.Owner.IsAutoClose)
            {
                this.VerifyCommon(order, context);
            }
        }
    }

    internal sealed class TradeSideExecuteVerifier : TradeSideVerifierBase
    {
        internal static readonly TradeSideExecuteVerifier Default = new TradeSideExecuteVerifier();

        static TradeSideExecuteVerifier() { }
        private TradeSideExecuteVerifier() { }

        protected override void InnerVerify(Order order, PlaceContext context)
        {
            if (order.Owner.Owner.IsAutoClose)
            {
                this.VerifyCommon(order, context);
            }
        }
    }


    internal sealed class BOOrderTradeSideVerifier : TradeSideVerifierBase
    {
        internal static readonly BOOrderTradeSideVerifier Default = new BOOrderTradeSideVerifier();

        private BOOrderTradeSideVerifier() { }

        protected override void InnerVerify(Order order, PlaceContext context)
        {
        }
    }


    internal abstract class PhysicalOrderTradeSideVerifierBase : TradeSideVerifierBase
    {
        protected void VerifyPhysicalTradeSide(Order order)
        {
            var physicalOrder = (PhysicalOrder)order;
            var tradePolicyDetail = order.Owner.TradePolicyDetail();
            bool tradeSideAllowed = (tradePolicyDetail.AllowedPhysicalTradeSides & physicalOrder.PhysicalTradeSide) == physicalOrder.PhysicalTradeSide;
            if (!tradeSideAllowed)
            {
                string errorMsg = string.Format("Order side is not allowed, AllowedPhysicalTradeSides={0}, order.PhysicalTradeSide={1}", tradePolicyDetail.AllowedPhysicalTradeSides, physicalOrder.PhysicalTradeSide);
                if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell)
                {
                    throw new TransactionServerException(TransactionError.ShortSellNotAllowed, errorMsg);
                }
                else
                {
                    var error = physicalOrder.IsSpotOrder ? TransactionError.PriceChangedSincePlace : TransactionError.OrderTypeIsNotAcceptable;
                    throw new TransactionServerException(error, errorMsg);
                }
            }
        }
    }

    internal sealed class PhysicalOrderTradeSidePlaceVerifier : PhysicalOrderTradeSideVerifierBase
    {
        internal static readonly PhysicalOrderTradeSidePlaceVerifier Default = new PhysicalOrderTradeSidePlaceVerifier();

        static PhysicalOrderTradeSidePlaceVerifier() { }
        private PhysicalOrderTradeSidePlaceVerifier() { }

        protected override void InnerVerify(Order order,PlaceContext context)
        {
            this.VerifyPhysicalTradeSide(order);
        }
    }


    internal sealed class PhysicalOrderTradeSideExecuteVerifier : PhysicalOrderTradeSideVerifierBase
    {
        public static readonly PhysicalOrderTradeSideExecuteVerifier Default = new PhysicalOrderTradeSideExecuteVerifier();

        static PhysicalOrderTradeSideExecuteVerifier() { }
        private PhysicalOrderTradeSideExecuteVerifier() { }

        protected override void InnerVerify(Order order,PlaceContext context)
        {
            this.VerifyPhysicalTradeSide(order);
            this.VerifyCommon(order,context);
        }
    }

}
