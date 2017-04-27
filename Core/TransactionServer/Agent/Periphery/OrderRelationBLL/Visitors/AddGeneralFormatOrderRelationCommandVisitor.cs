using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors
{
    internal sealed class AddGeneralFormatOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        internal static readonly AddGeneralFormatOrderRelationCommandVisitor Default = new AddGeneralFormatOrderRelationCommandVisitor();

        private AddGeneralFormatOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParseOrderRelationCommon((Commands.AddGeneralOrderRelationCommand)command);
        }

        internal override void VisitPhysicalOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParseOrderRelationCommon((Commands.AddGeneralPhysicalOrderRelationCommand)command);
        }

        internal override void VisitBOOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParseOrderRelationCommon((Commands.AddGeneralBOOrderRelationCommand)command);
        }

        private void ParseOrderRelationCommon(Commands.AddGeneralOrderRelationCommandBase command)
        {
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        private void ParseCommon(Commands.AddGeneralOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.OpenOrder = command.OpenOrder;
            constructParams.CloseOrder = command.CloseOrder;
            constructParams.ClosedLot = command.ClosedLot;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.OpenOrderExecuteTime = command.OpenOrder.ExecuteTime;
            if (command.OpenOrder.ExecutePrice != null)
            {
                constructParams.OpenOrderExecutePrice = command.OpenOrder.ExecutePrice.ToString();
            }
        }
    }
}
