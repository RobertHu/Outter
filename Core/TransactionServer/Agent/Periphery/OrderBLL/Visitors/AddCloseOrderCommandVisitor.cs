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
    internal sealed class AddCloseOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddCloseOrderCommandVisitor Default = new AddCloseOrderCommandVisitor();

        private AddCloseOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(AddOrderCommandBase  command)
        {
            throw new NotImplementedException();
        }

        public override void VisitAddPhysicalOrderCommand(AddOrderCommandBase  command)
        {
            throw new NotImplementedException();
        }

        public override void VisitAddBOOrderCommand(AddOrderCommandBase  command)
        {
            this.ParseBOOrder((AddBOCloseOrdeCommand)command);
        }

        private void ParseBOOrder(AddBOCloseOrdeCommand command)
        {
            this.ParseBOConstructParams(command);
            this.CreateOrder(command);
        }

        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
            var openOrder = ((AddBOCloseOrdeCommand)command).OpenOrder;
            var addOrderRelationCommand = command.AddOrderRelationFactory.Create(openOrder, command.Result, openOrder.Lot);
            addOrderRelationCommand.Execute();
        }


        private void ParseBOConstructParams(AddBOCloseOrdeCommand command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Phase = OrderPhase.Executed;
            constructParams.IsOpen = false;
            constructParams.IsBuy = false;
            var openOrder = command.OpenOrder;
            constructParams.SetPrice = openOrder.BestPrice;
            constructParams.ExecutePrice = openOrder.ExecutePrice;
            constructParams.Lot = openOrder.Lot;
            constructParams.OriginalLot = openOrder.Lot;
            constructParams.LotBalance = 0;
            constructParams.TradeOption = TradeOption.Invalid;
        }

    }
}
