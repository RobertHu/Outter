using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.Commands
{
    public sealed class AddGeneralTransactionCommand : AddTransactionCommandBase
    {

        internal AddGeneralTransactionCommand(Account account, DataRow dataRowTran)
            : base(account, dataRowTran, AddDataRowFormatTransactionCommandVisitor.Default)
        {
        }

        internal AddGeneralTransactionCommand(Account account, Protocal.TransactionData tran)
            : base(account, tran, AddCommunicationTransactionCommandVisitor.Default)
        {
        }

        internal AddGeneralTransactionCommand(Account account, Order openOrder, Price closePrice, OrderType orderType)
            : base(account, openOrder, closePrice, orderType, AddAutoCloseTransactionCommandVisitor.Default)
        {
        }

        internal AddGeneralTransactionCommand(Account account, Order openOrder)
            : base(account, openOrder, AddCloseTransactionCommandVisitor.Default)
        {
        }


        internal AddGeneralTransactionCommand(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
            : base(account, ifTran, sourceOrderId, limitPrice, stopPrice, AddDoneTransactionCommandVisitor.Default)
        {
        }

        internal AddGeneralTransactionCommand(Account account, CutTransactionParams cutTransactionParams)
            : base(account, cutTransactionParams, AddCutTransactionCommandVisitor.Default)
        {
        }

        protected override TransactionConstructParams CreateConstructParams()
        {
            return new TransactionConstructParams();
        }

        internal override Transaction CreateTransaction()
        {
            return GeneralTransactionServiceFactory.Default.CreateTransaction(this.Account, this.ConstructParams);
        }

        internal override void Accept(AddTransactionCommandVisitorBase visitor)
        {
            visitor.VisitAddGeneralTransactionCommand(this);
        }

        internal override AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return AddGeneralOrderCommandFactory.Default; }
        }
    }
}
