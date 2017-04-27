using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using iExchange.Common;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Xml.Linq;
using log4net;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class Setting
    {
        internal static readonly Setting Default = new Setting(new SettingInfo());
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Setting));
        private Dictionary<DateTime, SettingInfo> _historySettingsDict = new Dictionary<DateTime, SettingInfo>();
        private SettingInfo _settingInfo;

        private Setting(SettingInfo settingInfo)
        {
            _settingInfo = settingInfo;
        }

        internal SettingInfo SettingInfo
        {
            get { return _settingInfo; }
        }

        internal SystemParameter SystemParameter
        {
            get { return _settingInfo.SystemParameter; }
        }

        internal Dictionary<Guid, Instrument> Instruments
        {
            get { return _settingInfo.Instruments; }
        }

        public DealingPolicy GetDealingPolicy(Guid dealingPolicyId)
        {
            return _settingInfo.GetDealingPolicy(dealingPolicyId);
        }

        public TradingTime GetTradingTime(Guid instrumentId)
        {
            return _settingInfo.GetTradingTime(instrumentId);
        }

        public QuotationParameter GetQuotationParameter(Guid instrumentId)
        {
            return _settingInfo.GetQuotationParameter(instrumentId);
        }

        internal void AddHistorySettings(DateTime tradeDay, SettingInfo info)
        {
            if (!_historySettingsDict.ContainsKey(tradeDay))
            {
                _historySettingsDict.Add(tradeDay, info);
            }
        }

        internal bool ExistsHistorySettings(DateTime tradeDay)
        {
            return _historySettingsDict.ContainsKey(tradeDay);
        }


        public void RemoveHistorySettings(DateTime tradeDay)
        {
            if (_historySettingsDict.ContainsKey(tradeDay))
            {
                var setting = _historySettingsDict[tradeDay];
                setting.Clear();
                _historySettingsDict.Remove(tradeDay);
                SettingInfoPool.Default.Add(setting);
            }
        }

        internal void FillCurrencyRate(List<CurrencyRate> target)
        {
            _settingInfo.FillCurrencyRate(target);
        }


        internal void ClearHistorySettings()
        {
            foreach (var eachSetting in _historySettingsDict.Values)
            {
                eachSetting.Clear();
            }
            _historySettingsDict.Clear();
        }


        public Account GetAccount(Guid accountId, DateTime? tradeDay = null)
        {
            return this.GetData(accountId, tradeDay, _settingInfo.Accounts, (setting, key) =>
            {
                Account account;
                setting.Accounts.TryGetValue(key, out account);
                return account;
            });
        }

        public Instrument GetInstrument(Guid instrumentId, DateTime? tradeDay = null)
        {
            return this.GetData(instrumentId, tradeDay, _settingInfo.Instruments, (setting, key) => setting.Instruments[key]);
        }

        public Customer GetCustomer(Guid customerId, DateTime? tradeDay = null)
        {
            return this.GetData(customerId, tradeDay, _settingInfo.Customers, (setting, key) => setting.Customers[key]);
        }

        internal TradeDay GetTradeDay(DateTime? tradeDay = null)
        {
            return this.GetData<Guid?, TradeDay>(null, tradeDay, null, (setting, key) => setting.TradeDay, _settingInfo.TradeDay);
        }


        public Currency GetCurrency(Guid currencyId, DateTime? tradeDay = null)
        {
            return this.GetData(currencyId, tradeDay, _settingInfo.Currencies, (setting, key) => setting.Currencies[key]);
        }

        public TradePolicy GetTradePolicy(Guid tradePolicyId, DateTime? tradeDay = null)
        {
            return this.GetData(tradePolicyId, tradeDay, _settingInfo.TradePolicies, (setting, key) => setting.TradePolicies[key]);
        }

        internal TradePolicyDetail GetTradePolicyDetail(Guid instrumentId, Guid tradePolicyId, DateTime? tradeDay)
        {
            return this.GetData(new TradePolicyDetailKey(instrumentId, tradePolicyId), tradeDay, _settingInfo.TradePolicyDetails, (setting, key) => setting.TradePolicyDetails[key]);
        }

        internal InterestPolicy GetInterestPolicy(Guid interestPolicyId, DateTime? tradeDay = null)
        {
            return this.GetData(interestPolicyId, tradeDay, _settingInfo.InterestPolicies, (setting, key) => setting.InterestPolicies[key]);
        }

        public SpecialTradePolicy GetSpecialTradePolicy(Guid specialTradePolicyId, DateTime? tradeDay = null)
        {
            return this.GetData(specialTradePolicyId, tradeDay, _settingInfo.SpecialTradePolicies, (setting, key) => setting.SpecialTradePolicies[key]);
        }


        public CurrencyRate GetCurrencyRate(Guid sourceCurrencyId, Guid targetCurrencyId, DateTime? tradeDay = null)
        {
            try
            {
                return this.GetData(new CurrencyIdPair(sourceCurrencyId, targetCurrencyId), tradeDay, _settingInfo.CurrencyRates, (setting, key) => setting.CurrencyRates[key]);
            }
            catch
            {
                Logger.ErrorFormat("GetCurrencyRate sourceCurrencyId={0}, targetCurrencyId={1}, tradeDay = {2}", sourceCurrencyId, targetCurrencyId, tradeDay);
                throw;
            }
        }


        internal PhysicalPaymentDiscountPolicy GetPhysicalPaymentDiscountPolicy(Guid id, DateTime? tradeDay = null)
        {
            return this.GetData(id, tradeDay, _settingInfo.PhysicalPaymentDiscountPolicyDict, (setting, key) => setting.PhysicalPaymentDiscountPolicyDict[key]);
        }

        public InstalmentPolicy GetInstalmentPolicy(Guid instalmentPolicyId, DateTime? tradeDay = null)
        {
            try
            {
                return this.GetData(instalmentPolicyId, tradeDay, _settingInfo.InstalmentPolicies, (setting, key) => setting.InstalmentPolicies[key]);
            }
            catch
            {
                Logger.ErrorFormat("Can't find instalment Policy Id = {0}", instalmentPolicyId);
                throw;
            }
        }

        public QuotePolicyDetail GetQuotePolicyDetail(IQuotePolicyProvider quotePolicyProvider, Guid instrumentId, DateTime? tradeDay = null)
        {
            QuotePolicyDetail result = quotePolicyProvider.Get<QuotePolicyDetail>(delegate(Guid id, out QuotePolicyDetail qpd)
            {
                return _settingInfo.QuotePolicyDetails.TryGetValue(new QuotePolicyInstrumentIdPair(id, instrumentId), out qpd);
            });
            return this.GetData(quotePolicyProvider, tradeDay, null, (setting, key) => setting.GetQuotePolicyDetail(key, instrumentId), result);
        }

        internal VolumeNecessary GetVolumeNecessary(Guid volumeNecessaryId, DateTime? tradeDay)
        {
            return this.GetData(volumeNecessaryId, tradeDay, _settingInfo.VolumeNecessaries, (setting, key) => setting.VolumeNecessaries[key]);
        }

        private TValue GetData<TKey, TValue>(TKey key, DateTime? tradeDay, Dictionary<TKey, TValue> modelDict, Func<SettingInfo, TKey, TValue> getHistoryData, TValue defalutValue = null) where TValue : class
        {
            try
            {
                TValue result = defalutValue != null ? defalutValue : modelDict[key];
                if (tradeDay == null)
                {
                    return result;
                }
                try
                {
                    var historySetting = this.GetHistorySetting(tradeDay.Value);
                    if (historySetting != null)
                    {
                        result = getHistoryData(historySetting, key);
                    }
                }
                catch (Exception exx)
                {
                    Logger.WarnFormat(string.Format("get historyData error key = {0}, tradeDay={1}, modelType= {2}", key, tradeDay, typeof(TValue)), exx);
                    result = modelDict[key];
                }
                return result;
            }
            catch
            {
                return null;
            }
        }



        private SettingInfo GetHistorySetting(DateTime tradeDay)
        {
            SettingInfo historySetting;
            if (_historySettingsDict.TryGetValue(tradeDay, out historySetting))
            {
                return historySetting;
            }
            return null;
        }
    }
}