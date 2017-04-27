using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    public abstract class OpenOrderCalculatorBase
    {
        protected Order _order;
        protected OrderSettings _settings;
        private Lazy<OrderFloating> _floatPLCalculator;
        private Lazy<OrderSplitCalculator> _splitOrderCalculator;
        private Account _account;

        protected OpenOrderCalculatorBase(Order order, OrderSettings settings, OpenOrderServiceFactoryBase openOrderServiceFactory)
        {
            _order = order;
            _account = order.Owner.Owner;
            _settings = settings;
            _floatPLCalculator = new Lazy<OrderFloating>(() => openOrderServiceFactory.CreateFloatPLCalcaulator(order));
            _splitOrderCalculator = new Lazy<OrderSplitCalculator>(() => openOrderServiceFactory.CreateSplitOrderCalculator(order, settings));
        }

        internal OrderFloating FloatPLCalculator
        {
            get { return _floatPLCalculator.Value; }
        }

        internal OrderSplitCalculator SplitOrderCalculator
        {
            get { return _splitOrderCalculator.Value; }
        }

        public virtual bool IsValued()
        {
            return _order.NotValuedDayInterestAndStorage.IsValued;
        }

        public virtual void UpdateLotBalance(decimal lot)
        {
            if (!_order.IsExecuted)
            {
                throw new ApplicationException(string.Format("Invalid Phase: Order = {0} of {1}, deltaLotBalance = {2}", _order.Id, _order.Owner, lot));
            }
            if (_order.LotBalance < lot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance, string.Format("Order = {0} of {1}, deltaLotBalance = {2}", _order.Id, _order.Owner, lot));
            }
            _settings.LotBalance -= lot;
        }

        public abstract decimal CanBeClosedLot { get; }

        internal List<KeyValuePair<Order, decimal>> GetAllCloseOrderAndClosedLot()
        {
            Debug.Assert(_order.IsOpen);
            List<KeyValuePair<Order, decimal>> result = new List<KeyValuePair<Order, decimal>>();
            foreach (var eachTran in _account.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsOpen) continue;
                    foreach (var eachOrderRelation in eachOrder.OrderRelations)
                    {
                        if (eachOrderRelation.OpenOrder == _order)
                        {
                            result.Add(new KeyValuePair<Order, decimal>(eachOrderRelation.CloseOrder, eachOrderRelation.ClosedLot));
                        }
                    }
                }
            }
            return result;
        }

        public List<OrderRelation> GetAllOrderRelations()
        {
            Debug.Assert(_order.IsOpen);
            List<OrderRelation> result = new List<OrderRelation>();
            var account = _order.Owner.Owner;
            foreach (Transaction eachTran in account.Transactions)
            {
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsOpen) continue;
                    foreach (OrderRelation eachOrderRelation in eachOrder.OrderRelations)
                    {
                        if (eachOrderRelation.OpenOrder == _order)
                        {
                            result.Add(eachOrderRelation);
                        }
                    }
                }
            }
            return result;
        }

    }

    internal sealed class OpenOrderCalculator : OpenOrderCalculatorBase
    {
        internal OpenOrderCalculator(Order order, OrderSettings settings, OpenOrderServiceFactoryBase openOrderServiceFactory)
            : base(order, settings, openOrderServiceFactory)
        {
        }


        public override decimal CanBeClosedLot
        {
            get { return _order.LotBalance; }
        }
    }
}
