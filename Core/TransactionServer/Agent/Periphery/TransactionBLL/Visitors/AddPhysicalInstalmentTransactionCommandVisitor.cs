using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddPhysicalInstalmentTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddPhysicalInstalmentTransactionCommandVisitor Default = new AddPhysicalInstalmentTransactionCommandVisitor();

        private AddPhysicalInstalmentTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            var addInstalmentTranCommand = (Commands.AddPhysicalInstalmentTransactionCommand)command;
            this.ParseCommon(addInstalmentTranCommand);
             command.CreateTransaction();
            var factory = (AddPhysicalOrderCommandFactory)command.AddOrderCommandFactory;
            var addOrderCommand = factory.CreateInstalmentOrder((PhysicalTransaction)command.Result, addInstalmentTranCommand.OldOrder, addInstalmentTranCommand.Lot, addInstalmentTranCommand.IsOpen, addInstalmentTranCommand.IsBuy);
            addOrderCommand.Execute();
            TransactionCodeGenerater.Default.FillTranAndOrderCode(command.Result);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void ParseCommon(Commands.AddPhysicalInstalmentTransactionCommand command)
        {
            var baseTime = command.BaseTime ??  Market.MarketManager.Now;
            TransactionConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            constructParams.Id = Guid.NewGuid();
            constructParams.InstrumentId = command.OldTran.InstrumentId;
            constructParams.Type = TransactionType.Single;
            constructParams.SubType = TransactionSubType.Amend;
            constructParams.Phase = TransactionPhase.Placed;
            constructParams.OrderType = command.OldTran.OrderType;
            constructParams.ConstractSize = command.OldTran.ContractSize(null);
            constructParams.BeginTime = baseTime;
            constructParams.EndTime = baseTime.AddMinutes(5);
            constructParams.SubmitTime = baseTime;
            constructParams.ExecuteTime = baseTime.AddSeconds(1);
            constructParams.SubmitorId = Guid.Empty;
            constructParams.SourceOrderId = command.SourceOrderId;
        }

    }
}
