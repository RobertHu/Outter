using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Factory;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    internal sealed class OrderRelationFacade
    {
        internal static readonly OrderRelationFacade Default = new OrderRelationFacade();

        static OrderRelationFacade() { }
        private OrderRelationFacade()
        {
        }

        internal AddOrderRelationFactoryBase GetAddOrderRelationFactory(Order closeOrder)
        {
            var tran = closeOrder.Owner;
            if (tran.OrderType == iExchange.Common.OrderType.BinaryOption)
            {
                return AddBOOrderRelationFactory.Default;
            }
            else if (tran.IsPhysical)
            {
                return AddPhysicalOrderRelationFactory.Default;
            }
            else
            {
                return AddOrderRelationFactory.Default;
            }
        }

    }
}
