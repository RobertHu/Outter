using Core.TransactionServer.Agent.BLL.TransactionBusiness.Commands;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BO = Core.TransactionServer.Agent.BinaryOption;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.Factory
{
    public sealed class AddGeneralTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddGeneralTransactionCommandFactory Default = new AddGeneralTransactionCommandFactory();

        private AddGeneralTransactionCommandFactory() { }

        internal override AddTransactionCommandBase Create(Account account, DataRow dataRowTran)
        {
            return new AddGeneralTransactionCommand(account, dataRowTran);
        }

        internal override AddTransactionCommandBase CreateByAutoClose(Account account, Order openOrder, Price closePrice, OrderType orderType)
        {
            return new AddGeneralTransactionCommand(account, openOrder, closePrice, orderType);
        }

        internal override AddTransactionCommandBase CreateByClose(Account account, Order openOrder)
        {
            return new AddGeneralTransactionCommand(account, openOrder);
        }

        internal override AddTransactionCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
        {
            return new AddGeneralTransactionCommand(account, ifTran, sourceOrderId, limitPrice, stopPrice);
        }

        internal override AddTransactionCommandBase CreateCutTransaction(Account account, CutTransactionParams cutTransactionParams)
        {
            return new AddGeneralTransactionCommand(account, cutTransactionParams);
        }

        public override AddTransactionCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            return new AddGeneralTransactionCommand(account, tranData);
        }
    }
}
