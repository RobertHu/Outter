using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using iExchange.Common;
using System.Data;
using System.Diagnostics;
using log4net;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Engine.iExchange;
using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.BLL.PreCheck;
using Core.TransactionServer.Engine;
using Protocal;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Services;
using Core.TransactionServer.Agent.BLL.TypeExtensions;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Factory;
using Core.TransactionServer.Agent.BLL;

namespace Core.TransactionServer.Agent
{
    public class Transaction : BusinessRecord, IEquatable<Transaction>, IEqualityComparer<Transaction>, IKeyProvider<Guid>
    {
        private const int OCO_TRAN_ORDERS_COUNT = 2;
        private const int DEFAULT_ITEMS_CAPACITY = 20;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Transaction));
        internal readonly static IComparer<Transaction> AutoCloseComparer = new AutoCloseComparer();
        private Account _owner;
        private BusinessRecordList<Order> _orders;
        protected TransactionSettings _settings;
        private Lazy<TransactionExecuteService> _executeService;
        private Lazy<AutoFillServiceBase> _autoFillService;
        private Lazy<IfDoneService> _ifDoneService;
        private Lazy<BLL.TransactionBusiness.CancelService> _cancelService;
        private Lazy<PreCheckVerifierBase> _precheckVerifier;
        private Lazy<TransactionPreCheckService> _preCheckService;
        private Lazy<FillServiceBase> _fillService;
        private Lazy<TransactionExecuteNecessaryCheckServiceBase> _executeNecessaryCheckService;
        private Guid _instrumentId;
        private Settings.Instrument _settingInstrument;

        internal Transaction(Account owner, TransactionConstructParams param, ITransactionServiceFactory serviceFactory)
            : base(BusinessRecordNames.Transaction, DEFAULT_ITEMS_CAPACITY)
        {
            _owner = owner;
            this.Initialize(param, serviceFactory);
            owner.AddTransaction(this, param.OperationType);
        }

        private void Initialize(TransactionConstructParams param, ITransactionServiceFactory serviceFactory)
        {
            _settings = new TransactionSettings(this, param);
            _orders = new BusinessRecordList<Order>("Orders", this, DEFAULT_ITEMS_CAPACITY);
            _autoFillService = new Lazy<AutoFillServiceBase>(() => serviceFactory.CreateAutoFillService());
            _ifDoneService = new Lazy<IfDoneService>(() => new IfDoneService(this, _settings));
            _executeService = new Lazy<TransactionExecuteService>(() => serviceFactory.CreateExecuteService(this, _settings));
            _cancelService = new Lazy<BLL.TransactionBusiness.CancelService>(() => new BLL.TransactionBusiness.CancelService(this, _settings));
            _precheckVerifier = new Lazy<PreCheckVerifierBase>(() => serviceFactory.CreatePreCheckVerifier(this));
            _preCheckService = new Lazy<TransactionPreCheckService>(() => serviceFactory.CreatePreCheckService(this));
            _fillService = new Lazy<FillServiceBase>(() => serviceFactory.CreateFillService(this, _settings));
            _executeNecessaryCheckService = new Lazy<TransactionExecuteNecessaryCheckServiceBase>(() => serviceFactory.CreateExecuteNecessaryCheckService());
            _instrumentId = _settings.InstrumentId;
            _settingInstrument = Setting.Default.GetInstrument(_instrumentId);
        }

        #region Properties

        public TransactionSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        internal Guid Id
        {
            get
            {
                return _settings.Id;
            }
        }

        public string Code
        {
            get { return _settings.Code; }
            set
            {
                _settings.Code = value;
            }
        }

        internal InstrumentCategory InstrumentCategory { get { return _settings.InstrumentCategory; } }

        internal bool IsPhysical { get { return _settings.InstrumentCategory == InstrumentCategory.Physical; } }

        public DateTime? ExecuteTime
        {
            get { return _settings.ExecuteTime; }
            set { _settings.ExecuteTime = value; }
        }

        internal Guid InstrumentId
        {
            get { return _instrumentId; }
        }

        internal Guid AccountId { get { return _owner.Id; } }

        internal Guid? ApproverId
        {
            get { return _settings.ApproverID; }
            private set { _settings.ApproverID = value; }
        }

        internal Guid SubmitorId { get { return _settings.SubmitorID; } }

        internal ExpireType ExpireType { get { return _settings.ExpireType; } }

        internal DateTime SubmitTime { get { return _settings.SubmitTime; } }

        internal DateTime? SetPriceTimestamp { get { return _settings.SetPriceTimestamp; } }

        internal bool PlacedByRiskMonitor { get { return _settings.PlacedByRiskMonitor; } }

        internal bool FreePlacingPreCheck { get { return _settings.FreePlacingPreCheck; } }

        internal bool IsOrderCountEqualOCO
        {
            get
            {
                return this.OrderCount == OCO_TRAN_ORDERS_COUNT;
            }
        }

        internal bool IsPending
        {
            get
            {
                return this.OrderType.IsPendingType() || (this.OrderType == OrderType.Risk && this.FirstOrder.TradeOption != TradeOption.Invalid);
            }
        }

        internal Guid? SourceOrderId
        {
            get { return _settings.SourceOrderId; }
            private set { _settings.SourceOrderId = value; }
        }

        internal Guid ExecuteOrderId
        {
            get { return _orders[0].Id; }
        }

        internal int OrderCount
        {
            get { return _orders.Count; }
        }

        internal TradePolicy TradePolicy
        {
            get
            {
                return _owner.Setting().TradePolicy();
            }
        }


        internal Account Owner
        {
            get { return this._owner; }
        }

        public bool CanAmend
        {
            get
            {
                return this.Phase == TransactionPhase.Placed && !_owner.ExistsPendingConfirmLimitOrder(this);
            }
        }

        internal Order AmendedOrder
        {
            get
            {
                if (this.SourceOrderId == null) return null;
                if (this.SubType == TransactionSubType.Amend || this.SubType == TransactionSubType.IfDone)
                {
                    return this.Owner.GetOrder(this.SourceOrderId.Value);
                }
                return null;
            }
        }

        internal Transaction AmendedTran
        {
            get
            {
                return this.AmendedOrder.Owner;
            }
        }



        internal TransactionType Type
        {
            get { return _settings.Type; }
            set { _settings.Type = value; }
        }

        internal OrderType OrderType
        {
            get { return _settings.OrderType; }
        }

        internal TransactionSubType SubType
        {
            get { return _settings.SubType; }
            set { _settings.SubType = value; }
        }


        internal DateTime BeginTime
        {
            get { return _settings.BeginTime; }
        }

        internal DateTime EndTime
        {
            get { return _settings.EndTime; }
        }

        public TransactionPhase Phase
        {
            get { return _settings.Phase; }
            set
            {
                if (_settings.Phase != value)
                {
                    _settings.Phase = value;
                    _settings.UpdateTime = DateTime.Now;
                }
            }
        }

        public Guid CurrencyId { get { return this.SettingInstrument().CurrencyId; } }


        internal PlacePhase PlacePhase
        {
            get { return _settings.PlacePhase; }
            set { _settings.PlacePhase = value; }
        }

        internal string PlaceDetail
        {
            get { return _settings.PlaceDetail; }
            set { _settings.PlaceDetail = value; }
        }

        internal IEnumerable<Order> Orders
        {
            get { return _orders.GetValues(); }
        }

        internal bool PlacedWithDQMaxMove
        {
            get
            {
                return this.Type == TransactionType.Single && this.FirstOrder.DQMaxMove > 0;
            }
        }


        internal Settings.Instrument SettingInstrument(DateTime? tradeDay = null)
        {
            if (tradeDay == null) return _settingInstrument;
            return Setting.Default.GetInstrument(this.InstrumentId, tradeDay);
        }

        internal AccountClass.Instrument AccountInstrument
        {
            get
            {
                return this.Owner.GetOrCreateInstrument(this.InstrumentId);
            }
        }

        internal bool DisableAcceptLmtVariation
        {
            get { return _settings.DisableAcceptLmtVariation; }
        }

        internal BLL.InstrumentBusiness.TradingInstrument TradingInstrument
        {
            get { return TradingSetting.Default.GetInstrument(this.InstrumentId); }
        }


        internal Settings.TradePolicyDetail TradePolicyDetail(DateTime? tradeDay = null)
        {
            return this.AccountInstrument.TradePolicyDetail(tradeDay);
        }

        internal Settings.SpecialTradePolicyDetail SpecialTradePolicyDetail(DateTime? tradeDay = null)
        {
            return this.AccountInstrument.SpecialTradePolicyDetail(tradeDay);
        }

        internal Settings.DealingPolicyPayload DealingPolicyPayload(DateTime? tradeDay = null)
        {
            if (this.Submitor.DealingPolicy != null && this.Submitor.DealingPolicy.ExistDealingPolicyDetail(this.InstrumentId))
            {
                return this.Submitor.DealingPolicy[this.InstrumentId];
            }
            return this.SettingInstrument(tradeDay);
        }

        internal bool IsFreeOfNecessaryCheck
        {
            get
            {
                return this.OrderType == OrderType.Risk ||
                       this.OrderType == OrderType.MultipleClose ||
                       this.Type == TransactionType.Pair ||
                       this.Type == TransactionType.MultipleClose;
            }
        }


        internal CurrencyRate CurrencyRate(DateTime? tradeDay)
        {
            return this.AccountInstrument.CurrencyRate(tradeDay);
        }

        internal decimal ContractSize(DateTime? tradeDay)
        {
            return _settings.ContractSize > 0 ? _settings.ContractSize : this.TradePolicyDetail(tradeDay).ContractSize;
        }

        internal Framework.IQuotePolicyProvider SubmitorQuotePolicyProvider
        {
            get
            {
                var provider = Setting.Default.GetCustomer(this.SubmitorId);
                if (provider.PrivateQuotePolicyId == null)
                {
                    return this.Owner;
                }
                return provider;
            }
        }

        internal Customer Submitor
        {
            get { return Setting.Default.GetCustomer(this.SubmitorId); }
        }


        internal bool ShouldTryHit
        {
            get
            {
                var order = this.FirstOrder;
                return this.Type == TransactionType.Single && this.OrderType == OrderType.SpotTrade &&
                       order.Phase == OrderPhase.Placed && order.DQMaxMove > 0;
            }
        }

        internal virtual bool DeferredToFill
        {
            get { return this.DealingPolicyPayload().AutoDQDelay > TimeSpan.Zero && this.Type == TransactionType.Single; }
        }


        public bool CanExecute { get { return this.Phase == TransactionPhase.Placed; } }



        public bool IsDoneTran
        {
            get
            {
                return this.SubType == TransactionSubType.IfDone && this.SourceOrderId != null;
            }
        }


        internal bool IsExpired(DateTime? baseTime, DateTime? tradeDay)
        {
            return this.EndTime <= (baseTime ?? Market.MarketManager.Now) && this.EndTime <= this.SettingInstrument(tradeDay).DayCloseTime;
        }


        public Order FirstOrder
        {
            get
            {
                if (this.OrderCount == 0)
                {
                    Logger.ErrorFormat("When get first order, orderCount = 0, tranId = {0}", this.Id);
                    return null;
                }
                return _orders[0];
            }
        }

        internal Order SecondOrder
        {
            get
            {
                if (this.OrderCount < 2)
                {
                    Logger.ErrorFormat("When get second order, orderCount = {0}, tranId = {1}", this.OrderCount, this.Id);
                    return null;
                }
                return _orders[1];
            }
        }

        internal BLL.TransactionBusiness.CancelService CancelService
        {
            get { return _cancelService.Value; }
        }

        internal FillServiceBase FillService
        {
            get
            {
                return _fillService.Value;
            }
        }

        internal TransactionExecuteNecessaryCheckServiceBase ExecuteNecessaryCheckService
        {
            get
            {
                return _executeNecessaryCheckService.Value;
            }
        }

        internal AppType AppType
        {
            get { return _settings.AppType; }
        }


        internal bool HasPosition
        {
            get
            {
                foreach (var eachOrder in this.Orders)
                {
                    if (eachOrder.Phase == OrderPhase.Executed && eachOrder.LotBalance > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal bool DoneCondition { get { return _ifDoneService.Value.DoneCondition; } }

        internal virtual bool ShouldCheckIsExceedMaxOpenLot
        {
            get
            {
                return true;
            }
        }


        #endregion

        internal bool IsFreeOfPlaceMarginCheck()
        {
            return _precheckVerifier.Value.IsFreeOfMarginCheck();
        }

        internal void AddOrder(Order order, OperationType operationType)
        {
            _orders.AddItem(order, operationType);
        }

        internal void RemoveOrder(Order order)
        {
            order.Phase = OrderPhase.Deleted;
            var reason = OrderDeletedReasonRepository.Default.GetReasonModel(CancelReason.RiskMonitorDelete);
            order.CancelReasonId = reason.ID;
            order.CancelReasonDesc = reason.ReasonDesc;
            _orders.RemoveItem(order);
        }

        internal bool ExistsPlacingOrPlacedOrder()
        {
            foreach (var eachOrder in this.Orders)
            {
                if (eachOrder.CanCancel) return true;
            }
            return false;
        }

        internal virtual bool CanBeClosedBySplit(Transaction targetTran)
        {
            return this != targetTran && this.Phase == TransactionPhase.Executed && this.ExecuteTime.Value <= targetTran.ExecuteTime.Value;
        }

        public virtual bool IsFreeOfPriceCheck(bool isForExecuting)
        {
            return this.OrderType == OrderType.Risk || this.OrderType == OrderType.MultipleClose
                || (isForExecuting && (this.OrderType == OrderType.Limit || this.OrderType == OrderType.Stop))
                || this.Type == TransactionType.MultipleClose || this.SubType == TransactionSubType.Mapping;
        }

        internal virtual bool CanAutoAcceptPlace()
        {
            DealingPolicyPayload dealingPolicyDetail = this.DealingPolicyPayload();
            bool result = (this.OrderType == OrderType.SpotTrade || this.OrderType == OrderType.Market
                || this.OrderType == OrderType.Risk || this.OrderType == OrderType.MultipleClose
                || this.OrderType == OrderType.MarketOnClose || this.OrderType == OrderType.MarketOnOpen
                   || this.Type == TransactionType.Assign || this.Type == TransactionType.MultipleClose
                   || this.SubType == TransactionSubType.Mapping
                   || this.GetLotForAutoJudgment() <= dealingPolicyDetail.AutoAcceptMaxLot);
            if (!result)
            {
                Logger.WarnFormat("CanAutoAcceptPlace = false, orderType = {0}, subType = {1}, GetLotForAutoJudgment = {2}, AutoAcceptMaxLot = {3}", this.OrderType, this.SubType, this.GetLotForAutoJudgment(), dealingPolicyDetail.AutoAcceptMaxLot);
            }
            return result;
        }

        internal virtual void ExecuteDirectly(ExecuteContext context)
        {
            this.ExecuteOrders(context, o => true);
        }

        internal decimal ExecuteOrders(ExecuteContext context, Predicate<Order> verify)
        {
            decimal result = 0m;
            foreach (var order in this.Orders)
            {
                if (!verify(order)) continue;
                order.Execute(context);
                result += order.CalculateBalance(context);
            }
            if (result != 0)
            {
                this.Owner.AddBalance(this.CurrencyId, result, context.ExecuteTime);
            }
            return result;
        }


        internal virtual void Execute(ExecuteContext context)
        {
            _executeService.Value.Execute(this, context);
            if (context.ShouldCancelExecute)
            {
                context.Tran.Phase = TransactionPhase.Canceled;
            }
        }


        internal bool ShouldAutoFill()
        {
            return _autoFillService.Value.ShouldAutoFill(this);
        }

        internal bool IsPriceInRangeOfAutoFill()
        {
            return _autoFillService.Value.IsPriceInRangeOfAutoFill(this);
        }


        internal bool IsValidDonePrice(Price basePrice)
        {
            return _ifDoneService.Value.IsValidDoneOrderPrice(basePrice);
        }

        internal void RemoveDoneTransactions()
        {
            _ifDoneService.Value.RemoveDoneTrans();
        }

        internal Transaction GetDoneTransaction(Guid ifOrderId)
        {
            return _ifDoneService.Value.GetDoneTransaction(ifOrderId);
        }

        internal IEnumerable<Transaction> GetDoneTransactionsForOCO(Guid executeOrderId)
        {
            return _ifDoneService.Value.GetDoneTransactionsForOCO(executeOrderId);
        }

        internal List<Transaction> GetDoneTransactions()
        {
            return _ifDoneService.Value.GetDoneTransactions();
        }

        internal decimal GetLotForAutoJudgment()
        {
            decimal lotOfFirstOrder = this.FirstOrder.Lot;
            if (this.Type == TransactionType.Single || this.Type == TransactionType.OneCancelOther)
            {
                return lotOfFirstOrder;
            }
            else if (this.Type == TransactionType.Pair)
            {
                return 2 * lotOfFirstOrder;
            }
            else
            {
                return 0;
            }
        }

        internal void Cancel(CancelReason cancelType, ExecuteContext context = null)
        {
            this.CancelOrders(cancelType, context);
            if (this.DoneCondition)

            {
                this.CancelService.CancelDoneTrans(cancelType);
            }
        }

        private void CancelOrders(CancelReason cancelType, ExecuteContext context)
        {
            foreach (var order in this.Orders)
            {
                if (order.CanCancel)
                {
                    order.Cancel(cancelType, context);
                }
            }
        }

        internal void RejectPlace()
        {
            this.Phase = TransactionPhase.Placing;
            foreach (var order in this.Orders)
            {
                order.RejectPlace();
            }
        }

        internal bool ExistsCloseOrder()
        {
            foreach (Order order in this.Orders)
            {
                if (order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Executed
                    || order.Phase == OrderPhase.Completed)
                {
                    if (!order.IsOpen)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ExistsOpenOrder()
        {
            foreach (var order in this.Orders)
            {
                if (order.IsOpen) return true;
            }
            return false;
        }

        internal Order GetExecuteOrder(Guid executeId)
        {
            foreach (var order in this.Orders)
            {
                if (order.Id == executeId) return order;
            }
            return null;
        }

        public void ChangeToIfDone()
        {
            this.SubType = TransactionSubType.IfDone;
        }

        internal void ChangePhaseToCancel()
        {
            this.Phase = TransactionPhase.Canceled;
        }

        internal void ChangePhaseToPlaced()
        {
            if (this.Phase != TransactionPhase.Placed)
            {
                this.Phase = TransactionPhase.Placed;
            }

            foreach (var eachOrder in this.Orders)
            {
                if (eachOrder.Phase != OrderPhase.Placed)
                {
                    eachOrder.ChangeToPlaced();
                }
            }
        }

        internal void ChangePhaseToPlacing()
        {
            this.Phase = TransactionPhase.Placing;
            foreach (var eachOrder in this.Orders)
            {
                eachOrder.ChangeToPlacing();
            }
        }

        internal MarginAndQuantityResult CalculateUnfilledMarginAndQuantity(decimal? effectiveLot = null)
        {
            return _preCheckService.Value.CalculateUnfilledMarginAndQuantity(effectiveLot);
        }

        internal bool ShouldCalculatePreCheckNecessary(AccountClass.Instrument instrument, bool isBuy, Dictionary<Guid, decimal> unfilledLotsPerTran, out decimal? unfilledLot)
        {
            return _preCheckService.Value.ShouldCalculatePreCheckNecessary(instrument, isBuy, unfilledLotsPerTran, out unfilledLot);
        }

        internal bool ShouldSumPlaceMargin()
        {
            return _preCheckService.Value.ShouldSumPlaceMargin();
        }

        internal override void ChangeToDeleted()
        {
            bool isAllOrderDeleted = true;
            foreach (var eachOrder in this.Orders)
            {
                if (eachOrder.Status != ChangeStatus.Deleted)
                {
                    isAllOrderDeleted = false;
                    break;
                }
            }
            if (isAllOrderDeleted)
            {
                base.ChangeToDeleted();
            }
        }

        #region IEqualable and IEqualableComparer interface methods
        public bool Equals(Transaction other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        public bool Equals(Transaction x, Transaction y)
        {
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(Transaction obj)
        {
            return this.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this.Equals((Transaction)obj);
        }

        public static bool operator ==(Transaction left, Transaction right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Transaction left, Transaction right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return string.Format("Id={0}, Phase={1}, OrderType={2}, SubTpe={3}", this.Id, this.Phase, this.OrderType, this.SubType);
        }
        #endregion

        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.Id;
        }
    }

    internal sealed class AutoCloseComparer : IComparer<Transaction>
    {
        public int Compare(Transaction x, Transaction y)
        {
            var account = x.Owner;
            Debug.Assert(x.ExecuteTime != null && y.ExecuteTime != null);
            if (account.AutoCloseFirstInFirstOut)
            {
                return x.ExecuteTime.Value.CompareTo(y.ExecuteTime.Value);
            }
            else
            {
                return y.ExecuteTime.Value.CompareTo(x.ExecuteTime.Value);
            }
        }
    }
}