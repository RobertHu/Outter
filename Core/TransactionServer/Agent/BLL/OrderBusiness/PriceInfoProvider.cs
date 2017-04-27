using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class PriceInfoProvider
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PriceInfoProvider));

        internal static OrderPriceInfo CreateOrderPriceInfo(this Order order)
        {
            Price buyPrice, sellPrice;
            if (order.Owner.PlacedWithDQMaxMove)
            {
                order.GetBuyAndSellPrice(out buyPrice, out sellPrice);
            }
            else
            {
                order.GetBuyAndSellSetPrice(out buyPrice, out sellPrice);
            }
            var orderInfo = new OrderPriceInfo(order.Id, buyPrice, sellPrice);
            Logger.InfoFormat("auto fill orderPriceInfo={0}, accountId = {1}, tranId = {2},placedWithDQMaxMove = {3}", orderInfo, order.Owner.AccountId, order.Owner.Id, order.Owner.PlacedWithDQMaxMove);
            return orderInfo;
        }

        private static void GetBuyAndSellSetPrice(this Order order, out Price buyPrice, out Price sellPrice)
        {
            buyPrice = sellPrice = null;
            if (order.IsBuy) buyPrice = order.SetPrice;
            else sellPrice = order.SetPrice;
            if (buyPrice == null && sellPrice == null)
            {
                buyPrice = sellPrice = order.ExecutePrice;
            }
        }

        internal static void GetBuyAndSellPrice(this Order order, out Price buyPrice, out Price sellPrice)
        {
            buyPrice = sellPrice = null;
            if (order.IsBuy) buyPrice = order.GetPrice();
            else sellPrice = order.GetPrice();
            if (buyPrice == null && sellPrice == null)
            {
                buyPrice = sellPrice = order.ExecutePrice;
            }
        }

        private static Price GetPrice(this Order order)
        {
            return order.BestPrice != null ? order.BestPrice : order.SetPrice;
        }

    }
}
