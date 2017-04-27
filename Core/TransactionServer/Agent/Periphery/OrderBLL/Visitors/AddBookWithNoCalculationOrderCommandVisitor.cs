using Core.TransactionServer.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal abstract class AddBookOrderCommandVisitorBase : AddCommunicationOrderCommandVisitorBase
    {
        protected void Visit(Commands.AddCommunicationCommandBase command)
        {
            this.ParseCommon(command);
            command.ConstructParams.Phase = iExchange.Common.OrderPhase.Executed;
            this.CreateOrder(command);
            this.AddBills(command.Result, (Protocal.OrderBookData)command.OrderData);
        }


        private void AddBills(Order order, Protocal.OrderBookData orderData)
        {
            if (orderData.CommissionSum != null)
            {
                order.AddBill(Framework.Bill.CreateForOrder(order.AccountId, order.CurrencyId, orderData.CommissionSum.Value, Protocal.BillType.Commission));
            }

            if (orderData.LevySum != null)
            {
                order.AddBill(Framework.Bill.CreateForOrder(order.AccountId, order.CurrencyId,orderData.LevySum.Value, Protocal.BillType.Levy));
            }

            if (orderData.OtherFeeSum != null)
            {
                order.AddBill(Framework.Bill.CreateForOrder(order.AccountId, order.CurrencyId,orderData.OtherFeeSum.Value, Protocal.BillType.OtherFee));
            }

        }

        private void ParseCommon(Commands.AddCommunicationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            var bookData = (Protocal.OrderBookData)command.OrderData;
            constructParams.FillOrderCommonData(bookData, command.InstrumentId, command.TradeDay);
            constructParams.Code = command.GenerateOrderCode();
            constructParams.BlotterCode = base.ParseBlotterCode(command);
            constructParams.ExecutePrice = PriceHelper.CreatePrice(bookData.ExecutePrice, command.InstrumentId, command.TradeDay);
            constructParams.OrderBatchInstructionID = bookData.OrderBatchInstructionID;
            constructParams.OriginCode = bookData.OrginCode;
            if (!bookData.IsOpen)
            {
                this.ValidateForCloseOrder(bookData, constructParams);
            }
        }

        protected override OrderRelationBLL.Commands.AddOrderRelationCommandBase CreateAddOrderRelationCommand(OrderRelationBLL.Factory.AddOrderRelationFactoryBase factory, Order closeOrder, object orderRelationData)
        {
            return factory.CreateBookOrderRelation(closeOrder, (Protocal.OrderRelationBookData)orderRelationData);
        }

    }


    internal sealed class AddBookWithNoCalculationOrderCommandVisitor : AddBookOrderCommandVisitorBase
    {
        internal static readonly AddBookWithNoCalculationOrderCommandVisitor Default = new AddBookWithNoCalculationOrderCommandVisitor();

        static AddBookWithNoCalculationOrderCommandVisitor() { }
        private AddBookWithNoCalculationOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.Visit((Commands.AddBookWithNoCalculationOrderCommand)command);
        }


        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.Visit((Commands.AddBookWithNoCalculationPhysicalOrderCommand)command);
        }


        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

    }

    internal sealed class AddBookOrderCommandVisitor : AddBookOrderCommandVisitorBase
    {
        internal static readonly AddBookOrderCommandVisitor Default = new AddBookOrderCommandVisitor();

        static AddBookOrderCommandVisitor() { }
        private AddBookOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.Visit((Commands.AddBookOrderCommand)command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.Visit((Commands.AddBookPhysicalOrderCommand)command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }
    }


}
