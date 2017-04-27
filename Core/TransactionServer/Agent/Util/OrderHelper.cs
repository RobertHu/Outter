using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Core.TransactionServer.Agent.Physical;

namespace Core.TransactionServer.Agent.Util
{
    internal static class OrderHelper
    {
        internal static bool IsPartialPaymentPhysicalOrder(this Order order)
        {
            if (!order.IsPhysical) return false;
            return ((PhysicalOrder)order).IsPartialPaymentPhysicalOrder;
        }


        internal static decimal GetDeliveryLockLot(this Order order)
        {
            if (!order.IsPhysical || !order.IsOpen) return 0;
            return ((PhysicalOrder)order).DeliveryLockLot;
        }

    }
}
