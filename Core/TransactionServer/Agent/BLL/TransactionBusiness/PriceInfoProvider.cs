using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.BLL.OrderBusiness;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal static class PriceInfoProvider
    {
        private static ILog Logger = LogManager.GetLogger(typeof(PriceInfoProvider));

        internal static List<OrderPriceInfo> CreateOrderPriceInfo(this Transaction tran)
        {
            List<OrderPriceInfo> result = new List<OrderPriceInfo>();
            foreach (var eachOrder in tran.Orders)
            {
                result.Add(eachOrder.CreateOrderPriceInfo());
            }
            return result;
        }
    }
}
