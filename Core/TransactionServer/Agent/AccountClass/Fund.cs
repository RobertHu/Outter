using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Data;
using Core.TransactionServer.Agent.Framework;
using log4net;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class Fund
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Fund));
        private FundContent _content;
        private BusinessItem<decimal> _equityItem;
        private Account _owner;

        internal Fund(Account account)
        {
            _owner = account;
            _content = new FundContent(account, null, null, account);
            _equityItem = BusinessItemFactory.CreateVolatileItem("Equity", this.CalculateEquity, account);
        }

        private decimal CalculateEquity()
        {
            decimal result = _content.CalculateEquityCommon();
            if (Settings.Setting.Default.SystemParameter.IncludeFeeOnRiskAction)
            {
                result -= _owner.EstimateCloseCommission + _owner.EstimateCloseLevy;
            }
            return result;
        }


        public RiskData RiskRawData
        {
            get { return this._content.RiskRawData; }
        }

        internal decimal Balance
        {
            get { return this._content.Balance ?? 0; }
        }

        internal decimal FrozenFund
        {
            get { return _content.FrozenFund ?? 0; }
        }


        internal decimal PartialPaymentPhysicalNecessary
        {
            get { return _content.RiskRawData.PartialPaymentPhysicalNecessary; }
        }

        internal decimal TradePLFloat
        {
            get
            {
                return _content.TradePLFloat;
            }
        }

        internal decimal Equity
        {
            get
            {
                return _equityItem.Value;
            }
        }


        internal decimal Necessary
        {
            get { return _content.RiskRawData.Necessary; }
        }

        internal decimal Credit
        {
            get { return _content.RiskRawData.RiskCredit; }
            set
            {
                _content.RiskRawData.RiskCredit = value;
            }
        }

        internal decimal TotalDeposit
        {
            get
            {
                return _content.TotalDeposit;
            }
            set
            {
                _content.TotalDeposit = value;
            }
        }

        internal decimal FeeForCutting
        {
            get { return _content.RiskRawData.FeeForCutting; }
        }


        internal void AddBalance(decimal balance)
        {
            Logger.InfoFormat("AddBalance deltaBalance = {0}, accountId = {1}, accountCurrencyId = {2}, LastBalance = {3}", balance, _owner.Id, _owner.Setting().CurrencyId, this.Balance);
            _content.AddBalance(balance);
        }


        internal void Add(SubFund subFund)
        {
            _content.AddBalance(subFund.CurrencyRate.AddByExchange(subFund.Balance));
            _content.AddFrozenFund(subFund.CurrencyRate.AddByExchange(subFund.FrozenFund));
            _content.TotalDeposit += subFund.CurrencyRate.AddByExchange(subFund.TotalDeposit);
        }

        internal void Reset(FundData fundData)
        {
            if (_content.Balance == null)
            {
                _content.Balance = fundData.Balance;
                _content.FrozenFund = fundData.FrozenFund;
            }
            _content.TotalDeposit = fundData.TotalDeposit;
            _content.RiskRawData.Reset(fundData);
        }

        internal void ResetToZero()
        {
            _content.RiskRawData.Clear();
        }


        internal void AddFeeForCutting(SubFund subFund)
        {
            _content.RiskRawData.FeeForCutting += subFund.CurrencyRate.Exchange(subFund.FeeForCutting);
        }

        internal void ClearFeeForCutting()
        {
            _content.RiskRawData.FeeForCutting = 0m;
        }

        internal void Clear()
        {
            _content.Balance = 0m;
            _content.FrozenFund = 0m;
            _content.TotalDeposit = 0;
        }

        internal void ChangeSomeFieldsToModifiedWhenExecuted(Transaction tran)
        {
            _content.ChangeSomeFieldsToModifiedWhenExecuted(tran);
        }
    }


}