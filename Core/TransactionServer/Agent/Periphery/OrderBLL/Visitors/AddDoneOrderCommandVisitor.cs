using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddDoneOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddDoneOrderCommandVisitor Default = new AddDoneOrderCommandVisitor();

        private AddDoneOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParseOrder((AddDoneOrderCommand)command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParsePhysicalOrder((AddPhysicalDoneOrderCommand)command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void ParseOrder(AddDoneOrderCommandBase command)
        {
            this.ParseCommon(command.ConstructParams, command);
            this.CreateOrder(command);
        }

        private void ParsePhysicalOrder(AddPhysicalDoneOrderCommand command)
        {
            PhysicalOrderConstructParams physicalOrderConstructParams = (PhysicalOrderConstructParams)command.ConstructParams;
            physicalOrderConstructParams.PhysicalSettings = new PhysicalConstructParams() { PhysicalTradeSide = PhysicalTradeSide.None };
            this.ParseCommon(physicalOrderConstructParams, command);
            this.CreateOrder(command);
        }


        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
            var doneCommand = (AddDoneOrderCommandBase)command;
            var addOrderRelationCommand = command.AddOrderRelationFactory.Create(doneCommand.OpenOrder, command.Result, doneCommand.OpenOrder.LotBalance);
            addOrderRelationCommand.Execute();
        }


        private void ParseCommon(OrderConstructParams constructParams, AddDoneOrderCommandBase command)
        {
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = TransactionCodeGenerater.Default.GenerateOrderCode(command.OpenOrder.Owner.Owner.Setting().OrganizationId);
            constructParams.Phase = OrderPhase.Placing;
            constructParams.IsOpen = false;
            constructParams.IsBuy = !command.OpenOrder.IsBuy;
            constructParams.SetPrice = command.ClosePrice;
            constructParams.OriginalLot = command.OpenOrder.LotBalance;
            constructParams.Lot = command.OpenOrder.LotBalance;
            constructParams.TradeOption = command.TradeOption;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
        }

    }
}
