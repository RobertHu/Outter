using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption.Factory
{
    public sealed class AddBOOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddBOOrderRelationFactory Default = new AddBOOrderRelationFactory();
        private AddBOOrderRelationFactory() { }

        public override AddOrderRelationCommandBase Create(Agent.Order closeOrder, System.Xml.Linq.XElement xmlOrderRelation)
        {
            throw new NotImplementedException();
        }

        public override AddOrderRelationCommandBase Create(Agent.Order closeOrder, System.Data.DataRow dr)
        {
            return new Command.AddBOOrderRelationCommand((Order)closeOrder, dr);
        }

        public override AddOrderRelationCommandBase Create(Agent.Order openOrder, Agent.Order closeOrder, decimal closedLot)
        {
            Debug.Assert(openOrder.IsOpen && !closeOrder.IsOpen);
            return new Command.AddBOOrderRelationCommand((Order)closeOrder, (Order)openOrder, closedLot);
        }

        public override AddOrderRelationCommandBase Create(Agent.Order closeOrder, Protocal.OrderRelationData orderRelaitonData)
        {
            throw new NotImplementedException();
        }
    }
}
