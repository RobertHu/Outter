using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors
{
    internal sealed class AddBookOrderRelationCommandVisitor : AddCommunicationOrderRelaitonVistorBase
    {
        internal static readonly AddBookOrderRelationCommandVisitor Default = new AddBookOrderRelationCommandVisitor();

        static AddBookOrderRelationCommandVisitor() { }
        private AddBookOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.Visit((Commands.AddBookOrderRelationCommand)command);
        }

        internal override void VisitPhysicalOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.Visit((Commands.AddBookPhysicalOrderRelationCommand)command);
        }

        internal override void VisitBOOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            throw new NotImplementedException();
        }
    }

}
