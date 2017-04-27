using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    internal sealed class AddCancelDeliveryWithShortSellTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddCancelDeliveryWithShortSellTransactionCommandVisitor Default = new AddCancelDeliveryWithShortSellTransactionCommandVisitor();

        static AddCancelDeliveryWithShortSellTransactionCommandVisitor() { }
        private AddCancelDeliveryWithShortSellTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            var deliveryCommand = (Commands.AddCancelDeliveryWithShortSellTranCommand)command;
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateTransactionCode(iExchange.Common.OrderType.Risk);
            constructParams.InstrumentId = deliveryCommand.InstrumentId;
            constructParams.ConstractSize = deliveryCommand.ContractSize;
            constructParams.SubmitorId = Guid.Empty;
            constructParams.ApproveId = Guid.Empty;
            constructParams.Type = iExchange.Common.TransactionType.Single;
            constructParams.SubType = iExchange.Common.TransactionSubType.None;
            constructParams.Phase = iExchange.Common.TransactionPhase.Executed;
            constructParams.OrderType = iExchange.Common.OrderType.Risk;
            DateTime now = Market.MarketManager.Now;
            constructParams.BeginTime = now;
            constructParams.EndTime = now.AddMinutes(5);
            constructParams.SubmitTime = now;
            constructParams.ExecuteTime = now.AddSeconds(10);
            command.CreateTransaction();
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }
    }
}
