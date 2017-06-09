using Core.TransactionServer.Agent.Market;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddAutoCloseTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddAutoCloseTransactionCommandVisitor Default = new AddAutoCloseTransactionCommandVisitor();

        private AddAutoCloseTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon((Commands.AddAutoCloseTransactionCommandBase)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon((Commands.AddAutoCloseTransactionCommandBase)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon((Commands.AddAutoCloseTransactionCommandBase)command);
        }

        private void ParseTransactionCommon(Commands.AddAutoCloseTransactionCommandBase command)
        {
            this.ParseCommon(command);
             command.CreateTransaction();
            TradeOption tradeOption = command.OrderType == OrderType.Limit ? TradeOption.Better : TradeOption.Stop;
            var addAutoCloseOrderCommand = command.AddOrderCommandFactory.CreateByAutoClose(command.Result, command.OpenOrder, command.ClosePrice, tradeOption);
            addAutoCloseOrderCommand.Execute();
        }

        private void ParseCommon(Commands.AddAutoCloseTransactionCommandBase command)
        {
            var tran = command.OpenOrder.Owner;
            DateTime baseTime = MarketManager.Now;
            var constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.InstrumentId = tran.InstrumentId;
            constructParams.Type = TransactionType.Single;
            constructParams.SubType = TransactionSubType.None;
            constructParams.Phase = TransactionPhase.Executed;
            constructParams.OrderType = command.OrderType;
            constructParams.ConstractSize = tran.ContractSize(null);
            constructParams.BeginTime = baseTime;
            constructParams.EndTime = new DateTime(9999, 12, 31);
            constructParams.SubmitTime = baseTime;
            constructParams.ExecuteTime = baseTime;
            constructParams.SubmitorId = tran.SubmitorId;
        }


    }
}
