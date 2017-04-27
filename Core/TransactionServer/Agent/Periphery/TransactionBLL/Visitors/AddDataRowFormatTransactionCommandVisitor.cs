using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public sealed class AddDataRowFormatTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddDataRowFormatTransactionCommandVisitor));

        internal static readonly AddDataRowFormatTransactionCommandVisitor Default = new AddDataRowFormatTransactionCommandVisitor();

        private AddDataRowFormatTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon(command);
        }

        internal override void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon(command);
        }

        internal override void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command)
        {
            this.ParseTransactionCommon(command);
        }

        private void ParseTransactionCommon(Commands.AddTranCommandBase command)
        {
            this.ParseCommon((Commands.AddDataRowTransactionCommandBase)command);
             command.CreateTransaction();
        }

        private void ParseCommon(Commands.AddDataRowTransactionCommandBase command)
        {
            var dataRowTran = command.DataRow;
            var constructParams = command.ConstructParams;
            constructParams.OperationType = command.OperationType; 
            constructParams.Id = (Guid)dataRowTran["TransactionID"];
            var accountId = (Guid)dataRowTran["AccountID"];
            Debug.Assert(command.Account != null);
            constructParams.InstrumentId = (Guid)dataRowTran["InstrumentID"];
            constructParams.Code = (string)dataRowTran["TransactionCode"];
            constructParams.Type = (TransactionType)(byte)dataRowTran["TransactionType"];
            constructParams.SubType = (TransactionSubType)(byte)dataRowTran["TransactionSubType"];
            constructParams.Phase = (TransactionPhase)(byte)dataRowTran["Phase"];
            constructParams.OrderType = (OrderType)(int)dataRowTran["OrderTypeID"];
            constructParams.ConstractSize = (decimal)dataRowTran["ContractSize"];
            constructParams.BeginTime = (DateTime)dataRowTran["BeginTime"];
            constructParams.EndTime = (DateTime)dataRowTran["EndTime"];
            constructParams.ExpireType = (ExpireType)(int)dataRowTran["ExpireType"];
            constructParams.SubmitTime = (DateTime)dataRowTran["SubmitTime"];
            if (dataRowTran["ExecuteTime"] != DBNull.Value)
            {
                constructParams.ExecuteTime = (DateTime)dataRowTran["ExecuteTime"];
            }
            constructParams.SubmitorId = (Guid)dataRowTran["SubmitorID"];
            if (dataRowTran["ApproverID"] != DBNull.Value)
            {
                constructParams.ApproveId = (Guid)dataRowTran["ApproverID"];
            }
        }
    }
}
