using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption.Calculator
{
    internal sealed class BOCloseOrderCalculator : CloseOrderCalculator
    {
        private Order _boOrder;
        internal BOCloseOrderCalculator(Order order, BOOrderSettings settings)
            : base(order, settings)
        {
            _boOrder = order;
        }

        public override bool IsValued()
        {
            return false;
        }

        protected override void UpdateOpenOrder(OrderRelation orderRelation, bool isLast,ExecuteContext context)
        {
            var boOrderRelation = (BOOrderRelation)orderRelation;
            base.UpdateOpenOrder(orderRelation, isLast,context);
            boOrderRelation.UpdateOpenOrderPledge();
            boOrderRelation.CalculatePayBackPledge();
            _boOrder.PayBackPledge += boOrderRelation.PayBackPledge;
        }

    }
}
