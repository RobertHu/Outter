using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using Core.TransactionServer.Agent.Settings;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent
{
    internal sealed class UnclearDepositManager
    {
        private Account owner;
        private Dictionary<Guid, UnclearDeposit> unclearDeposits;


        internal UnclearDepositManager(Account account)
        {
            this.owner = account;
        }

        internal void Add(UnclearDeposit deposit)
        {
            if (this.unclearDeposits == null)
            {
                this.unclearDeposits = new Dictionary<Guid, UnclearDeposit>();
            }
            this.unclearDeposits.Add(deposit.Id, deposit);
        }

        internal void Remove(Guid depositId)
        {
            if (this.unclearDeposits != null)
            {
                this.unclearDeposits.Remove(depositId);
            }
        }

        internal decimal Sum()
        {
            if (this.unclearDeposits == null) return 0m;
            decimal sum = 0;
            foreach (UnclearDeposit deposit in this.unclearDeposits.Values)
            {
                if (this.owner.Setting().IsMultiCurrency)
                {
                    CurrencyRate currencyRate = Settings.Setting.Default.GetCurrencyRate(deposit.TargetCurrencyId, this.owner.Setting().CurrencyId);
                    sum += currencyRate.Exchange(deposit.TargetAmount);
                }
                else
                {
                    sum += deposit.TargetAmount;
                }
            }
            return sum;
        }
    }

    internal sealed class UnclearDeposit
    {
        internal Guid Id
        {
            get;
            private set;
        }

        internal Guid AccountId
        {
            get;
            private set;
        }

        internal Guid TargetCurrencyId
        {
            get;
            private set;
        }

        internal decimal TargetAmount
        {
            get;
            private set;
        }

        internal UnclearDeposit(IDBRow row)
        {
            this.Id = (Guid)row["ID"];
            this.AccountId = (Guid)row["AccountID"];
            this.TargetCurrencyId = (Guid)row["TargetCurrencyID"];
            this.TargetAmount = (decimal)row["TargetAmount"];
        }

        internal UnclearDeposit(XElement row)
        {
            this.Id = row.AttrToGuid("DepositID");
            this.AccountId = row.AttrToGuid("AccountID");
            this.TargetCurrencyId = row.AttrToGuid("CurrencyID");
            this.TargetAmount = row.AttrToDecimal("Balance");
        }
    }
}