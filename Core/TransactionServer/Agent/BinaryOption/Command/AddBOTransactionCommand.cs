using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BinaryOption.Command
{
    public sealed class AddBOTransactionCommand : AddTransactionCommandBase
    {
        internal AddBOTransactionCommand(Account account, DataRow dataRowTran)
            : base(account, dataRowTran, AddDataRowFormatTransactionCommandVisitor.Default)
        {
        }

        internal AddBOTransactionCommand(Account account, Order openOrder)
            : base(account, openOrder, AddCloseTransactionCommandVisitor.Default)
        {
        }

        protected override BLL.TransactionBusiness.TransactionConstructParams CreateConstructParams()
        {
            return new BLL.TransactionBusiness.TransactionConstructParams();
        }

        internal override Transaction CreateTransaction()
        {
            return Factory.BOTransactionServiceFactory.Default.CreateTransaction(this.Account, this.ConstructParams);
        }

        internal override void Accept(AddTransactionCommandVisitorBase visitor)
        {
            visitor.VisitAddBOTransactionCommand(this);
        }

        internal override BLL.OrderBusiness.Factory.AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return Factory.AddBOOrderCommandFactory.Default; }
        }
    }
}
