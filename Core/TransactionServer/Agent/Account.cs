using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using iExchange.Common;

using CachingAssistant = iExchange.Common.Caching.Transaction.Assistant;
using Core.TransactionServer.Agent.BLL.TypeExtensions;
using System.Data;
using CommonPrice = iExchange.Common.Price;
using System.Diagnostics;
using log4net;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.AccountBusiness;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Protocal;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.Reset.Exceptions;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.BroadcastBLL;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Protocal.CommonSetting;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;

namespace Core.TransactionServer.Agent
{
    public sealed class Account : BusinessRecord, IQuotePolicyProvider
    {
        private sealed class AccountAdapter
        {
            private BusinessItem<bool> _isMultiCurrency;
            private BusinessItem<AccountType> _accountType;
            private BusinessItem<decimal> _minUpkeepEquity;
            private BusinessItem<string> _customerName;
            private BusinessItem<string> _currencyCode;
            private BusinessItem<string> _code;
            private BusinessItem<decimal> _creditAmount;
            private BusinessItem<decimal> _rateMarginD;
            private BusinessItem<decimal> _rateMarginO;
            private BusinessItem<decimal> _rateMarginLockD;
            private BusinessItem<decimal> _rateMarginLockO;


            internal AccountAdapter(Account parent)
            {
                _customerName = BusinessItemFactory.CreateVolatileItem("CustomerName", () => parent.Customer == null ? string.Empty : parent.Customer.Name, parent);
                _isMultiCurrency = BusinessItemFactory.Create("IsMultiCurrency", parent.Setting().IsMultiCurrency, PermissionFeature.Key, parent);
                _minUpkeepEquity = BusinessItemFactory.CreateVolatileItem("MinUpkeepEquity", () => parent.Setting().RiskActionMinimumEquity ?? parent.SumFund.RiskRawData.MinEquityAvoidRiskLevel3, parent);
                _accountType = BusinessItemFactory.CreateVolatileItem("Type", () => parent.Setting().Type, parent);
                _currencyCode = BusinessItemFactory.CreateVolatileItem("CurrencyCode", () => parent.Setting().Currency(null).Code, parent);
                _code = BusinessItemFactory.CreateVolatileItem("Code", () => parent.Setting().Code, parent);
                _creditAmount = BusinessItemFactory.CreateVolatileItem("CreditAmount", () => parent.Setting().CreditAmount, parent);
                _rateMarginD = BusinessItemFactory.CreateVolatileItem("RateMarginD", () => parent.Setting().RateMarginD, parent);
                _rateMarginO = BusinessItemFactory.CreateVolatileItem("RateMarginO", () => parent.Setting().RateMarginO, parent);
                _rateMarginLockD = BusinessItemFactory.CreateVolatileItem("RateMarginLockD", () => parent.Setting().RateMarginLockD, parent);
                _rateMarginLockO = BusinessItemFactory.CreateVolatileItem("RateMarginLockO", () => parent.Setting().RateMarginLockO, parent);
            }

        }
        #region Fields
        private const int DEFAULT_ITEMS_CAPACITY = 30;
        private const int DEFAULT_ORDER_CACHE_CAPACITY = 10;
        private const int DEFAULT_FUNDS_CAPACITY_Factor = 4;
        private const int DEFAULT_PENDING_CONFIRM_LIMIT_ORDERS_CAPACITY = 30;
        private const int DEFAULAT_DELIVERY_REQUEST_FACTOR = 33;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(Account));
        private object _mutex = new object();
        private BusinessItem<Guid> _id;
        private BusinessItem<Guid> _currencyId;
        private TransactionCache _transactions;
        private DeliveryRequestCache _deliveryRequests = null;
        private Fund _fund;
        private BusinessRecordDictionary<Guid, SubFund> _subFunds;
        private UnclearDepositManager _unclearDepositManager;
        private bool _hasUnassignedOvernightOrders;
        private bool _isLocked;
        private AccountRisk _accountRisk;
        private Lazy<AccountNecessaryCheckService> _necessaryCheckService;
        private Lazy<BLL.AccountBusiness.BroadcastService> _broadcastService;
        private Lazy<HitService> _hitService;
        private Lazy<AccountResetService> _resetService;
        private InstrumentManager _instrumentManager;
        private BusinessRecordDictionary<Guid, OrderResetItem> _resetOrderDict;
        private BusinessRecordList<Reset.ResetBalance> _resetBalances;
        private BusinessRecordList<Bill> _bills;
        private AccountMoneyManager _moneyManager;
        private List<Order> _pendingConfirmLimitOrders = new List<Order>();
        private Settings.Account _settingAccount;
        private Guid _plainId;
        private BusinessItem<int?> _leverage;
        private BusinessItem<Int64> _version;
        private BusinessItem<decimal> _estimateCloseCommission;
        private BusinessItem<decimal> _estimateCloseLevy;

        internal static readonly Guid TEST_ACCOUNTID = Guid.Parse("640D1A8F-BADF-4C3A-8928-005EA23A54FF");

        #endregion

        internal Account(IDBRow dataRow)
            : base(BusinessRecordNames.Account, DEFAULT_ITEMS_CAPACITY)
        {
            this.State = AccountState.Initialize;
            _hasUnassignedOvernightOrders = (bool)dataRow["HasUnassignedOvernightOrders"];
            _isLocked = (bool)dataRow["IsLocked"];
            Guid accountId = (Guid)dataRow["ID"];
            Int64 version = 0;
            if (dataRow["Version"] != DBNull.Value)
            {
                version = (Int64)dataRow["Version"];
            }
            this.Initialize(accountId, version);
            _accountRisk = new AccountRisk(this, dataRow, _instrumentManager);
            this.State = AccountState.None;
        }

        internal Account(Guid accountId)
            : base(BusinessRecordNames.Account, DEFAULT_ITEMS_CAPACITY)
        {
            this.Initialize(accountId, 0);
            _accountRisk = new AccountRisk(this, _instrumentManager);
        }


        private void Initialize(Guid accountId, Int64 version)
        {
            _instrumentManager = new InstrumentManager(this);
            _plainId = accountId;
            _id = BusinessItemFactory.Create(AccountBusinessItemNames.Id, accountId, PermissionFeature.Key, this);
            _settingAccount = Settings.Setting.Default.GetAccount(accountId);
            _currencyId = BusinessItemFactory.Create("CurrencyID", this.Setting().CurrencyId, PermissionFeature.Key, this);
            _leverage = BusinessItemFactory.Create<int?>("Leverage", null, PermissionFeature.Sound, this);
            _version = BusinessItemFactory.Create<Int64>("Version", version, PermissionFeature.Sound, this);
            new AccountAdapter(this);
            _fund = new Fund(this);
            _subFunds = new BusinessRecordDictionary<Guid, SubFund>(BusinessRecordCollectionNames.Funds, this, DEFAULT_FUNDS_CAPACITY_Factor);
            _transactions = new TransactionCache(this, DEFAULT_ORDER_CACHE_CAPACITY);
            _deliveryRequests = new DeliveryRequestCache(this, DEFAULAT_DELIVERY_REQUEST_FACTOR);
            _unclearDepositManager = new UnclearDepositManager(this);
            _necessaryCheckService = new Lazy<AccountNecessaryCheckService>(() => new AccountNecessaryCheckService(this, _fund));
            _broadcastService = new Lazy<BroadcastService>(() => new BroadcastService(this));
            _resetService = new Lazy<AccountResetService>(() => new AccountResetService(this, _instrumentManager));
            _resetOrderDict = new BusinessRecordDictionary<Guid, OrderResetItem>("ResetOrders", this, 50);
            _resetBalances = new BusinessRecordList<Reset.ResetBalance>("Balances", this, 10);
            _moneyManager = new AccountMoneyManager(this, _resetBalances);
            _bills = new BusinessRecordList<Bill>("Bills", this, 10);
            _hitService = new Lazy<HitService>(() => new HitService(this));
            _estimateCloseCommission = BusinessItemFactory.Create("EstimateCloseCommission", 0m, PermissionFeature.Sound, this);
            _estimateCloseLevy = BusinessItemFactory.Create("EstimateCloseLevy", 0m, PermissionFeature.Sound, this);
        }

        #region Properties

        internal DateTime? LastResetDay
        {
            get { return _resetService.Value.LastResetDay; }
            set
            {
                _resetService.Value.LastResetDay = value;
            }
        }

        internal Settings.Account Setting(DateTime? tradeDay = null)
        {
            if (tradeDay == null) return _settingAccount;
            return Settings.Setting.Default.GetAccount(this.Id, tradeDay);
        }


        internal Guid Id
        {
            get
            {
                return _plainId;
            }
        }


        internal bool IsLocked
        {
            get { return _isLocked; }
        }

        internal bool HasUnassignedOvernightOrders
        {
            get { return _hasUnassignedOvernightOrders; }
        }

        internal bool IsMultiCurrency
        {
            get { return this.Setting().IsMultiCurrency; }
        }

        internal bool IsAutoClose
        {
            get { return this.Setting().IsAutoClose; }
        }


        internal decimal ShortMargin
        {
            get { return this.Setting().ShortMargin; }
        }

        internal IEnumerable<Transaction> Transactions
        {
            get
            {
                lock (_mutex)
                {
                    return _transactions.GetValues();
                }
            }
        }

        public int TransactionCount
        {
            get
            {
                lock (_mutex)
                {
                    return _transactions.Count;
                }
            }
        }

        internal DeliveryRequestCache DeliveryRequests
        {
            get { return this._deliveryRequests; }
        }

        internal IEnumerable<SubFund> Funds
        {
            get
            {
                lock (_mutex)
                {
                    return _subFunds.GetValues();
                }
            }
        }

        internal int FundCount
        {
            get
            {
                lock (_mutex)
                {
                    return _subFunds.Count;
                }
            }
        }


        internal Fund SumFund
        {
            get
            {
                return this._fund;
            }
        }

        internal AlertLevel AlertLevel
        {
            get { return _accountRisk.AlertLevel; }
        }

        internal DateTime? AlertTime
        {
            get
            {
                return _accountRisk.AlertTime;
            }
        }


        internal UnclearDepositManager UnclearDepositManager
        {
            get { return this._unclearDepositManager; }
        }

        internal int InstrumentCount
        {
            get
            {
                lock (_mutex)
                {
                    return _instrumentManager.Count;
                }
            }
        }


        public Guid PublicQuotePolicyId
        {
            get
            {
                return Settings.Setting.Default.SystemParameter.DefaultQuotePolicyId.Value;
            }
        }

        public Guid? PrivateQuotePolicyId
        {
            get
            {
                return this.Customer.PrivateQuotePolicyId ?? this.Setting().QuotePolicyID;
            }
        }


        public decimal MinUpkeepEquity
        {
            get
            {
                return this.Setting().RiskActionMinimumEquity == null ? this.SumFund.RiskRawData.MinEquityAvoidRiskLevel3 : this.Setting().RiskActionMinimumEquity.Value;
            }
        }

        internal bool IsNecessaryWithinThreshold
        {
            get
            {
                lock (_mutex)
                {
                    return _necessaryCheckService.Value.IsNecessaryWithinThreshold;
                }
            }
        }

        internal AccountState State { get; private set; }

        internal decimal Balance
        {
            get { return this.SumFund.Balance; }
        }

        internal decimal Necessary
        {
            get { return this.SumFund.Necessary; }
        }

        internal decimal Equity
        {
            get { return this.SumFund.Equity; }
        }

        internal Customer Customer
        {
            get
            {
                return Settings.Setting.Default.GetCustomer(_settingAccount.CustomerId);
            }
        }

        internal IEnumerable<Bill> Bills
        {
            get
            {
                lock (_mutex)
                {
                    return _bills.GetValues();
                }
            }
        }

        internal bool AutoCloseFirstInFirstOut
        {
            get { return ExternalSettings.Default.AutoCloseFirstInFirstOut; }
        }

        internal int? Leverage
        {
            get { return _leverage.Value; }
            set { _leverage.SetValue(value); }
        }

        internal Int64 Version
        {
            get { return _version.Value; }
            private set
            {
                _version.SetValue(value);
            }
        }

        internal decimal EstimateCloseCommission
        {
            get { return _estimateCloseCommission.Value; }
            private set
            {
                _estimateCloseCommission.SetValue(value);
            }
        }

        internal decimal EstimateCloseLevy
        {
            get
            {
                return _estimateCloseLevy.Value;
            }
            private set
            {
                _estimateCloseLevy.SetValue(value);
            }
        }

        internal bool IsResetFailed
        {
            get
            {
                if (this.LastResetDay == null) return false;
                return this.LastResetDay.Value.AddDays(1) < Settings.Setting.Default.GetTradeDay().Day;
            }
        }


        #endregion

        internal void UpdateState(AccountState state)
        {
            lock (_mutex)
            {
                this.State = state;
            }
        }


        public Transaction GetTran(Guid tranId)
        {
            lock (_mutex)
            {
                return _transactions.Get(tranId);
            }
        }

        internal List<Transaction> GetTransactions(Guid instrumentId)
        {
            lock (_mutex)
            {
                List<Transaction> result = new List<Transaction>();
                foreach (var eachTransaction in _transactions.GetValues())
                {
                    if (eachTransaction.InstrumentId == instrumentId)
                    {
                        result.Add(eachTransaction);
                    }
                }
                return result;
            }
        }

        internal void AddOrderResetItem(OrderResetItem item)
        {
            if (!_resetOrderDict.ContainsKey(item.Id))
            {
                _resetOrderDict.AddItem(item, OperationType.AsNewRecord);
            }
        }

        internal void AddResetBill(Guid orderId, decimal value, ResetBillType type, DateTime tradeDay)
        {
            OrderResetItem orderResetItem;
            if (!_resetOrderDict.TryGetValue(orderId, out orderResetItem))
            {
                Logger.WarnFormat("AddResetBill order={0} not exist, billType = {1}, tradeDay={2}", orderId, type, tradeDay);
                return;
            }
            orderResetItem.AddBill(orderId, value, type, tradeDay);
        }

        internal void AddBill(Bill bill, OperationType operationType)
        {
            lock (_mutex)
            {
                _bills.AddItem(bill, operationType);
            }
        }

        internal void AddOrderBill(Guid orderId, Bill bill, OperationType operationType)
        {
            lock (_mutex)
            {
                var order = _transactions.GetOrder(orderId);
                if (order == null)
                {
                    return;
                }
                order.AddBill(bill, operationType);
            }
        }


        public Order GetOrder(Guid orderId)
        {
            lock (_mutex)
            {
                return _transactions.GetOrder(orderId);
            }
        }

        public bool ExistInstrument(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _instrumentManager.Exists(instrumentId);
            }
        }


        internal void AddBalance(Guid currencyId, decimal balance, DateTime? updateTime)
        {
            lock (_mutex)
            {
                _moneyManager.AddBalance(currencyId, balance, updateTime);
            }
        }


        internal void AddDeposit(Guid currencyId, DateTime effectiveDateTime, decimal balance, bool isDeposit)
        {
            lock (_mutex)
            {
                _moneyManager.AddDeposit(currencyId, effectiveDateTime, balance, isDeposit);
            }
        }


        internal void AddHistoryBalanceOnly(Guid currencyId, DateTime tradeDay, decimal value)
        {
            lock (_mutex)
            {
                _moneyManager.AddHistoryBalance(currencyId, tradeDay, value);
            }
        }


        internal void ProcessForInstruments(Action<AccountClass.Instrument> action)
        {
            lock (_mutex)
            {
                foreach (var eachInstrument in _instrumentManager.Instruments)
                {
                    action(eachInstrument);
                }
            }
        }

        internal IEnumerable<AccountClass.Instrument> Instruments
        {
            get
            {
                lock (_mutex)
                {
                    return _instrumentManager.Instruments;
                }
            }
        }


        internal void AddCurrencyForReset(DateTime tradeDay)
        {
            foreach (var eachFund in this.Funds)
            {
                decimal instrumentResetBalance = -_instrumentManager.GetResetBalanceGreatThanTradeDay(eachFund.CurrencyId, tradeDay);
                this.AddHistoryBalanceOnly(eachFund.CurrencyId, tradeDay, eachFund.Balance + instrumentResetBalance);
            }
        }

        internal void CalculateInit()
        {
            lock (_mutex)
            {
                this.CalculateEstimateFee();
                this.CalculateRiskData();
                this.AcceptChanges();
            }
        }

        internal void RecalculateEstimateFee()
        {
            lock (_mutex)
            {
                try
                {
                    this.EstimateCloseCommission = 0m;
                    this.EstimateCloseLevy = 0m;

                    foreach (var eachTran in this.Transactions)
                    {
                        foreach (var eachOrder in eachTran.Orders)
                        {
                            if (eachOrder.ShouldCalculateEstimateFee)
                            {
                                var result = eachOrder.CalculateEstimateFee(ExecuteContext.CreateExecuteDirectly(this.Id, eachTran.Id, ExecuteStatus.None));
                                eachOrder.UpdateEstimateFee(result);
                                this.AddEstimateFee(eachOrder.EstimateCloseCommission, eachOrder.EstimateCloseLevy, eachOrder.EstimateCurrencyRate(null));
                            }
                        }
                    }
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }

        internal void RecalculateEstimateFee(TradePolicyDetail detail)
        {
            lock (_mutex)
            {
                try
                {
                    if (this.Setting().TradePolicyId != detail.TradePolicy.ID) return;
                    foreach (var eachTran in this.Transactions)
                    {
                        if (eachTran.InstrumentId != detail.InstrumentId) continue;
                        foreach (var eachOrder in eachTran.Orders)
                        {
                            if (eachOrder.ShouldCalculateEstimateFee)
                            {
                                eachOrder.RecalculateEstimateFee(ExecuteContext.CreateExecuteDirectly(this.Id, eachTran.Id, ExecuteStatus.None));
                            }
                        }
                    }
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }

        internal void AddEstimateFee(decimal commission, decimal levy, CurrencyRate currencyRate)
        {
            decimal closeCommission = this.EstimateCloseCommission + commission;
            decimal closeLevy = this.EstimateCloseLevy + levy;
            if (this.IsMultiCurrency)
            {
                decimal deltaCommission = currencyRate.Exchange(closeCommission) - currencyRate.Exchange(this.EstimateCloseCommission);
                decimal deltaLevy = currencyRate.Exchange(closeLevy) - currencyRate.Exchange(this.EstimateCloseLevy);
                this.EstimateCloseCommission += deltaCommission;
                this.EstimateCloseLevy += deltaLevy;
            }
            else
            {
                this.EstimateCloseCommission += commission;
                this.EstimateCloseLevy += levy;
            }
        }

        private void CalculateEstimateFee()
        {
            foreach (var eachTran in _transactions.GetValues())
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.ShouldCalculateEstimateFee)
                    {
                        this.AddEstimateFee(eachOrder.EstimateCloseCommission, eachOrder.EstimateCloseLevy, eachOrder.EstimateCurrencyRate(null));
                    }
                }
            }
        }




        internal void DoInstrumentReset(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    Logger.InfoFormat("DoInstrumentResetForSetDailyClosePrice accountId = {0}, instrumentId = {1}, tradeDay = {2}", this.Id, instrumentId, tradeDay);
                    this.UpdateState(AccountState.InReset);
                    _resetService.Value.DoInstrumentReset(instrumentId, tradeDay, closeQuotations);
                    string content;
                    this.SaveAndBroadcastResetContent(Agent.Caching.CacheType.Reset, out content);
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
                finally
                {
                    this.UpdateState(AccountState.None);
                }
            }

        }

        internal void DoInstrumentReset(DateTime tradeDay)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    this.UpdateState(AccountState.InReset);
                    _resetService.Value.DoInstrumentReset(tradeDay);
                    string content;
                    this.SaveAndBroadcastResetContent(Agent.Caching.CacheType.Reset, out content);
                }
                catch (OrderDayHistoryException orderDayHistoryException)
                {
                    Logger.ErrorFormat("Get OrderDayHistory error, orderId= {0}, tradeDay = {1}, detail={2}", orderDayHistoryException.OrderId, orderDayHistoryException.TradeDay, orderDayHistoryException);
                    this.RejectChanges();
                }
                catch (PhysicalOrderNotFoundException physicalOrderNotFoundException)
                {
                    Logger.ErrorFormat("PhysicalOrderNotFound id = {0}, detail={1}", physicalOrderNotFoundException.OrderId, physicalOrderNotFoundException);
                    this.RejectChanges();
                }
                catch (InstalmentInfoNotFoundException instalmentInfoNotFoundException)
                {
                    Logger.ErrorFormat("InstalmentInfoNotFound, orderId={0}, physicalType={1}, detail={2}", instalmentInfoNotFoundException.OrderId, instalmentInfoNotFoundException.PhysicalType, instalmentInfoNotFoundException);
                    this.RejectChanges();
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
                finally
                {
                    this.UpdateState(AccountState.None);
                }
            }
        }

        internal void DoSystemReset(DateTime tradeDay)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    this.UpdateState(AccountState.InReset);
                    _resetBalances.Clear();
                    _resetOrderDict.Clear();
                    _resetService.Value.DoSystemReset(tradeDay);
                    string content;
                    this.SaveAndBroadcastResetContent(Agent.Caching.CacheType.Reset, out content);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("accountId = {0}, tradeDay = {1}", this.Id, tradeDay), ex);
                    this.RejectChanges();
                }
                finally
                {
                    this.UpdateState(AccountState.None);
                }
            }
        }

        internal bool IsInstrumentsReseted(DateTime tradeDay)
        {
            lock (_mutex)
            {
                return _resetService.Value.IsInstrumentsReseted(tradeDay);
            }
        }


        internal IEnumerable<Order> GetResetOrders(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _resetService.Value.GetOrders(instrumentId);
            }
        }

        internal void RemoveOrder(Order order)
        {
            lock (_mutex)
            {
                Logger.InfoFormat("remove order id = {0}", order.Id);
                _transactions.RemoveOrder(order);
                order.Owner.RemoveOrder(order);
            }
        }

        internal void RemoveOrderFromCache(Order order)
        {
            lock (_mutex)
            {
                _transactions.RemoveOrder(order);
            }
        }


        internal IEnumerable<OrderRelation> GetResetOrderRelations(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _resetService.Value.GetOrderRelations(instrumentId);
            }
        }


        internal DateTime? GetInstrumentLastResetDay(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _resetService.Value.GetInstrumentLastResetDay(instrumentId);
            }
        }


        internal AccountClass.Instrument GetInstrument(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _instrumentManager.Get(instrumentId);
            }
        }


        internal bool HasFilledShortSellOrders(Guid instrumentId)
        {
            lock (_mutex)
            {
                return PhysicalHelper.HasFilledShortSellOrders(this, instrumentId);
            }
        }

        internal IEnumerable<Order> GetFilledShortSellOrders(Guid instrumentId)
        {
            lock (_mutex)
            {
                return PhysicalHelper.GetFilledShortSellOrders(this, instrumentId);
            }
        }

        internal bool IsInAlerting(AccountClass.Instrument instrument, BuySellLot oldLotBalance)
        {
            lock (_mutex)
            {
                return _accountRisk.IsInAlerting(instrument, oldLotBalance);
            }
        }

        internal void AddPendingConfirmLimitOrder(Order order)
        {
            lock (_mutex)
            {
                _pendingConfirmLimitOrders.Add(order);
            }
        }

        internal void RemovePendingConfirmLimitOrder(Order order)
        {
            lock (_mutex)
            {
                _pendingConfirmLimitOrders.Remove(order);
            }
        }

        internal bool HasPendingConfirmLimitOrder()
        {
            lock (_mutex)
            {
                if (_pendingConfirmLimitOrders.Count == 0)
                {
                    return false;
                }

                foreach (Order order in _pendingConfirmLimitOrders)
                {
                    if (order.Owner.CanExecute) return true;
                }
                return false;
            }
        }



        internal IEnumerable<Order> GetPendingConfirmLimitOrders()
        {
            lock (_mutex)
            {
                return _pendingConfirmLimitOrders;
            }
        }

        internal bool HasPosition()
        {
            lock (_mutex)
            {
                foreach (var tran in this.Transactions)
                {
                    foreach (var order in tran.Orders)
                    {
                        if (order.IsRisky && order.IsExecuted && order.LotBalance > 0) return true;
                    }
                }
                return false;
            }
        }

        internal TransactionError DeleteOrder(Guid orderId, bool isPayForInstalmentDebitInterest, Guid? deliveryRequestId = null)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    Logger.InfoFormat("Delete order, orderId = {0}", orderId);
                    if (deliveryRequestId != null)
                    {
                        DeliveryRequest deliveryRequest = DeliveryRequestManager.Default[deliveryRequestId.Value];
                        deliveryRequest.Cancel();
                        Logger.InfoFormat("Delete order for cancel delivery, deliveryRequestId = {0}, orderId = {1}, accountId = {2}", deliveryRequestId, orderId, this.Id);
                    }
                    if (this.ShouldLoadCompletedOrders(orderId))
                    {
                        Logger.InfoFormat("LoadCompletedOrders for Delete order, orderId = {0}", orderId);
                        DataSet ds = DB.DBRepository.Default.GetCompletedOrderForDelete(orderId);
                        Logger.InfoFormat("ParseCompletedOrders for Delete order, orderId = {0}", orderId);
                        TradingSetting.Default.ParseDBRecords(ds);
                    }
                    var order = this.GetOrder(orderId);
                    if (order == null)
                    {
                        this.RejectChanges();
                        return TransactionError.HasNoOrders;
                    }
                    var instrument = this.GetOrCreateInstrument(order.Owner.InstrumentId);
                    instrument.ClearResetItems();// when make the save tradeDay and instrument order , instrument reset item would be affected, so clear it when called
                    HistoryOrderDeleter deleter = new HistoryOrderDeleter(order, Settings.Setting.Default, isPayForInstalmentDebitInterest);
                    Logger.InfoFormat(" begin delete order, orderId = {0}", orderId);
                    deleter.Delete();
                    Logger.InfoFormat(" end delete order, orderId = {0}", orderId);
                    this.InvalidateInstrumentCache(order.Owner);
                    this.RecalculateEstimateFee();
                    this.CalculateRiskData();
                    return TransactionError.OK;
                }
                catch (TransactionServerException tse)
                {
                    Logger.Error(tse);
                    this.RejectChanges();
                    return tse.ErrorCode;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
                finally
                {
                    this.ClearHistoryItems();
                }
            }
        }

        private void ClearHistoryItems(Guid? instrumentId = null)
        {
            _resetBalances.Clear();
            _resetOrderDict.Clear();
            if (instrumentId != null)
            {
                var instrument = this.GetOrCreateInstrument(instrumentId.Value);
                instrument.ClearResetItems();
            }
        }


        private bool ShouldLoadCompletedOrders(Guid orderId)
        {
            var order = this.GetOrder(orderId);
            if (order == null)
            {
                Logger.InfoFormat("ShouldLoadCompletedOrders  order id = {0} is null ", orderId);
                return true;
            }

            if (order.Phase == OrderPhase.Completed)
            {
                Logger.ErrorFormat("ShouldLoadCompletedOrders orderId = {0} phase = 3, in an error state", orderId);
                this.RemoveOrder(order);
                if (order.Owner != null)
                {
                    this.RemoveTransaction(order.Owner);
                }
                return true;
            }

            if (!order.IsOpen)
            {
                foreach (var eachOrderRelation in order.OrderRelations)
                {
                    if (eachOrderRelation.OpenOrder == null)
                    {
                        Logger.InfoFormat("ShouldLoadCompletedOrders close order id = {0} openOrder is null", orderId);
                        return true;
                    }
                }
            }
            return false;
        }

        internal TransactionError Book(Token token, Protocal.TransactionBookData tranData)
        {
            lock (_mutex)
            {
                try
                {
                    Logger.InfoFormat("Book  tranId= {0}", tranData.Id);
                    this.CheckState();
                    var tran = this.GetTran(tranData.Id);
                    if (tran != null) return TransactionError.TransactionAlreadyExists;
                    tranData.TradeDay = DB.DBRepository.Default.GetTradeDay(tranData.ExecuteTime);
                    ResetManager.Default.RemoveHistroySetting(tranData.TradeDay);
                    if (Booker.Book(this, token, tranData))
                    {
                        var changeContent = this.SaveTradingContent();
                        Broadcaster.Default.Add(CommandFactory.CreateBookCommand(this.Id, changeContent));
                        HistoryOrderFactory.Process(this.GetTran(tranData.Id), Settings.Setting.Default, tranData.TradeDay); // recover interest and strage
                        this.CheckRisk();
                        return TransactionError.OK;
                    }
                    else
                    {
                        this.RejectChanges();
                        return TransactionError.RuntimeError;
                    }

                }
                catch (TransactionServerException tex)
                {
                    Logger.Error(tex);
                    this.RejectChanges();
                    return tex.ErrorCode;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
                finally
                {
                    this.ClearHistoryItems(tranData.InstrumentId);
                    ResetManager.Default.RemoveHistroySetting(tranData.TradeDay);
                }
            }
        }

        private string SaveTradingContent()
        {
            this.Version++;
            string result = _broadcastService.Value.SaveCommon(Caching.CacheType.Transaciton);
            this.AcceptChanges();
            return result;
        }



        public TransactionError Place(Protocal.TransactionData tranData, out string tranCode)
        {
            lock (this._mutex)
            {
                tranCode = null;
                try
                {
                    this.CheckState();
                    if (_transactions.ContainsKey(tranData.Id))
                    {
                        Logger.ErrorFormat("Place transaction exists id = {0}", tranData.Id);
                        return TransactionError.TransactionAlreadyExists;
                    }
                    TransactionPlacer.Default.Place(this, tranData, out tranCode);
                    return TransactionError.OK;
                }
                catch (TransactionServerException tranEx)
                {
                    Logger.Error(tranEx);
                    this.ProcessForPlaceFailed(tranData.Id, tranEx.ErrorCode);
                    return tranEx.ErrorCode;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.ProcessForPlaceFailed(tranData.Id, TransactionError.RuntimeError);
                    return TransactionError.RuntimeError;
                }
            }
        }


        private void ProcessForPlaceFailed(Guid? tranId, TransactionError error)
        {
            this.RejectChanges();
            if (tranId == null) return;
            var tran = this.GetTran(tranId.Value);
            if (tran == null) return;
            tran.Cancel(error.ToCancelReason());
            this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
        }

        internal void OnPlaced(PlaceEventArgs e)
        {
            lock (_mutex)
            {
                try
                {
                    var tran = this.GetTran(e.TransactionId);
                    Logger.InfoFormat("accept place , place status = {0}, tranId = {1}", e.Status, tran.Id);
                    if (e.Status == PlaceStatus.Accepted)
                    {
                        tran.CancelService.CancelAmendedAndDoneTransactions();
                    }
                    else if (e.Status == PlaceStatus.Rejected)
                    {
                        tran.Owner.RejectChanges();
                        tran.Cancel(CancelReason.OtherReason);
                        tran.PlacePhase = BLL.TransactionBusiness.PlacePhase.PlaceRejected;
                        tran.PlaceDetail = e.ErrorDetail;
                        tran.Owner.InvalidateInstrumentCache(tran);
                        TransactionExpireChecker.Default.Remove(e.TransactionId);
                    }
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

            }
        }


        public void GetInitializeData(StringBuilder root)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    this.WriteXml(root, m =>
                        {
                            return true;
                        });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        internal void CloseForOpenOrderExceedOpenDays(List<Guid> openOrders, DateTime tradeDay)
        {
            lock (_mutex)
            {
                if (this.LastResetDay != null)
                {
                    for (DateTime deltaTradeDay = this.LastResetDay.Value; deltaTradeDay <= tradeDay; deltaTradeDay = deltaTradeDay.AddDays(1))
                    {
                        this.CloseForTradeDay(openOrders, deltaTradeDay);
                    }
                }
                else
                {
                    this.CloseForTradeDay(openOrders, tradeDay);
                }
            }
        }


        private void CloseForTradeDay(List<Guid> openOrders, DateTime tradeDay)
        {
            foreach (var eachTran in this.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (openOrders.Contains(eachOrder.Id))
                    {
                        var accountId = eachOrder.Owner.Owner.Id;
                        var factory = TransactionFacade.CreateAddTranCommandFactory(eachOrder.Owner.OrderType, eachOrder.Owner.InstrumentCategory);
                        var buySellPrice = ResetManager.Default.LoadDailyClosePrice(accountId, eachOrder.Owner.InstrumentId, tradeDay);
                        Price price = eachOrder.IsBuy ? buySellPrice.Item2 : buySellPrice.Item1;
                        var command = factory.CreateCutTransaction(eachOrder.Owner.Owner, eachOrder.Owner.AccountInstrument, eachOrder.LotBalance, price, !eachOrder.IsBuy);
                        command.Execute();
                    }
                }
            }

        }

        public TransactionError Execute(Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID, bool shouldCheckRisk = true)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    Logger.InfoFormat("Execute tranId = {0}, buyPrice = {1}, sellPrice = {2}, lot = {3}, executedOrderID = {4}, shouldCheckRisk = {5}", tranID, buyPrice, sellPrice,
                        lot, executedOrderID, shouldCheckRisk);
                    Transaction tran = this.GetTran(tranID);
                    if (tran == null) return TransactionError.TransactionNotExists;
                    decimal? executingLot = null;
                    if (!string.IsNullOrEmpty(lot))
                    {
                        executingLot = decimal.Parse(lot);
                    }
                    List<OrderPriceInfo> infos = new List<OrderPriceInfo>()
                    {
                         new OrderPriceInfo(executedOrderID, PriceHelper.CreatePrice(buyPrice, tran.InstrumentId,null),  PriceHelper.CreatePrice(sellPrice, tran.InstrumentId,null))
                    };
                    ExecuteContext context = new ExecuteContext(this.Id, tranID, executedOrderID, false, true, ExecuteStatus.Filled, infos);
                    this.Execute(context);
                    return TransactionError.OK;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
            }
        }


        internal void Execute(ExecuteContext context)
        {
            lock (_mutex)
            {
                try
                {
                    var tran = this.GetTran(context.TranId);
                    TransactionExecutor.Default.Execute(context);
                    this.ChangeFieldsToModifedWhenExecuted(tran);
                    this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
                    this.UpdateOrderPhase(tran, context.TradeDay);
                    _accountRisk.CheckRisk(MarketManager.Now, CalculateType.CheckRisk);
                }
                catch (TransactionServerException tse)
                {
                    Logger.Error(tse);
                    this.CancelExecute(context, tse.ErrorCode);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.CancelExecute(context, TransactionError.RuntimeError);
                }
            }
        }


        internal void ExecuteDirectly(ExecuteContext context)
        {
            lock (_mutex)
            {
                try
                {
                    var tran = context.Tran;
                    tran.ExecuteDirectly(context);
                    this.ChangeFieldsToModifedWhenExecuted(tran);
                    this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
                    this.UpdateOrderPhase(tran, context.TradeDay);
                    _accountRisk.CheckRisk(MarketManager.Now, CalculateType.CheckRisk);
                }
                catch (TransactionServerException tse)
                {
                    Logger.Error(tse);
                    this.CancelExecute(context, tse.ErrorCode);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.CancelExecute(context, TransactionError.RuntimeError);
                }
            }
        }

        internal bool ExecuteCutTran(Transaction tran)
        {
            lock (_mutex)
            {
                var context = ExecuteContext.CreateExecuteDirectly(this.Id, tran.Id, ExecuteStatus.Filled);
                try
                {
                    tran.ExecuteDirectly(context);
                    this.ChangeFieldsToModifedWhenExecuted(tran);
                    this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
                    this.UpdateOrderPhase(tran, null);
                    return true;
                }
                catch (TransactionServerException tse)
                {
                    Logger.Error(tse);
                    this.CancelExecute(context, tse.ErrorCode);
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.CancelExecute(context, TransactionError.RuntimeError);
                    return false;
                }
            }
        }

        private void ChangeFieldsToModifedWhenExecuted(Transaction tran)
        {
            this.SumFund.ChangeSomeFieldsToModifiedWhenExecuted(tran);
        }

        private void UpdateOrderPhase(Transaction tran, DateTime? tradeDay)
        {
            try
            {
                if (tran.ExecuteTime == null)
                {
                    Logger.WarnFormat("UpdateOrderPhase tranId = {0}, tran.phase = {1} execute time is null", tran.Id, tran.Phase);
                    return;
                }
                foreach (var eachOrder in tran.Orders)
                {
                    eachOrder.UpdateCloseOrderPhase(tradeDay ?? Settings.Setting.Default.GetTradeDay().Day, tran.InstrumentId, null);
                }
                this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        internal TransactionError MultipleClose(Guid[] orderIds)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    Logger.InfoFormat("MultipleClose accountId = {0}, orderId = '{1}'", this.Id, orderIds.Aggregate("", (s, m) => string.Format("{0}, {1}", s, m)));
                    var tran = MultipleCloser.Close(this, orderIds);
                    this.SaveAndBroadcastChanges();
                    Broadcaster.Default.Add(CommandFactory.CreateExecuteCommand(this.Id, tran.InstrumentId, tran.Id));
                    return TransactionError.OK;
                }
                catch (TransactionServerException tsex)
                {
                    Logger.Error(tsex.ErrorDetail);
                    this.RejectChanges();
                    return tsex.ErrorCode;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
            }
        }

        internal void ResetHit(List<Guid> orderIds)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    foreach (var eachOrderId in orderIds)
                    {
                        Logger.InfoFormat("Reset hit accountId = {0}, orderId = {1}", this.Id, eachOrderId);
                        Order order = this.GetOrder(eachOrderId);
                        order.ResetHit();
                        if (order.OrderType == OrderType.Limit)
                        {
                            this.RemovePendingConfirmLimitOrder(order);
                        }
                        else if (order.OrderType == OrderType.Market)
                        {
                            MarketOrderProcessor.Default.Add(order);
                        }
                    }
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                }
            }
        }

        internal XElement GetProfitWithinXMl(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            lock (_mutex)
            {
                try
                {
                    decimal profit = this.SumFund.TotalDeposit - this.SumFund.Equity;
                    decimal minProfitValue = minProfit == null ? decimal.MinValue : minProfit.Value;
                    decimal maxProfitValue = maxProfit == null ? decimal.MaxValue : maxProfit.Value;
                    if ((includeMinProfit ? profit >= minProfitValue : profit > minProfitValue)
                        && (includeMaxProfit ? profit <= maxProfitValue : profit < maxProfitValue))
                    {
                        XElement accountNode = new XElement("Account");
                        accountNode.SetAttributeValue("ID", XmlConvert.ToString(this.Id));
                        accountNode.SetAttributeValue("Code", this.Setting().Code);
                        accountNode.SetAttributeValue("BeginTime", XmlConvert.ToString(this.Setting().BeginTime, DateTimeFormat.Xml));
                        accountNode.SetAttributeValue("TotalDeposit", XmlConvert.ToString(this.SumFund.TotalDeposit));
                        accountNode.SetAttributeValue("Equity", XmlConvert.ToString(this.SumFund.Equity));
                        return accountNode;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return null;
                }
            }
        }


        internal void RemoveUnclearDeposit(Guid id)
        {
            lock (_mutex)
            {
                _unclearDepositManager.Remove(id);
            }
        }

        internal void AddUnclearDeposit(UnclearDeposit deposit)
        {
            lock (_mutex)
            {
                _unclearDepositManager.Add(deposit);
            }
        }

        internal void ResetAlertLevel()
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    _accountRisk.ResetAlertLevel();
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                }
            }
        }


        internal void CancelExecute(ExecuteContext context, TransactionError error)
        {
            var tran = context.Tran;
            this.CancelExecute(context, error.ToCancelReason());
            if (tran == null) return;
            if (tran.SubType == TransactionSubType.IfDone)
            {
                IfDoneTransactionManager.Default.CancelDoneTransIfExist(tran);
            }
        }



        internal void CancelExecute(ExecuteContext context, CancelReason cancelReason)
        {
            lock (_mutex)
            {
                var tran = context.Tran;
                this.RejectChanges();
                if (tran == null)
                {
                    Logger.InfoFormat("CancelExecute accountId = {0}, cancelReason = {1}  tran is null", this.Id, cancelReason);
                    return;
                }
                tran.Cancel(cancelReason, context);
                this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
            }
        }

        internal TransactionError Cancel(Token token, Guid tranId, CancelReason cancelReason)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    var tran = this.GetTran(tranId);
                    TransactionError error = this.VerifyForCancel(token, tran, cancelReason);
                    Logger.InfoFormat("cancel tranId = {0}, cancelReseason = {1}, error= {2}, tran.phase = {3}, appType = {4}", tranId, cancelReason, error, tran.Phase, token.AppType);
                    if (error != TransactionError.OK) return error;
                    InteractFacade.Default.TradingEngine.Cancel(tran, cancelReason);
                    tran.PlacePhase = PlacePhase.Canceled;
                    return error;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
            }
        }

        private TransactionError VerifyForCancel(Token token, Transaction tran, CancelReason cancelReason)
        {
            if (tran == null)
            {
                return TransactionError.TransactionNotExists;
            }
            if (!tran.CancelService.CanCancel)
            {
                return TransactionError.TransactionCannotBeCanceled;
            }
            var baseTime = MarketManager.Now;

            if (token.AppType == AppType.TradingConsole || token.AppType == AppType.Mobile || token.AppType == AppType.CppTrader || token.AppType == AppType.TradingConsoleSilverLight)
            {
                string errorDetail;
                if (!tran.TradingInstrument.CanPlace(baseTime, tran.OrderType.IsPendingType(), tran.AccountInstrument.GetQuotation(), PlaceContext.Empty, out errorDetail))
                {
                    Logger.WarnFormat("VerifyForCancel accountId= {0}, tranId= {1}, errorDetail = {2}", this.Id, tran.Id, errorDetail);
                    return TransactionError.PriceIsDisabled;
                }
                if (!tran.CancelService.CanBeCanceledByCustomer())
                {
                    return TransactionError.TransactionCannotBeCanceled;
                }
                if (!tran.CancelService.ShouldAutoCancel())
                {
                    if (token.AppType == AppType.CppTrader)
                    {
                        this.ProcessForShouldCanceledByManager(tran, cancelReason);
                    }
                    return TransactionError.Action_NeedDealerConfirmCanceling;
                }
            }
            return TransactionError.OK;
        }

        private void ProcessForShouldCanceledByManager(Transaction tran, CancelReason cancelReason)
        {
            var command = new Protocal.Commands.TradingCancelByManagerCommand
            {
                AccountId = this.Id,
                InstrumentId = tran.InstrumentId,
                TransactionId = tran.Id,
                ErrorCode = TransactionError.Action_NeedDealerConfirmCanceling,
                Reason = cancelReason
            };
            Broadcaster.Default.Add(command);
        }



        internal void OnTransactionCanceled(Transaction tran, CancelStatus status, CancelReason cancelReason)
        {
            lock (_mutex)
            {
                try
                {
                    if (status == CancelStatus.Accepted)
                    {
                        tran.Cancel(cancelReason);
                        this.InvalidateInstrumentCacheAndBroadcastChanges(tran);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                }
            }
        }

        internal void NotifyDelivery(DeliveryRequest deliveryRequest, DateTime availableDeliveryTime)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    deliveryRequest.DeliveryRequestStatus = DeliveryRequestStatus.Stocked;
                    deliveryRequest.AvailableDeliveryTime = availableDeliveryTime;
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                }
            }
        }

        internal bool CancelDelivery(Guid deliveryRequestId, out int status)
        {
            lock (_mutex)
            {
                status = (int)DeliveryRequestStatus.Cancelled;
                try
                {
                    this.CheckState();
                    bool result = DeliveryManager.CancelDelivery(deliveryRequestId, this, out status);
                    if (result)
                    {
                        this.SaveAndBroadcastChanges();
                    }
                    else
                    {
                        this.RejectChanges();
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return false;
                }
            }
        }

        internal TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData, out string code, out string balance, out string usableMargin)
        {
            lock (_mutex)
            {
                code = balance = usableMargin = null;
                var error = this.ApplyDelivery(requestData);
                if (error == TransactionError.OK)
                {
                    var request = DeliveryRequestManager.Default[requestData.Id];
                    code = request.Code;
                    balance = this.Balance.ToString();
                    usableMargin = (this.Equity - this.Necessary).ToString();
                }
                return error;
            }
        }

        internal TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    DeliveryHelper.ApplyDelivery(this, requestData);
                    string content;
                    this.SaveAndBroadcastChanges(out content);
                    Logger.InfoFormat("ApplyDelivery content = {0}", content);
                    return TransactionError.OK;
                }
                catch (TransactionServerException tranEx)
                {
                    Logger.Error(tranEx);
                    this.RejectChanges();
                    return tranEx.ErrorCode;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }

            }
        }


        internal TransactionError ApplyTransfer(Guid userID, Guid sourceCurrencyID, decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount, decimal rate, DateTime expireDate)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    return BLL.Transfers.TransferManager.ApplyTransfer(userID, this, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return TransactionError.RuntimeError;
                }
            }
        }


        internal void InvalidateInstrumentCacheAndBroadcastChanges(Transaction tran)
        {
            lock (_mutex)
            {
                this.InvalidateInstrumentCache(tran);
                this.SaveAndBroadcastChanges();
            }
        }

        internal void InvalidateInstrumentCache(Transaction tran)
        {
            lock (_mutex)
            {
                var instrument = this.GetOrCreateInstrument(tran.InstrumentId);
                instrument.InvalidateCache();
            }
        }

        public bool SaveAndBroadcastChanges()
        {
            string content;
            return this.SaveAndBroadcastChangesCommon(out content);
        }


        public bool SaveAndBroadcastChanges(out string content)
        {
            return this.SaveAndBroadcastChangesCommon(out content);
        }


        private bool SaveAndBroadcastChangesCommon(out string content)
        {
            lock (_mutex)
            {
                content = string.Empty;
                if (this.Status == ChangeStatus.None) return true;
                this.Version++;
                return _broadcastService.Value.SaveAndBroadcastChanges(out content);
            }
        }

        internal bool SaveAndBroadcastResetContent(Agent.Caching.CacheType cacheType, out string content)
        {
            lock (_mutex)
            {
                content = string.Empty;
                if (this.Status == ChangeStatus.None) return true;
                this.Version++;
                return _broadcastService.Value.SaveResetContent(cacheType, out content);
            }
        }

        internal void UpdateQuotation(QuotationBulk quotationBatch)
        {
            lock (this._mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    bool needCheckRisk = false;
                    DateTime baseTime = MarketManager.Now;
                    foreach (var eachInstrument in _instrumentManager.Instruments)
                    {
                        Quotation quotation;
                        if (quotationBatch.TryGetQuotation(eachInstrument.Id, this, out quotation))
                        {
                            needCheckRisk |= eachInstrument.UpdateQuotationAndCheckIsRiskRised(quotationBatch, quotation, baseTime);
                        }
                    }
                    if (needCheckRisk)
                    {
                        _accountRisk.CheckRisk(MarketManager.Now, CalculateType.CheckRiskForQuotation);
                        this.SaveAndBroadcastChanges();
                    }
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }


        internal void CheckRisk()
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    _accountRisk.CheckRisk(MarketManager.Now, CalculateType.CheckRisk);
                    this.SaveAndBroadcastChanges();
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }

        internal void CalculateRiskData(IQuotePolicyProvider quotePolicyProvider = null)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckAccountIsExpired();
                    _accountRisk.CalculateRiskData(MarketManager.Now, CalculateType.CheckRisk, quotePolicyProvider ?? this);
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }


        internal AccountClass.Instrument GetOrCreateInstrument(Guid instrumentId)
        {
            lock (_mutex)
            {
                AccountClass.Instrument result;
                if (!_instrumentManager.Exists(instrumentId))
                {
                    var quotation = MarketManager.Default[instrumentId].Quotation;
                    result = InstrumentFacade.Default.CreateInstrument(this, instrumentId, quotation);
                    _instrumentManager.Add(result);
                }
                else
                {
                    result = _instrumentManager.Get(instrumentId);
                }
                return result;
            }
        }

        internal void SetInstrumentLastResetDay(Guid instrumentId, DateTime lastResetDay)
        {
            lock (_mutex)
            {
                _instrumentManager.SetInstrumentLastResetDay(instrumentId, lastResetDay);
            }
        }


        internal SubFund GetOrCreateFund(Guid currencyId)
        {
            lock (_mutex)
            {
                SubFund fund = null;
                if (this.IsMultiCurrency)
                {
                    if (!this._subFunds.TryGetValue(currencyId, out fund))
                    {
                        fund = new SubFund(this, currencyId, 0, 0, OperationType.AsNewRecord);
                    }
                }
                else
                {
                    if (!this._subFunds.TryGetValue(this.Setting().CurrencyId, out fund))
                    {
                        fund = new SubFund(this, this.Setting().CurrencyId, 0, 0, OperationType.AsNewRecord);
                    }
                }
                return fund;
            }
        }

        internal SubFund GetFund(Guid currencyId)
        {
            lock (_mutex)
            {
                SubFund result = null;
                _subFunds.TryGetValue(currencyId, out result);
                return result;
            }
        }


        internal void CalculateForCurrencyRateChanged()
        {
            lock (_mutex)
            {
                this.SumFund.Clear();
                foreach (var eachFund in _subFunds.GetValues())
                {
                    this.SumFund.Add(eachFund);
                }
                _accountRisk.CheckRisk(MarketManager.Now, CalculateType.CheckRiskForInit);
            }
        }



        internal void AddDeliveryRequest(DeliveryRequest deliveryRequest, OperationType operationType)
        {
            _deliveryRequests.AddItem(deliveryRequest, operationType);
        }

        internal void AddSubFund(SubFund fund, OperationType operationType)
        {
            _subFunds.AddItem(fund, operationType);
        }

        internal void AddTransaction(Transaction tran, OperationType operationType)
        {
            _transactions.AddItem(tran, operationType);
        }

        public void RemoveTransaction(Transaction tran)
        {
            _transactions.RemoveItem(tran);
        }

        internal bool HasEnoughMoneyToFill(AccountClass.Instrument instrument, bool existsCloseOrder, decimal fee, bool isNecessaryFreeOrder, decimal lastEquity, out string errorInfo, bool isForPayoff = false)
        {
            return _necessaryCheckService.Value.HasEnoughMoneyToFill(instrument, existsCloseOrder, fee, isNecessaryFreeOrder, lastEquity, isForPayoff, out errorInfo);
        }

        internal bool HasEnoughMoneyToDelivery(AccountClass.Instrument instrument)
        {
            this.SumFund.Credit = this.CalculateCredit(instrument);
            decimal unclearBalance = this.CaculateUnclearBalance();
            return this.SumFund.Necessary <= this.SumFund.Equity - unclearBalance + this.ShortMargin + this.SumFund.Credit;
        }

        internal bool HasEnoughMoneyToPayOff(Order prepaymentOrder, decimal payOffAmount, Guid currencyId, decimal lastEquity, out string errorInfo)
        {
            errorInfo = string.Empty;
            decimal payOffAmountExchanged;
            if (!this.HasEnoughMoneyToPayCommon(payOffAmount, currencyId, out payOffAmountExchanged)) return false;
            return _necessaryCheckService.Value.HasEnoughMoneyToFill(prepaymentOrder.Owner.AccountInstrument, !prepaymentOrder.IsOpen, payOffAmountExchanged, prepaymentOrder.Owner.IsFreeOfNecessaryCheck, lastEquity, true, out errorInfo);
        }

        internal bool HasEnoughMoneyToPayArrears(decimal payOffAmount, Guid currencyId)
        {
            decimal payOffAmountExchanged;
            return this.HasEnoughMoneyToPayCommon(payOffAmount, currencyId, out payOffAmountExchanged) ? this.SumFund.Equity >= payOffAmountExchanged : false; ;
        }

        private bool HasEnoughMoneyToPayCommon(decimal payOffAmount, Guid currencyId, out decimal payOffAmountExchanged)
        {
            payOffAmountExchanged = payOffAmount;
            if (this.Setting().CurrencyId != currencyId)
            {
                CurrencyRate currencyRate = Settings.Setting.Default.GetCurrencyRate(currencyId, this.Setting().CurrencyId);
                payOffAmountExchanged = -currencyRate.Exchange(-payOffAmount);
            }

            if (this.IsMultiCurrency)
            {
                if (!_subFunds.ContainsKey(currencyId))
                {
                    return false;
                }
                var fund = this.GetFund(currencyId);
                if (fund.Balance < payOffAmount) return false;
            }
            else if (this.Balance < payOffAmountExchanged)
            {
                return false;
            }
            return true;
        }

        internal TransactionError PrePayForInstalment(Guid submitorId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return this.CallByExternal(() => PhysicalPayer.PrePayForInstalment(this, submitorId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData), () => TransactionError.RuntimeError);
        }

        internal TransactionError InstalmentPayoff(Guid submitorId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<Protocal.Physical.InstalmentData> instalments)
        {
            return this.CallByExternal(() => PhysicalPayer.InstalmentPayoff(this, submitorId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalments), () => TransactionError.RuntimeError);
        }


        internal TransactionError AcceptPlace(Guid tranID)
        {
            lock (_mutex)
            {
                try
                {
                    Logger.InfoFormat("accept place accountId = {0}, tranId = {1}", this.Id, tranID);
                    var tran = this.GetTran(tranID);
                    if (tran == null) return TransactionError.TransactionNotExists;
                    if (tran.Phase == TransactionPhase.Placed || tran.Phase == TransactionPhase.Executed)
                    {
                        return TransactionError.OK;
                    }
                    if (tran.OrderType == OrderType.Market)
                    {
                        tran.ChangePhaseToPlaced();
                        MarketOrderProcessor.Default.Add(tran.FirstOrder);
                    }
                    else
                    {
                        tran.ChangePhaseToPlaced();
                        InteractFacade.Default.TradingEngine.AcceptPlace(tran);
                    }
                    this.SaveAndBroadcastChanges();
                    Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateAcceptPlaceCommand(this.Id, tranID, tran.InstrumentId));
                    return TransactionError.OK;
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                    return TransactionError.RuntimeError;
                }
            }
        }

        internal bool ChangeLeverage(int leverage, out decimal necessary)
        {
            lock (_mutex)
            {
                return this.ChangeAccountLeverage(leverage, out necessary);
            }
        }

        internal void Hit(QuotationBulk quotationBulk)
        {
            lock (_mutex)
            {
                if (this.Id == HitService.TEST_ACCOUNT)
                {
                    Logger.InfoFormat("Hit accountId = {0}, instrumentCount = {1}", this.Id, _instrumentManager.Count);
                }
                if (_instrumentManager.Count == 0) return;
                foreach (var eachInstrument in _instrumentManager.Instruments)
                {
                    if (this.Id == HitService.TEST_ACCOUNT)
                    {
                        Logger.InfoFormat("Hit accountId = {0}, hit instrument = {1}, waitingForHitOrderCount = {2}", this.Id, eachInstrument.Id, eachInstrument.WaitingForHitOrders.Count);
                    }
                    _hitService.Value.HitOrders(eachInstrument, quotationBulk);
                }
            }
        }


        internal OrderHitStatus TryHit(Order order, Quotation quotation)
        {
            lock (_mutex)
            {
                return _hitService.Value.HitPlacedOrder(order, quotation, true);
            }
        }


        internal DateTime? GetPositionDay()
        {
            lock (_mutex)
            {
                return _resetService.Value.GetPositionDay();
            }
        }

        internal void GetInstrumentPrice(Guid instrumentId, out string buyPrice, out string sellPrice)
        {
            lock (_mutex)
            {
                buyPrice = sellPrice = null;
                var instrument = this.GetOrCreateInstrument(instrumentId);
                var quotation = instrument.GetQuotation();
                if (quotation != null)
                {
                    buyPrice = quotation.BuyPrice.ToString();
                    sellPrice = quotation.SellPrice.ToString();
                }
            }
        }

        internal AccountFloatingStatus GetAccountFloatingStatus()
        {
            lock (_mutex)
            {
                AccountFloatingStatus result = new AccountFloatingStatus();
                result.FloatingPL = this.SumFund.TradePLFloat;
                result.Equity = this.SumFund.Equity;
                result.Necessary = this.SumFund.Necessary;

                foreach (var eachFund in this.Funds)
                {
                    result.FundStatus.Add(new FundStatus
                    {
                        CurrencyId = eachFund.CurrencyId,
                        FloatingPL = eachFund.TradePLFloat,
                        Equity = eachFund.Equity,
                        Necessary = eachFund.Necessary
                    });
                }


                foreach (var eachTran in this.Transactions)
                {
                    foreach (var eachOrder in eachTran.Orders)
                    {
                        if (eachOrder.Phase == OrderPhase.Executed)
                        {
                            result.OrderStatus.Add(new OrderFloatingStatus
                            {
                                ID = eachOrder.Id,
                                FloatingPL = eachOrder.TradePLFloat,
                                LivePrice = eachOrder.LivePrice != null ? eachOrder.LivePrice.ToString() : string.Empty
                            });
                        }
                    }
                }
                return result;
            }
        }

        public void NotifyDeliveryApproved(Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime)
        {
            this.CallByExternal(() =>
            {
                var request = DeliveryRequestManager.Default[deliveryRequestId];
                foreach (var eachRelation in request.DeliveryRequestOrderRelations)
                {
                    Order openOrder = this.GetOrder(eachRelation.OpenOrderId);
                    openOrder.LotBalance -= eachRelation.DeliveryLot;
                }
                request.DeliveryRequestStatus = DeliveryRequestStatus.Approved;
                this.SaveAndBroadcastChanges();
            });
        }

        public bool NotifyDeliveried(Guid deliveryRequestId)
        {
            return this.CallByExternal(() =>
                {
                    var request = DeliveryRequestManager.Default[deliveryRequestId];
                    request.DeliveryRequestStatus = DeliveryRequestStatus.OrderCreated;
                    this.SaveAndBroadcastChanges();
                    return true;
                }, () => false);
        }

        private void CallByExternal(Action action)
        {
            lock (_mutex)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    this.HanderError(ex);
                }
            }
        }

        private T CallByExternal<T>(Func<T> fun, Func<T> errorHandle)
        {
            lock (_mutex)
            {
                try
                {
                    this.CheckState();
                    return fun();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    this.RejectChanges();
                    return errorHandle();
                }
            }
        }

        private void HanderError(Exception ex)
        {
            Logger.Error(ex);
            this.RejectChanges();
        }

        private void CheckState()
        {
            this.CheckAccountIsExpired();

            if (this.State == AccountState.Initialize)
            {
                throw new TransactionServerException(TransactionError.AccountIsIntializing);
            }
            else if (this.State == AccountState.InReset)
            {
                throw new TransactionServerException(TransactionError.AccountIsInReset);
            }
        }

        private void CheckAccountIsExpired()
        {
            if (_settingAccount.EndTime < DateTime.Now)
            {
                throw new AccountExpiredException(this.Id);
            }
        }


        public override string ToString()
        {
            return string.Format("ID ={0}, Code= {3} ,State={1}, Status={2}, transactionCount = {4}, AlertLevel = {5}, AlertTime = {6}", this.Id, this.State, this.Status, this.Setting().Code, this.TransactionCount, this.AlertLevel, this.AlertTime);
        }
    }


    internal sealed class AccountExpiredException : Exception
    {
        internal AccountExpiredException(Guid accountId)
        {
            this.AccountId = accountId;
        }

        internal Guid AccountId { get; private set; }

        public override string ToString()
        {
            return string.Format("AccountId = {0}", this.AccountId);
        }
    }

}