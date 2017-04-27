using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddCloseTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddCloseTransactionCommandVisitor Default = new AddCloseTransactionCommandVisitor();

        private AddCloseTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseBOTransaction((Commands.AddCloseTransactionCommandBase)command);
        }

        private void ParseBOTransaction(Commands.AddCloseTransactionCommandBase command)
        {
            this.ParseBOConstructParams(command);
             command.CreateTransaction();
            var addOrderCommand = ((OrderBLL.Factory.AddBOOrderCommandFactory)command.AddOrderCommandFactory).CreateByClose(command.Result, (BinaryOption.Order)command.OpenOrder);
            addOrderCommand.Execute();
        }

        private void ParseBOConstructParams(Commands.AddCloseTransactionCommandBase command)
        {
            var baseTime = Market.MarketManager.Now;
            TransactionConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.InstrumentId = command.OpenOrder.Owner.InstrumentId;
            constructParams.Type = TransactionType.Single;
            constructParams.SubType = TransactionSubType.None;
            constructParams.Phase = TransactionPhase.Executed;
            constructParams.OrderType = OrderType.BinaryOption;
            constructParams.ConstractSize = 1;
            constructParams.BeginTime = baseTime;
            constructParams.EndTime = baseTime.AddMinutes(15);
            constructParams.SubmitTime = baseTime;
            constructParams.ExecuteTime = baseTime;
            constructParams.SubmitorId = Guid.Empty;
            constructParams.ApproveId = Guid.Empty;
        }

    }
}
