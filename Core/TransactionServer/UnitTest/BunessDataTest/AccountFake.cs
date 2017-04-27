using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Framework;
using iExchange.Common;

namespace Core.TransactionServer.UnitTest.BunessDataTest
{
    internal sealed class FakeAccount : BusinessRecord
    {
        private BusinessRecordDictionary<Guid, FakeTransaction> _trans;
        private BusinessItem<Guid> _id;
        private BusinessItem<string> _code;

        internal FakeAccount(Guid id, string code)
            : base("Account", 10)
        {
            _trans = new BusinessRecordDictionary<Guid, FakeTransaction>("Transactions", this);
            _id = BusinessItemFactory.Create("Id", id, PermissionFeature.Key, this);
            _code = BusinessItemFactory.Create("Code", code, PermissionFeature.ReadOnly, this);
        }


        internal Guid Id
        {
            get
            {
                return _id.Value;
            }
        }

        internal string Code
        {
            get
            {
                return _code.Value;
            }
        }

        internal int TransactionCount
        {
            get
            {
                return _trans.Count;
            }
        }

        internal FakeTransaction GetTran(Guid tranId)
        {
            return _trans[tranId];
        }

        internal void AddTran(FakeTransaction tran, OperationType type)
        {
            _trans.AddItem(tran, type);
        }

        internal void RemoveTran(FakeTransaction tran)
        {
            _trans.RemoveItem(tran);
        }

        internal string SaveChanges()
        {
            StringBuilder sb = new StringBuilder(100);
            this.WriteXml(sb, m => false);
            this.AcceptChanges();
            return sb.ToString();
        }

    }

    internal sealed class FakeTransaction : BusinessRecord, IKeyProvider<Guid>
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<string> _code;
        private BusinessItem<DateTime> _updateTime;
        private BusinessRecordList<FakeOrder> _orders;


        internal FakeTransaction(Guid id, string code, FakeAccount parent, OperationType type = OperationType.None)
            : base("Transaction", 5)
        {
            _id = BusinessItemFactory.Create("Id", id, PermissionFeature.Key, this);
            _code = BusinessItemFactory.Create("Code", code, PermissionFeature.ReadOnly, this);
            _updateTime = BusinessItemFactory.Create("UpdateTime", DateTime.Now, PermissionFeature.Sound, this);
            _orders = new BusinessRecordList<FakeOrder>("Orders", this);
            parent.AddTran(this, type);
        }

        internal void AddOrder(FakeOrder order, OperationType operationType)
        {
            _orders.AddItem(order, operationType);
        }

        internal Guid Id
        {
            get { return _id.Value; }
        }

        internal int OrderCount
        {
            get { return _orders.Count; }
        }

        internal void RemoveOrder(FakeOrder order)
        {
            order.ChangeToDeleted();
            order.Phase = OrderPhase.Deleted;
            _orders.RemoveItem(order);
        }


        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.Id;
        }
    }

    internal sealed class FakeOrder : BusinessRecord
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<string> _code;
        private BusinessItem<decimal> _lot;
        private BusinessItem<decimal> _lotBalance;
        private BusinessItem<OrderPhase> _phase;

        internal FakeOrder(FakeTransaction tran, Guid id, string code, decimal lot, decimal lotBalance, OperationType type = OperationType.None)
            : base("Order", 5)
        {
            tran.AddOrder(this, type);
            _id = BusinessItemFactory.Create("Id", id, PermissionFeature.Key, this);
            _code = BusinessItemFactory.Create("Code", code, PermissionFeature.ReadOnly, this);
            _lot = BusinessItemFactory.Create("Lot", lot, PermissionFeature.ReadOnly, this);
            _lotBalance = BusinessItemFactory.Create("LotBalance", lotBalance, PermissionFeature.Sound, this);
            _phase = BusinessItemFactory.Create("Phase", OrderPhase.Placed, PermissionFeature.Sound, this);
        }

        internal Guid Id
        {
            get
            {
                return _id.Value;
            }
        }

        internal string Code
        {
            get
            {
                return _code.Value;
            }
        }

        internal decimal Lot
        {
            get { return _lot.Value; }
        }

        internal OrderPhase Phase
        {
            get
            {
                return _phase.Value;
            }
            set
            {
                _phase.SetValue(value);
            }
        }


        internal decimal LotBalance
        {
            get
            {
                return _lotBalance.Value;
            }
            set
            {
                _lotBalance.SetValue(value);
            }
        }
    }


}
