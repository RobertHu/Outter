using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Physical.OrderRelationBusiness
{
    internal sealed class AddPhysicalOrderRelationCommand : AddOrderRelationCommandBase
    {
        internal AddPhysicalOrderRelationCommand(PhysicalOrder closeOrder, XElement xmlOrderRelation)
            : base(closeOrder, xmlOrderRelation, AddXmlFormatOrderRelationCommandVisitor.Default)
        { }

        internal AddPhysicalOrderRelationCommand(PhysicalOrder closeOrder, DataRow dataRowOrderRelation)
            : base(closeOrder, dataRowOrderRelation, AddDataRowFormatOrderRelationCommandVisitor.Default)
        {
        }

        internal AddPhysicalOrderRelationCommand(PhysicalOrder closeOrder, PhysicalOrder openOrder, decimal closedLot)
            : base(closeOrder, openOrder, closedLot, AddGeneralFormatOrderRelationCommandVisitor.Default)
        {
        }

        internal AddPhysicalOrderRelationCommand(PhysicalOrder closeOrder, Protocal.OrderRelationData orderRelaitonData)
            : base(closeOrder, orderRelaitonData, AddCommunicationOrderRelationCommandVisitor.Default)
        {
        }

        protected override OrderRelationConstructParams CreateConstructParams()
        {
            return new PhysicalOrderRelationConstructParams();
        }

        internal override OrderRelation CreateOrderRelation()
        {
            return new PhysicalOrderRelation((PhysicalOrderRelationConstructParams)this.ConstructParams);
        }

        protected override void Accept(AddOrderRelationCommandVisitorBase visitor)
        {
            visitor.VisitPhysicalOrderRelationCommand(this);
        }
    }
}
