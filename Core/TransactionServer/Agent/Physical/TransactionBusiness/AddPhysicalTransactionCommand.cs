using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Physical.TransactionBusiness
{
    public sealed class AddPhysicalTransactionCommand : AddTransactionCommandBase
    {
        internal AddPhysicalTransactionCommand(Account account, DataRow dataRowTran)
            : base(account, dataRowTran, AddDataRowFormatTransactionCommandVisitor.Default)
        {
        }

        internal AddPhysicalTransactionCommand(Account account, Protocal.TransactionData tranData)
            : base(account, tranData, AddCommunicationTransactionCommandVisitor.Default) { }

        internal AddPhysicalTransactionCommand(Account account, PhysicalOrder openOrder, Price closePrice, OrderType orderType)
            : base(account, openOrder, closePrice, orderType, AddAutoCloseTransactionCommandVisitor.Default)
        {
        }

        internal AddPhysicalTransactionCommand(Account account, PhysicalOrder openOrder)
            : base(account, openOrder, AddCloseTransactionCommandVisitor.Default)
        {
        }

        internal AddPhysicalTransactionCommand(Account account, PhysicalTransaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
            : base(account, ifTran, sourceOrderId, limitPrice, stopPrice, AddDoneTransactionCommandVisitor.Default)
        {
        }

        internal AddPhysicalTransactionCommand(Account account, CutTransactionParams cutTransactionParams)
            : base(account, cutTransactionParams, AddCutTransactionCommandVisitor.Default)
        {
        }


        internal AddPhysicalTransactionCommand(Account account, PhysicalInstalmentTransactionParams physicalInstalmentTransactionParams)
            : base(account, false, AddPhysicalInstalmentTransactionCommandVisitor.Default)
        {
            this.PhysicalInstalmentTransactionParams = physicalInstalmentTransactionParams;
        }

        internal PhysicalInstalmentTransactionParams PhysicalInstalmentTransactionParams { get; private set; }

        protected override BLL.TransactionBusiness.TransactionConstructParams CreateConstructParams()
        {
            return new BLL.TransactionBusiness.TransactionConstructParams();
        }

        internal override Transaction CreateTransaction()
        {
            return PhysicalTransactionServiceFactory.Default.CreateTransaction(this.Account, this.ConstructParams);
        }

        internal override void Accept(AddTransactionCommandVisitorBase visitor)
        {
            visitor.VisitAddPhysicalTransactionCommand(this);
        }

        internal override BLL.OrderBusiness.Factory.AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return AddPhysicalOrderCommandFactory.Default; }
        }
    }
}
