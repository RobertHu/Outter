using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class ResetBalance : BusinessRecord
    {
        private BusinessItem<DateTime> _tradeDay;
        private BusinessItem<decimal> _value;
        private BusinessItem<Guid> _currencyId;
        private BusinessItem<Guid> _accountId;

        internal ResetBalance(DateTime tradeDay,Guid accountId, Guid currencyId, decimal value)
            : base("Balance", 3)
        {
            _tradeDay = BusinessItemFactory.Create("TradeDay", tradeDay, PermissionFeature.Key, this);
            _value = BusinessItemFactory.Create("Value", value, PermissionFeature.Sound, this);
            _currencyId = BusinessItemFactory.Create("CurrencyID", currencyId, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create("AccountID", accountId, PermissionFeature.Key, this);
        }

        internal DateTime TradeDay
        {
            get { return _tradeDay.Value; }
        }

        internal Guid CurrencyId
        {
            get { return _currencyId.Value; }
        }

        internal decimal Value
        {
            get { return _value.Value; }
            set
            {
                _value.SetValue(value);
            }
        }

    }
}
