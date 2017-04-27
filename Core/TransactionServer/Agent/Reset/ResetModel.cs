using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.Framework;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderResetItem : BusinessRecord, IKeyProvider<Guid>
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<bool> _isBuy;
        private BusinessRecordList<ResetBill> _bills;

        internal OrderResetItem(Guid id, bool isBuy)
            : base("Order", 5)
        {
            _isBuy = BusinessItemFactory.Create("IsBuy", isBuy, PermissionFeature.ReadOnly, this);
            _id = BusinessItemFactory.Create("ID", id, PermissionFeature.Key, this);
            _bills = new BusinessRecordList<ResetBill>("Bills", this, 10);
        }

        internal void AddBill(Guid orderId, decimal value, ResetBillType type, DateTime tradeDay)
        {
            _bills.AddItem(new ResetBill(orderId, value, type, tradeDay), OperationType.AsNewRecord);
        }

        internal Guid Id
        {
            get { return _id.Value; }
        }

        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.Id;
        }
    }


    public sealed class InstrumentResetItem : BusinessRecord, IKeyProvider<DateTime>
    {
        private BusinessItem<DateTime> _tradeDay;
        private BusinessItem<decimal> _resetBalance;
        private BusinessItem<decimal> _floatingPL;
        private BusinessItem<decimal> _interestPLNotValued;
        private BusinessItem<decimal> _storagePLNotValued;
        private BusinessItem<decimal> _tradePLNotValued;
        private BusinessItem<decimal> _necessary;
        private BusinessItem<Guid> _accountId;
        private BusinessItem<Guid> _instrumentId;
        private BusinessItem<Price> _buyPrice;
        private BusinessItem<Price> _sellPrice;


        public InstrumentResetItem(DateTime tradeDay, Guid accountId, Guid instrumentId)
            : base("ResetItem", 6)
        {
            this.Initialize(tradeDay, accountId, instrumentId, 0, 0, 0, 0, 0, 0);
        }

        public InstrumentResetItem(InstrumentResetResult result)
            : base("ResetItem", 6)
        {
            this.Initialize(result.TradeDay, result.AccountID, result.InstrumentID, result.ResetBalance, result.FloatingPL, result.InterestPLNotValued, result.StoragePLNotValued, result.TradePLNotValued, result.Necessary);
        }

        private void Initialize(DateTime tradeDay, Guid accountId, Guid instrumentId, decimal balance, decimal floatingPL, decimal interestPLNotValued, decimal storagePLNotValued, decimal tradePLNotValued,
            decimal necessary)
        {
            _tradeDay = BusinessItemFactory.Create("TradeDay", tradeDay, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create("AccountID", accountId, PermissionFeature.Key, this);
            _instrumentId = BusinessItemFactory.Create("InstrumentID", instrumentId, PermissionFeature.Key, this);
            _resetBalance = BusinessItemFactory.Create("ResetBalance", balance, PermissionFeature.Sound, this);
            _floatingPL = BusinessItemFactory.Create("FloatingPL", floatingPL, PermissionFeature.Sound, this);
            _interestPLNotValued = BusinessItemFactory.Create("InterestPLNotValued", interestPLNotValued, PermissionFeature.Sound, this);
            _storagePLNotValued = BusinessItemFactory.Create("StoragePLNotValued", storagePLNotValued, PermissionFeature.Sound, this);
            _tradePLNotValued = BusinessItemFactory.Create("TradePLNotValued", tradePLNotValued, PermissionFeature.Sound, this);
            _necessary = BusinessItemFactory.Create("Necessary", necessary, PermissionFeature.Sound, this);
            _buyPrice = BusinessItemFactory.Create<Price>("BuyPrice", null, PermissionFeature.Sound, this);
            _sellPrice = BusinessItemFactory.Create<Price>("SellPrice", null, PermissionFeature.Sound, this);

        }

        public void Clear()
        {
            _resetBalance.SetValue(0m);
            _floatingPL.SetValue(0m);
            _interestPLNotValued.SetValue(0m);
            _storagePLNotValued.SetValue(0m);
            _tradePLNotValued.SetValue(0m);
            _necessary.SetValue(0m);
            _buyPrice.SetValue(null);
            _sellPrice.SetValue(null);
        }

        public DateTime TradeDay
        {
            get { return _tradeDay.Value; }
        }

        public decimal ResetBalance
        {
            get { return _resetBalance.Value; }
            set
            {
                _resetBalance.SetValue(value);
            }
        }

        public decimal FloatingPL
        {
            get { return _floatingPL.Value; }
            set
            {
                _floatingPL.SetValue(value);
            }
        }

        public decimal InterestPLNotValued
        {
            get { return _interestPLNotValued.Value; }
            set
            {
                _interestPLNotValued.SetValue(value);
            }
        }

        public decimal StoragePLNotValued
        {
            get { return _storagePLNotValued.Value; }
            set
            {
                _storagePLNotValued.SetValue(value);
            }
        }

        public decimal TradePLNotValued
        {
            get { return _tradePLNotValued.Value; }
            set
            {
                _tradePLNotValued.SetValue(value);
            }
        }

        public decimal Necessary
        {
            get { return _necessary.Value; }
            set
            {
                _necessary.SetValue(value);
            }
        }

        public Price BuyPrice
        {
            get { return _buyPrice.Value; }
            set { _buyPrice.SetValue(value); }
        }

        public Price SellPrice
        {
            get { return _sellPrice.Value; }
            set { _sellPrice.SetValue(value); }
        }

        public override string ToString()
        {
            return string.Format("accountId = {0}, instrumentId = {1}, buyPrice = {2}, sellPrice = {3}, tradeDay = {4}, floatingPL = {5}, resetBalance = {6}",
                _accountId.Value, _instrumentId.Value, _buyPrice.Value, _sellPrice.Value, _tradeDay.Value, _floatingPL.Value, _resetBalance.Value);
        }


        DateTime IKeyProvider<DateTime>.GetKey()
        {
            return this.TradeDay;
        }
    }

}
