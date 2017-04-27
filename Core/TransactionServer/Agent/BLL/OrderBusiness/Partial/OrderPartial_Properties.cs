using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent
{
    public partial class Order
    {
        public DateTime? PriceTimestamp
        {
            get
            {
                return _orderSettings.PriceTimestamp;
            }
        }

        public Guid Id
        {
            get { return _orderSettings.Id; }
            private set { _orderSettings.Id = value; }
        }

        internal string Code
        {
            get { return _orderSettings.Code; }
            set { _orderSettings.Code = value; }
        }

        internal string OriginCode { get { return _orderSettings.OriginCode; } }

        internal string BlotterCode { get { return _orderSettings.BlotterCode; } }

        internal decimal OriginalLot { get { return _orderSettings.OriginalLot; } }

        internal decimal Lot
        {
            get { return _orderSettings.Lot; }
            private set { _orderSettings.Lot = value; }
        }

        public decimal LotBalance
        {
            get { return _orderSettings.LotBalance; }
            set { _orderSettings.LotBalance = value; }
        }


        internal decimal LevySum
        {
            get
            {
                return this.GetBillValue(BillType.Levy);
            }
        }

        internal decimal CommissionSum
        {
            get
            {
                return this.GetBillValue(BillType.Commission);
            }
        }

        internal decimal OtherFeeSum
        {
            get
            {
                return this.GetBillValue(BillType.OtherFee);
            }
        }

        internal decimal InterestPLValued
        {
            get { return this.GetValuedBill(BillType.InterestPL); }
        }

        internal decimal StoragePLValued
        {
            get { return this.GetValuedBill(BillType.StoragePL); }
        }

        internal decimal TradePLValued
        {
            get
            {
                return this.GetValuedBill(BillType.TradePL);
            }
        }

        internal decimal InterestPLNotValued
        {
            get
            {
                return this.GetNotValuedBill(BillType.InterestPL);
            }
        }

        internal decimal StoragePLNotValued
        {
            get
            {
                return this.GetNotValuedBill(BillType.StoragePL);
            }
        }

        internal decimal TradePLNotValued
        {
            get
            {
                return this.GetNotValuedBill(BillType.TradePL);
            }
        }

        internal decimal InterestPLFloat
        {
            get { return _openOrderCalculator.Value.FloatPLCalculator.InterestPLFloat; }
        }

        internal decimal StoragePLFloat
        {
            get { return _openOrderCalculator.Value.FloatPLCalculator.StoragePLFloat; }
        }

        internal decimal TradePLFloat
        {
            get { return _openOrderCalculator.Value.FloatPLCalculator.TradePLFloat; }
        }

        internal decimal Necessary
        {
            get { return _openOrderCalculator.Value.FloatPLCalculator.Necessary; }
        }

        internal IEnumerable<OrderRelation> OrderRelations
        {
            get
            {
                if (!this.IsOpen)
                {
                    return _orderRelations.GetValues();
                }
                else
                {
                    return _openOrderCalculator.Value.GetAllOrderRelations();
                }
            }
        }

        internal int OrderRelationsCount
        {
            get { return _orderRelations.Count; }
        }


        internal TradeOption TradeOption
        {
            get { return _orderSettings.TradeOption; }
        }

        internal NotValuedDayInterestAndStorage NotValuedDayInterestAndStorage
        {
            get { return _orderSettings.NotValuedDayInterestAndStorage; }
        }

        public decimal CanBeClosedLot
        {
            get
            {
                if (!this.IsOpen) return 0m;
                return _openOrderCalculator.Value.CanBeClosedLot;
            }
        }

        private HitOrderSettings HitSettings
        {
            get { return _orderSettings.HitSettings; }
        }



        public Price SetPrice
        {
            get { return _orderSettings.SetPrice; }
        }

        internal Price SetPrice2
        {
            get { return _orderSettings.SetPrice2; }
        }

        public Price ExecutePrice
        {
            get { return _orderSettings.ExecutePrice; }
            set { _orderSettings.ExecutePrice = value; }
        }

        internal Price LivePrice
        {
            get { return _openOrderCalculator.Value.FloatPLCalculator.LivePrice; }
            private set
            {
                _openOrderCalculator.Value.FloatPLCalculator.LivePrice = value;
            }
        }

        public Price BestPrice
        {
            get { return this.HitSettings.BestPrice; }
            protected set { this.HitSettings.BestPrice = value; }
        }

        public int HitCount
        {
            get { return this.HitSettings.HitCount; }
            set { this.HitSettings.HitCount = value; }
        }

        public DateTime? BestTime
        {
            get { return this.HitSettings.BestTime; }
            protected set { this.HitSettings.BestTime = value; }
        }

        public OrderHitStatus HitStatus
        {
            get { return this.HitSettings.HitStatus; }
            private set { this.HitSettings.HitStatus = value; }
        }


        internal int DQMaxMove
        {
            get { return _orderSettings.DQMaxMove; }
        }

        public Transaction Owner
        {
            get { return this._owner; }
        }

        internal decimal StoragePerLot
        {
            get { return _orderSettings.StoragePerLot; }
            set { _orderSettings.StoragePerLot = value; }
        }

        internal decimal InterestPerLot
        {
            get { return _orderSettings.InterestPerLot; }
            set { _orderSettings.InterestPerLot = value; }
        }


        internal CurrencyRate CurrencyRate
        {
            get
            {
                return this.Owner.CurrencyRate(null);
            }
        }

        internal CurrencyRate EstimateCurrencyRate(DateTime? tradeDay)
        {
            var sourceCurrencyId = this.Owner.SettingInstrument(tradeDay).CurrencyId;
            var targetCurrencyId = this.Account.Setting(tradeDay).CurrencyId;
            return Setting.Default.GetCurrencyRate(sourceCurrencyId, targetCurrencyId, tradeDay);
        }



        public bool IsOpen
        {
            get { return _orderSettings.IsOpen; }
            private set { _orderSettings.IsOpen = value; ; }
        }

        public OrderPhase Phase
        {
            get { return _orderSettings.Phase; }
            set { _orderSettings.Phase = value; }
        }

        internal bool IsExecuted
        {
            get { return this.Phase == OrderPhase.Executed; }
        }

        internal Settings.Instrument Instrument(DateTime? tradeDay = null)
        {
            return this.Owner.SettingInstrument(tradeDay);
        }


        internal bool IsBuy
        {
            get { return _orderSettings.IsBuy; }
        }


        internal decimal QuantityBalance
        {
            get { return this.LotBalance * this.Owner.ContractSize(null); }
        }


        internal OrderType OrderType
        {
            get { return this.Owner.OrderType; }
        }

        internal bool IsSpotOrder
        {
            get
            {
                return this.OrderType == OrderType.SpotTrade;
            }
        }

        internal bool IsLimitOrder
        {
            get
            {
                return this.OrderType == OrderType.Limit || this.OrderType == OrderType.OneCancelOther;
            }
        }

        internal virtual bool IsRisky
        {
            get { return true; }
        }

        internal virtual bool IsFreeOfNecessaryCheck
        {
            get { return false; ; }
        }


        internal bool IsPhysical
        {
            get { return _owner.InstrumentCategory == InstrumentCategory.Physical; }
        }

        internal bool ShouldSportOrderDelayFill
        {
            get
            {
                if (!this.IsSpotOrder) return false;
                TimeSpan autoDQDelay = this.Owner.DealingPolicyPayload().AutoDQDelay;
                bool shouldDelayAutoFill = this.Phase == OrderPhase.Placed && this.ShouldAutoFill && autoDQDelay > TimeSpan.Zero;
                if (shouldDelayAutoFill && _orderSettings.AutoFillDelayTime == null)
                {
                    _orderSettings.AutoFillDelayTime = autoDQDelay;
                }
                return shouldDelayAutoFill;
            }
        }

        internal bool ShouldAutoFill
        {
            get
            {
                if (this.Owner.SettingInstrument().IsAutoFill)
                {
                    OrderType orderType = this.Owner.OrderType;
                    if (this.IsSpotOrder)
                    {
                        return this.Lot <= this.Owner.DealingPolicyPayload().AutoDQMaxLot;
                    }
                    else if (orderType == OrderType.Limit || orderType == OrderType.Market)
                    {
                        return this.Lot <= this.Owner.DealingPolicyPayload().AutoLmtMktMaxLot;
                    }
                }
                return false;
            }
        }

        internal int CurrencyDecimals(DateTime? tradeDay)
        {
            var tran = this.Owner;
            var account = tran.Owner;
            return account.Setting().IsMultiCurrency ? tran.AccountInstrument.Currency(tradeDay).Decimals : account.Setting(tradeDay).Currency(tradeDay).Decimals;
        }


        internal int SetPriceMaxMovePips
        {
            get { return _orderSettings.SetPriceMaxMovePips; }
        }

        internal DateTime? InterestValueDate
        {
            get { return _orderSettings.InterestValueDate; }
        }
    }
}
