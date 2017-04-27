using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Concurrent;

namespace iExchange.StateServer.Adapter
{
    internal sealed class TransactionMapping
    {
        internal static readonly TransactionMapping Default = new TransactionMapping();

        private ConcurrentDictionary<Guid, Guid> _tran2account = new ConcurrentDictionary<Guid, Guid>();

        private ConcurrentDictionary<Guid, Guid> _order2AccountMapping = new ConcurrentDictionary<Guid, Guid>();

        static TransactionMapping() { }
        private TransactionMapping() { }

        internal bool TryGetAccountId(Guid tranId, out Guid accountId)
        {
            return this._tran2account.TryGetValue(tranId, out accountId);
        }

        internal bool TryGetAccountIdByOrder(Guid orderId, out Guid accountId)
        {
            return _order2AccountMapping.TryGetValue(orderId, out accountId);
        }

        internal void Initialize(Account account)
        {
            foreach (var eachTran in account.Transactions)
            {
                this.TryAddOrUpdate(eachTran, account.Id);
            }
        }

        private void TryAddOrUpdate(Protocal.Commands.Transaction tran, Guid accountId)
        {
            Guid oldAccountId;
            if (!_tran2account.TryGetValue(tran.Id, out oldAccountId))
            {
                _tran2account.TryAdd(tran.Id, accountId);
                foreach (var eachOrder in tran.Orders)
                {
                    _order2AccountMapping.TryAdd(eachOrder.Id, accountId);
                }
            }
            else
            {
                _tran2account.TryUpdate(tran.Id, accountId, oldAccountId);
                foreach (var eachOrder in tran.Orders)
                {
                    _order2AccountMapping.TryUpdate(eachOrder.Id, accountId, oldAccountId);
                }
            }
        }

        internal void Update(Account account, IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            foreach (OrderChange eachChange in orderChanges)
            {
                if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Placing || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Placed
                    || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Executed || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Cut)
                {
                    this.TryAddOrUpdate(eachChange.Tran, account.Id);
                }
                else if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Canceled || eachChange.ChangeType == Protocal.Commands.OrderChangeType.Deleted)
                {
                    this.RemoveMapping(eachChange.Tran);
                }
            }
        }

        private void RemoveMapping(Transaction tran)
        {
            Guid accountId;
            _tran2account.TryRemove(tran.Id, out accountId);
            foreach (var eachOrder in tran.Orders)
            {
                _order2AccountMapping.TryRemove(eachOrder.Id, out accountId);
            }
        }


    }

}