using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    public class CloseOrderCalculator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CloseOrderCalculator));

        protected Order _order;
        protected OrderSettings _settings;

        internal CloseOrderCalculator(Order order, OrderSettings settings)
        {
            _order = order;
            _settings = settings;
        }

        public virtual bool IsValued()
        {
            return _order.OrderRelations.All(m => m.IsValued);
        }

        public void UpdateOpenOrderWhenExecuted(ExecuteContext context)
        {
            this.VerifyCloseLot();
            int index = 0;
            foreach (var orderRelation in _order.OrderRelations)
            {
                ++index;
                bool isLast = index == _order.OrderRelationsCount;
                this.UpdateOpenOrder(orderRelation, isLast, context);
            }
        }

        protected virtual void UpdateOpenOrder(OrderRelation orderRelation, bool isLast, ExecuteContext context)
        {
            var openOrder = orderRelation.OpenOrder;
            orderRelation.OpenOrder.UpdateLotBalance(orderRelation.ClosedLot, false);
            openOrder.RecalculateEstimateFee(context);
            orderRelation.EstimateCloseCommissionOfOpenOrder = openOrder.EstimateCloseCommission;
            orderRelation.EstimateCloseLevyOfOpenOrder = openOrder.EstimateCloseLevy;
        }



        private void VerifyCloseLot()
        {
            var totalClosedLot = 0m;
            foreach (var orderRelation in _order.OrderRelations)
            {
                if (orderRelation.OpenOrder == null)
                {
                    throw new TransactionServerException(TransactionError.OpenOrderNotExists, string.Format("CloseOrder = {0} of {1}, OpenOrderID={2}", _order.Id, _order, orderRelation.OpenOrderId));
                }
                totalClosedLot += orderRelation.ClosedLot;
            }
            if (totalClosedLot != _order.Lot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance, string.Format("CloseOrder = {0} of {1}, totalClosedLot={2}", _order.Id, _order, totalClosedLot));
            }
        }
    }
}
