using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddCancelDeliveryWithShortSellOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddCancelDeliveryWithShortSellOrderCommandVisitor Default = new AddCancelDeliveryWithShortSellOrderCommandVisitor();

        static AddCancelDeliveryWithShortSellOrderCommandVisitor() { }
        private AddCancelDeliveryWithShortSellOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            var deliveryCommand = (Commands.AddCancelDeliveryWithShortSellOrderCommand)command;
            var constructParams = (PhysicalOrderConstructParams)command.ConstructParams;
            constructParams.PhysicalSettings = new PhysicalConstructParams();
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateOrderCode();
            constructParams.TradeOption = deliveryCommand.TradeOption;
            constructParams.Phase = iExchange.Common.OrderPhase.Executed;
            constructParams.IsOpen = deliveryCommand.IsOpen;
            constructParams.IsBuy = deliveryCommand.IsBuy;
            constructParams.SetPrice = deliveryCommand.SetPrice;
            constructParams.ExecutePrice = deliveryCommand.ExecutePrice;
            constructParams.Lot = deliveryCommand.Lot;
            constructParams.OriginalLot = deliveryCommand.Lot;
            constructParams.LotBalance = deliveryCommand.LotBalance;
            constructParams.PhysicalSettings.PhysicalTradeSide = iExchange.Common.PhysicalTradeSide.Delivery;
            constructParams.PhysicalSettings.PhysicalRequestId = deliveryCommand.PhysicalRequestId;
            this.CreateOrder(command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        protected override void CreateOrderRelation(Order closeOrder, Commands.AddOrderCommandBase command)
        {
            var deliveryCommand = (Commands.AddCancelDeliveryWithShortSellOrderCommand)command;
            foreach (var eachOrderRelationRecord in deliveryCommand.OrderRelations)
            {
                var addCommand = command.AddOrderRelationFactory.Create(eachOrderRelationRecord.OpenOrder, command.Result, eachOrderRelationRecord.ClosedLot);
                addCommand.Execute();
            }
        }
    }
}
