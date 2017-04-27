using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public abstract class AddOrderRelationFactoryBase
    {
        public abstract AddOrderRelationCommandBase Create(Order closeOrder, XElement xmlOrderRelation);
        public abstract AddOrderRelationCommandBase Create(Order closeOrder, DataRow dr);
        public abstract AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelaitonData);

        public abstract AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot);

    }

    public sealed class AddGeneralOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddGeneralOrderRelationFactory Default = new AddGeneralOrderRelationFactory();
        private AddGeneralOrderRelationFactory() { }
        public override AddOrderRelationCommandBase Create(Order closeOrder, XElement xmlOrderRelation)
        {
            return new AddGeneralOrderRelationCommand(closeOrder, xmlOrderRelation);
        }

        public override AddOrderRelationCommandBase Create(Order closeOrder, DataRow dr)
        {
            return new AddGeneralOrderRelationCommand(closeOrder, dr);
        }

        public override AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot)
        {
            return new AddGeneralOrderRelationCommand(closeOrder, openOrder, closedLot);
        }


        public override AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelaitonData)
        {
            return new AddGeneralOrderRelationCommand(closeOrder, orderRelaitonData);
        }
    }


}
