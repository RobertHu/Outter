using Core.TransactionServer.Agent.BLL.OrderBusiness;
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
    internal sealed class AddAutoCloseOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddAutoCloseOrderCommandVisitor Default = new AddAutoCloseOrderCommandVisitor();

        private AddAutoCloseOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(AddOrderCommandBase command)
        {
            this.ParseOrderRelaitonCommon((AddAutoCloseOrderCommandBase)command);
        }

        public override void VisitAddPhysicalOrderCommand(AddOrderCommandBase  command)
        {
            this.ParseOrderRelaitonCommon((AddAutoCloseOrderCommandBase)command);
        }

        public override void VisitAddBOOrderCommand(AddOrderCommandBase  command)
        {
            throw new NotImplementedException();
        }

        private void ParseOrderRelaitonCommon(AddAutoCloseOrderCommandBase command)
        {
            this.ParseCommon(command);
            this.CreateOrder(command);
        }

        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
            var autoCloseCommand = (AddAutoCloseOrderCommandBase)command;
            var addOrderRelationCommand = command.AddOrderRelationFactory.Create(autoCloseCommand.OpenOrder, command.Result, autoCloseCommand.OpenOrder.LotBalance);
            addOrderRelationCommand.Execute();
        }


        private void ParseCommon(AddAutoCloseOrderCommandBase command)
        {
            OrderConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.Phase = OrderPhase.Executed;
            constructParams.IsOpen = false;
            constructParams.IsBuy = !command.OpenOrder.IsBuy;
            constructParams.SetPrice = command.ClosePrice;
            constructParams.ExecutePrice = command.ClosePrice;
            constructParams.Lot = command.OpenOrder.LotBalance;
            constructParams.OriginalLot = command.OpenOrder.LotBalance;
            constructParams.TradeOption = command.TradeOption;
        }
    }
}
