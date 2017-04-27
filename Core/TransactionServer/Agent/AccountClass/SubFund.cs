using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Physical;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal class SubFund : BusinessRecord, IKeyProvider<Guid>
    {
        private Account _owner;
        private BusinessItem<Guid> _currencyId;
        private BusinessItem<Guid> _accountId;
        private FundContent _content;
        private BusinessItem<decimal> _equityItem;

        public SubFund(Account account, Guid currencyId, decimal balance, decimal frozenFund, OperationType asAdded)
            : base(BusinessRecordNames.Fund, 10)
        {
            this._owner = account;
            this._currencyId = BusinessItemFactory.Create(FundBusinessItemNames.CurrencyId, currencyId, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create("AccountID", account.Id, PermissionFeature.Key, this);
            BusinessItemFactory.CreateVolatileItem("CurrencyCode", () => Settings.Setting.Default.GetCurrency(currencyId).Code, this);
            _equityItem = BusinessItemFactory.CreateVolatileItem("Equity", this.CalculateEquity, this);
            this.Initialize(balance, frozenFund, account);
            account.AddSubFund(this, asAdded);
        }


        private void Initialize(decimal balance, decimal frozenFund, Account account)
        {
            this._content = new FundContent(this, balance, frozenFund, account);
        }

        private decimal CalculateEquity()
        {
            decimal result = _content.CalculateEquityCommon();
            if (Settings.Setting.Default.SystemParameter.IncludeFeeOnRiskAction)
            {
                result -= this.CalculateEstimateFee();
            }
            return result;
        }

        private decimal CalculateEstimateFee()
        {
            if (_owner.IsMultiCurrency)
            {
                decimal result = 0m;
                foreach (var eachTran in _owner.Transactions)
                {
                    if (eachTran.CurrencyId != this.CurrencyId) continue;
                    foreach (var eachOrder in eachTran.Orders)
                    {
                        if (eachOrder.IsOpen && eachOrder.Phase == OrderPhase.Executed && eachOrder.LotBalance > 0)
                        {
                            result += eachOrder.EstimateCloseCommission + eachOrder.EstimateCloseLevy;
                        }
                    }
                }
                return result;
            }
            else
            {
                return _owner.EstimateCloseCommission + _owner.EstimateCloseLevy;
            }

        }



        internal Guid CurrencyId
        {
            get { return this._currencyId.Value; }
        }

        public FundContent FundContent
        {
            get { return this._content; }
        }

        internal decimal Balance
        {
            get { return _content.Balance ?? 0m; }
        }

        internal decimal FrozenFund
        {
            get { return _content.FrozenFund ?? 0m; }
        }

        internal decimal Equity
        {
            get
            {
                return _equityItem.Value;
            }
        }

        internal decimal TradePLFloat
        {
            get { return _content.TradePLFloat; }
        }


        internal decimal Necessary
        {
            get
            {
                return _content.RiskRawData.Necessary;
            }
        }

        internal decimal PartialPaymentPhysicalNecessary
        {
            get { return _content.RiskRawData.PartialPaymentPhysicalNecessary; }
        }


        public decimal TotalPaidAmount { get { return _content.RiskRawData.TotalPaidAmount; } }


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


        internal CurrencyRate CurrencyRate
        {
            get
            {
                return Settings.Setting.Default.GetCurrencyRate(this.CurrencyId, _owner.Setting().CurrencyId);
            }
        }

        internal decimal FeeForCutting
        {
            get { return _content.RiskRawData.FeeForCutting; }
        }



        internal static void Create(IDBRow dr)
        {
            Guid currencyId = (Guid)dr["CurrencyID"];
            decimal balance = (decimal)dr["Balance"];
            decimal frozenFund = dr.GetColumn<decimal>("FrozenFund");
            Guid accountId = (Guid)dr["AccountID"];
            Account account = TradingSetting.Default.GetAccount(accountId);
            new SubFund(account, currencyId, balance, frozenFund, OperationType.None);
        }

        internal void AddRiskData(RiskData riskData)
        {
            _content.RiskRawData.Add(riskData);
        }

        internal void ResetRiskData(FundData FundData)
        {
            _content.RiskRawData.Reset(FundData);
        }


        internal void ResetToZero()
        {
            _content.RiskRawData.Clear();
        }

        internal void AddBalance(decimal balance)
        {
            _content.AddBalance(balance);
        }

        internal void AddFrozenFund(decimal frozenFund)
        {
            _content.AddFrozenFund(frozenFund);
        }

        internal void AddValueAsMargin(decimal valueAsMargin)
        {
            _content.RiskRawData.ValueAsMargin += valueAsMargin;
        }

        internal void AddFeeForCutting(decimal feeForCutting)
        {
            _content.RiskRawData.FeeForCutting += feeForCutting;
        }

        internal void ClearFeeForCutting()
        {
            _content.RiskRawData.FeeForCutting = 0m;
        }


        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.CurrencyId;
        }
    }
}
