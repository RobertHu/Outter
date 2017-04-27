using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Commands;
using Core.TransactionServer.Agent.Physical;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Factory
{
    internal abstract class AddOrderRelationFactoryBase
    {
        internal abstract AddOrderRelationCommandBase Create(Order closeOrder, IDBRow dr);
        internal abstract AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelationData);

        internal abstract AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot);

        internal abstract AddOrderRelationCommandBase CreateBookOrderRelation(Order closeOrder, Protocal.OrderRelationBookData orderRelationData);

    }

    internal sealed class AddOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddOrderRelationFactory Default = new AddOrderRelationFactory();
        private AddOrderRelationFactory() { }

        internal override AddOrderRelationCommandBase Create(Order closeOrder, IDBRow  dr)
        {
            return new AddDataRowOrderRelationCommand(closeOrder, dr);
        }

        internal override AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot)
        {
            return new AddGeneralOrderRelationCommand(closeOrder, openOrder, closedLot);
        }

        internal override AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelationData)
        {
            return new AddCommunicationOrderRelationCommand(closeOrder, orderRelationData);
        }

        internal override AddOrderRelationCommandBase CreateBookOrderRelation(Order closeOrder, Protocal.OrderRelationBookData orderRelationData)
        {
            return new AddBookOrderRelationCommand(closeOrder, orderRelationData);
        }
    }

    internal sealed class AddPhysicalOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddPhysicalOrderRelationFactory Default = new AddPhysicalOrderRelationFactory();

        static AddPhysicalOrderRelationFactory() { }
        private AddPhysicalOrderRelationFactory() { }


        internal override AddOrderRelationCommandBase Create(Order closeOrder, IDBRow  dr)
        {
            return new AddDataRowPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, dr);
        }

        internal override AddOrderRelationCommandBase Create(Order openOrder, Order closeOrder, decimal closedLot)
        {
            return new AddGeneralPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, (PhysicalOrder)openOrder, closedLot);
        }

        internal override AddOrderRelationCommandBase Create(Order closeOrder, Protocal.OrderRelationData orderRelationData)
        {
            return new AddCommunicationPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, orderRelationData);
        }

        internal override AddOrderRelationCommandBase CreateBookOrderRelation(Order closeOrder, Protocal.OrderRelationBookData orderRelationData)
        {
            return new AddBookPhysicalOrderRelationCommand((PhysicalOrder)closeOrder, orderRelationData);
        }
    }

    internal sealed class AddBOOrderRelationFactory : AddOrderRelationFactoryBase
    {
        internal static readonly AddBOOrderRelationFactory Default = new AddBOOrderRelationFactory();
        private AddBOOrderRelationFactory() { }

        internal override AddOrderRelationCommandBase Create(Agent.Order closeOrder, IDBRow dr)
        {
            return new AddDataRowBOOrderRelationCommand((BinaryOption.Order)closeOrder, dr);
        }

        internal override AddOrderRelationCommandBase Create(Agent.Order openOrder, Agent.Order closeOrder, decimal closedLot)
        {
            Debug.Assert(openOrder.IsOpen && !closeOrder.IsOpen);
            return new AddGeneralBOOrderRelationCommand((BinaryOption.Order)closeOrder, (BinaryOption.Order)openOrder, closedLot);
        }

        internal override AddOrderRelationCommandBase Create(Agent.Order closeOrder, Protocal.OrderRelationData orderRelationData)
        {
            return new AddCommunicationOrderRelationCommand(closeOrder, orderRelationData);
        }

        internal override AddOrderRelationCommandBase CreateBookOrderRelation(Order closeOrder, Protocal.OrderRelationBookData orderRelationData)
        {
            throw new NotImplementedException();
        }
    }

}
