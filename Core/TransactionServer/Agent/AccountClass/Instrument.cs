using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using CommonPrice = iExchange.Common.Price;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using log4net;
using Core.TransactionServer.Agent.Util;
using System.Data;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.DB;
namespace Core.TransactionServer.Agent.AccountClass
{
    internal class Instrument : BusinessRecord, IEquatable<Instrument>, Framework.IKeyProvider<Guid>
    {
        private sealed class QuotationManager
        {
            private QuotationBulk _bulk;
            private Account _account;
            private Instrument _instrument;


            internal QuotationManager(Account account, Instrument instrument, QuotationBulk bulk)
            {
                _account = account;
                _instrument = instrument;
                _bulk = bulk;
            }

            internal bool UpdateQuotationAndCheckIsRiskRised(QuotationBulk bulk, Quotation quotation, DateTime baseTime)
            {
                Quotation lastQuotation = this.GetQuotation(_account);
                _bulk = bulk;
                if (lastQuotation != null)
                {
                    QuotationTrend trend = quotation.CalculateTrend(lastQuotation);
                    if ((_instrument.TotalBuyQuantity > _instrument.TotalSellQuantity && trend == QuotationTrend.Down)
                        || (_instrument.TotalSellQuantity > _instrument.TotalBuyQuantity && trend == QuotationTrend.Up))
                    {
                        return true;
                    }
                    return false;
                }
                return true;

            }

            internal Quotation GetQuotation(IQuotePolicyProvider provider)
            {
                Quotation quotation;
                _bulk.TryGetQuotation(_instrument.Id, provider ?? _account, out quotation);
                return quotation;
            }


            internal bool HasTradingQuotation(IQuotePolicyProvider provider)
            {
                if (_bulk == null) return false;
                Quotation quotation;
                if (_bulk.TryGetQuotation(_instrument.Id, provider, out quotation))
                {
                    if (quotation.Timestamp >= _instrument.Setting.DayOpenTime) return true;
                }
                return false;
            }
        }


        private static readonly ILog Logger = LogManager.GetLogger(typeof(Instrument));
        protected Account _owner;
        private RiskData _riskRawData;
        private CuttingFee _cuttingFee;
        private Lazy<LotCalculator> _lotCalculator;
        private Lazy<OrderCollector> _orderCollector;
        private Lazy<InstrumentCalculator> _calculator;
        private BusinessItem<DateTime?> _lastResetDay;
        private BusinessItem<Guid> _id;
        private BusinessItem<Guid> _accountId;
        private BusinessRecordDictionary<DateTime, InstrumentResetItem> _resetItemHistoryDict;
        private CurrencyRate _currencyRate;
        private CacheData<List<Order>> _waitingForHitOrders;
        private CacheData<List<Order>> _executedAndHasPositionOrders;
        private CacheData<decimal> _totalBuyQuantity;
        private CacheData<decimal> _TotalSellQuantity;
        private QuotationManager _quotationManager;

        #region Constructors
        internal Instrument(Account owner, Guid id, QuotationBulk initQuotation, InstrumentServiceFactory factory)
            : base("Instrument", 15)
        {
            _owner = owner;
            this.Id = id;
            this.QuotationBulk = initQuotation;
            _orderCollector = new Lazy<OrderCollector>(() => factory.CreateOrderCollector(this));
            _lotCalculator = new Lazy<LotCalculator>(() => factory.CreateLotCalculator(this));
            _calculator = new Lazy<InstrumentCalculator>(() => factory.CreateInstrumentCalculator(this));
            _riskRawData = new RiskData(null);
            _cuttingFee = new CuttingFee(this);
            _lastResetDay = BusinessItemFactory.Create<DateTime?>("LastResetDay", null, PermissionFeature.Sound, this);
            _id = BusinessItemFactory.Create("ID", id, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create("AccountID", owner.Id, PermissionFeature.Key, this);
            _resetItemHistoryDict = new BusinessRecordDictionary<DateTime, InstrumentResetItem>("ResetItemHistory", this);
            _currencyRate = this.DoGetCurrencyRate(null);
            _waitingForHitOrders = new CacheData<List<Order>>(_orderCollector.Value.CollectWaitingForHitOrders);
            _executedAndHasPositionOrders = new CacheData<List<Order>>(_orderCollector.Value.CollectExecutedAndHasPositionOrders);
            _totalBuyQuantity = new CacheData<decimal>(_lotCalculator.Value.CalculateBuyQuantity);
            _TotalSellQuantity = new CacheData<decimal>(_lotCalculator.Value.CalculateSellQuantity);
            _quotationManager = new QuotationManager(owner, this, initQuotation);
        }

        #endregion

        #region Properties

        internal DateTime? LastResetDay
        {
            get
            {
                var result = _lastResetDay.Value;
                if (result != null) return result;
                return this.LastPositionDay;
            }
            set
            {
                _lastResetDay.SetValue(value);
            }
        }


        internal DateTime? LastPositionDay
        {
            get
            {
                var trans = this.GetTransactions();
                if (trans == null || trans.Count == 0) return null;
                DateTime minExecuteTime = DateTime.MaxValue;
                foreach (var eachTran in trans)
                {
                    if (eachTran.ExecuteTime != null && eachTran.ExecuteTime < minExecuteTime && eachTran.HasPosition)
                    {
                        minExecuteTime = eachTran.ExecuteTime.Value;
                    }
                }
                if (minExecuteTime < DateTime.MaxValue)
                {
                    return DB.DBRepository.Default.GetTradeDay(minExecuteTime).AddDays(-1);
                }
                else
                {
                    return null;
                }
            }
        }


        internal LotCalculator LotCalculator
        {
            get
            {
                return _lotCalculator.Value;
            }
        }

        public Guid Id { get; private set; }

        internal Account Owner
        {
            get { return this._owner; }
        }

        internal Guid CurrencyId
        {
            get { return this.Setting.CurrencyId; }
        }

        private QuotationBulk QuotationBulk { get; set; }

        internal OrderCollector OrderCollector
        {
            get { return _orderCollector.Value; }
        }

        internal InstrumentCalculator Calculator
        {
            get { return _calculator.Value; }
        }

        internal decimal TotalBuyQuantity
        {
            get { return _totalBuyQuantity.Value; }
        }

        internal decimal TotalSellQuantity
        {
            get { return _TotalSellQuantity.Value; }
        }

        public decimal TotalBuyLotBalance
        {
            get
            {
                return this.LotCalculator.CalculateBuyLotBalance();
            }
        }

        public decimal TotalSellLotBalance
        {
            get
            {
                return this.LotCalculator.CalculateSellLotBalance();
            }
        }

        public decimal TotalBuyMargin
        {
            get
            {
                return this.Calculator.CalculateBuyMargin();
            }
        }

        public decimal TotalSellMargin
        {
            get
            {
                return this.Calculator.CalculateSellMargin();
            }
        }

        internal List<Order> WaitingForHitOrders
        {
            get { return _waitingForHitOrders.Value; }
        }

        internal List<Order> ExecutedAndHasPositionOrders
        {
            get { return _executedAndHasPositionOrders.Value; }
        }

        internal ICollection<Order> NotValuedOrders
        {
            get { return this.OrderCollector.CollectNotValuedOrders(); }
        }

        internal Currency Currency(DateTime? tradeDay = null)
        {
            return Settings.Setting.Default.GetCurrency(this.CurrencyId, tradeDay);
        }

        internal CurrencyRate CurrencyRate(DateTime? tradeDay = null)
        {
            if (tradeDay == null) return _currencyRate;
            return this.DoGetCurrencyRate(tradeDay);
        }

        internal RiskData RiskRawData
        {
            get { return this._riskRawData; }
        }

        internal Settings.Instrument Setting
        {
            get
            {
                return Settings.Setting.Default.GetInstrument(this.Id);
            }
        }

        internal BLL.InstrumentBusiness.TradingInstrument Trading
        {
            get { return TradingSetting.Default.GetInstrument(this.Id); }
        }

        internal TradePolicyDetail TradePolicyDetail(DateTime? tradeDay = null)
        {
            return _owner.Setting(tradeDay).TradePolicy(tradeDay)[this.Id, tradeDay];
        }

        internal SpecialTradePolicyDetail SpecialTradePolicyDetail(DateTime? tradeDay)
        {
            var specialTradePolicy = _owner.Setting(tradeDay).SpecialTradePolicy(tradeDay);
            if (specialTradePolicy != null)
            {
                return specialTradePolicy[this.Id];
            }
            else
            {
                return null;
            }
        }


        internal bool IsPhysical
        {
            get { return this.Setting.Category == InstrumentCategory.Physical; }
        }

        internal CuttingFee CuttingFee
        {
            get { return _cuttingFee; }
        }

        internal IEnumerable<InstrumentResetItem> ResetItems
        {
            get
            {
                return _resetItemHistoryDict.GetValues();
            }
        }

        #endregion


        internal void InvalidateCache()
        {
            _waitingForHitOrders.Clear();
            _executedAndHasPositionOrders.Clear();
            _totalBuyQuantity.Clear();
            _TotalSellQuantity.Clear();
        }

        private CurrencyRate DoGetCurrencyRate(DateTime? tradeDay)
        {
            var targetCurrencyId = this.Owner.IsMultiCurrency ? this.CurrencyId : this.Owner.Setting(tradeDay).CurrencyId;
            return Settings.Setting.Default.GetCurrencyRate(this.CurrencyId, targetCurrencyId, tradeDay);
        }

        internal InstrumentResetItem GetResetItem(DateTime tradeDay)
        {
            InstrumentResetItem result;
            if (!_resetItemHistoryDict.TryGetValue(tradeDay, out result))
            {
                var histories = ResetManager.Default.GetAccountInstrumentResetHistory(_owner.Id, this.Id, tradeDay);
                if (histories == null || histories.Count() == 0) return null;
                result = new InstrumentResetItem(histories.Single());
                _resetItemHistoryDict.AddItem(result, OperationType.AsNewRecord);
            }
            return _resetItemHistoryDict[tradeDay];
        }

        internal void AddResetItem(DateTime tradeDay, InstrumentResetItem resetItem)
        {
            if (_resetItemHistoryDict.ContainsKey(tradeDay))
            {
                Logger.WarnFormat("AddResetItem already Exists tradeDay = {0}, instrumentId ={1}, resetItem = {2}, previousResetItem = {3}", tradeDay, this.Id, resetItem,
                    _resetItemHistoryDict[tradeDay]);
            }
            else
            {

                _resetItemHistoryDict.AddItem(resetItem, OperationType.AsNewRecord);
            }
        }

        internal void ClearResetItems()
        {
            _resetItemHistoryDict.Clear();
        }

        internal List<Transaction> GetTransactions()
        {
            return _owner.GetTransactions(this.Id);
        }

        internal bool UpdateQuotationAndCheckIsRiskRised(QuotationBulk bulk, Quotation quotation, DateTime baseTime)
        {
            return _quotationManager.UpdateQuotationAndCheckIsRiskRised(bulk, quotation, baseTime);
        }

        internal Quotation GetQuotation(IQuotePolicyProvider provider = null)
        {
            return _quotationManager.GetQuotation(provider);
        }


        internal bool HasTradingQuotation(IQuotePolicyProvider provider)
        {
            return _quotationManager.HasTradingQuotation(provider);
        }

        internal void Calculate(DateTime baseTime, CalculateType calculateType, Quotation quotation)
        {
            this.Calculator.Calculate(baseTime, calculateType, quotation);
        }

        internal void CalcuateFeeForCutting()
        {
            this.Calculator.CalculateFeeForCutting();
        }

        internal BuySellLot GetBuySellLotBalance()
        {
            var buyLot = this.TotalBuyLotBalance;
            var sellLot = this.TotalSellLotBalance;
            return new BuySellLot(buyLot, sellLot);
        }

        internal bool HasTradePrice(IQuotePolicyProvider provider)
        {
            return this.HasTradingQuotation(provider);
        }

        public bool IsPriceInRangeOfAutoFill(bool isBuy, Price price, Quotation quotation, IQuotePolicyProvider quotePolicyProvider)
        {
            var quotePolicyDetail = Settings.Setting.Default.GetQuotePolicyDetail(quotePolicyProvider, this.Id);

            if (this.Setting.IsNormal ^ isBuy)
            {
                if (quotePolicyDetail.IsOriginHiLo)
                {
                    return price <= quotation.High && price >= (quotation.Low - (quotePolicyDetail.SpreadPoints - this.Setting.MaxMinAdjust));
                }
                else
                {
                    return price <= (quotation.High - (quotePolicyDetail.SpreadPoints + this.Setting.MaxMinAdjust)) && price >= quotation.Low;
                }
            }
            else
            {
                if (quotePolicyDetail.IsOriginHiLo)
                {
                    return price <= (quotation.High + (quotePolicyDetail.SpreadPoints - this.Setting.MaxMinAdjust)) && price >= quotation.Low;
                }
                else
                {
                    return price <= quotation.High && price >= (quotation.Low + (quotePolicyDetail.SpreadPoints + this.Setting.MaxMinAdjust));
                }
            }
        }

        internal bool IsFreeOfFillMarginCheck(BuySellLot oldLots, bool existsCloseOrder)
        {
            TradePolicy tradePolicy = this.Owner.Setting().TradePolicy();
            BuySellLot newLots = this.GetBuySellLotBalance();
            if (Math.Abs(newLots.NetPosition) <= Math.Abs(oldLots.NetPosition))
            {
                if (tradePolicy.IsFreeOverHedge) return true;
                if ((newLots.NetPosition * oldLots.NetPosition >= 0) && (existsCloseOrder || tradePolicy.IsFreeHedge))
                {
                    return true;
                }
            }
            Logger.InfoFormat("IsFreeOfFillMarginCheck, netLots = {0}, oldLots = {1}, accountId = {2}", newLots.ToString(), oldLots.ToString(), _owner.Id);
            return false;
        }

        internal void CalculateNetAndHedgeNecessary(decimal buyNecessarySum, decimal sellNecessarySum,
               decimal buyQuantitySum, decimal sellQuantitySum, decimal partialPhysicalNecessarySum,
               out decimal netNecessary, out decimal hedgeNecessary)
        {
            this.Calculator.CalculateNetAndHedgeNecessary(buyNecessarySum, sellNecessarySum, buyQuantitySum, sellQuantitySum, ref partialPhysicalNecessarySum, out netNecessary, out hedgeNecessary);
        }

        public override string ToString()
        {
            return string.Format("Id={0}", this.Id);
        }

        public bool Equals(Instrument other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Instrument;
            return this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public static bool operator ==(Instrument left, Instrument right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Instrument left, Instrument right)
        {
            return !(left == right);
        }


        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.Id;
        }
    }


}