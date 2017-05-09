using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.AccountClass;
using System.Threading.Tasks;
using System.Threading;
using log4net;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class SystemResetter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SystemResetter));

        private Account _account;
        private BusinessItem<DateTime?> _lastResetDay;
        private InstrumentManager _instrumentManager;

        internal SystemResetter(Account account, InstrumentManager instrumentManager)
        {
            _account = account;
            _instrumentManager = instrumentManager;
            _lastResetDay = BusinessItemFactory.Create<DateTime?>("LastResetDay", null, PermissionFeature.Sound, account);
        }

        internal DateTime? LastResetDay
        {
            get { return _lastResetDay.Value; }
            set
            {
                _lastResetDay.SetValue(value);
            }
        }


        private bool CanSystemResetBegin(DateTime tradeDate)
        {
#if ACCOUNTRESET
            return true;
#else
            if (_account.LastResetDay >= tradeDate) return false;
            ResetManager.Default.LoadHistorySetting(tradeDate, "CanSystemResetBegin");
            var tradeDay = Settings.Setting.Default.GetTradeDay(tradeDate);
            Debug.Assert(tradeDate != null);
            return tradeDay.EndTime <= Market.MarketManager.Now;
#endif
        }

        internal void DoSystemReset(DateTime tradeDay)
        {
            if (!this.CanSystemResetBegin(tradeDay)) return;
            Logger.InfoFormat("begin do system reset accountId = {0}, tradeDay = {1}, lastResetDay = {2}", _account.Id, tradeDay, this.LastResetDay);
            this.DoInstrumentReset(tradeDay);
            if (this.LastResetDay == null)
            {
                this.ProcessWhenLastRestDayNotExists(tradeDay);
            }
            else
            {
                this.ProcessWhenLastResetDayExists(tradeDay);
            }
            _account.LastResetDay = tradeDay;
            OrderUpdater.Default.UpdateInterestPerLotAndStoragePerLot(_account, tradeDay);
        }

        /// <summary>
        /// 检查instrument日结是否完成，如果没做完，则做instrrument的日结
        /// </summary>
        /// <param name="tradeDay"></param>
        private void DoInstrumentReset(DateTime tradeDay)
        {
            if (!_account.IsInstrumentsReseted(tradeDay))
            {
                _account.DoInstrumentReset(tradeDay);
            }
        }


        private void ProcessWhenLastRestDayNotExists(DateTime tradeDay)
        {
            var accountBalanceDayHistorys = DB.DBRepository.Default.GetLastAccountBalanceDayHistory(_account.Id);
            if (accountBalanceDayHistorys == null || accountBalanceDayHistorys.Count() == 0)
            {
                this.ProcessWhenAccountBalanceDayHistoryNotExists(tradeDay);
            }
            else
            {
                DateTime lastTradeDay = accountBalanceDayHistorys.First().TradeDay;
                this.DoContinuousReset(lastTradeDay, tradeDay);
            }
        }


        private void DoContinuousReset(DateTime lastTradeDay, DateTime tradeDay)
        {
            if (lastTradeDay >= tradeDay) return;
            for (DateTime deltaTradeDay = lastTradeDay.AddDays(1); deltaTradeDay <= tradeDay; deltaTradeDay = deltaTradeDay.AddDays(1))
            {
                ResetManager.Default.LoadHistorySetting(deltaTradeDay, string.Format("SystemResetter.DoContinuousReset lastResetDay = {0}", lastTradeDay));
                this.DoSystemResetPerTradeDay(deltaTradeDay);
            }
        }

        private void ProcessWhenAccountBalanceDayHistoryNotExists(DateTime tradeDay)
        {
            DateTime? lastTradeDay = _account.GetPositionDay();
            Logger.InfoFormat("ProcessWhenAccountBalanceDayHistoryNotExists accountId = {0}, lastTradeDay = {1}, tradeDay = {2}", _account.Id, lastTradeDay, tradeDay);
            if (lastTradeDay != null && lastTradeDay.Value < tradeDay)
            {
                this.DoContinuousReset(lastTradeDay.Value, tradeDay);
            }
            else
            {
                _account.AddCurrencyForReset(tradeDay);
            }

        }

        private void ProcessWhenLastResetDayExists(DateTime tradeDay)
        {
            this.DoContinuousReset(this.LastResetDay.Value, tradeDay);
        }


        private void DoSystemResetPerTradeDay(DateTime tradeDay)
        {
            if (this.NeedDoSystemReset(_account.Id, tradeDay))
            {
                this.CalculateUsableInterest(tradeDay);
            }
            _account.AddCurrencyForReset(tradeDay);
        }

        private bool NeedDoSystemReset(Guid accountId, DateTime tradeDay)
        {
            Settings.Account historyAccount = Settings.Setting.Default.GetAccount(_account.Id, tradeDay);
            if (historyAccount == null)
            {
                throw new NullReferenceException(string.Format("NeedDoSystemReset historyAccount not exists accountId = {0}, tradeDay = {1}", accountId, tradeDay));
            }
            bool result = historyAccount.IsActive && historyAccount.MarginInterestOption != Settings.MarginInterestOption.None;
            if (result)
            {
                Logger.InfoFormat("NeedDoSystemReset = {0}, accountId = {1}, tradeDay = {2}, historyAccount.IsActive = {3}, historyAccount.MarginInterestOption = {4}", result,
                    accountId, tradeDay, historyAccount.IsActive, historyAccount.MarginInterestOption);
            }
            return result;
        }

        private void CalculateUsableInterest(DateTime tradeDate)
        {
            Settings.Account historyAccount = Settings.Setting.Default.GetAccount(_account.Id, tradeDate);
            var tradingAccount = TradingSetting.Default.GetAccount(_account.Id);
            var usableAmountList = this.CalculateUsableAmount(tradingAccount, historyAccount.MarginInterestOption, tradeDate);
            foreach (var eachUsable in usableAmountList)
            {
                Guid currencyId = eachUsable.Key;
                decimal usableAmount = eachUsable.Value;
                var currency = Settings.Setting.Default.GetCurrency(currencyId);
                if (currency.InterestPolicyId == null) continue;
                var interestPolicy = Settings.Setting.Default.GetInterestPolicy(currency.InterestPolicyId.Value);
                var interestRate = usableAmount > 0 ? currency.UInterestIn : currency.UInterestOut;
                var interest = usableAmount * interestRate / currency.UsableInterestDayYear;
                decimal usableInterest = interest * this.GetInterestMultiple(tradeDate, interestPolicy);
                var tradeDay = Settings.Setting.Default.GetTradeDay(tradeDate);
                _account.AddBalance(currencyId, usableInterest, tradeDay.EndTime);
            }
        }

        private int GetInterestMultiple(DateTime tradeDay, Settings.InterestPolicy interestPolicy)
        {
            DayOfWeek weekDay = tradeDay.DayOfWeek;
            int result = 0;
            switch (weekDay)
            {
                case DayOfWeek.Monday:
                    result = interestPolicy.Mon;
                    break;
                case DayOfWeek.Tuesday:
                    result = interestPolicy.Tue;
                    break;
                case DayOfWeek.Wednesday:
                    result = interestPolicy.Wed;
                    break;
                case DayOfWeek.Thursday:
                    result = interestPolicy.Thu;
                    break;
                case DayOfWeek.Friday:
                    result = interestPolicy.Fri;
                    break;
                case DayOfWeek.Saturday:
                    result = interestPolicy.Sat;
                    break;
                case DayOfWeek.Sunday:
                    result = interestPolicy.Sun;
                    break;
            }
            return result;
        }

        private List<KeyValuePair<Guid, decimal>> CalculateUsableAmount(Agent.Account account, Settings.MarginInterestOption interestOption, DateTime tradeDay)
        {
            Dictionary<Guid, decimal> floatingPLDict = this.GetFloatingPLs(account, tradeDay);
            var balanceDict = this.GetBalances(account, tradeDay);
            var result = new List<KeyValuePair<Guid, decimal>>(balanceDict.Count);
            Dictionary<Guid, MarginResult> usableDict = null;
            if (interestOption == Settings.MarginInterestOption.Usable)
            {
                usableDict = AccountUsableNecessaryCalculator.Default.Calculate(account, tradeDay, _instrumentManager);
            }
            foreach (var eachPair in balanceDict)
            {
                decimal usable = 0m;
                decimal balance = eachPair.Value;
                Guid currencyId = eachPair.Key;

                if (interestOption == Settings.MarginInterestOption.Usable)
                {
                    usable = balance + this.GetFloatingPL(currencyId, floatingPLDict, tradeDay) - this.GetUsableMargin(currencyId, usableDict);
                }
                else if (interestOption == Settings.MarginInterestOption.Balance)
                {
                    usable = balance;
                }
                else if (interestOption == Settings.MarginInterestOption.Equity)
                {
                    usable = balance + this.GetFloatingPL(currencyId, floatingPLDict, tradeDay);
                }
                result.Add(new KeyValuePair<Guid, decimal>(currencyId, usable));
            }
            return result;
        }

        private decimal GetUsableMargin(Guid currencyId, Dictionary<Guid, MarginResult> usableMarginDict)
        {
            MarginResult result;
            if (!usableMarginDict.TryGetValue(currencyId, out result))
            {
                return 0m;
            }
            return result.Margin;
        }


        private decimal GetFloatingPL(Guid currencyId, Dictionary<Guid, decimal> floatingPLDict, DateTime tradeDay)
        {
            decimal result = 0m;
            if (_account.IsMultiCurrency)
            {
                floatingPLDict.TryGetValue(currencyId, out result);
            }
            else
            {
                var settingAccount = Settings.Setting.Default.GetAccount(_account.Id, tradeDay);
                foreach (var eachPair in floatingPLDict)
                {
                    Guid instrumentCurrencyId = eachPair.Key;
                    decimal floatingPl = eachPair.Value;
                    var currencyRate = Settings.Setting.Default.GetCurrencyRate(instrumentCurrencyId, settingAccount.CurrencyId, tradeDay);
                    var accountCurrency = Settings.Setting.Default.GetCurrency(settingAccount.CurrencyId, tradeDay);
                    result += floatingPl.Exchange(currencyRate.RateIn, currencyRate.RateOut, accountCurrency.Decimals);
                }
            }
            return result;
        }


        private Dictionary<Guid, decimal> GetBalances(Agent.Account account, DateTime tradeDate)
        {
            var tradeDay = Settings.Setting.Default.GetTradeDay(tradeDate);
            Dictionary<Guid, decimal> result = new Dictionary<Guid, decimal>(account.Funds.Count());

            foreach (var eachFund in account.Funds)
            {
                result.Add(eachFund.CurrencyId, eachFund.Balance);
            }

            foreach (var eachBill in account.Bills)
            {
                if (eachBill.UpdateTime > tradeDay.EndTime)
                {
                    decimal balance;
                    if (result.TryGetValue(eachBill.CurrencyID, out balance))
                    {
                        result[eachBill.CurrencyID] = balance - eachBill.Value;
                    }
                }
            }
            return result;
        }


        private Dictionary<Guid, decimal> GetFloatingPLs(Agent.Account account, DateTime tradeDay)
        {
            Dictionary<Guid, decimal> result = new Dictionary<Guid, decimal>();
            foreach (var eachInstrument in _instrumentManager.Instruments)
            {
                InstrumentResetItem resetItem = eachInstrument.GetResetItem(tradeDay);
                if (resetItem == null) continue;
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachInstrument.Id, tradeDay);
                decimal lastFloatingPL;
                if (!result.TryGetValue(settingInstrument.CurrencyId, out lastFloatingPL))
                {
                    result.Add(settingInstrument.CurrencyId, resetItem.FloatingPL);
                }
                else
                {
                    result[settingInstrument.CurrencyId] = lastFloatingPL + resetItem.FloatingPL;
                }
            }
            return result;
        }


    }
}
