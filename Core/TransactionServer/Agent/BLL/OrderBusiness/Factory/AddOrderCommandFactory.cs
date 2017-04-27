using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Factory
{
    internal abstract class AddOrderCommandFactoryBase
    {
        internal abstract AddOrderCommandBase CreateByCommunication(Transaction tran, AddCommunicationOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateByDataRow(Transaction tran, AddDataRowOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateByAutoClose(Transaction tran, AddAutoCloseOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateByGeneral(Transaction tran, OrderConstructParams constructParams);
        internal abstract AddOrderCommandBase CreateByClose(Transaction tran, AddCloseOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateDoneOrder(Transaction tran, AddDoneOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateCutOrder(Transaction tran, AddCutOrderParameter parameter);
        internal abstract AddOrderCommandBase CreateByReset(AddResetOrderParamter parameter);

    }


    internal sealed class AddGeneralOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddGeneralOrderCommandFactory Default = new AddGeneralOrderCommandFactory();
        private AddGeneralOrderCommandFactory() { }


        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, AddDataRowOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateByGeneral(Transaction tran, OrderConstructParams constructParams)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, AddAutoCloseOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateByClose(Transaction tran, AddCloseOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, AddDoneOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, AddCutOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, AddCommunicationOrderParameter parameter)
        {
            return new AddGeneralOrderCommand(tran, parameter);
        }

        internal override AddOrderCommandBase CreateByReset(AddResetOrderParamter paramter)
        {
            return new AddGeneralOrderCommand(null, paramter);
        }
    }

}
