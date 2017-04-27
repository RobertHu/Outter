using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class OCOOrderHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OCOOrderHelper));

        internal static bool CancelOCOOrderIfExists(this Order order, Guid? executeOrderId, TransactionType type)
        {
            if (order.ShouldCancelOCOOrder(executeOrderId, type))
            {
                order.Cancel(CancelReason.OneCancelOther);
                Logger.InfoFormat("CancelOCOOrderIfExists order.id = {0}", order.Id);
                return true;
            }
            return false;
        }

        private static bool ShouldCancelOCOOrder(this Order order, Guid? executeOrderId, TransactionType type)
        {
            return type == TransactionType.OneCancelOther && order.Id != (executeOrderId ?? Guid.Empty);
        }

    }
}
