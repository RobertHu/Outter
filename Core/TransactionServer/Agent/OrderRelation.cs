using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using iExchange.Common;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using Core.TransactionServer.Agent.Physical;
using log4net;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.Reset;

namespace Core.TransactionServer.Agent
{
    public class OrderRelation : BillBusinessRecord, IKeyProvider<Guid>
    {
        internal const int PairRelationFactor = 1;

        private class InternalAutoCloseComparer : IComparer<OrderRelation>
        {
            int IComparer<OrderRelation>.Compare(OrderRelation x, OrderRelation y)
            {
                return Transaction.AutoCloseComparer.Compare(x.OpenOrder.Owner, y.OpenOrder.Owner);
            }
        }

        internal static readonly IComparer<OrderRelation> AutoCloseComparer = new InternalAutoCloseComparer();

        internal static readonly OpenOrderExecuteTimeComparer OpenOrderExecuteTimeAscendingComparer = new OpenOrderExecuteTimeComparer(SortDirection.Ascending);
        internal static readonly OpenOrderExecuteTimeComparer OpenOrderExecuteTimeDescendingComparer = new OpenOrderExecuteTimeComparer(SortDirection.Descending);
        private const int CAPACITY = 20;
        private const int BillListCapacityFactor = 7;
        private Order _openOrder;
        protected OrderRelationSettings _settings;
        private OrderRelationExecuteService _executeService;
        protected Guid _accountId;

        #region Constructors

        internal OrderRelation(OrderRelationConstructParams constructParams)
            : base(BusinessRecordNames.OrderRelation, CAPACITY)
        {
            this.CloseOrder = constructParams.CloseOrder;
            _openOrder = constructParams.OpenOrder;
            _settings = new OrderRelationSettings(this, this.CloseOrder, constructParams);
            _accountId = this.CloseOrder.Owner.Owner.Id;
            this.CloseOrder.AddOrderRelation(this, constructParams.OperationType);
        }

        protected virtual OrderRelationExecuteService InitializeExecuteService()
        {
            return new OrderRelationExecuteService(this, _settings);
        }

        #endregion

        #region Properties
        internal Order OpenOrder { get { return _openOrder; } }

        internal Order CloseOrder { get; private set; }

        internal Guid Id
        {
            get { return _settings.Id; }
        }


        public Guid OpenOrderId
        {
            get { return this.OpenOrder.Id; }
        }

        internal decimal ClosedLot
        {
            get { return _settings.ClosedLot; }
        }

        internal DateTime? CloseTime
        {
            get { return _settings.CloseTime; }
            private set { _settings.CloseTime = value; }
        }

        public decimal Commission
        {
            get { return _settings.Commission; }
            set { _settings.Commission = value; ; }
        }

        public decimal Levy
        {
            get { return _settings.Levy; }
            set { _settings.Levy = value; }
        }

        public decimal OtherFee
        {
            get { return _settings.OtherFee; }
            set { _settings.OtherFee = value; }
        }

        public DateTime? ValueTime
        {
            get { return _settings.ValueTime; }
            set { _settings.ValueTime = value; }
        }

        public int Decimals
        {
            get { return _settings.Decimals; }
            set { _settings.Decimals = value; }
        }

        public decimal RateIn
        {
            get { return _settings.RateIn; }
            set { _settings.RateIn = value; }
        }

        public decimal RateOut
        {
            get { return _settings.RateOut; }
            set { _settings.RateOut = value; }
        }

        internal bool IsValued
        {
            get { return this.ValueTime != null; }
        }

        internal decimal InterestPL
        {
            get { return _settings.InterestPL; }
            set { _settings.InterestPL = value; }
        }

        internal decimal StoragePL
        {
            get { return _settings.StoragePL; }
            set { _settings.StoragePL = value; }
        }

        internal decimal TradePL
        {
            get { return _settings.TradePL; }
            set { _settings.TradePL = value; }
        }


        internal int CurrencyDecimals(DateTime? tradeDay)
        {
            return this.OpenOrder.Owner.Owner.IsMultiCurrency ? this.OpenOrder.Owner.AccountInstrument.Currency(tradeDay).Decimals : this.OpenOrder.Owner.Owner.Setting(tradeDay).Currency(tradeDay).Decimals;
        }



        internal OrderRelationExecuteService ExecuteService
        {
            get
            {
                if (_executeService == null)
                {
                    _executeService = this.InitializeExecuteService();
                }
                return _executeService;
            }
        }

        internal decimal EstimateCloseCommissionOfOpenOrder
        {
            get
            {
                return _settings.EstimateCloseCommissionOfOpenOrder;
            }
            set
            {
                _settings.EstimateCloseCommissionOfOpenOrder = value;
            }
        }

        internal decimal EstimateCloseLevyOfOpenOrder
        {
            get
            {
                return _settings.EstimateCloseLevyOfOpenOrder;
            }
            set
            {
                _settings.EstimateCloseLevyOfOpenOrder = value;
            }
        }


        #endregion


        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.OpenOrder.Id;
        }


        internal PLValue CalculateValuedPL()
        {
            var interestPLValued = this.Value(this.InterestPL);
            var storagePLValued = this.Value(this.StoragePL);
            var tradePLValued = this.GetValuedTradePL(this.TradePL);
            var result = new PLValue(interestPLValued, storagePLValued, tradePLValued);
            return result;
        }


        internal void CalculatePL(ExecuteContext context)
        {
            if (context != null && context.ShouldUseHistorySettings)
            {
                OrderUpdater.Default.UpdateOrderInterestPerLotAndStoragePerLot(_openOrder.Owner, this.CloseOrder.Owner.ExecuteTime.Value.Date.AddDays(-1));
            }
            this.CloseTime = this.CloseOrder.Owner.ExecuteTime;
            if (this.ShouldCalculatePL())
            {
                this.InterestPL = this.CalculateInterestPL(context);
                this.StoragePL = this.CalculateStoragePL(context);
                this.TradePL = this.CalculateTradePL(context);
            }
        }

        private bool ShouldCalculatePL()
        {
            var tran = this.CloseOrder.Owner;
            return tran.OrderType != OrderType.MultipleClose || this.CloseOrder.ExecutePrice != this.OpenOrder.ExecutePrice;
        }

        protected virtual decimal CalculateTradePL(ExecuteContext context)
        {
            Price buyPrice, sellPrice, closePrice;
            if (this.CloseOrder.IsBuy)
            {
                buyPrice = this.CloseOrder.ExecutePrice;
                sellPrice = this.OpenOrder.ExecutePrice;
            }
            else
            {
                buyPrice = this.OpenOrder.ExecutePrice;
                sellPrice = this.CloseOrder.ExecutePrice;
            }
            closePrice = this.CloseOrder.ExecutePrice;
            var instrument = this.CloseOrder.Owner.SettingInstrument(context.TradeDay);
            decimal contractSize = this.OpenOrder.Owner.ContractSize(context.TradeDay);
            var result = TradePLCalculator.Calculate(instrument.TradePLFormula, this.ClosedLot, contractSize, (decimal)buyPrice, (decimal)sellPrice, (decimal)closePrice, CloseOrder.Owner.AccountInstrument.Currency(context.TradeDay).Decimals);
            return result;
        }

        private decimal CalculateInterestPL(ExecuteContext context)
        {
            int decimals = this.CloseOrder.Owner.AccountInstrument.Currency(context.TradeDay).Decimals;
            var result = Math.Round(this.ClosedLot * this.OpenOrder.InterestPerLot, decimals, MidpointRounding.AwayFromZero);
            return result;
        }

        private decimal CalculateStoragePL(ExecuteContext context)
        {
            int decimals = this.CloseOrder.Owner.AccountInstrument.Currency(context.TradeDay).Decimals;
            var result = Math.Round(this.ClosedLot * this.OpenOrder.StoragePerLot, decimals, MidpointRounding.AwayFromZero);
            return result;
        }


        internal virtual void CancelExecute(bool isForDelivery)
        {
            this.OpenOrder.UpdateLotBalance(-this.ClosedLot, isForDelivery);
        }


        internal void DoValue(DateTime? tradeDay)
        {
            if (this.ValueTime != null)
            {
                throw new TransactionServerException(TransactionError.AlreadyValued);
            }
            this.ValueTime = this.CloseTime;
            var currencyRate = this.CloseOrder.Owner.CurrencyRate(tradeDay);
            var account = this.CloseOrder.Owner.Owner;
            if (account.Setting().IsMultiCurrency)
            {
                this.Decimals = currencyRate.SourceCurrency.Decimals;
                this.RateIn = 1;
                this.RateOut = 1;
            }
            else
            {
                this.Decimals = currencyRate.TargetCurrency.Decimals;
                this.RateIn = currencyRate.RateIn;
                this.RateOut = currencyRate.RateOut;
            }
        }

        protected virtual decimal GetValuedTradePL(decimal tradePL)
        {
            return this.Value(tradePL);
        }

        protected decimal Value(decimal amount)
        {
            return Math.Round(amount * (amount > 0 ? this.RateIn : this.RateOut), this.Decimals, MidpointRounding.AwayFromZero);
        }

        internal void CalculateFee(ExecuteContext context)
        {
            var setting = this.GetHistorySetting(context);
            var instrument = setting.Item1;
            var closeTran = this.CloseOrder.Owner;
            var openTran = this.OpenOrder.Owner;
            var currencyRate = setting.Item5;
            var tradePolicyDetail = setting.Item3;
            var specialTradePolicyDetail = setting.Item4;
            decimal contractSize = !context.ShouldUseHistorySettings ? this.OpenOrder.Owner.ContractSize(context.TradeDay) : tradePolicyDetail.ContractSize;
            var account = setting.Item2;
            decimal pairRelationFactor = OrderRelation.PairRelationFactor;
            if (this.CloseOrder.Owner.Type == TransactionType.Pair)
            {
                pairRelationFactor = tradePolicyDetail.PairRelationFactor;
            }

            decimal commission = 0, levy = 0, otherFee = 0;
            if (this.CloseOrder.Owner.OrderType != OrderType.BinaryOption)
            {
                if (instrument.CommissionFormula.IsDependOnPL() || instrument.LevyFormula.IsDependOnPL())
                {
                    this.CalculatePL(context);
                }
                var feeParameter = new FeeParameter
                {
                    Account = account,
                    TradePolicyDetail = tradePolicyDetail,
                    SpecialTradePolicyDetail = specialTradePolicyDetail,
                    Instrument = instrument,
                    CurrencyRate = currencyRate,
                    ContractSize = contractSize,
                    OpenOrderExecuteTime = openTran.ExecuteTime.Value,
                    ClosedLot = this.ClosedLot,
                    ExecutePrice = this.CloseOrder.ExecutePrice,
                    TradePL = this.TradePL,
                    Context = context,
                    PairRelationFactor = pairRelationFactor
                };
                OrderRelation.CalculateFee(feeParameter, out commission, out levy, out otherFee);
            }
            this.Commission = commission;
            this.Levy = levy;
            this.OtherFee = otherFee;
        }

        private Tuple<Settings.Instrument, Settings.Account, Settings.TradePolicyDetail, Settings.SpecialTradePolicyDetail, Settings.CurrencyRate> GetHistorySetting(ExecuteContext context)
        {
            var instrument = this.CloseOrder.Owner.SettingInstrument(context.TradeDay);
            var account = this.CloseOrder.Owner.Owner.Setting(context.TradeDay);
            var currencyRate = this.CloseOrder.Owner.CurrencyRate(context.TradeDay);
            var tradePolicyDetail = this.CloseOrder.Owner.TradePolicyDetail(context.TradeDay);
            var specialTradePolicyDetail = this.CloseOrder.Owner.SpecialTradePolicyDetail(context.TradeDay);
            return Tuple.Create(instrument, account, tradePolicyDetail, specialTradePolicyDetail, currencyRate);
        }


        internal static void CalculateFee(FeeParameter feeParameter, out decimal commission, out decimal levy, out decimal otherFee)
        {
            commission = CalculateCommission(feeParameter);
            otherFee = CalculateOtherFee(feeParameter);
            levy = CalculateLevy(feeParameter);
        }

        private static decimal CalculateOtherFee(FeeParameter feeParameter)
        {
            decimal otherFee = feeParameter.TradePolicyDetail.OtherFeeClose;
            otherFee = FeeCalculator.CalculateLevy(feeParameter.Instrument.OtherFeeFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateOtherFee * otherFee, feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate);
            return otherFee;
        }


        private static decimal CalculateLevy(FeeParameter feeParameter)
        {
            decimal levy = feeParameter.TradePolicyDetail.LevyClose;
            if (!feeParameter.Instrument.LevyFormula.IsDependOnPL() && feeParameter.SpecialTradePolicyDetail != null && feeParameter.SpecialTradePolicyDetail.IsFractionLevyOn)
            {
                decimal fractionLevy = feeParameter.SpecialTradePolicyDetail.LevyClose;

                levy = FeeCalculator.CalculateLevy(feeParameter.Instrument.LevyFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateLevy * levy, (int)feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate)
                    + FeeCalculator.CalculateLevy(feeParameter.Instrument.LevyFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateLevy * fractionLevy, feeParameter.ClosedLot - (int)feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate);
            }
            else
            {
                levy = FeeCalculator.CalculateLevy(feeParameter.Instrument.LevyFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateLevy * levy, feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate, feeParameter.TradePL);
            }

            if (!feeParameter.Instrument.LevyFormula.IsDependOnPL() && feeParameter.SpecialTradePolicyDetail != null)
            {
                CurrencyRate cgseLevyCurrencyRate = FeeCalculator.GetCGSELevyCurrencyRate(feeParameter.Account, feeParameter.Instrument, feeParameter.SpecialTradePolicyDetail, feeParameter.CurrencyRate, feeParameter.Context);
                levy += FeeCalculator.CalculateCGSELevy(feeParameter.ClosedLot, false, feeParameter.SpecialTradePolicyDetail, cgseLevyCurrencyRate);
            }
            return levy;
        }


        private static decimal CalculateCommission(FeeParameter feeParameter)
        {
            var context = feeParameter.Context;
            var tradeDay = context != null && context.ShouldUseHistorySettings ? Settings.Setting.Default.GetTradeDay(context.TradeDay) : Settings.Setting.Default.GetTradeDay();
            bool isDayCloseRelation = feeParameter.OpenOrderExecuteTime >= tradeDay.BeginTime;
            decimal commission = feeParameter.PairRelationFactor * feeParameter.TradePolicyDetail.GetCommissionClose(isDayCloseRelation);
            if (!feeParameter.Instrument.CommissionFormula.IsDependOnPL() && feeParameter.SpecialTradePolicyDetail != null && feeParameter.SpecialTradePolicyDetail.IsFractionCommissionOn)
            {
                decimal fractionCommission = feeParameter.PairRelationFactor * feeParameter.SpecialTradePolicyDetail.GetCommissionClose(isDayCloseRelation);

                commission = FeeCalculator.CalculateCommission(feeParameter.Instrument.CommissionFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateCommission * commission, (int)feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate)
                    + FeeCalculator.CalculateCommission(feeParameter.Instrument.CommissionFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateCommission * fractionCommission, feeParameter.ClosedLot - (int)feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate);
            }
            else
            {
                commission = FeeCalculator.CalculateCommission(feeParameter.Instrument.CommissionFormula, feeParameter.Instrument.TradePLFormula, feeParameter.Account.RateCommission * commission, feeParameter.ClosedLot, feeParameter.ContractSize, feeParameter.ExecutePrice, feeParameter.CurrencyRate, feeParameter.TradePL);
            }
            return commission;
        }

    }


    internal sealed class FeeParameter
    {
        internal FeeParameter()
        {
            this.PairRelationFactor = OrderRelation.PairRelationFactor;
        }

        private FeeParameter(ExecuteContext context, Order openOrder)
            : this()
        {
            this.Context = context;
            var tran = openOrder.Owner;
            var tradeDay = context.TradeDay;
            this.Account = context.Account.Setting(tradeDay);
            this.TradePolicyDetail = tran.TradePolicyDetail(tradeDay);
            this.SpecialTradePolicyDetail = tran.SpecialTradePolicyDetail(tradeDay);
            this.Instrument = tran.SettingInstrument(tradeDay);
            this.CurrencyRate = tran.CurrencyRate(tradeDay);
            this.ContractSize = tran.ContractSize(tradeDay);

            this.OpenOrderExecuteTime = openOrder.Owner.ExecuteTime.Value;
            this.ClosedLot = openOrder.LotBalance;
            this.ExecutePrice = openOrder.ExecutePrice;
            this.TradePL = 0m;
        }

        internal static FeeParameter CreateByOpenOrder(ExecuteContext context, Order openOrder)
        {
            return new FeeParameter(context, openOrder);
        }

        internal DateTime OpenOrderExecuteTime { get; set; }
        internal decimal ClosedLot { get; set; }
        internal Price ExecutePrice { get; set; }
        internal decimal TradePL { get; set; }

        internal ExecuteContext Context { get; set; }

        internal Settings.Account Account { get; set; }
        internal TradePolicyDetail TradePolicyDetail { get; set; }
        internal SpecialTradePolicyDetail SpecialTradePolicyDetail { get; set; }
        internal Settings.Instrument Instrument { get; set; }
        internal CurrencyRate CurrencyRate { get; set; }
        internal decimal ContractSize { get; set; }

        internal decimal PairRelationFactor { get; set; }

    }


}