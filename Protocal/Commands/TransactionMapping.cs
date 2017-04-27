using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal.Commands
{
    public sealed class TransactionMapping
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionMapping));
        private const int CAPACITY = 1000;

        public static readonly TransactionMapping Default = new TransactionMapping();

        private Dictionary<Guid, Guid> _tran2account = new Dictionary<Guid, Guid>(CAPACITY);
        private Dictionary<Guid, Guid> _order2AccountMapping = new Dictionary<Guid, Guid>(CAPACITY);
        private Dictionary<Guid, List<Guid>> _account2deal = new Dictionary<Guid, List<Guid>>(CAPACITY);

        private object _mutex = new object();

        static TransactionMapping() { }
        private TransactionMapping() { }

        public bool GetAccountId(Guid tranId, out Guid accountId)
        {
            lock (_mutex)
            {
                return this._tran2account.TryGetValue(tranId, out accountId);
            }
        }

        public bool GetAccountIdByOrder(Guid orderId, out Guid accountId)
        {
            lock (_mutex)
            {
                return _order2AccountMapping.TryGetValue(orderId, out accountId);
            }
        }

        //clear all orders and trans of the account
        internal void Clear(Guid accountId)
        {
            lock (_mutex)
            {
                List<Guid> deals = null;
                if (_account2deal.TryGetValue(accountId, out deals))
                {
                    foreach (Guid dealId in deals)
                    {
                        if (!this._tran2account.Remove(dealId))
                        {
                            this._order2AccountMapping.Remove(dealId);
                        }
                    }
                }
                _account2deal.Remove(accountId);
            }
        }

        public void Initialize(Account account)
        {
            lock (_mutex)
            {
                foreach (var eachTran in account.Transactions)
                {
                    this.Add(eachTran, account.Id);
                }
            }
        }

        private void AddDeal(Guid accountId, Guid dealId)
        {
            List<Guid> deals = null;
            if (!_account2deal.TryGetValue(accountId, out deals))
            {
                deals = new List<Guid>();
                _account2deal[accountId] = deals;
            }
            deals.Add(dealId);
        }

        private void Add(Protocal.Commands.Transaction tran, Guid accountId)
        {
            if (!_tran2account.ContainsKey(tran.Id))
            {
                Logger.InfoFormat("add tranId = {0}, accountId ={1}", tran.Id, accountId);
                _tran2account.Add(tran.Id, accountId);
                AddDeal(accountId, tran.Id);
                foreach (var eachOrder in tran.Orders)
                {
                    _order2AccountMapping.Add(eachOrder.Id, accountId);
                    AddDeal(accountId, eachOrder.Id);
                }
            }
        }

        public void Update(Account account, IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            lock (_mutex)
            {
                foreach (var eachChange in orderChanges)
                {
                    if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Placing || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Placed
                        || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Executed || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Cut)
                    {
                        this.Add(eachChange.Tran, account.Id);
                    }
                    else if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Canceled || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Deleted)
                    {
                        this.RemoveMapping(eachChange.Tran);
                    }
                }
            }
        }

        private void RemoveMapping(Transaction tran)
        {
            if (_tran2account.ContainsKey(tran.Id))
            {
                _tran2account.Remove(tran.Id);
            }
            foreach (var eachOrder in tran.Orders)
            {
                if (_order2AccountMapping.ContainsKey(eachOrder.Id))
                {
                    _order2AccountMapping.Remove(eachOrder.Id);
                }
            }
        }
    }
}
