using Core.TransactionServer.Agent.Util.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    internal sealed class AddMultipleCloseTranCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddMultipleCloseTranCommandVisitor Default = new AddMultipleCloseTranCommandVisitor();

        static AddMultipleCloseTranCommandVisitor() { }
        private AddMultipleCloseTranCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.VisitCommon((Commands.AddMultipleCloseTranCommand)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.VisitCommon((Commands.AddMultipleClosePhysicalTranCommand)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void VisitCommon(Commands.AddMultipleCloseTranCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateTransactionCode(iExchange.Common.OrderType.MultipleClose);
            constructParams.InstrumentId = command.InstrumentId;
            constructParams.Type = iExchange.Common.TransactionType.MultipleClose;
            constructParams.SubType = iExchange.Common.TransactionSubType.None;
            constructParams.Phase = iExchange.Common.TransactionPhase.Executed;
            constructParams.ConstractSize = command.ContractSize;
            constructParams.BeginTime = DateTime.Now;
            constructParams.EndTime = new DateTime(9999, 12, 31);
            constructParams.OrderType = iExchange.Common.OrderType.MultipleClose;
            constructParams.SubmitTime = DateTime.Now;
            constructParams.ExecuteTime = DateTime.Now;
            constructParams.SubmitorId = command.SubmitorId;
            constructParams.ApproveId = Guid.Empty;
            command.CreateTransaction();
        }


    }
}
