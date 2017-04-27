using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.AccountClass
{
    public sealed class FundContent
    {
        private RiskData _riskRawData;
        private BusinessItem<decimal> _totalDeposit;
        private BusinessItem<decimal?> _balance;
        private BusinessItem<decimal?> _frozenFund;
        private Account _account;

        internal FundContent(BusinessRecord parent, decimal? balance, decimal? frozenFund, Account account)
        {
            _totalDeposit = BusinessItemFactory.Create("TotalDeposit", 0m, PermissionFeature.Sound, parent);
            _balance = BusinessItemFactory.Create(FundBusinessItemNames.Balance, balance, PermissionFeature.Sound, parent);
            _frozenFund = BusinessItemFactory.Create(FundBusinessItemNames.FrozenFund, frozenFund, PermissionFeature.Sound, parent);
            this._riskRawData = new RiskData(parent);
            _account = account;
        }


        internal decimal TotalDeposit
        {
            get { return _totalDeposit.Value; }
            set
            {
                _totalDeposit.SetValue(value);
            }
        }


        internal RiskData RiskRawData
        {
            get { return this._riskRawData; }
        }

        internal decimal TradePLFloat
        {
            get { return _riskRawData.TradePLFloat; }
        }

        internal decimal? Balance
        {
            get { return _balance.Value; }
            set { _balance.SetValue(value); }
        }

        internal decimal? FrozenFund
        {
            get { return _frozenFund.Value; }
            set { _frozenFund.SetValue(value); }
        }

        internal decimal CalculateEquityCommon()
        {
            return (this.Balance ?? 0) + _riskRawData.InterestPLNotValued + _riskRawData.StoragePLNotValued + _riskRawData.TradePLNotValued
                + _riskRawData.ValueAsMargin + _riskRawData.TotalPaidAmount + _riskRawData.InterestPLFloat + _riskRawData.StoragePLFloat + _riskRawData.TradePLFloat;
        }


        internal void ResetBalance(decimal balance)
        {
            this.Balance = balance;
        }

        internal void AddBalance(decimal balance)
        {
            if (this.Balance == null)
            {
                this.Balance = balance;
            }
            else
            {
                this.Balance += balance;
            }
        }

        internal void AddFrozenFund(decimal frozenFund)
        {
            if (this.FrozenFund == null)
            {
                this.FrozenFund = frozenFund;
            }
            else
            {
                this.FrozenFund += frozenFund;
            }
        }

    }


    internal sealed class FundData
    {
        internal FundData(Account account, Guid currencyId)
        {
            this.Account = account;
            this.CurrencyId = currencyId;
            this.RiskData = new RiskData(null);
        }

        internal decimal Balance { get; private set; }
        internal decimal FrozenFund { get; private set; }
        internal decimal TotalDeposit { get; private set; }

        internal Account Account { get; private set; }

        internal Guid CurrencyId { get; private set; }

        internal void Empty(Account account, Guid currencyId)
        {
            this.Account = account;
            this.CurrencyId = currencyId;
            this.Balance = 0;
            this.FrozenFund = 0;
            this.TotalDeposit = 0;
            this.RiskData.Clear();
        }

        internal CurrencyRate CurrencyRate
        {
            get
            {
                return Settings.Setting.Default.GetCurrencyRate(this.CurrencyId, this.Account.Setting().CurrencyId);
            }
        }

        internal RiskData RiskData { get; private set; }


        //internal decimal? Necessary { get; private set; }

        //internal decimal? NetNecessary { get; private set; }

        //internal decimal? HedgeNecessary { get; private set; }

        //internal decimal? MinEquityAvoidRiskLevel1 { get; private set; }

        //internal decimal? MinEquityAvoidRiskLevel2 { get; private set; }

        //internal decimal? MinEquityAvoidRiskLevel3 { get; private set; }

        //internal decimal? NecessaryFillingOpenOrder { get; private set; }

        //internal decimal? NecessaryFillingCloseOrder { get; private set; }

        //internal decimal? TradePLFloat { get; private set; }

        //internal decimal? InterestPLFloat { get; private set; }

        //internal decimal? StoragePLFloat { get; private set; }

        //internal decimal? ValueAsMargin { get; private set; }

        //internal decimal? TradePLNotValued { get; private set; }

        //internal decimal? InterestPLNotValued { get; private set; }

        //internal decimal? StoragePLNotValued { get; private set; }

        //internal decimal? LockOrderTradePLFloat { get; private set; }

        //internal decimal? FeeForCutting { get; private set; }

        //internal decimal? RiskCredit { get; private set; }

        //internal decimal? PartialPaymentPhysicalNecessary { get; private set; }

        //internal decimal? TotalPaidAmount { get; private set; }

        internal void InitializeBalanceAndFrozenFund(decimal balance, decimal frozenFund, decimal totalDeposit)
        {
            this.Balance = balance;
            this.FrozenFund = frozenFund;
            this.TotalDeposit = totalDeposit;
        }

        internal void Add(FundData other)
        {
            var currencyRate = other.CurrencyRate;

            this.Balance += this.AddByExchange(other.Balance, currencyRate);

            this.FrozenFund += this.AddByExchange(other.FrozenFund, currencyRate);

            this.TotalDeposit += this.AddByExchange(other.TotalDeposit, currencyRate);

            this.Add(other.RiskData, other.CurrencyRate);
        }


        internal void Add(RiskData other, CurrencyRate currencyRate)
        {
            this.RiskData.Add(other, currencyRate);
        }


        private decimal AddByExchange(decimal value, CurrencyRate currencyRate)
        {
            return currencyRate.AddByExchange(value);
        }
    }


    internal sealed class FundDataPool : Protocal.PoolBase<FundData>
    {
        internal static readonly FundDataPool Default = new FundDataPool();
        static FundDataPool() { }
        private FundDataPool() { }

        internal FundData Get(Account account, Guid currencyId)
        {
            return this.Get(() => new FundData(account, currencyId), m => m.Empty(account, currencyId));
        }
    }


    internal static class CurrencyRateExtension
    {
        internal static decimal AddByExchange(this CurrencyRate currencyRate, decimal value)
        {
            return currencyRate == null ? value : currencyRate.Exchange(value);
        }
    }

}
