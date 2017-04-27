using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddCommunicationTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddCommunicationTransactionCommandVisitor));

        public static readonly AddCommunicationTransactionCommandVisitor Default = new AddCommunicationTransactionCommandVisitor();

        private AddCommunicationTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransaction((Commands.AddCommunicationTransactionCommandBase)command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransaction((Commands.AddCommunicationTransactionCommandBase)command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransaction((Commands.AddCommunicationTransactionCommandBase)command);
        }


        private void ParseTransaction(Commands.AddCommunicationTransactionCommandBase command)
        {
            this.ParseCommon(command);
            var tran = command.CreateTransaction();
            this.CreateOrders(command);
            if (tran.SubType == TransactionSubType.IfDone)
            {
                IfDoneTransactionParser.Default.FillDoneTrans(tran, command.TranData);
            }
            else if (tran.SubType == TransactionSubType.Amend && this.IsChangeToIfDone(command.TranData))
            {
                IfDoneTransactionParser.Default.FillDoneTrans(tran, command.TranData);
            }
        }

        private bool IsChangeToIfDone(Protocal.TransactionData tranData)
        {
            if (tranData.SubType != TransactionSubType.Amend) return false;
            foreach (var eachOrderData in tranData.Orders)
            {
                if (eachOrderData.IfDoneOrderSetting != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void CreateOrders(Commands.AddCommunicationTransactionCommandBase command)
        {
            var tranData = command.TranData;
            if (tranData.Orders == null || tranData.Orders.Count == 0) return;
            var tran = command.Result;
            foreach (var eachOrder in tranData.Orders)
            {
                var addOrderCommand = command.AddOrderCommandFactory.CreateByCommunication(tran, eachOrder);
                addOrderCommand.Execute();
            }
        }



        private void ParseCommon(Commands.AddCommunicationTransactionCommandBase command)
        {
            TransactionConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            var transactionData = command.TranData;
            Logger.InfoFormat("parse place data , tranId = {0}, subType = {1}", transactionData.Id, transactionData.SubType);
            constructParams.Fill(transactionData);
            constructParams.Code = command.GenerateTransactionCode(constructParams.OrderType);
            constructParams.Phase = TransactionPhase.Placing;
        }

    }

}
