using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using iExchange.Common;
using System.Diagnostics;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Hit;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using Core.TransactionServer.Agent.AccountClass;
using log4net;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Engine;
using Protocal;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Services;
using Core.TransactionServer.Agent.BLL.PreCheck;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.BLL;

namespace Core.TransactionServer.Agent
{

    public partial class Order : BillBusinessRecord, IPriceParameterProvider, IEquatable<Order>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Order));
        private const int ORDER_RELATION_CAPACITY = 5;
        private const int ATTRIBUTE_ITEMS_CAPACITY = 30;
        private BusinessRecordDictionary<Guid, OrderRelation> _orderRelations;
        private Transaction _owner;
        private bool _pairOrderIsHit;
        protected OrderSettings _orderSettings;
        protected Lazy<OrderFeeCalculatorBase> _feeCalculator;
        protected Lazy<OpenOrderCalculatorBase> _openOrderCalculator;
        protected Lazy<CloseOrderCalculator> _closeOrderCalculator;
        protected Lazy<OrderExecuteServiceBase> _executeService;
        private Lazy<ValuedPLCalculatorBase> _valuedPLCalculator;
        private Lazy<OrderPreCheckServiceBase> _precheckServcie;
        private BusinessItem<Guid> _currencyId;

        internal Order(Transaction owner, OrderConstructParams constructParams, OrderServiceFactoryBase serviceFactory)
            : base(BusinessRecordNames.Order, ATTRIBUTE_ITEMS_CAPACITY)
        {
            this.ConstructParams = constructParams;
            this.ServiceFactory = serviceFactory;
            _pairOrderIsHit = false;
            this.IsHitReseted = false;
            _owner = owner;
            _orderSettings = serviceFactory.CreateOrderSettings(this, constructParams);
            if (owner != null)
            {
                owner.AddOrder(this, constructParams.OperationType);
            }
            _orderRelations = new BusinessRecordDictionary<Guid, OrderRelation>(BusinessRecordCollectionNames.OrderRelations, this, ORDER_RELATION_CAPACITY);
            _feeCalculator = new Lazy<OrderFeeCalculatorBase>(() => serviceFactory.CreateOrderFeeCalculator(this, _orderSettings));
            _openOrderCalculator = new Lazy<OpenOrderCalculatorBase>(() => serviceFactory.CreateOpenOrderCalculator(this, _orderSettings));
            _closeOrderCalculator = new Lazy<CloseOrderCalculator>(() => serviceFactory.CreateCloseOrderCalculator(this, _orderSettings));
            _executeService = new Lazy<OrderExecuteServiceBase>(() => serviceFactory.CreateOrderExecuteService(this, _orderSettings));
            _precheckServcie = new Lazy<OrderPreCheckServiceBase>(() => serviceFactory.CreatePreCheckService(this));
            _valuedPLCalculator = new Lazy<ValuedPLCalculatorBase>(() => serviceFactory.CreateValuedPLCalculator(this));
            _currencyId = BusinessItemFactory.Create("CurrencyID", owner.CurrencyId, PermissionFeature.ReadOnly, this);
        }

        internal OrderConstructParams ConstructParams { get; private set; }

        internal OrderServiceFactoryBase ServiceFactory { get; private set; }

        internal DateTime? ExecuteTime
        {
            get
            {
                return _owner.ExecuteTime;
            }
        }

        internal Guid AccountId { get { return _owner.Owner.Id; } }

        internal bool IsHitReseted { get; set; }

        internal Guid CurrencyId
        {
            get
            {
                return _currencyId.Value;
            }
        }

        internal virtual bool ShouldCalculateAutoPrice
        {
            get { return this.Phase == OrderPhase.Executed && this.LotBalance > 0; }
        }

        internal Account Account
        {
            get
            {
                return this.Owner.Owner;
            }
        }

        internal Price AutoLimitPrice
        {
            get { return _orderSettings.AutoLimitPrice; }
            set { _orderSettings.AutoLimitPrice = value; }
        }

        internal Price AutoStopPrice
        {
            get { return _orderSettings.AutoStopPrice; }
            set { _orderSettings.AutoStopPrice = value; }
        }

        internal virtual decimal LotBalanceReal
        {
            get
            {
                return this.LotBalance;
            }
        }

        internal bool IsValued
        {
            get
            {
                if (this.IsOpen)
                {
                    return _openOrderCalculator.Value.IsValued();
                }
                return _closeOrderCalculator.Value.IsValued();
            }
        }

        internal Price JudgePrice
        {
            get { return _orderSettings.JudgePrice; }
            set { _orderSettings.JudgePrice = value; }
        }

        internal DateTime? JudgePriceTimestamp
        {
            get { return _orderSettings.JudgePriceTimestamp; }
            set { _orderSettings.JudgePriceTimestamp = value; }
        }

        internal Guid? CancelReasonId
        {
            get { return _orderSettings.CancelReasonId; }
            set { _orderSettings.CancelReasonId = value; }
        }

        internal string CancelReasonDesc
        {
            get { return _orderSettings.CancelReasonDesc; }
            set { _orderSettings.CancelReasonDesc = value; }
        }

        internal CancelReason? CancelReason
        {
            get { return _orderSettings.CancelReason; }
            set { _orderSettings.CancelReason = value; }
        }

        internal bool IsAutoFill
        {
            get { return _orderSettings.IsAutoFill; }
            set { _orderSettings.IsAutoFill = value; }
        }

        internal decimal EstimateCloseCommission
        {
            get { return _orderSettings.EstimateCloseCommission; }
            set { _orderSettings.EstimateCloseCommission = value; }
        }

        internal decimal EstimateCloseLevy
        {
            get { return _orderSettings.EstimateCloseLevy; }
            set { _orderSettings.EstimateCloseLevy = value; }
        }

        internal bool ShouldCalculateEstimateFee
        {
            get
            {
                return this.IsOpen && this.Phase == OrderPhase.Executed && this.LotBalance > 0;
            }
        }


        internal Price GetMarketPrice(out DateTime priceTimeStamp)
        {
            var quotation = this.Owner.AccountInstrument.GetQuotation(this.Owner.SubmitorQuotePolicyProvider);
            priceTimeStamp = quotation.Timestamp;
            return this.IsBuy ? quotation.SellPrice : quotation.BuyPrice;
        }

        internal void ClearAutoPrice()
        {
            this.AutoLimitPrice = null;
            this.AutoStopPrice = null;
        }

        internal virtual decimal SumFee()
        {
            return this.CommissionSum + this.LevySum + this.OtherFeeSum;
        }


        internal virtual void CalculateInit()
        {
            if (this.Phase != OrderPhase.Executed) return;
            AutoPriceCalculator.CalculateAutoPrice(this);
            this.CalculateNotValuedPL();
        }

        internal OrderRelation GetOrderRelation(Guid openOrderId)
        {
            return _orderRelations[openOrderId];
        }

        internal bool ContainsOrderRelation(Guid openOrderId)
        {
            return _orderRelations.ContainsKey(openOrderId);
        }


        internal virtual bool CanBeClosed()
        {
            if (this.IsOpen) return true;
            return false;
        }


        internal TimeSpan SubtractDelayFillTime(TimeSpan reduction)
        {
            _orderSettings.AutoFillDelayTime = _orderSettings.AutoFillDelayTime - reduction;
            return _orderSettings.AutoFillDelayTime.Value;
        }

        internal List<KeyValuePair<Order, decimal>> GetAllCloseOrderAndClosedLot()
        {
            return _openOrderCalculator.Value.GetAllCloseOrderAndClosedLot();
        }


        internal void SplitOrder(Dictionary<Guid, decimal> openOrderPerClosedLotDict, bool isForCut = false)
        {
            _openOrderCalculator.Value.SplitOrderCalculator.Split(openOrderPerClosedLotDict, isForCut);
        }

        internal void UpdateOpenOrder(ExecuteContext context)
        {
            Debug.Assert(!this.IsOpen);
            _closeOrderCalculator.Value.UpdateOpenOrderWhenExecuted(context);
        }

        internal Price GetLivePriceForCalculateNecessary()
        {
            return _orderSettings.GetLivePriceForCalculateNecessary();
        }

        internal void CopyHitInfoFrom(Order originOrder)
        {
            this.LivePrice = originOrder.LivePrice;
            this.BestPrice = originOrder.BestPrice;
            this.BestTime = originOrder.BestTime;
            this.HitCount = originOrder.HitCount;
        }

        internal void ResetHit()
        {
            this.HitCount = 0;
            this.BestPrice = null;
            this.BestTime = null;
            this.IsHitReseted = true;
        }

        internal void Cancel(CancelReason cancelReason)
        {
            if (this.Owner.OrderType == iExchange.Common.OrderType.Limit)
            {
                this.Account.RemovePendingConfirmLimitOrder(this);
            }
            this.Phase = OrderPhase.Canceled;
            this.ChangeToDeleted();
            this.CancelReason = cancelReason;
            DB.DBMapping.OrderDeletedReason model = OrderDeletedReasonRepository.Default.GetReasonModel(cancelReason);
            if (model != null)
            {
                this.CancelReasonId = model.ID;
                if (cancelReason == iExchange.Common.CancelReason.MarginIsNotEnough)
                {
                    this.CancelReasonDesc = string.Format("balance:{0},necessary:{1},equity:{2}", this.Account.Balance, this.Account.Necessary, this.Account.Equity);
                }
                else
                {
                    this.CancelReasonDesc = model.ReasonDesc;
                }
            }

        }

        internal void RejectPlace()
        {
            this.Phase = OrderPhase.Placing;
        }

        internal void ChangeToCompleted()
        {
            this.Phase = OrderPhase.Completed;
            this.ChangeToDeleted();
        }

        internal override void ChangeToDeleted()
        {
            base.ChangeToDeleted();
            this.Owner.ChangeToDeleted();
        }


        internal void ChangeToPlacing()
        {
            this.Phase = OrderPhase.Placing;
        }

        internal void ChangeToPlaced()
        {
            this.Phase = OrderPhase.Placed;
        }


        internal void CancelExecute()
        {
            if (this.Phase != OrderPhase.Executed) return;
            this.InnerCancelExecute(false);
        }

        protected virtual void InnerCancelExecute(bool isForDelivery)
        {
            decimal deltaBalance = this.SumBillsForBalance();
            this.Account.AddBalance(this.CurrencyId, -deltaBalance, null);
            this.RecoverBills();
            if (!this.IsOpen)
            {
                foreach (var eachOrderRelation in this.OrderRelations)
                {
                    eachOrderRelation.CancelExecute(isForDelivery);
                }
            }
        }

        private void RecoverBills()
        {
            foreach (var eachBill in this.Bills)
            {
                this.AddBill(-eachBill, OperationType.AsNewRecord);
            }
        }


        internal void LoadForMarketOnCloseExecute(DataRow dr)
        {
            _orderSettings.LoadForMarketOnCloseExecute(dr);
        }

        public OrderHitStatus HitSetPrice(Quotation newQuotation, bool ignoreHitTimes, DateTime? baseTime = null)
        {
            baseTime = baseTime ?? Market.MarketManager.Now;
            return this.HitSettings.HitSetPrice(newQuotation, baseTime.Value, ignoreHitTimes);
        }

        public OrderHitStatus CalculateLimitMarketHitStatus(bool ignoreHitTimes)
        {
            return this.HitSettings.CalculateLimitMarketHitStatus(ignoreHitTimes);
        }

        internal OrderHitStatus HitAutoClosePrice(Quotation newQuotation, DateTime baseTime)
        {
            return LimitAndMarketOrderHitter.HitAutoClosePrice(this, newQuotation, baseTime);
        }

        internal void CalculateFloatPL(Quotation quotation)
        {
            _openOrderCalculator.Value.FloatPLCalculator.Calculate(quotation);
        }

        internal void CalculateFloatPLForcely(Quotation quotation)
        {
            _openOrderCalculator.Value.FloatPLCalculator.CalculateFloatPLForcely(quotation);
        }

        internal void CalculateNotValuedPL()
        {
            decimal interestPL;
            decimal storagePL;
            decimal tradePL;
            NotValuedPLCalculator.Calculate(this, out interestPL, out storagePL, out tradePL);
            if (interestPL != 0m)
            {
                this.AddNotValuedPL(interestPL, BillType.InterestPL, DateTime.Now);
            }
            if (storagePL != 0m)
            {
                this.AddNotValuedPL(storagePL, BillType.StoragePL, DateTime.Now);
            }
            if (tradePL != 0m)
            {
                this.AddNotValuedPL(tradePL, BillType.TradePL, DateTime.Now);
            }
        }

        internal void CalculateValuedPL(ExecuteContext context)
        {
            if (this.IsOpen) return;
            var valuedPL = _valuedPLCalculator.Value.Calculate(context);
            DateTime updateTime = context.ExecuteTime ?? DateTime.Now;
            if (valuedPL.InterestPL != 0m)
            {
                this.AddValuedPL(valuedPL.InterestPL, BillType.InterestPL, updateTime);
            }
            if (valuedPL.StoragePL != 0m)
            {
                this.AddValuedPL(valuedPL.StoragePL, BillType.StoragePL, updateTime);
            }
            if (valuedPL.TradePL != 0m)
            {
                this.AddValuedPL(valuedPL.TradePL, BillType.TradePL, updateTime);
            }
        }

        private void AddNotValuedPL(decimal value, BillType billType, DateTime updateTime)
        {
            this.AddPLBillCommon(value, billType, updateTime, false, null);
        }

        private void AddValuedPL(decimal value, BillType billType, DateTime updateTime)
        {
            this.AddPLBillCommon(value, billType, updateTime, true, this.Owner.ExecuteTime);
        }

        private void AddPLBillCommon(decimal value, BillType billType, DateTime updateTime, bool isValued, DateTime? valuedTime)
        {
            this.AddBill(new PLBill(this.AccountId, this.CurrencyId, value, billType, BillOwnerType.Order, valuedTime, isValued, updateTime));
        }

        internal bool IsValid(Quotation quotation)
        {
            return this.PriceTimestamp != null && quotation != null && quotation.Timestamp > this.PriceTimestamp.Value;
        }


        internal void Execute(ExecuteContext context)
        {
            _executeService.Value.Execute(context);
        }

        internal bool ShouldCalculateFilledMarginAndQuantity(bool isBuy)
        {
            return _precheckServcie.Value.ShouldCalculateFilledMarginAndQuantity(isBuy);
        }

        internal decimal CalculatePreCheckNecessary(Price preCheckPrice, decimal? effectiveLot)
        {
            return _precheckServcie.Value.CalculatePreCheckNecessary(preCheckPrice, effectiveLot);
        }

        internal MarginAndQuantityResult CalculateFilledMarginAndQuantity(bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            return _precheckServcie.Value.CalculateFilledMarginAndQuantity(isBuy, remainFilledLotPerOrderDict);
        }

        internal MarginAndQuantityResult CalculateUnfilledMarginAndQuantityForOrder(Price price, decimal? effectiveLot)
        {
            return _precheckServcie.Value.CalculateUnfilledMarginAndQuantityForOrder(price, effectiveLot);
        }


        internal virtual IFees CalculateFee(ExecuteContext context)
        {
            var fees = _feeCalculator.Value.Calculate(context);
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, -fees.CommissionSum, BillType.Commission, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, -fees.LevySum, BillType.Levy, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, -fees.OtherFee, BillType.OtherFee, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            return fees;
        }

        internal void CalculateFeeAsCost(ExecuteContext context)
        {
            _feeCalculator.Value.CalculateFeeAsCost(context);
        }

        internal virtual void UpdateLotBalance(decimal lot, bool isForDelivery)
        {
            _openOrderCalculator.Value.UpdateLotBalance(lot);
        }

        internal decimal CalculatePreCheckBalance()
        {
            return _precheckServcie.Value.CalculatePrecheckBalance();
        }


        internal decimal CalculateBalance(ExecuteContext context)
        {
           return this.SumBillsForBalance();
        }



        internal OrderHitStatus HitMarketOrder(DateTime baseTime, Price marketBuy, Price marketSell, DateTime priceTime)
        {
            if (this.Owner.Type == TransactionType.Pair)
            {
                if (_pairOrderIsHit)
                {
                    _pairOrderIsHit = false;
                }
                else
                {
                    this.InnerHitMarketOrder(baseTime, marketBuy, marketSell, priceTime);

                    Order pairOrder = (Order)(this.Owner.FirstOrder == this ? this.Owner.SecondOrder : this.Owner.FirstOrder);
                    pairOrder.InnerHitMarketOrder(baseTime, marketBuy, marketSell, priceTime);

                    _pairOrderIsHit = true;
                }
            }
            else
            {
                this.InnerHitMarketOrder(baseTime, marketBuy, marketSell, priceTime);
            }

            return OrderHitStatus.None;//always return None, let MarketOrderProcessor to process
        }

        private void InnerHitMarketOrder(DateTime baseTime, Price marketBuy, Price marketSell, DateTime priceTime)
        {
            Price marketOppositePrice = this.IsBuy ? marketSell : marketBuy;

            if (priceTime > this.Owner.SubmitTime)
            {
                var instrument = this.Owner.SettingInstrument();
                Price lastBestPrice = this.BestPrice == null ? this.SetPrice : this.BestPrice;
                if (this.IsBuy == instrument.IsNormal)
                {
                    if (marketOppositePrice > lastBestPrice)
                    {
                        this.BestTime = baseTime;
                        this.BestPrice = marketOppositePrice;
                        this.HitCount++;
                    }
                }
                else
                {
                    if (marketOppositePrice < lastBestPrice)
                    {
                        this.BestTime = baseTime;
                        this.BestPrice = marketOppositePrice;
                        this.HitCount++;
                    }
                }
            }

            if (this.BestPrice == null)
            {
                this.BestPrice = this.SetPrice;
                this.BestTime = this.Owner.SubmitTime;
                this.HitCount = 1;
            }
            this.IsHitReseted = false;
        }



        #region  Interface Implementations
        int IPriceParameterProvider.NumeratorUnit
        {
            get { return _owner.SettingInstrument().NumeratorUnit; }
        }

        int IPriceParameterProvider.Denominator
        {
            get { return _owner.SettingInstrument().Denominator; }
        }

        public bool Equals(Order other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        #endregion

        internal void AddOrderRelation(OrderRelation item, OperationType operationType)
        {
            _orderRelations.AddItem(item, operationType);
        }


        #region Overrided Equal And GetHashCode methods
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var order = obj as Order;
            return this.Equals(order);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public static bool operator ==(Order left, Order right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Order left, Order right)
        {
            return !(left == right);
        }
        #endregion

        public override string ToString()
        {
            StringBuilder info = new StringBuilder();
            info.AppendFormat("Id = {0}, isOpen = {1} , isBuy = {2} ", this.Id, this.IsOpen, this.IsBuy);
            info.AppendFormat("lot = {0}, lotBalance = {1}, phase = {2}", this.Lot, this.LotBalance, this.Phase);
            info.AppendFormat(" accountId = {0}, instrumentId = {1}", this.AccountId, this.Instrument().Id);
            return info.ToString();
        }
    }

}