using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors
{
    internal sealed class AddCommunicationOrderRelationCommandVisitor : AddCommunicationOrderRelaitonVistorBase
    {
        internal static readonly AddCommunicationOrderRelationCommandVisitor Default = new AddCommunicationOrderRelationCommandVisitor();

        static AddCommunicationOrderRelationCommandVisitor() { }
        private AddCommunicationOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.Visit((Commands.AddCommunicationOrderRelationCommand)command);
        }

        internal override void VisitPhysicalOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.Visit((Commands.AddCommunicationPhysicalOrderRelationCommand)command);
        }

        internal override void VisitBOOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            throw new NotImplementedException();
        }



    }
}
