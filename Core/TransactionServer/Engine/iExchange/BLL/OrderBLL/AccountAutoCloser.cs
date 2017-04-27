using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange.BLL.OrderBLL
{
    internal static class OrderAutoCloser
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderAutoCloser));

        internal static Transaction Close(Order order, Price price, OrderType orderType)
        {
            try
            {
                return InnerClose(order, price, orderType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private static Transaction InnerClose(Order order, Price price, OrderType orderType)
        {
            DateTime baseTime = MarketManager.Now;
            TradeOption tradeOption = orderType == OrderType.Limit ? TradeOption.Better : TradeOption.Stop;
            var account = order.Owner.Owner;
            var tran = order.Owner;
            var closeTran = OrderAutoCloser.CreateTran(tran, orderType, baseTime);
            Order closeOrder = OrderAutoCloser.CreateOrder(order, price, tradeOption);
            OrderAutoCloser.CreateOrderRelation(closeOrder, order, order.LotBalance);
            return closeTran;
        }

        private static void CreateOrderRelation(Order closeOrder, Order openOrder, decimal closedLot)
        {
            var factory = OrderRelationFacade.Default.GetAddOrderRelationFactory(closeOrder);
            var command = factory.Create(openOrder,closeOrder,closedLot);
            command.Execute();
        }

        private static Order CreateOrder(Order order, Price price, TradeOption tradeOption)
        {
            OrderConstructParams orderConstructParams = new OrderConstructParams
            {
                Id = Guid.NewGuid(),
                Phase = OrderPhase.Executed,
                IsOpen = false,
                IsBuy = !order.IsBuy,
                SetPrice = price,
                ExecutePrice = price,
                Lot = order.LotBalance,
                OriginalLot = order.LotBalance,
                TradeOption = tradeOption
            };
            return OrderFacade.Default.CreateOrder(order.Owner, orderConstructParams);
        }

        private static Transaction CreateTran(Transaction tran, OrderType orderType, DateTime baseTime)
        {
            //AddGeneralFormatTransactionCommand command = new AddGeneralFormatTransactionCommand(tran.Owner,tranConstructParams);
            //command.Execute();
            //return command.Result;
            return null;
        }

    }
}
