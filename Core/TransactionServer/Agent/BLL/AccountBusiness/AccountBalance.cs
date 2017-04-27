using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Reset;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class AccountBalance
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountBalance));
        private Account _account;
        private BusinessRecordList<Reset.ResetBalance> _resetBalances;

        internal AccountBalance(Account account, BusinessRecordList<Reset.ResetBalance> resetBalances)
        {
            _account = account;
            _resetBalances = resetBalances;
        }

        internal Guid AccountCurrencyId
        {
            get { return _account.Setting().CurrencyId; }
        }


        internal void AddBalance(Guid currencyId, decimal balance, DateTime? updateTime)
        {
            this.AddNormalBalance(currencyId, balance, updateTime);
        }

        internal void AddHistoryBalance(Guid currencyId, DateTime tradeDay, decimal value)
        {
            this.AddResetBalance(tradeDay, currencyId, value);
        }

        private void AddResetBalance(DateTime tradeDay, Guid currencyId, decimal value)
        {
            var resetBalance = new ResetBalance(tradeDay, _account.Id, currencyId, value);
            _resetBalances.AddItem(resetBalance, OperationType.AsNewRecord);
        }

        private void AddNormalBalance(Guid currencyId, decimal balance, DateTime? updateTime = null)
        {
            this.AddNormalBalanceOnly(currencyId, balance);
            this.AddBillOnly(currencyId, balance, updateTime);
        }

        internal void AddNormalBalanceOnly(Guid currencyId, decimal balance)
        {
            Logger.InfoFormat("AddNormalBalanceOnly accountId = {0}, currencyId = {1}, deltaBalance = {2}, lastBalance = {3}, isMultiCurrency = {4}", _account.Id, currencyId, balance, _account.Balance, _account.IsMultiCurrency); 
            if (_account.IsMultiCurrency)
            {
                var fund = _account.GetOrCreateFund(currencyId);
                fund.AddBalance(balance);
                decimal exchangedBalance = fund.CurrencyRate.Exchange(balance);
                _account.SumFund.AddBalance(exchangedBalance);
            }
            else
            {
                var fund = _account.GetOrCreateFund(this.AccountCurrencyId);
                fund.AddBalance(balance);
                _account.SumFund.AddBalance(balance);
            }
        }

        private void AddBillOnly(Guid currencyId, decimal balance, DateTime? updateTime = null)
        {
            if (_account.IsMultiCurrency)
            {
                var fund = _account.GetOrCreateFund(currencyId);
                decimal exchangedBalance = fund.CurrencyRate.Exchange(balance);
                this.AddBill(currencyId, exchangedBalance, updateTime);
            }
            else
            {
                this.AddBill(this.AccountCurrencyId, balance, updateTime);
            }
        }

        private void AddBill(Guid currencyId, decimal balance, DateTime? updateTime)
        {
            _account.AddBill(new Bill(_account.Id, currencyId, balance, Protocal.BillType.DeltaBalance, BillOwnerType.Account, updateTime ?? DateTime.Now), OperationType.AsNewRecord);
        }


    }


    internal sealed class AccountMoneyManager
    {
        private AccountBalance _accountBalance;
        private AccountDeposit _accountDeposit;
        private Account _account;

        internal AccountMoneyManager(Account account, BusinessRecordList<Reset.ResetBalance> resetBalances)
        {
            _account = account;
            _accountDeposit = new AccountDeposit(account);
            _accountBalance = new AccountBalance(account, resetBalances);
        }

        internal void AddDeposit(Guid currencyId, DateTime effectiveDateTime, decimal balance, bool isDeposit)
        {
            if (balance == 0) return;
            _accountBalance.AddNormalBalanceOnly(currencyId, balance);
            if (isDeposit)
            {
                _accountDeposit.AddDeposit(currencyId, balance);
            }
            this.RecoverHistoryBalance(currencyId, effectiveDateTime, balance);
        }

        private void RecoverHistoryBalance(Guid currencyId, DateTime effectiveDateTime, decimal balance)
        {
            DateTime currentTradeDay = Settings.Setting.Default.GetTradeDay().Day;
            if (effectiveDateTime.Date < currentTradeDay)
            {
                var historyBalancePerTradeDay = DBRepository.Default.GetAccountBalanceDayHistory(_account.Id, currencyId, effectiveDateTime.Date, currentTradeDay);
                for (DateTime tradeDay = effectiveDateTime.Date; tradeDay < currentTradeDay; tradeDay = tradeDay.AddDays(1))
                {
                    decimal historyBalance = 0m;
                    historyBalancePerTradeDay.TryGetValue(tradeDay, out historyBalance);
                    this.AddHistoryBalance(currencyId, tradeDay, historyBalance + balance);
                }
            }
        }


        internal void AddBalance(Guid currencyId, decimal balance, DateTime? updateTime)
        {
            _accountBalance.AddBalance(currencyId, balance, updateTime);
        }

        internal void AddHistoryBalance(Guid currencyId, DateTime tradeDay, decimal value)
        {
            _accountBalance.AddHistoryBalance(currencyId, tradeDay, value);
        }

        private sealed class AccountDeposit
        {
            private Account _account;


            internal AccountDeposit(Account account)
            {
                _account = account;

            }

            internal void AddDeposit(Guid currencyId, decimal amount)
            {
                if (_account.IsMultiCurrency)
                {
                    var fund = _account.GetOrCreateFund(currencyId);
                    fund.TotalDeposit += amount;
                    _account.SumFund.TotalDeposit += fund.CurrencyRate.Exchange(amount);
                    _account.AddBill(Bill.CreateForAccount(_account.Id, currencyId, amount, Protocal.BillType.Deposit), OperationType.AsNewRecord);
                }
                else
                {
                    Guid accountCurrencyId = _account.Setting().CurrencyId;
                    var fund = _account.GetOrCreateFund(accountCurrencyId);
                    fund.TotalDeposit += amount;
                    _account.SumFund.TotalDeposit += amount;
                    _account.AddBill(Bill.CreateForAccount(_account.Id, accountCurrencyId, amount, Protocal.BillType.Deposit), OperationType.AsNewRecord);
                }
            }
        }
    }

}
