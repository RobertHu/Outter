using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors
{
    internal sealed class AddDataRowFormatOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddDataRowFormatOrderRelationCommandVisitor));
        internal static readonly AddDataRowFormatOrderRelationCommandVisitor Default = new AddDataRowFormatOrderRelationCommandVisitor();

        private AddDataRowFormatOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParseCommon((Commands.AddDataRowOrderRelationCommand)command);
            command.CreateOrderRelation();
        }

        internal override void VisitPhysicalOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParsePhysical((Commands.AddDataRowPhysicalOrderRelationCommand)command);
            command.CreateOrderRelation();
        }

        internal override void VisitBOOrderRelationCommand(Commands.AddOrderRelationCommandBase command)
        {
            this.ParseCommon((Commands.AddDataRowBOOrderRelationCommand)command);
            command.CreateOrderRelation();
        }

        private void ParsePhysical(Commands.AddDataRowPhysicalOrderRelationCommand command)
        {
            this.ParseCommon(command);
            var physicalConstructParams = (PhysicalOrderRelationConstructParams)command.ConstructParams;
            var dataRowOrderRelation = command.DataRow;
            if (physicalConstructParams.CloseTime != null)
            {
                if (dataRowOrderRelation["PhysicalValueMatureDate"] != DBNull.Value)
                {
                    physicalConstructParams.PhysicalValueMatureDay = (DateTime)dataRowOrderRelation["PhysicalValueMatureDate"];
                }
                if (dataRowOrderRelation["RealPhysicalValueMatureDate"] != DBNull.Value)
                {
                    physicalConstructParams.PhysicalValueMatureDay = null;//means the value is unfrozen
                }
                physicalConstructParams.OverdueCutPenalty = (decimal)dataRowOrderRelation["OverdueCutPenalty"];
                physicalConstructParams.ClosePenalty = (decimal)dataRowOrderRelation["ClosePenalty"];
                physicalConstructParams.PayBackPledge = (decimal)dataRowOrderRelation["PayBackPledge"];
                physicalConstructParams.PhysicalValue = (decimal)dataRowOrderRelation["PhysicalValue"];
                physicalConstructParams.ClosedPhysicalValue = (decimal)dataRowOrderRelation["ClosedPhysicalValue"];
            }
        }

        private void ParseCommon(Commands.AddDataRowOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            var dataRowOrderRelation = command.DataRow;
            constructParams.CloseOrder = command.CloseOrder;
            constructParams.OpenOrder = command.Account.GetOrder((Guid)dataRowOrderRelation["OpenOrderID"]);
            if (constructParams.OpenOrder == null)
            {
                throw new OpenOrderNotFoundException((Guid)dataRowOrderRelation["OpenOrderID"], constructParams.CloseOrder.Id);
            }
            constructParams.ClosedLot = (decimal)dataRowOrderRelation["ClosedLot"];
            if (dataRowOrderRelation["CloseTime"] != DBNull.Value)
            {
                constructParams.CloseTime = (DateTime)dataRowOrderRelation["CloseTime"];
                constructParams.Commission = (decimal)dataRowOrderRelation["Commission"];
                constructParams.Levy = (decimal)dataRowOrderRelation["Levy"];
                constructParams.OtherFee = (decimal)dataRowOrderRelation["OtherFee"];
                constructParams.InterestPL = (decimal)dataRowOrderRelation["InterestPL"];
                constructParams.StoragePL = (decimal)dataRowOrderRelation["StoragePL"];
                constructParams.TradePL = (decimal)dataRowOrderRelation["TradePL"];

                if (dataRowOrderRelation["ValueTime"] != DBNull.Value)
                {
                    constructParams.ValueTime = (DateTime)dataRowOrderRelation["ValueTime"];
                    constructParams.Decimals = (byte)dataRowOrderRelation["TargetDecimals"];
                    constructParams.RateIn = (decimal)(double)dataRowOrderRelation["RateIn"];
                    constructParams.RateOut = (decimal)(double)dataRowOrderRelation["RateOut"];
                }
            }

            if (dataRowOrderRelation["EstimateCloseLevyOfOpenOrder"] != DBNull.Value)
            {
                constructParams.EstimateCloseLevyOfOpenOrder = (decimal)dataRowOrderRelation["EstimateCloseLevyOfOpenOrder"];
            }

            if (dataRowOrderRelation["EstimateCloseCommissionOfOpenOrder"] != DBNull.Value)
            {
                constructParams.EstimateCloseCommissionOfOpenOrder = (decimal)dataRowOrderRelation["EstimateCloseCommissionOfOpenOrder"];
            }


            constructParams.OperationType = Framework.OperationType.None;
        }
    }
}
