using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddMultipleCloseOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddMultipleCloseOrderCommandVisitor Default = new AddMultipleCloseOrderCommandVisitor();

        static AddMultipleCloseOrderCommandVisitor() { }
        private AddMultipleCloseOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.VisitCommon((Commands.AddMultipleCloseOrderCommand)command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.VisitCommon((Commands.AddMultipleClosePhysicalOrderCommand)command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void VisitCommon(Commands.AddMultipleCloseOrderCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateOrderCode();
            constructParams.Phase = iExchange.Common.OrderPhase.Executed;
            constructParams.TradeOption = iExchange.Common.TradeOption.Invalid;
            constructParams.IsOpen = false;
            constructParams.IsBuy = command.IsBuy;
            constructParams.ExecutePrice = command.ExecutePrice;
            constructParams.SetPrice = command.ExecutePrice;
            constructParams.OriginalLot = command.ClosedLot;
            constructParams.Lot = command.ClosedLot;
            constructParams.LotBalance = 0m;
            this.CreateOrder(command);
        }


        protected override void CreateOrderRelation(Order closeOrder, Commands.AddOrderCommandBase command)
        {
            var multipleCloseCommand = (Commands.AddMultipleCloseOrderCommandBase)command;
            foreach (var eachOrderRelation in multipleCloseCommand.OrderRelations)
            {
                var addOrderRelationCommand = command.AddOrderRelationFactory.Create(eachOrderRelation.OpenOrder, closeOrder, eachOrderRelation.ClosedLot);
                addOrderRelationCommand.Execute();
            }
        }
    }
}
