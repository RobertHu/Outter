using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption.Command
{
    internal sealed class AddBOOrderRelationCommand : AddOrderRelationCommandBase
    {
        internal AddBOOrderRelationCommand(Order closeOrder, DataRow dataRowOrderRelation)
            : base(closeOrder, dataRowOrderRelation, AddDataRowFormatOrderRelationCommandVisitor.Default)
        {
        }

        internal AddBOOrderRelationCommand(Order closeOrder, Order openOrder, decimal closedLot)
            : base(closeOrder, openOrder, closedLot, AddGeneralFormatOrderRelationCommandVisitor.Default)
        {
        }

        protected override OrderRelationConstructParams CreateConstructParams()
        {
            return new OrderRelationConstructParams();
        }

        protected override void Accept(AddOrderRelationCommandVisitorBase visitor)
        {
            visitor.VisitBOOrderRelationCommand(this);
        }

        internal override OrderRelation CreateOrderRelation()
        {
            return new BOOrderRelation(this.ConstructParams);
        }
    }
}
