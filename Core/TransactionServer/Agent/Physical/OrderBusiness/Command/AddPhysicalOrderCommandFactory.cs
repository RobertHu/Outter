using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class AddPhysicalOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddPhysicalOrderCommandFactory Default = new AddPhysicalOrderCommandFactory();
        private AddPhysicalOrderCommandFactory() { }


        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, AddDataRowOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateByGeneral(Transaction tran, OrderConstructParams constructParams)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, AddAutoCloseOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateByClose(Transaction tran, AddCloseOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, AddDoneOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, AddCutOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal AddOrderCommandBase CreateInstalmentOrder(PhysicalTransaction tran, AddInstalmentOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, AddCommunicationOrderParameter parameter)
        {
            return new AddPhysicalOrderCommand((PhysicalTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateByReset(AddResetOrderParamter parameter)
        {
            return new AddPhysicalOrderCommand(null, parameter);
        }
    }
}
