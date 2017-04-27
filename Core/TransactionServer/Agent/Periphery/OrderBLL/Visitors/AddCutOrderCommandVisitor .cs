using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddCutOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddCutOrderCommandVisitor Default = new AddCutOrderCommandVisitor();

        private AddCutOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(AddOrderCommandBase command)
        {
            this.ParseOrder((AddCutOrderCommand)command);
            this.CreateOrder(command);
        }

        public override void VisitAddPhysicalOrderCommand(AddOrderCommandBase command)
        {
            this.ParsePhysicalOrder((AddPhysicalCutOrderCommand)command);
            this.CreateOrder(command);
        }

        public override void VisitAddBOOrderCommand(AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void ParseOrder(AddCutOrderCommand command)
        {
            this.ParseCommon(command);
        }

        private void ParsePhysicalOrder(AddPhysicalCutOrderCommand command)
        {
            this.ParseCommon(command);
            var physicalConstructParams = (PhysicalOrderConstructParams)command.ConstructParams;
            physicalConstructParams.PhysicalSettings = new PhysicalConstructParams();
            physicalConstructParams.PhysicalSettings.PhysicalTradeSide = PhysicalTradeSide.None;
        }


        private void ParseCommon(AddCutOrderCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Phase = OrderPhase.Executed;
            constructParams.IsOpen = true;
            constructParams.Code = command.GenerateOrderCode();
            constructParams.IsBuy = command.IsBuy;
            constructParams.SetPrice = command.SetPrice;
            constructParams.ExecutePrice = command.SetPrice;
            constructParams.Lot = command.LotBalance;
            constructParams.OriginalLot = command.LotBalance;
            constructParams.LotBalance = command.LotBalance;
            constructParams.TradeOption = TradeOption.Invalid;
            constructParams.OperationType = OperationType.AsNewRecord;
            constructParams.SetPriceMaxMovePips = 0;
        }


        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
        }
    }
}
