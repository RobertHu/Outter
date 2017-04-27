using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderRelationBusiness
{
    public sealed class AddPhysicalOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddPhysicalOrderRelationFactory Default = new AddPhysicalOrderRelationFactory();

        static AddPhysicalOrderRelationFactory() { }
        private AddPhysicalOrderRelationFactory() { }

        public override AddOrderRelationCommandBase Create(Order closeOrder, System.Xml.Linq.XElement xmlOrderRelation)
        {
            return new AddPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, xmlOrderRelation);
        }

        public override AddOrderRelationCommandBase Create(Order closeOrder, System.Data.DataRow dr)
        {
            return new AddPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, dr);
        }

        public override AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot)
        {
            return new AddPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, (PhysicalOrder)openOrder, closedLot);
        }

        public override AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelaitonData)
        {
            return new AddPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, orderRelaitonData);
        }
    }
}
