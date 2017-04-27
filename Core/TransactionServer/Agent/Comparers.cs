using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent
{
    internal class AutoCloseOrderComparer : IComparer<Order>
    {
        private static readonly Lazy<AutoCloseOrderComparer> defaultInstance
            = new Lazy<AutoCloseOrderComparer>(() => { return new AutoCloseOrderComparer(); });

        private AutoCloseOrderComparer()
        {
        }

        internal static AutoCloseOrderComparer Default
        {
            get { return AutoCloseOrderComparer.defaultInstance.Value; }
        }

        int IComparer<Order>.Compare(Order x, Order y)
        {
            var xExecuteTime = x.Owner.ExecuteTime ?? DateTime.MinValue;
            var yExecuteTime = y.Owner.ExecuteTime ?? DateTime.MinValue;
            if (x.Owner.Owner.AutoCloseFirstInFirstOut)
            {
                return xExecuteTime.CompareTo(yExecuteTime);
            }
            return yExecuteTime.CompareTo(xExecuteTime);
        }
    }
}