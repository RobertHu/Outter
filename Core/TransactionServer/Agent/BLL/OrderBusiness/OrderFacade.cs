using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using Core.TransactionServer.Agent.Periphery.Facades;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Services;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal sealed class OrderFacade
    {
        internal static readonly OrderFacade Default = new OrderFacade();

        static OrderFacade() { }
        private OrderFacade()
        {
        }

        internal Order CreateMultipleCloseOrder(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
        {
            var addOrderCommandFactory = this.GetAddOrderCommandFactory(tran);
            var addOrderCommand = addOrderCommandFactory.CreateMultipleCloseOrder(tran, closedLot, executePrice, isBuy, orderRelations);
            addOrderCommand.Execute();
            return addOrderCommand.Result;
        }

        internal Order CreateCancelDeliveryWithShortSellOrder(Transaction tran, CancelDeliveryWithShortSellOrderParam param)
        {
            var addCommand = AddPhysicalOrderCommandFactory.Default.CreateCancelDeliveryWithShortSellOrder(tran, param);
            addCommand.Execute();
            return addCommand.Result;
        }


        internal AddOrderCommandFactoryBase GetAddOrderCommandFactory(Transaction tran)
        {
            if (tran.OrderType == OrderType.BinaryOption)
            {
                return AddBOOrderCommandFactory.Default;
            }
            else if (tran.IsPhysical)
            {
                return AddPhysicalOrderCommandFactory.Default;
            }
            else
            {
                return AddGeneralOrderCommandFactory.Default;
            }
        }

        internal OrderPlaceVerifierBase GetPlaceVerifier(Transaction tran)
        {
            if (tran.OrderType == OrderType.BinaryOption)
            {
                return BOOrderPlaceVerifier.Default;
            }
            else if (tran.IsPhysical)
            {
                return PhysicalOrderPlaceVerifier.Default;
            }
            else
            {
                return OrderPlaceVerifier.Default;
            }
        }

        internal OrderVerifierBase GetExecuteVerifier(Transaction tran)
        {
            if (tran.OrderType == OrderType.BinaryOption)
            {
                return BOOrderExecuteVerifier.Default;
            }
            else if (tran.IsPhysical)
            {
                return PhysicalOrderExecuteVerifier.Default;
            }
            else
            {
                return OrderExecuteVerifier.Default;
            }
        }



        internal Order CreateOrder(Transaction tran, OrderConstructParams constructParams)
        {
            if (tran.IsPhysical)
            {
                return PhysicalOrderServiceFactory.Default.CreateOrder(tran, constructParams);
            }
            else
            {
                return GeneralOrderServiceFactory.Default.CreateOrder(tran, constructParams);
            }
        }

    }

}
