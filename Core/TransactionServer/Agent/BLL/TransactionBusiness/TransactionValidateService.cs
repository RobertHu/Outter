using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using Protocal;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal class ExecuteValidateService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExecuteValidateService));
        protected Transaction _owner;
        internal ExecuteValidateService(Transaction owner)
        {
            _owner = owner;
        }

        public void Validate()
        {
            this.ValidateTransactionSettings();
            this.ValidateOrders();
        }

        private void ValidateOrders()
        {
            foreach (Order eachOrder in _owner.Orders)
            {
                this.ValidateIndividualOrder(eachOrder);
            }
        }

        private void ValidateIndividualOrder(Order order)
        {
            if (order.ShouldValidateLotAfterExecute)
            {
                this.ValidateLot(order);
            }
            if (!order.IsOpen)
            {
                this.CheckCorrespondingOpenOrderCanBeClosed(order);
            }
        }

        private void CheckCorrespondingOpenOrderCanBeClosed(Order order)
        {
            foreach (OrderRelation orderRelation in order.OrderRelations)
            {
                if (!orderRelation.OpenOrder.CanBeClosed())
                {
                    throw new TransactionServerException(TransactionError.PrepaymentIsNotAllowed);
                }
            }
        }

        private void ValidateLot(Order order)
        {
            var dealingPolicyDetail = order.Owner.DealingPolicyPayload;
            switch (order.Owner.OrderType)
            {
                case OrderType.SpotTrade:
                    if (order.Lot > dealingPolicyDetail.MaxDQLot)
                        throw new TransactionServerException(TransactionError.OrderLotExceedMaxLot);
                    break;
                default:
                    if (Settings.SettingManager.Default.Setting.SystemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot != ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.ExecuteWithSetLot)
                    {
                        if (order.Lot > dealingPolicyDetail.MaxOtherLot)
                        {
                            if (Settings.SettingManager.Default.Setting.SystemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot == ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.Cancel)
                            {
                                throw new TransactionServerException(TransactionError.OrderLotExceedMaxLot);
                            }
                            else if (Settings.SettingManager.Default.Setting.SystemParameter.ExecuteActionWhenPendingOrderLotExceedMaxOtherLot == ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.ReplacedWithMaxLot)
                            {
                                throw new ShouldBeExecuteWithMaxOtherLotException(order, dealingPolicyDetail.MaxOtherLot);
                            }
                        }
                    }
                    break;
            }
        }

        private void ValidateTransactionSettings()
        {
            if (_owner.IsExpired)
            {
                var msg = string.Format("TranId {0} is canceled for instrument is not acceptable, ProcessBaseTime={1}", _owner.Id, Market.MarketManager.Now);
                Logger.Warn(msg);
                throw new TransactionServerException(TransactionError.TimingIsNotAcceptable);
            }

            var account = _owner.Owner;
            if (!account.Setting.IsTrading(Market.MarketManager.Now))
            {
                var msg = string.Format("TranId {0} is canceled for account is not trading, ProcessBaseTime={1}", _owner.Id, Market.MarketManager.Now);
                throw new TransactionServerException(TransactionError.AccountIsNotTrading);
            }

            TradeDay tradeDay = Settings.SettingManager.Default.Setting.GetTradeDay();
            if (_owner.OrderType != OrderType.Risk || _owner.ExecuteTime > tradeDay.BeginTime)
            {
                if (!_owner.TradePolicyDetail.IsTradeActive)
                    throw new TransactionServerException(TransactionError.TradePolicyIsNotActive);

                if (this.ShouldVerifyInstrumentCanPlaceAndTrade())
                {
                    this.VerifyInstrumentCanPlaceAndTrade();
                }

                if (account.Setting.Type == AccountType.Agent)
                {
                    if (account.HasUnassignedOvernightOrders)
                    {
                        throw new TransactionServerException(TransactionError.HasUnassignedOvernightOrders);
                    }
                }
                else
                {
                    if (account.IsLocked)
                    {
                        throw new TransactionServerException(TransactionError.IsLockedByAgent);
                    }
                }
            }
        }

        protected virtual bool ShouldVerifyInstrumentCanPlaceAndTrade()
        {
            return true;
        }

        private void VerifyInstrumentCanPlaceAndTrade()
        {
            if (!_owner.TradingInstrument.CanPlace(Market.MarketManager.Now, _owner.IsPending))
            {
                throw new TransactionServerException(TransactionError.TimingIsNotAcceptable, "Instrument is not in trading time to place a order");
            }
            if (!_owner.TradingInstrument.CanTrade(Market.MarketManager.Now))
            {
                var msg = string.Format("TranId {0} is canceled because instrument is not trading , isActive = {1}", _owner.Id, _owner.SettingInstrument.IsActive);
                Logger.Warn(msg);
                throw new TransactionServerException(TransactionError.InstrumentIsNotAccepting);
            }
        }

    }




    public class ShouldBeExecuteWithMaxOtherLotException : TransactionServerException
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

}
