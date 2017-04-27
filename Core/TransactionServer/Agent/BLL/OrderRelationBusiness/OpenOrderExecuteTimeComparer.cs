using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    internal enum SortDirection
    {
        Ascending,
        Descending
    }

    internal class OpenOrderExecuteTimeComparer : IComparer<OrderRelation>
    {
        private SortDirection sortDirection;

        public OpenOrderExecuteTimeComparer(SortDirection sortDirection)
        {
            this.sortDirection = sortDirection;
        }

        int IComparer<OrderRelation>.Compare(OrderRelation x, OrderRelation y)
        {
            var xOpenTranExecuteTime = x.OpenOrder.Owner.ExecuteTime.Value;
            var yOpenTranExecuteTime = y.OpenOrder.Owner.ExecuteTime.Value;
            return (this.sortDirection == SortDirection.Ascending ?
                    xOpenTranExecuteTime.CompareTo(yOpenTranExecuteTime) :
                    yOpenTranExecuteTime.CompareTo(xOpenTranExecuteTime));
        }
    }

    internal class InternalAutoCloseComparer : IComparer<OrderRelation>
    {
        int IComparer<OrderRelation>.Compare(OrderRelation x, OrderRelation y)
        {
            return Transaction.AutoCloseComparer.Compare(x.OpenOrder.Owner, y.OpenOrder.Owner);
        }
    }

}
