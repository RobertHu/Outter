using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class TradeDayCalculatorExtension
    {
        internal static bool ShouldCalculate(this Order order, List<Guid> affectedOrders)
        {
            return order.Id.ShouldCalculate(affectedOrders);
        }

        internal static bool ShouldCalculate(this Guid orderId, List<Guid> affectedOrders)
        {
            return affectedOrders == null || affectedOrders.Contains(orderId);
        }
    }
}
