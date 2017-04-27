using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.Agent.Reset
{
    internal enum TradingBillType
    {
        None,
        PhysicalPledge,
        PhysicalInstalment
    }

    internal class TradingBill : BusinessRecord
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<Guid> _accountId;
        private BusinessItem<Guid> _currencyId;
        private BusinessItem<DateTime> _updateTime;
        private BusinessItem<decimal> _value;
        private BusinessItem<TradingBillType> _type;

        internal TradingBill(Guid id, Guid accountId, Guid currencyId, decimal value, TradingBillType type, DateTime updateTime)
            : base("TradingBill", 6)
        {
            _id = BusinessItemFactory.Create("ID", id, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create("AccountId", accountId, PermissionFeature.ReadOnly, this);
            _currencyId = BusinessItemFactory.Create("CurrencyId", currencyId, PermissionFeature.ReadOnly, this);
            _value = BusinessItemFactory.Create("Value", value, PermissionFeature.Sound, this);
            _updateTime = BusinessItemFactory.Create("updateTime", updateTime, PermissionFeature.Sound, this);
            _type = BusinessItemFactory.Create("Type", type, PermissionFeature.ReadOnly, this);
        }

        internal Guid Id
        {
            get { return _id.Value; }
        }

        internal Guid AccountId
        {
            get { return _accountId.Value; }
        }

        internal Guid CurrencyId
        {
            get { return _currencyId.Value; }
        }

        internal decimal Value
        {
            get { return _value.Value; }
            set { _value.SetValue(value); }
        }

        internal DateTime UpdateTime
        {
            get { return _updateTime.Value; }
            set { _updateTime.SetValue(value); }
        }

    }


    internal sealed class CloseTradingBill : TradingBill
    {
        private BusinessItem<Guid> _closeOrderId;
        private BusinessItem<Guid> _openOrderId;

        internal CloseTradingBill(Guid id, Guid accountId, Guid currencyId, decimal value, TradingBillType type, DateTime updateTime, Guid closeOrderId, Guid openOrderId)
            : base(id, accountId, currencyId, value, type, updateTime)
        {
            _closeOrderId = BusinessItemFactory.Create("CloseOrderId", closeOrderId, PermissionFeature.ReadOnly, this);
            _openOrderId = BusinessItemFactory.Create("OpenOrderId", openOrderId, PermissionFeature.ReadOnly, this);
        }

        internal Guid CloseOrderId
        {
            get { return _closeOrderId.Value; }
        }

        internal Guid OpenOrderId
        {
            get { return _openOrderId.Value; }
        }

    }


}
