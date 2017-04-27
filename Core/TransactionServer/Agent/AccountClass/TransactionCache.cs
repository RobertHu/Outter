using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.Framework;
using System.Diagnostics;
using log4net;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class TransactionCache : BusinessRecordDictionary<Guid,Transaction>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionCache));
        private Dictionary<Guid, Order> _ordersCache;

        internal TransactionCache(Account account, int capacity)
            : base(BusinessRecordCollectionNames.Transactions, account, capacity)
        {
            int defaultCapacity = (int)(capacity * 1.5);
            _ordersCache = new Dictionary<Guid, Order>(defaultCapacity);
        }

        internal Transaction Get(Guid tranId)
        {
            if (!this.ContainsKey(tranId)) return null;
            return this[tranId];
        }

        internal Order GetOrder(Guid orderId)
        {
            try
            {
                if (_ordersCache.ContainsKey(orderId))
                {
                    return _ordersCache[orderId];
                }
                var order = this.FindOrder(orderId);
                if (order != null)
                {
                    Debug.Assert(order != null);
                    _ordersCache.Add(order.Id, order);
                }
                return order;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        internal void RemoveOrder(Order order)
        {
            if (_ordersCache.ContainsKey(order.Id))
            {
                Logger.InfoFormat("RemoveOrder id = {0}", order.Id);
                _ordersCache.Remove(order.Id);
            }
        }

        public override void RemoveItem(Transaction child)
        {
            base.RemoveItem(child);
            this.RemoveTransaction(child);
        }

        private void RemoveTransaction(Transaction tran)
        {
            foreach (var eachOrder in tran.Orders)
            {
                if (_ordersCache.ContainsKey(eachOrder.Id))
                {
                    _ordersCache.Remove(eachOrder.Id);
                }
            }
        }


        private Order FindOrder(Guid id)
        {
            foreach (Transaction transaction in this.GetValues())
            {
                foreach (Order order in transaction.Orders)
                {
                    if (order.Id == id )
                    {
                        return order;
                    }
                }
            }
            return null;
        }

    }
}