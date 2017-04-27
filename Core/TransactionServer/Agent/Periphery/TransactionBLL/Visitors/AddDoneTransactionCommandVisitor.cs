using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddDoneTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddDoneTransactionCommandVisitor Default = new AddDoneTransactionCommandVisitor();

        private AddDoneTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseDoneTransactoin((Commands.AddDoneTransactionCommandBase)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseDoneTransactoin((Commands.AddDoneTransactionCommandBase)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseDoneTransactoin((Commands.AddDoneTransactionCommandBase)command);
        }

        private void ParseDoneTransactoin(Commands.AddDoneTransactionCommandBase command)
        {
            this.ParseCommon(command);
            command.CreateTransaction();
            var openOrder = this.GetOpenOrder(command.IfTran, command.SourceOrderId);
            if (openOrder == null)
            {
                throw new ApplicationException("Can't find the open order of " + command.SourceOrderId);
            }
            if (command.LimitPrice != null)
            {
                var addLimitOrderCommand = command.AddOrderCommandFactory.CreateDoneOrder(command.Result, openOrder, command.LimitPrice, TradeOption.Better);
                addLimitOrderCommand.Execute();
            }

            if (command.StopPrice != null)
            {
                var addStopOrderCommand = command.AddOrderCommandFactory.CreateDoneOrder(command.Result, openOrder, command.StopPrice, TradeOption.Stop);
                addStopOrderCommand.Execute();
            }
        }

        private Order GetOpenOrder(Transaction ifTran, Guid sourceOrderId)
        {
            foreach (Order order in ifTran.Orders)
            {
                if (order.Id == sourceOrderId)
                {
                    return order;
                }
            }
            return null;
        }

        private void ParseCommon(Commands.AddDoneTransactionCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateTransactionCode(OrderType.Limit);
            constructParams.InstrumentId = command.IfTran.InstrumentId;
            var transactionType = command.LimitPrice != null && command.StopPrice != null ? TransactionType.OneCancelOther : TransactionType.Single;
            constructParams.Type = transactionType;
            constructParams.SubType = TransactionSubType.IfDone;
            constructParams.Phase = TransactionPhase.Placing;
            constructParams.OrderType = OrderType.Limit;
            constructParams.ConstractSize = command.IfTran.ContractSize(null);
            constructParams.BeginTime = command.IfTran.BeginTime;
            constructParams.EndTime = command.IfTran.EndTime;
            constructParams.SubmitTime = command.IfTran.SubmitTime;
            constructParams.ExecuteTime = command.IfTran.ExecuteTime;
            constructParams.SubmitorId = command.IfTran.Submitor.Id;
            constructParams.ApproveId = null;
            constructParams.SourceOrderId = command.SourceOrderId;
        }

    }
}
