using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    public sealed class ShouldBeExecuteWithMaxOtherLotException : TransactionServerException
    {
        internal ShouldBeExecuteWithMaxOtherLotException(Order exceedMaxOtherLotOrder, decimal maxOtherLot)
            : base(TransactionError.ReplacedWithMaxLot)
        {
            this.ExceedMaxOtherLotOrder = exceedMaxOtherLotOrder;
            this.MaxOtherLot = maxOtherLot;
        }
        internal Order ExceedMaxOtherLotOrder { get; private set; }
        internal decimal MaxOtherLot { get; private set; }
    }

    internal abstract class OrderVerifierBase
    {
        private TradeSideVerifierBase _tradeSideVerifier;

        protected OrderVerifierBase()
        {
            _tradeSideVerifier = this.CreateTradeSideVerifier();
        }

        protected abstract ILog Logger { get; }

        protected DateTime BaseTime
        {
            get
            {
                return Market.MarketManager.Now;
            }
        }

        internal void Verify(Order order, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            this.VerifyCommon(order, context);
            this.InnerVerify(order, isPlaceByRiskMonitor, appType, context);
        }

        protected abstract bool IsTimingAcceptable(DateTime baseTime, Transaction tran, PlaceContext context);

        protected virtual void InnerVerify(Order order, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            if (this.ShouldVerifyBPointAndMaxDQLot(order, isPlaceByRiskMonitor, appType))
            {
                this.VerifyBPointAndMaxDQLot(order, context);
            }
            this.VerifyOpenOrderCanBeClosed(order);
        }

        private void VerifyOpenOrderCanBeClosed(Order order)
        {
            if (!order.IsOpen && order.Owner.SubType != TransactionSubType.Match)
            {
                foreach (OrderRelation eachOrderRelation in order.OrderRelations)
                {
                    if (!eachOrderRelation.OpenOrder.CanBeClosed())
                    {
                        throw new TransactionServerException(TransactionError.PrepaymentIsNotAllowed);
                    }
                }
            }
        }


        protected virtual bool ShouldVerifyBPointAndMaxDQLot(Order order, bool isPlaceByRiskMonitor, AppType appType)
        {
            return appType != AppType.RiskMonitor && !isPlaceByRiskMonitor && !(appType == AppType.Manager && order.Owner.OrderType == OrderType.Risk);
        }

        protected abstract void VerifyBPointAndMaxDQLot(Order order, PlaceContext context);


        protected void VerifyMaxDQLot(Order order, PlaceContext context)
        {
            var dealingPolicyDetail = order.Owner.DealingPolicyPayload(context.TradeDay);
            switch (order.Owner.OrderType)
            {
                case OrderType.SpotTrade:
                    if (order.Lot > dealingPolicyDetail.MaxDQLot)
                        throw new TransactionServerException(TransactionError.OrderLotExceedMaxLot);
                    break;
                default:
                    var systemParameter = Settings.Setting.Default.SystemParameter;

                    if (systemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot != ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.ExecuteWithSetLot)
                    {
                        if (order.Lot > dealingPolicyDetail.MaxOtherLot)
                        {
                            if (systemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot == ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.Cancel)
                            {
                                throw new TransactionServerException(TransactionError.OrderLotExceedMaxLot);
                            }
                            else if (systemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot == ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.ReplacedWithMaxLot)
                            {
                                throw new ShouldBeExecuteWithMaxOtherLotException(order, dealingPolicyDetail.MaxOtherLot);
                            }
                        }
                    }
                    break;
            }
        }


        protected abstract TradeSideVerifierBase CreateTradeSideVerifier();

        private void VerifyCommon(Order order, PlaceContext context)
        {
            DateTime now = Market.MarketManager.Now;
            if (context.IsBook)
            {
                now = context.ExecuteTime.Value;
            }
            Transaction tran = order.Owner;

            if (!tran.AccountInstrument.HasTradingQuotation(tran.SubmitorQuotePolicyProvider))
            {
                throw new TransactionServerException(TransactionError.HasNoQuotationExists, "The instrument has no quotation");
            }

            if (tran.SettingInstrument(context.TradeDay).ExchangeSystem == ExchangeSystem.Local)
            {
                if (tran.Type != TransactionType.Assign && tran.SubType != TransactionSubType.Mapping && !this.IsTimingAcceptable(now, tran, context))
                {
                    this.Logger.WarnFormat("TranId {0} is canceled for instrument is not acceptable, ProcessBaseTime={1}", tran.Id, now);
                    throw new TransactionServerException(TransactionError.TimingIsNotAcceptable);
                }
            }

            var settingAccount = order.Account.Setting(context.TradeDay);

            if (settingAccount == null || !settingAccount.IsTrading(now))
            {
                this.Logger.WarnFormat("TranId {0} is canceled for account is not trading, ProcessBaseTime={1}", tran.Id, now);
                throw new TransactionServerException(TransactionError.AccountIsNotTrading);
            }
            _tradeSideVerifier.Verify(order, context);
            this.VerifyTransaction(order.Owner, context);
        }

        protected abstract void VerifyInstrumentCanPlaceAndTrade(Transaction tran, PlaceContext context);

        protected virtual bool ShouldVerifyInstrumentCanPlaceAndTrade(Transaction tran)
        {
            return tran.OrderType != OrderType.Risk && tran.OrderType != OrderType.MarketOnOpen && tran.OrderType != OrderType.MarketOnClose;
        }

        private void VerifyTransaction(Transaction tran, PlaceContext context)
        {
            TradeDay tradeDay = Settings.Setting.Default.GetTradeDay(context.TradeDay);
            var tradePolicyDetail = tran.TradePolicyDetail(context.TradeDay);
            var settingAccount = tran.Owner.Setting(context.TradeDay);
            var account = tran.Owner;
            if (tran.OrderType != OrderType.Risk || (tran.ExecuteTime != null && tran.ExecuteTime.Value > tradeDay.BeginTime))
            {
                if (tran.Owner.IsResetFailed && tran.Type != TransactionType.Assign)
                {
                    throw new TransactionServerException(TransactionError.AccountResetFailed, string.Format("accountId = {0}, lastResetDay = {1}, tran.id = {2}, tran.Type = {3}", tran.Owner.Id, tran.Owner.LastResetDay, tran.Id, tran.Type));
                }

                if (tradePolicyDetail == null)
                {
                    Logger.InfoFormat("can't find tradePolicyDetail  tranId = {0}, tradePolicyId = {1}, instrumentId = {2}", tran.Id, tran.Owner.Setting(context.TradeDay).TradePolicyId, tran.InstrumentId);
                    throw new TransactionServerException(TransactionError.InstrumentNotInTradePolicy);
                }


                if (!tradePolicyDetail.IsTradeActive)
                {
                    throw new TransactionServerException(TransactionError.TradePolicyIsNotActive);
                }

#if PLACETEST
#else
                if (this.ShouldVerifyInstrumentCanPlaceAndTrade(tran))
                {
                    this.VerifyInstrumentCanPlaceAndTrade(tran, context);
                }
#endif
                if (settingAccount.Type == AccountType.Agent)
                {
                    if (account.HasUnassignedOvernightOrders)
                    {
                        throw new TransactionServerException(TransactionError.HasUnassignedOvernightOrders);
                    }
                }
                else
                {
                    if (tran.OrderType != OrderType.Risk && tran.Type != TransactionType.Assign)
                    {
                        if (account.IsLocked)
                        {
                            throw new TransactionServerException(TransactionError.IsLockedByAgent);
                        }
                    }
                }
            }
        }



    }
}
