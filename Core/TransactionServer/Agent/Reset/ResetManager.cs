using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Protocal.TypeExtensions;
using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.DB;
using Protocal.CommonSetting;
using System.Data.SqlClient;
using Core.TransactionServer.Agent.Settings;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class ResetManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ResetManager));
        private sealed class InstrumentDayOpenCloseHistoryRepository
        {
            private Dictionary<Guid, Dictionary<DateTime, InstrumentDayOpenCloseHistory>> _perInstrumentDict = new Dictionary<Guid, Dictionary<DateTime, InstrumentDayOpenCloseHistory>>();
            private object _mutex = new object();

            internal IEnumerable<InstrumentDayOpenCloseHistory> Get(Guid instrumentId)
            {
                lock (_mutex)
                {
                    Dictionary<DateTime, InstrumentDayOpenCloseHistory> result;
                    if (_perInstrumentDict.TryGetValue(instrumentId, out result))
                    {
                        return result.Values;
                    }
                    return null;
                }
            }

            internal void Load()
            {
                lock (_mutex)
                {
                    var reader = DBRepository.Default.LoadInstrumentDayOpenCloseHistory();
                    while (reader.Read())
                    {
                        this.AddCommon(new InstrumentDayOpenCloseHistory(new DBReader(reader)));
                    }
                }
            }

            internal void Add(Guid id, DateTime tradeDay, DateTime? dayOpenTime, DateTime? dayCloseTime, DateTime? valueDate, DateTime? realValueDate)
            {
                lock (_mutex)
                {
                    var model = new InstrumentDayOpenCloseHistory()
                    {
                        InstrumentID = id,
                        TradeDay = tradeDay,
                        DayOpenTime = dayOpenTime,
                        DayCloseTime = dayCloseTime,
                        ValueDate = valueDate,
                        RealValueDate = realValueDate
                    };
                    this.AddCommon(model);
                }
            }


            private void AddCommon(InstrumentDayOpenCloseHistory model)
            {
                Dictionary<DateTime, InstrumentDayOpenCloseHistory> historys;
                if (!_perInstrumentDict.TryGetValue(model.InstrumentID, out historys))
                {
                    historys = new Dictionary<DateTime, InstrumentDayOpenCloseHistory>();
                    _perInstrumentDict.Add(model.InstrumentID, historys);
                }
                if (!historys.ContainsKey(model.TradeDay))
                {
                    historys.Add(model.TradeDay, model);
                }
            }


            internal bool ExistsInstrumentTradeDay(Guid instrumentId, DateTime tradeDay)
            {
                lock (_mutex)
                {
                    Dictionary<DateTime, InstrumentDayOpenCloseHistory> instrumentDayOpenCloseHistoryPerTradeDayDict;
                    if (!_perInstrumentDict.TryGetValue(instrumentId, out instrumentDayOpenCloseHistoryPerTradeDayDict))
                    {
                        return false;
                    }
                    return instrumentDayOpenCloseHistoryPerTradeDayDict.ContainsKey(tradeDay);
                }
            }


            internal InstrumentDayOpenCloseHistory Get(Guid instrumentId, DateTime tradeDay)
            {
                lock (_mutex)
                {
                    Dictionary<DateTime, InstrumentDayOpenCloseHistory> instrumentDayOpenCloseHistoryPerTradeDayDict;
                    if (!_perInstrumentDict.TryGetValue(instrumentId, out instrumentDayOpenCloseHistoryPerTradeDayDict))
                    {
                        return null;
                    }
                    if (!instrumentDayOpenCloseHistoryPerTradeDayDict.ContainsKey(tradeDay))
                    {
                        return null;
                    }
                    return instrumentDayOpenCloseHistoryPerTradeDayDict[tradeDay];
                }
            }

            internal void ProcessForReset(Guid instrumentId, DateTime tradeDay)
            {
                lock (_mutex)
                {
                    try
                    {
                        var instrumentDayOpenCloseHistory = this.Get(instrumentId, tradeDay);
                        if (instrumentDayOpenCloseHistory == null || instrumentDayOpenCloseHistory.ValueDate == null) return;
                        var historyRecords = this.Get(instrumentId);
                        DateTime lastTradeDay = historyRecords.Where(m => m.TradeDay < tradeDay).Max(m => m.TradeDay);
                        foreach (var eachItem in historyRecords)
                        {
                            if (eachItem.RealValueDate == null
                                && ((eachItem.TradeDay == tradeDay && eachItem.ValueDate <= tradeDay)
                                    || (eachItem.TradeDay < tradeDay && eachItem.ValueDate > lastTradeDay && eachItem.ValueDate <= tradeDay)))
                            {
                                eachItem.RealValueDate = tradeDay;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }


        }
        public static readonly ResetManager Default = new ResetManager();
        private OrderDayHistoryRepository _orderDayHistoryRepository;
        private InstrumentDayOpenCloseHistoryRepository _instrumentDayOpenCloseHistoryRepository;
        private object _mutex = new object();
        private HistorySettingManager _historySettingManager;
        private DailyQuotationManager _dailyQuotationManager;

        static ResetManager() { }
        private ResetManager()
        {
            _orderDayHistoryRepository = new OrderDayHistoryRepository();
            _instrumentDayOpenCloseHistoryRepository = new InstrumentDayOpenCloseHistoryRepository();
            _historySettingManager = new HistorySettingManager();
            _dailyQuotationManager = new DailyQuotationManager();
        }

        internal IEnumerable<InstrumentDayOpenCloseHistory> GetInstrumentDayOpenCloseHistory(Guid instrumentId)
        {
            return _instrumentDayOpenCloseHistoryRepository.Get(instrumentId);
        }

        internal void ProcessForReset(Guid instrumentId, DateTime tradeDay)
        {
            _instrumentDayOpenCloseHistoryRepository.ProcessForReset(instrumentId, tradeDay);
        }

        internal InstrumentDayOpenCloseHistory GetInstrumentDayOpenCloseHistory(Guid instrumentId, DateTime tradeDay)
        {
            return _instrumentDayOpenCloseHistoryRepository.Get(instrumentId, tradeDay);
        }

        internal bool ExistsInstrumentTradeDay(Guid instrumentId, DateTime tradeDay)
        {
            return _instrumentDayOpenCloseHistoryRepository.ExistsInstrumentTradeDay(instrumentId, tradeDay);
        }

        internal void Add(Guid id, DateTime tradeDay, DateTime? dayOpenTime, DateTime? dayCloseTime, DateTime? valueDate, DateTime? realValueDate)
        {
            _instrumentDayOpenCloseHistoryRepository.Add(id, tradeDay, dayOpenTime, dayCloseTime, valueDate, realValueDate);
        }

        public void AddOrderDayHistory(Order order, OrderResetResult orderResetResult)
        {
            _orderDayHistoryRepository.AddOrderDayHistory(order, orderResetResult);
        }

        internal void AddOrderDayHistory(OrderDayHistory model)
        {
            _orderDayHistoryRepository.Add(model);
        }

        public Dictionary<DateTime, OrderDayHistory> GetOrderDayHistorysByOrderId(Guid orderId)
        {
            return _orderDayHistoryRepository.GetOrderDayHistorysByOrderId(orderId);
        }

        internal List<OrderDayHistory> GetOrderDayHistorys(List<Guid> orders, Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            return _orderDayHistoryRepository.GetOrderDayHistorys(orders, tradeDay);
        }

        internal OrderDayHistory GetOrderDayHistory(Guid orderId, DateTime tradeDay)
        {
            return _orderDayHistoryRepository.GetOrderDayHistory(orderId, tradeDay);
        }

        internal List<OrderDayHistory> GetOrderDayHistorys(List<KeyValuePair<Guid, DateTime?>> orders)
        {
            return _orderDayHistoryRepository.GetOrderDayHistorys(orders);
        }

        internal void RemoveOrderDayHistorys(List<Guid> orders, DateTime tradeDay)
        {
            _orderDayHistoryRepository.RemoveOrderDayHistorys(orders, tradeDay);
        }

        internal void RemoveOrderDayHistorys(Guid orderId)
        {
            _orderDayHistoryRepository.RemoveOrderDayHistorys(orderId);
        }

        internal void LoadOrderDayHistorys()
        {
            _orderDayHistoryRepository.LoadOrderDayHistorys();
        }

        internal void LoadInstrumentDayOpenCloseHistorys()
        {
            _instrumentDayOpenCloseHistoryRepository.Load();
        }


        internal InstrumentTradeDaySetting LoadInstrumentHistorySettingAndData(Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            return _historySettingManager.LoadInstrumentHistorySettingAndData(accountId, instrumentId, tradeDay);
        }

        internal void LoadHistorySetting(DateTime tradeDay, string msg )
        {
            _historySettingManager.LoadHistorySetting(tradeDay,msg);
        }

        internal void RemoveHistroySetting(DateTime tradeDay)
        {
            _historySettingManager.RemoveHistroySetting(tradeDay);
        }

        internal void ClearHistorySettings()
        {
            _historySettingManager.ClearHistorySettings();
        }



        internal Tuple<Price, Price> LoadDailyClosePrice(Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            lock (_mutex)
            {
                DataRow data = DBRepository.Default.GetInstrumentDailyClosePrice(instrumentId, accountId, tradeDay);
                return PriceHelper.ParseBuyAndSellPrice(data, instrumentId, tradeDay, Settings.Setting.Default);
            }
        }

        internal UsableMarginPrice GetRefPriceForUsableMargin(Guid instrumentId, Guid accountId, DateTime tradeDay)
        {
            lock (_mutex)
            {
                DataRow data = DBRepository.Default.GetRefPriceForUsableMargin(instrumentId, accountId, tradeDay);
                Price privateBid = PriceHelper.CreatePrice(data.GetColumn<string>("Bid_Private"), instrumentId, tradeDay);
                Price privateAsk = PriceHelper.CreatePrice(data.GetColumn<string>("Ask_Private"), instrumentId, tradeDay);
                Price publicBid = PriceHelper.CreatePrice(data.GetColumn<string>("Bid_Public"), instrumentId, tradeDay);
                Price publicAsk = PriceHelper.CreatePrice(data.GetColumn<string>("Ask_Public"), instrumentId, tradeDay);
                return new UsableMarginPrice(privateBid, privateAsk, publicBid, publicAsk);
            }
        }

        internal List<DB.DBMapping.InstrumentDayClosePrice> GetInstrumentDayClosePrice(Guid instrumentId, DateTime tradeDay)
        {
            return _dailyQuotationManager.GetDailyQuotation(instrumentId, tradeDay);
        }

        internal void ClearInstrumentDailylosePrice()
        {
            _dailyQuotationManager.Clear();
        }


        internal IEnumerable<InstrumentResetResult> GetAccountInstrumentResetHistory(Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            return DBRepository.Default.GetAccountInstrumentResetHistory(accountId, instrumentId, tradeDay);
        }

    }

    internal sealed class HistorySettingManager
    {
        private sealed class DictionaryPool
        {
            private Queue<Dictionary<Guid, InstrumentTradeDaySetting>> _queue = new Queue<Dictionary<Guid, InstrumentTradeDaySetting>>(100);

            internal void Add(Dictionary<Guid, InstrumentTradeDaySetting> dict)
            {
                _queue.Enqueue(dict);
            }

            internal Dictionary<Guid, InstrumentTradeDaySetting> Get()
            {
                if (_queue.Count > 0)
                {
                    var result = _queue.Dequeue();
                    result.Clear();
                    return result;
                }
                return new Dictionary<Guid, InstrumentTradeDaySetting>(50);
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(HistorySettingManager));
        private Dictionary<InstrumentSettingKey, Dictionary<Guid, InstrumentTradeDaySetting>> _instrumentSettingsPerTradeDayDict = new Dictionary<InstrumentSettingKey, Dictionary<Guid, InstrumentTradeDaySetting>>(1000);
        private object _mutex = new object();
        private DictionaryPool _pool = new DictionaryPool();

        internal InstrumentTradeDaySetting LoadInstrumentHistorySettingAndData(Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            lock (_mutex)
            {
                this.LoadHistorySetting(tradeDay, string.Format("LoadInstrumentHistorySettingAndData accountID = {0}, instrumentId = {1}", accountId, instrumentId));
                Dictionary<Guid, InstrumentTradeDaySetting> accountSetting;
                if (!_instrumentSettingsPerTradeDayDict.TryGetValue(new InstrumentSettingKey(instrumentId, tradeDay), out accountSetting))
                {
                    if (!this.LoadInstrumentAccountSettingData(accountId, instrumentId, tradeDay, out accountSetting)) return null;
                }
                if (!accountSetting.ContainsKey(accountId))
                {
                    throw new InstrumentTradeDaySettingLoadException(accountId, instrumentId, tradeDay, string.Format("accountId={0}, instrumentId={1}, tradeDay={2}", accountId, instrumentId, tradeDay));
                }
                return accountSetting[accountId];
            }
        }


        private bool LoadInstrumentAccountSettingData(Guid accountId, Guid instrumentId, DateTime tradeDay, out Dictionary<Guid, InstrumentTradeDaySetting> accountSetting)
        {
            var reader = DBRepository.Default.GetInstrumentTradeDaySettingData(instrumentId, tradeDay);
            accountSetting = null;
            if (reader == null) return false;
            accountSetting = _pool.Get();
            while (reader.Read())
            {
                Guid eachAccountId = (Guid)reader["AccountID"];
                accountSetting.Add(eachAccountId, new InstrumentTradeDaySetting(new DBReader(reader), instrumentId, tradeDay, Settings.Setting.Default));
            }
            _instrumentSettingsPerTradeDayDict.Add(new InstrumentSettingKey(instrumentId, tradeDay), accountSetting);
            return true;
        }


        internal void LoadHistorySetting(DateTime tradeDay,string msg = "")
        {
            lock (_mutex)
            {
                if (!Settings.Setting.Default.ExistsHistorySettings(tradeDay))
                {
                    Logger.InfoFormat("LoadHistorySetting tradeDay={0} msg = {1}", tradeDay, msg);
                    var reader = DBRepository.Default.GetHistorySettingsByReader(tradeDay.Date);
                    SettingInfo setting = SettingInfoPool.Default.Get();
                    this.ReadHistorySetting(reader, setting);
                    Settings.Setting.Default.AddHistorySettings(tradeDay, setting);
                }
            }
        }

        private void ReadHistorySetting(SqlDataReader reader, SettingInfo setting)
        {
            this.ReadCurrency(reader, setting);
            this.ReadCurrencyRate(reader, setting);
            this.ReadQuotePolicyDetail(reader, setting);
            this.ReadTradePolicy(reader, setting);
            this.ReadTradePolicyDetail(reader, setting);
            this.ReadDealingPolicy(reader, setting);
            this.ReadDealingPolicyDetail(reader, setting);
            this.ReadVolumeNecessary(reader, setting);
            this.ReadVolumeNecessaryDetail(reader, setting);
            setting.UpdateVolumeNecessaryOfTradePolicyDetail();
            this.ReadInstalmentPolicy(reader, setting);
            this.ReadInstalmentPolicyDetail(reader, setting);
            this.ReadCustomer(reader, setting);
            this.ReadAccount(reader, setting);
            this.ReadInstrument(reader, setting);
            this.ReadTradeDay(reader, setting);
            this.ReadPhysicalPaymentDiscount(reader, setting);
            this.ReadPhysicalPaymentDiscountDetail(reader, setting);
        }

        private void ReadCurrency(SqlDataReader reader, SettingInfo setting)
        {
            while (reader.Read())
            {
                setting.InitializeCurrency(reader);
            }
        }

        private void ReadCurrencyRate(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeCurrencyRate(reader);
            }
        }

        private void ReadQuotePolicyDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeQuotePolicyDetail(reader);
            }
        }

        private void ReadTradePolicy(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeTradePolicy(reader);
            }
        }

        private void ReadTradePolicyDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeTradePolicyDetail(reader);
            }
        }

        private void ReadDealingPolicy(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeDealingPolicy(reader);
            }
        }


        private void ReadDealingPolicyDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeDealingPolicyDetail(reader);
            }
        }


        private void ReadVolumeNecessary(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeVolumeNecessary(reader);
            }
        }

        private void ReadVolumeNecessaryDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeVolumeNecessaryDetail(reader);
            }
        }

        private void ReadInstalmentPolicy(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeInstalmentPolicy(reader);
            }
        }

        private void ReadInstalmentPolicyDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeInstalmentPolicyDetail(reader);
            }
        }

        private void ReadCustomer(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeCustomer(reader);
            }
        }

        private void ReadAccount(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeAccount(reader);
            }
        }


        private void ReadInstrument(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializeInstrument(reader);
            }
        }

        private void ReadTradeDay(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            if (reader.Read())
            {
                setting.InitializeTradeDay(reader);
            }
        }

        private void ReadPhysicalPaymentDiscount(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializePhysicalPaymentDiscount(reader);
            }
        }

        private void ReadPhysicalPaymentDiscountDetail(SqlDataReader reader, SettingInfo setting)
        {
            reader.NextResult();
            while (reader.Read())
            {
                setting.InitializePhysicalPaymentDiscountDetail(reader);
            }
        }

        internal void RemoveHistroySetting(DateTime tradeDay)
        {
            lock (_mutex)
            {
                Settings.Setting.Default.RemoveHistorySettings(tradeDay);
            }
        }

        internal void ClearHistorySettings()
        {
            lock (_mutex)
            {
                Settings.Setting.Default.ClearHistorySettings();
                foreach (var item in _instrumentSettingsPerTradeDayDict.Values)
                {
                    item.Clear();
                    _pool.Add(item);
                }
                _instrumentSettingsPerTradeDayDict.Clear();
            }
        }

    }

    internal sealed class DailyQuotationManager
    {
        private Dictionary<InstrumentSettingKey, List<DB.DBMapping.InstrumentDayClosePrice>> _quotationPerInstrumentTradeDayDict = new Dictionary<InstrumentSettingKey, List<InstrumentDayClosePrice>>(100);
        private HashSet<DateTime> _generatedTradeDays = new HashSet<DateTime>();
        private object _mutext = new object();

        internal List<DB.DBMapping.InstrumentDayClosePrice> GetDailyQuotation(Guid instrumentId, DateTime tradeDay)
        {
            lock (_mutext)
            {
                if (!_generatedTradeDays.Contains(tradeDay))
                {
                    this.GenerateDailyQuotation(tradeDay);
                }
                List<DB.DBMapping.InstrumentDayClosePrice> result = null;
                _quotationPerInstrumentTradeDayDict.TryGetValue(new InstrumentSettingKey(instrumentId, tradeDay), out result);
                return result;
            }
        }


        internal void Clear()
        {
            lock (_mutext)
            {
                _generatedTradeDays.Clear();
                _quotationPerInstrumentTradeDayDict.Clear();
            }
        }


        private void GenerateDailyQuotation(DateTime tradeDay)
        {
            var result = DBRepository.Default.GenerateDailyClosePrice(tradeDay);
            if (result == null) return;
            foreach (var eachItem in result)
            {
                List<DB.DBMapping.InstrumentDayClosePrice> prices;
                if (!_quotationPerInstrumentTradeDayDict.TryGetValue(new InstrumentSettingKey(eachItem.InstrumentID, eachItem.TradeDay), out prices))
                {
                    prices = new List<InstrumentDayClosePrice>();
                    _quotationPerInstrumentTradeDayDict.Add(new InstrumentSettingKey(eachItem.InstrumentID, eachItem.TradeDay), prices);
                }
                prices.Add(eachItem);
            }
            _generatedTradeDays.Add(tradeDay);
        }




    }


    internal sealed class UsableMarginPrice
    {
        internal UsableMarginPrice(Price privateBid, Price privateAsk, Price publicBid, Price publicAsk)
        {
            this.PrivateBid = privateBid;
            this.PrivateAsk = privateAsk;
            this.PublicBid = publicBid;
            this.PublicAsk = publicAsk;
        }

        internal Price PrivateBid { get; private set; }
        internal Price PrivateAsk { get; private set; }
        internal Price PublicBid { get; private set; }
        internal Price PublicAsk { get; private set; }

    }


    internal sealed class InstrumentTradeDaySetting
    {
        internal InstrumentTradeDaySetting() { }

        internal InstrumentTradeDaySetting(IDBRow dr, Guid instrumentId, DateTime tradeDate, Settings.Setting setting)
        {
            this.InterestMultiple = dr.GetColumn<int>("InterestMultiple");
            this.BeginTime = dr.GetColumn<DateTime>("BeginTime");
            this.ResetTime = dr.GetColumn<DateTime>("ResetTime");
            this.ValueDate = dr.GetColumn<DateTime?>("ValueDate");
            this.ShouldValueCurrentDayPL = dr.GetColumn<bool>("ShouldValueCurrentDayPL");
            this.IsUseSettlementPriceForInterest = dr.GetColumn<bool>("IsUseSettlementPriceForInterest");
            this.StoragePerLotInterestRateBuy = dr.GetColumn<decimal>("StoragePerLotInterestRateBuy");
            this.StoragePerLotInterestRateSell = dr.GetColumn<decimal>("StoragePerLotInterestRateSell");
            this.InterestRateBuy = dr.GetColumn<decimal>("InterestRateBuy");
            this.InterestRateSell = dr.GetColumn<decimal>("InterestRateSell");
            this.InstalmentInterestRateBuy = dr.GetColumn<decimal>("InstalmentInterestRateBuy");
            this.InstalmentInterestRateSell = dr.GetColumn<decimal>("InstalmentInterestRateSell");
            this.IsMonthLastDay = dr.GetColumn<bool>("IsMonthLastDay");
            this.WeekDay = dr.GetColumn<int>("WeekDay");
            this.IsInterestUseAccountCurrency = dr.GetColumn<bool>("IsInterestUseAccountCurrency");
        }

        internal int InterestMultiple { get; set; }
        internal Price BuyPrice { get; private set; }
        internal Price SellPrice { get; private set; }
        internal DateTime ResetTime { get; set; }
        internal DateTime? ValueDate { get; set; }
        internal DateTime BeginTime { get; set; }
        internal bool ShouldValueCurrentDayPL { get; set; }
        internal bool IsUseSettlementPriceForInterest { get; set; }
        internal decimal StoragePerLotInterestRateBuy { get; set; }
        internal decimal StoragePerLotInterestRateSell { get; set; }
        internal decimal InterestRateBuy { get; set; }
        internal decimal InterestRateSell { get; set; }
        internal decimal InstalmentInterestRateBuy { get; set; }
        internal decimal InstalmentInterestRateSell { get; set; }
        internal bool IsMonthLastDay { get; set; }
        internal int WeekDay { get; set; }
        internal bool IsInterestUseAccountCurrency { get; set; }
        internal bool UseCompatibleMode { get; set; }

        internal void UpdateInstrumentDayClosePrice(Price buyPrice, Price sellPrice)
        {
            this.BuyPrice = buyPrice;
            this.SellPrice = sellPrice;
        }

    }


    internal struct OrderDayHistoryKey : IEquatable<OrderDayHistoryKey>
    {
        private Guid _orderId;
        private DateTime _tradeDay;

        internal OrderDayHistoryKey(Guid orderId, DateTime tradeDay)
        {
            _orderId = orderId;
            _tradeDay = tradeDay;
        }

        internal Guid OrderId
        {
            get { return _orderId; }
        }

        internal DateTime TradeDay
        {
            get { return _tradeDay; }
        }

        public bool Equals(OrderDayHistoryKey other)
        {
            return this.OrderId == other.OrderId && this.TradeDay == other.TradeDay;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((OrderDayHistoryKey)obj);
        }

        public override int GetHashCode()
        {
            return this.OrderId.GetHashCode() ^ this.TradeDay.GetHashCode();
        }
    }

    internal struct InstrumentSettingKey : IEquatable<InstrumentSettingKey>
    {
        private Guid _id;
        private DateTime _tradeDay;

        internal InstrumentSettingKey(Guid id, DateTime tradeDay)
        {
            _id = id;
            _tradeDay = tradeDay;
        }

        internal Guid Id
        {
            get { return _id; }
        }

        internal DateTime TradeDay
        {
            get { return _tradeDay; }
        }

        public bool Equals(InstrumentSettingKey other)
        {
            return this.Id == other.Id && this.TradeDay == other.TradeDay;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((InstrumentSettingKey)obj);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^ this.TradeDay.GetHashCode();
        }
    }

    internal sealed class InstrumentTradeDaySettingLoadException : Exception
    {
        internal InstrumentTradeDaySettingLoadException(Guid accountId, Guid instrumentId, DateTime tradeDay, string msg)
            : base(msg)
        {
            this.AccountId = accountId;
            this.InstrumentId = instrumentId;
            this.TradeDay = tradeDay;
        }

        internal DateTime TradeDay { get; private set; }

        internal Guid InstrumentId { get; private set; }

        internal Guid AccountId { get; private set; }
    }


}
