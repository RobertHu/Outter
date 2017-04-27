using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Engine;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    internal abstract class AddBookTransactionCommandVistorBase : AddTransactionCommandVisitorBase
    {
        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            var bookCommand = (Commands.AddBookTransactionCommandBase)command;
            this.ParseTransactionCommon(bookCommand);
            this.BuildTran(bookCommand);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            var bookCommand = (Commands.AddBookTransactionCommandBase)command;
            this.ParseTransactionCommon(bookCommand);
            this.BuildTran(bookCommand);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        protected abstract OrderBLL.Commands.AddOrderCommandBase CreateAddOrderCommand(OrderBLL.Factory.AddOrderCommandFactoryBase factory, Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay);

        private void BuildTran(Commands.AddBookTransactionCommandBase command)
        {
            Transaction tran = command.CreateTransaction();
            foreach (var eachOrderData in command.TranData.Orders)
            {
                var addOrderCommand = this.CreateAddOrderCommand(command.AddOrderCommandFactory, tran, eachOrderData, command.TranData.TradeDay);
                addOrderCommand.Execute();
            }
        }


        private void ParseTransactionCommon(Commands.AddBookTransactionCommandBase command)
        {
            TransactionConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Fill(command.TranData);
            constructParams.Code = command.GenerateTransactionCode(constructParams.OrderType);
            constructParams.Phase = command.TranData.Phase;
            constructParams.ConstractSize = command.TranData.ContractSize;
            constructParams.ExecuteTime = command.TranData.ExecuteTime;
            constructParams.ApproveId = command.TranData.ApproverId.Value;
        }

    }


    internal sealed class AddBookWithNoCalculationTransactionCommandVistor : AddBookTransactionCommandVistorBase
    {
        internal static readonly AddBookWithNoCalculationTransactionCommandVistor Default = new AddBookWithNoCalculationTransactionCommandVistor();

        static AddBookWithNoCalculationTransactionCommandVistor() { }
        private AddBookWithNoCalculationTransactionCommandVistor() { }

        protected override OrderBLL.Commands.AddOrderCommandBase CreateAddOrderCommand(OrderBLL.Factory.AddOrderCommandFactoryBase factory, Transaction tran, OrderBookData orderData, DateTime tradeDay)
        {
            return factory.CreateBookOrderWithNoCalculation(tran, orderData, tradeDay);
        }
    }


    internal sealed class AddBookTransactionCommandVisitor : AddBookTransactionCommandVistorBase
    {
        internal static readonly AddBookTransactionCommandVisitor Default = new AddBookTransactionCommandVisitor();

        static AddBookTransactionCommandVisitor() { }
        private AddBookTransactionCommandVisitor() { }

        protected override OrderBLL.Commands.AddOrderCommandBase CreateAddOrderCommand(OrderBLL.Factory.AddOrderCommandFactoryBase factory, Transaction tran, OrderBookData orderData, DateTime tradeDay)
        {
            return factory.CreateBookOrder(tran, orderData, tradeDay);
        }
    }
}
