using Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    internal sealed class AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        internal static readonly AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor Default = new AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor();

        static AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor() { }
        private AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor() { }


        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.Parse((AddLmtQuantiryOnMaxLotChangeTransactionCommandBase)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.Parse((AddLmtQuantiryOnMaxLotChangeTransactionCommandBase)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void Parse(AddLmtQuantiryOnMaxLotChangeTransactionCommandBase command)
        {
            this.ParseCommon(command);
            command.CreateTransaction();
            this.CreateOrder(command);
        }


        private void CreateOrder(AddLmtQuantiryOnMaxLotChangeTransactionCommandBase command)
        {
            var tran = command.Result;
            var addOrderCommand = command.AddOrderCommandFactory.CreateAddLmtQuantiryOnMaxLotChangeOrderCommand(tran, command.OriginOrder, command.Lot);
            addOrderCommand.Execute();
        }



        private void ParseCommon(AddLmtQuantiryOnMaxLotChangeTransactionCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Code = command.GenerateTransactionCode(command.OriginTran.OrderType);
            constructParams.InstrumentId = command.OriginTran.InstrumentId;
            constructParams.Type = iExchange.Common.TransactionType.Single;
            constructParams.SubType = command.OriginTran.SubType;
            constructParams.Phase = iExchange.Common.TransactionPhase.Placed;
            constructParams.OrderType = command.OriginTran.OrderType;
            constructParams.ConstractSize = command.OriginTran.ContractSize(null);
            constructParams.BeginTime = command.OriginTran.BeginTime;
            constructParams.EndTime = command.OriginTran.EndTime;
            constructParams.SubmitTime = command.OriginTran.SubmitTime;
            constructParams.SubmitorId = command.OriginTran.SubmitorId;
            constructParams.SourceOrderId = command.OriginOrder.Id;
        }

    }
}
