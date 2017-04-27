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

namespace Core.TransactionServer.Agent.BinaryOption.Factory
{
    internal sealed class AddBOOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddBOOrderCommandFactory Default = new AddBOOrderCommandFactory();
        private AddBOOrderCommandFactory() { }

        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, AddDataRowOrderParameter parameter)
        {
            return new Command.AddBOOrderCommand((BOTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateByGeneral(Transaction tran, OrderConstructParams constructParams)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, AddAutoCloseOrderParameter parameter)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByClose(Transaction tran, AddCloseOrderParameter parameter)
        {
            return new Command.AddBOOrderCommand((BOTransaction)tran, parameter);
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, AddDoneOrderParameter parameter)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, AddCutOrderParameter parameter)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, AddCommunicationOrderParameter parameter)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByReset(AddResetOrderParamter parameter)
        {
            throw new NotImplementedException();
        }
    }

}
