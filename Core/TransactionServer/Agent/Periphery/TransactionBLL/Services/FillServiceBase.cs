using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.BinaryOption;
using Protocal.CommonSetting;
using log4net;
using Core.TransactionServer.Agent.BLL.OrderBusiness;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Services
{
    internal abstract class FillServiceBase
    {
        protected Transaction _owner;
        protected TransactionSettings _settings;

        protected FillServiceBase(Transaction owner, TransactionSettings settings)
        {
            _owner = owner;
            _settings = settings;
        }

        protected abstract ILog Logger { get; }

        public void Fill(ExecuteContext context)
        {
            this.Verify(context);
            this.FillProperties(context);
            if (this.ShouldCloseReverseDirectionOrders())
            {
                this.AutoCloseReverseDirectionOrders(context.ExecuteOrderId);
            }
            // this.CheckIsAllowNewMOOMOC();
        }

        private void Verify(ExecuteContext context)
        {
            DateTime baseTime = Market.MarketManager.Now;
            var instrument = _owner.SettingInstrument(context.TradeDay);
            if (context.ShouldUseHistorySettings)
            {
                baseTime = context.ExecuteTime.Value;
            }
            if (baseTime > _owner.EndTime)
            {
                this.Logger.Warn(string.Format("Execute: TranId {0}, ProcessBaseTime {1}, endTime {2}", _owner.Id, baseTime, _owner.EndTime));
                throw new TransactionServerException(TransactionError.TransactionExpired);
            }


            if (baseTime > instrument.DayCloseTime || baseTime < instrument.DayOpenTime)
            {
                this.Logger.Warn(string.Format("Execute: TranId {0}, ProcessBaseTime {1}, DayOpenTime {2}, DayCloseTime {3}", _owner.Id, baseTime, instrument.DayOpenTime, instrument.DayCloseTime));
                throw new TransactionServerException(TransactionError.FillOnMarketCloseNotAllowed);
            }

            if (_owner.Type == TransactionType.OneCancelOther && context.ExecuteOrderId == null)
            {
                throw new TransactionServerException(TransactionError.LossExecutedOrderInOco, "OCO Transaction executedOrderId is null");
            }
        }


        protected abstract bool ShouldCloseReverseDirectionOrders();

        private void FillProperties(ExecuteContext context)
        {
            this.FillTransactionProperties(context);
            this.FillOrderProperties(context.ExecuteOrderId, context.OrderInfos);
            this.FillBestPriceForLimitStopOrders(context);
        }

        private void FillOrderProperties(Guid? executeOrderId, List<OrderPriceInfo> infos)
        {
            foreach (var eachOrder in _owner.Orders)
            {
                if (!eachOrder.CancelOCOOrderIfExists(executeOrderId, _owner.Type))
                {
                    var info = GetOrderPriceInfo(infos, eachOrder.Id);
                    if (info == null)
                    {
                        info = eachOrder.CreateOrderPriceInfo();
                    }
                    this.FillIndividualOrderProperties(eachOrder, info.BuyPrice, info.SellPrice);
                }

            }
        }

        private OrderPriceInfo GetOrderPriceInfo(List<OrderPriceInfo> infos, Guid orderId)
        {
            foreach (var eachInfo in infos)
            {
                if (orderId == eachInfo.OrderId)
                {
                    return eachInfo;
                }
            }
            return null;
        }


        private void FillIndividualOrderProperties(Order order, Price buyPrice, Price sellPrice)
        {
            var executePrice = order.IsBuy ? buyPrice : sellPrice;
            order.Phase = OrderPhase.Executed;
            order.ExecutePrice = executePrice;
        }


        private void FillTransactionProperties(ExecuteContext context)
        {
            _settings.Phase = TransactionPhase.Executed;
            if (_owner.ExecuteTime == null)
            {
                _settings.ExecuteTime = context.ExecuteTime ?? MarketManager.Now;
            }
            this.FillContractSize(context.TradeDay);
            if (_owner.ApproverId == null)
            {
                _settings.ApproverID = IsTrader(_owner.AppType) ? Guid.Empty : _owner.SubmitorId;
            }
        }

        private static bool IsTrader(AppType appType)
        {
            return appType == AppType.TradingConsole || appType == AppType.CppTrader || appType == AppType.Mobile || appType == AppType.TradingConsoleSilverLight;
        }

        protected virtual void FillContractSize(DateTime? tradeDay)
        {
            _settings.ContractSize = _owner.TradePolicyDetail(tradeDay).ContractSize;
        }


        private void FillBestPriceForLimitStopOrders(ExecuteContext context)
        {
            foreach (var order in _owner.Orders)
            {
                this.FillBestPrice(order, context.IsFreeValidation, context.TradeDay);
            }
        }

        private void FillBestPrice(Order order, bool isFreeValidation, DateTime? tradeDay)
        {
            if (order.Phase != OrderPhase.Executed) return;
            this.VerifyExecutePrice(order);
            if (!isFreeValidation && _owner.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Stop)
            {
                this.VerifyBestPrice(order);
                if (Math.Abs(order.ExecutePrice - order.BestPrice) >= _owner.DealingPolicyPayload(tradeDay).HitPriceVariationForSTP)
                {
                    if (Settings.Setting.Default.SystemParameter.STPAtHitPriceOption == STPAtHitPriceOption.Always
                        || (Settings.Setting.Default.SystemParameter.STPAtHitPriceOption == STPAtHitPriceOption.OnlyWhenNetLotIncreased
                        && this.IncreaseOrChangeDirectionOfNetLotAfterExecute(order)))
                    {
                        order.ExecutePrice = order.BestPrice;
                    }
                }
            }
        }

        private void VerifyExecutePrice(Order order)
        {
            if (order.ExecutePrice == null)
            {
                throw new TransactionServerException(TransactionError.InvalidPrice);
            }
        }

        private void VerifyBestPrice(Order order)
        {
            if (order.BestPrice == null)
            {
                throw new TransactionServerException(TransactionError.InvalidPrice);
            }
        }


        private bool IncreaseOrChangeDirectionOfNetLotAfterExecute(Order order)
        {
            var tran = order.Owner;
            var account = tran.Owner;
            var buySellLot = account.CalculateLotSummary(tran);
            var buySellLotAfterExecute = this.CalculateLot(order, account.Setting().IsAutoClose, buySellLot);
            return buySellLotAfterExecute.IsNetPosWithDiffDirection(buySellLot) ||
                  buySellLotAfterExecute.IsAbsNetPosGreateThan(buySellLot);
        }

        private BuySellLot CalculateLot(Order order, bool isAccountAutoClose, BuySellLot buySellLot)
        {
            var buyLot = buySellLot.BuyLot;
            var sellLot = buySellLot.SellLot;
            if (!order.IsOpen || isAccountAutoClose)
            {
                if (order.IsBuy)
                {
                    sellLot -= order.Lot;
                }
                else
                {
                    buyLot -= order.Lot;
                }
            }
            else
            {
                if (order.IsBuy)
                {
                    buyLot += order.Lot;
                }
                else
                {
                    sellLot += order.Lot;
                }
            }
            return new BuySellLot(buyLot, sellLot);
        }

        private void AutoCloseReverseDirectionOrders(Guid? executeOrderId)
        {
            Dictionary<Guid, decimal> openOrderPerClosedLotDict = new Dictionary<Guid, decimal>();
            foreach (var eachOrder in _owner.Orders)
            {
                Logger.InfoFormat("AutoCloseReverseDirectionOrders executeOrderId = {0}, eachOrder.Id = {1}, tranId = {2}, accountId = {3}", executeOrderId, eachOrder.Id, _owner.Id, _owner.AccountId);
                if (executeOrderId != null && executeOrderId.Value != eachOrder.Id) continue;
                if (eachOrder.LotBalance > 0)
                {
                    eachOrder.SplitOrder(openOrderPerClosedLotDict);
                }
                if (!eachOrder.IsOpen)
                {
                    this.CollectOpenOrderClosedLot(eachOrder, openOrderPerClosedLotDict);
                }
            }
        }

        private void CollectOpenOrderClosedLot(Order order, Dictionary<Guid, decimal> openOrderPerClosedLotDict)
        {
            Debug.Assert(!order.IsOpen);
            foreach (var orderRelation in order.OrderRelations)
            {
                if (!openOrderPerClosedLotDict.ContainsKey(orderRelation.OpenOrderId))
                {
                    openOrderPerClosedLotDict.Add(orderRelation.OpenOrderId, orderRelation.ClosedLot);
                }
                else
                {
                    openOrderPerClosedLotDict[orderRelation.OpenOrderId] += orderRelation.ClosedLot;
                }
            }
        }
    }

    internal sealed class FillService : FillServiceBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(FillService));

        internal FillService(Transaction owner, TransactionSettings settings)
            : base(owner, settings) { }

        protected override bool ShouldCloseReverseDirectionOrders()
        {
            return _owner.Owner.IsAutoClose;
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal sealed class PhysicalFillService : FillServiceBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(PhysicalFillService));

        internal PhysicalFillService(PhysicalTransaction owner, TransactionSettings settings)
            : base(owner, settings) { }

        protected override bool ShouldCloseReverseDirectionOrders()
        {
            return true;
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal sealed class BOTransactionFillService : FillServiceBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(BOTransactionFillService));

        internal const int BO_CONTRACT_SIZE = 1;
        internal BOTransactionFillService(BOTransaction tran, TransactionSettings settings)
            : base(tran, settings)
        {
        }

        protected override bool ShouldCloseReverseDirectionOrders()
        {
            return false;
        }

        protected override void FillContractSize(DateTime? tradeDay)
        {
            _settings.ContractSize = BO_CONTRACT_SIZE;
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }
}
