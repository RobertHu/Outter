using Core.TransactionServer.Agent.BLL.AccountBusiness;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    internal abstract class OrderExecuteVerifierBase : OrderVerifierBase
    {
        protected override void VerifyInstrumentCanPlaceAndTrade(Transaction tran, PlaceContext context)
        {
            if ((!tran.TradingInstrument.CanTrade(this.BaseTime, context) || !tran.AccountInstrument.HasTradePrice(tran.SubmitorQuotePolicyProvider)))
            {
                this.Logger.WarnFormat("TranId {0} is canceled because instrument is not trading , isActive = {1}", tran.Id, tran.SettingInstrument(context.TradeDay).IsActive);
                throw new TransactionServerException(TransactionError.InstrumentIsNotAccepting);
            }
        }

        protected override bool IsTimingAcceptable(DateTime baseTime, Transaction tran, PlaceContext context)
        {
            return tran.EndTime > baseTime;
        }

        protected override void VerifyBPointAndMaxDQLot(Order order,PlaceContext context)
        {
            this.VerifyMaxDQLot(order,context);
        }
    }


    internal sealed class OrderExecuteVerifier : OrderExecuteVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(OrderExecuteVerifier));

        public static readonly OrderExecuteVerifier Default = new OrderExecuteVerifier();

        private OrderExecuteVerifier() { }
        static OrderExecuteVerifier() { }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return TradeSidePlaceVerifier.Default;
        }

        protected override void InnerVerify(Order order, bool isPlaceByRiskMonitor, AppType appType,PlaceContext context)
        {
            base.InnerVerify(order, isPlaceByRiskMonitor, appType, context);
            Transaction tran = order.Owner;
            if (!order.Owner.TradePolicyDetail(context.TradeDay).IsTradeActive)
            {
                throw new TransactionServerException(TransactionError.TradePolicyIsNotActive);
            }
        }
    }

    internal sealed class PhysicalOrderExecuteVerifier : OrderExecuteVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(PhysicalOrderExecuteVerifier));

        internal static readonly PhysicalOrderExecuteVerifier Default = new PhysicalOrderExecuteVerifier();

        static PhysicalOrderExecuteVerifier() { }
        private PhysicalOrderExecuteVerifier() { }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return PhysicalOrderTradeSideExecuteVerifier.Default;
        }

        protected override bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return base.ShouldVerifyBPointAndMaxDQLot(order, isPlaceByRiskMonitor, appType) && PhysicalVerifier.ShouldVerifyBPointAndMaxDQLot(order);
        }

    }

    internal sealed class BOOrderExecuteVerifier : OrderExecuteVerifierBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(BOOrderExecuteVerifier));

        internal static readonly BOOrderExecuteVerifier Default = new BOOrderExecuteVerifier();

        static BOOrderExecuteVerifier() { }
        private BOOrderExecuteVerifier() { }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return BOOrderTradeSideVerifier.Default;
        }

        protected override bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return false;
        }
    }



}
