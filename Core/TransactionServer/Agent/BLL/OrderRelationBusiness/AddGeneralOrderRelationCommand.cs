using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Diagnostics;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public sealed class AddGeneralOrderRelationCommand : AddOrderRelationCommandBase
    {
        internal AddGeneralOrderRelationCommand(Order closeOrder, XElement xmlOrderRelation)
            : base(closeOrder, xmlOrderRelation, AddXmlFormatOrderRelationCommandVisitor.Default)
        {
        }

        internal AddGeneralOrderRelationCommand(Order closeOrder, Protocal.OrderRelationData orderRelationData)
            : base(closeOrder, orderRelationData, AddCommunicationOrderRelationCommandVisitor.Default)
        {
        }


        internal AddGeneralOrderRelationCommand(Order closeOrder, DataRow dataRowOrderRelation)
            : base(closeOrder, dataRowOrderRelation, AddDataRowFormatOrderRelationCommandVisitor.Default)
        {
        }

        internal AddGeneralOrderRelationCommand(Order closeOrder, Order openOrder, decimal closedLot)
            : base(closeOrder, openOrder, closedLot, AddGeneralFormatOrderRelationCommandVisitor.Default) { }

        protected override OrderRelationConstructParams CreateConstructParams()
        {
            return new OrderRelationConstructParams();
        }

        internal override OrderRelation CreateOrderRelation()
        {
            return new OrderRelation(this.ConstructParams);
        }

        protected override void Accept(AddOrderRelationCommandVisitorBase visitor)
        {
            visitor.VisitGeneralOrderRelationCommand(this);
        }
    }
}
