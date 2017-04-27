using Core.TransactionServer.Agent.AccountClass;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.InstrumentBusiness
{
    internal abstract class OrderCollector
    {
        private const int DEFAULT_ORDER_CAPACITY = 20;
        protected Instrument _owner;
        protected OrderCollector(Instrument owner)
        {
            _owner = owner;
        }

        public List<Order> CollectCalculateInitOrders()
        {
            return this.CollectOrders(m => m.Phase == OrderPhase.Executed);
        }


        public List<Order> CollectWaitingForHitOrders()
        {
            var result = new List<Order>(DEFAULT_ORDER_CAPACITY);
            foreach (var eachTran in _owner.Owner.Transactions)
            {
                if (eachTran.InstrumentId != _owner.Id) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    bool isOk = eachOrder.Phase == OrderPhase.Placed && eachOrder.HitStatus.IsPending()
                        && ((eachOrder.Owner.OrderType == OrderType.SpotTrade && (eachOrder.ShouldSportOrderDelayFill || eachOrder.DQMaxMove > 0))
                        || eachOrder.Owner.OrderType == OrderType.Limit || eachOrder.Owner.OrderType == OrderType.Market);
                    if (isOk)
                    {
                        result.Add(eachOrder);
                    }
                }
            }
            return result;
        }

        public List<Order> CollectExecutedAndHasPositionOrders()
        {
            var result = new List<Order>(DEFAULT_ORDER_CAPACITY);
            foreach (var eachTran in _owner.Owner.Transactions)
            {
                if (eachTran.InstrumentId != _owner.Id) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsOpen && eachOrder.IsExecuted && eachOrder.LotBalance > 0)
                    {
                        result.Add(eachOrder);
                    }
                }
            }
            return result;
        }

        public List<Order> CollectNotValuedOrders()
        {
            return this.CollectOrders(m => !m.IsValued);
        }


        private List<Order> CollectOrders(Predicate<Order> predicate)
        {
            var result = new List<Order>(DEFAULT_ORDER_CAPACITY);
            foreach (var eachTran in _owner.Owner.Transactions)
            {
                if (eachTran.InstrumentId != _owner.Id) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (predicate(eachOrder))
                    {
                        result.Add(eachOrder);
                    }
                }
            }
            return result;
        }
    }

    internal sealed class GeneralOrderCollector : OrderCollector
    {
        internal GeneralOrderCollector(Instrument owner)
            : base(owner) { }
    }

}
