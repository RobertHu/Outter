using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddCutTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddCutTransactionCommandVisitor Default = new AddCutTransactionCommandVisitor();

        private AddCutTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransaction((Commands.AddCutTransactionCommandBase)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransaction((Commands.AddCutTransactionCommandBase)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void ParseTransaction(Commands.AddCutTransactionCommandBase command)
        {
            this.ParseCommon(command);
            command.CreateTransaction();
            var addOrderCommand = command.AddOrderCommandFactory.CreateCutOrder(command.Result, command.IsBuy, command.LotBalanceSum, command.SetPrice);
            addOrderCommand.Execute();
        }

        private void ParseCommon(Commands.AddCutTransactionCommandBase command)
        {
            DateTime baseTime = Market.MarketManager.Now;
            var constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.InstrumentId = command.Instrument.Id;
            constructParams.Type = TransactionType.Single;
            constructParams.SubType = TransactionSubType.None;
            constructParams.Phase = TransactionPhase.Executed;
            constructParams.OrderType = OrderType.Risk;
            constructParams.Code = command.GenerateTransactionCode(OrderType.Risk);
            constructParams.ConstractSize = command.Instrument.TradePolicyDetail().ContractSize;
            constructParams.BeginTime = baseTime;
            constructParams.EndTime = baseTime.AddMinutes(15);
            constructParams.SubmitTime = baseTime;
            constructParams.ExecuteTime = baseTime;
            constructParams.SubmitorId = Guid.Empty;
            constructParams.ApproveId = Guid.Empty;
        }

    }
}
