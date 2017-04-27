using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal static class DataRowOrderParser
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DataRowOrderParser));

        internal static void ParseForPhysical(PhysicalOrderConstructParams constructParams, IDBRow dataRowOrder, Guid instrumentId, Guid accountId, DateTime? tradeDay)
        {
            DataRowOrderParser.ParseForGeneral(constructParams, dataRowOrder, instrumentId, accountId, tradeDay);
            constructParams.PhysicalSettings = DataRowOrderParser.ParsePhysicalSettings(dataRowOrder);
            if (constructParams.PhysicalSettings.PhysicalType != Protocal.Physical.PhysicalType.FullPayment)
            {
                Logger.InfoFormat("ParseForPhysical orderId={0}", constructParams.Id);
                constructParams.Instalment = DataRowOrderParser.ParseInstalment(dataRowOrder);
            }
        }

        private static PhysicalConstructParams ParsePhysicalSettings(IDBRow dataRowOrder)
        {
            PhysicalConstructParams result = new PhysicalConstructParams();
            result.PhysicalTradeSide = (PhysicalTradeSide)((int)dataRowOrder["PhysicalTradeSide"]);

            if (dataRowOrder["PhysicalRequestId"] != DBNull.Value)
            {
                result.PhysicalRequestId = ((Guid)dataRowOrder["PhysicalRequestId"]);
            }

            if (dataRowOrder["PhysicalValueMatureDay"] != DBNull.Value)
            {
                result.PhysicalValueMatureDay = (int)dataRowOrder["PhysicalValueMatureDay"];
            }
            else
            {
                result.PhysicalValueMatureDay = 0;
            }
            result.PhysicalType = (Protocal.Physical.PhysicalType)dataRowOrder.GetColumn<byte>("PhysicalType");
            result.PhysicalOriginValue = (decimal)dataRowOrder["PhysicalOriginValue"];
            result.PhysicalOriginValueBalance = (decimal)dataRowOrder["PhysicalOriginValueBalance"];
            result.PaidPledgeBalance = (decimal)dataRowOrder["PaidPledgeBalance"];
            result.PaidPledge = dataRowOrder.GetColumn<decimal>("PaidPledge");

            return result;
        }

        private static InstalmentConstructParams ParseInstalment(IDBRow dataRowOrder)
        {
            InstalmentConstructParams result = new InstalmentConstructParams();
            if (dataRowOrder["InstalmentPolicyId"] == DBNull.Value) return null;
            result.InstalmentPolicyId = (Guid)dataRowOrder["InstalmentPolicyId"];
            int period = (int)dataRowOrder["Period"];
            if (dataRowOrder["InstalmentFrequence"] == DBNull.Value)
            {
                throw new InitializeEntityFromDBException("Order", "InstalmentFrequence");
            }
            InstalmentFrequence instalmentFrequence = (InstalmentFrequence)((int)dataRowOrder["InstalmentFrequence"]);
            result.Period = period;
            result.Frequence = instalmentFrequence;
            result.DownPayment = (decimal)dataRowOrder["DownPayment"];
            result.InstalmentType = (InstalmentType)((int)dataRowOrder["PhysicalInstalmentType"]);
            result.RecalculateRateType = (RecalculateRateType)((int)dataRowOrder["RecalculateRateType"]);
            result.IsInstalmentOverdue = (bool)dataRowOrder["IsInstalmentOverdue"];
            result.InstalmentOverdueDay = (int)dataRowOrder["InstalmentOverdueDay"];
            result.DownPaymentBasis = (Protocal.DownPaymentBasis)((int)dataRowOrder["DownPaymentBasis"]);
            return result;
        }

        internal static void ParseForBO(BOOrderConstructParams constructParams, IDBRow dataRowOrder, Guid instrumentId, Guid accountId, DateTime? tradeDay)
        {
            constructParams.ParseForGeneral(dataRowOrder, instrumentId, accountId, tradeDay);
            constructParams.PaidPledge = dataRowOrder.GetColumn<decimal>("PaidPledge");
            constructParams.PaidPledgeBalance = dataRowOrder.GetColumn<decimal>("PaidPledgeBalance");
            constructParams.BetTypeId = dataRowOrder.GetColumn<Guid>("BOBetTypeID");
            constructParams.Frequency = dataRowOrder.GetColumn<int>("BOFrequency");
            constructParams.Odds = dataRowOrder.GetColumn<decimal>("BOOdds");
            constructParams.BetOption = dataRowOrder.GetColumn<long>("BOBetOption");
            constructParams.SettleTime = dataRowOrder.GetColumn<DateTime?>("BOSettleTime");
        }

        internal static void ParseForGeneral(this OrderConstructParams constructParams, IDBRow dataRowOrder, Guid instrumentId, Guid accountId, DateTime? tradeDay)
        {
            constructParams.Id = (Guid)dataRowOrder["ID"];
            if (dataRowOrder["Code"] != DBNull.Value)
            {
                constructParams.Code = (string)dataRowOrder["Code"];
            }
            constructParams.BlotterCode = dataRowOrder["BlotterCode"] == DBNull.Value ? null : (string)dataRowOrder["BlotterCode"];
            constructParams.Phase = (OrderPhase)(byte)dataRowOrder["Phase"];
            constructParams.TradeOption = (TradeOption)(byte)dataRowOrder["TradeOption"];
            constructParams.IsOpen = (bool)dataRowOrder["IsOpen"];
            constructParams.IsBuy = (bool)dataRowOrder["IsBuy"];

            if (dataRowOrder["SetPrice"] != DBNull.Value)
            {
                constructParams.SetPrice = PriceHelper.CreatePrice((string)dataRowOrder["SetPrice"], instrumentId, tradeDay);
            }


            if (dataRowOrder["ExecutePrice"] != DBNull.Value)
            {
                constructParams.ExecutePrice = PriceHelper.CreatePrice((string)dataRowOrder["ExecutePrice"], instrumentId, tradeDay);
            }

            if (dataRowOrder["SetPriceMaxMovePips"] != DBNull.Value)
            {
                constructParams.SetPriceMaxMovePips = (int)dataRowOrder["SetPriceMaxMovePips"];
            }
            if (dataRowOrder["DQMaxMove"] != DBNull.Value)
            {
                constructParams.DQMaxMove = (int)dataRowOrder["DQMaxMove"];
            }

            constructParams.Lot = (decimal)dataRowOrder["Lot"];
            constructParams.OriginalLot = (decimal)dataRowOrder["OriginalLot"];
            constructParams.LotBalance = (decimal)dataRowOrder["LotBalance"];
            constructParams.InterestPerLot = dataRowOrder.GetColumn<decimal>("InterestPerLot");
            constructParams.StoragePerLot = dataRowOrder.GetColumn<decimal>("StoragePerLot");
            constructParams.HitCount = (short)dataRowOrder["HitCount"];

            if (dataRowOrder["InterestValueDate"] != DBNull.Value)
            {
                constructParams.InterestValueDate = (DateTime)dataRowOrder["InterestValueDate"];
            }

            if (constructParams.HitCount != 0 && dataRowOrder["BestPrice"] != DBNull.Value)
            {
                constructParams.BestPrice = PriceHelper.CreatePrice((string)dataRowOrder["BestPrice"], instrumentId, tradeDay);
            }

            if (constructParams.HitCount != 0 && dataRowOrder["BestTime"] != DBNull.Value)
            {
                constructParams.BestTime = (DateTime)dataRowOrder["BestTime"];
            }

            if (dataRowOrder["EstimateCloseCommission"] != DBNull.Value)
            {
                constructParams.EstimateCloseCommission = (decimal)dataRowOrder["EstimateCloseCommission"];
            }

            if (dataRowOrder["EstimateCloseLevy"] != DBNull.Value)
            {
                constructParams.EstimateCloseLevy = (decimal)dataRowOrder["EstimateCloseLevy"];
            }
        }
    }


    internal sealed class AddDataRowFormatOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddDataRowFormatOrderCommandVisitor));
        internal static readonly AddDataRowFormatOrderCommandVisitor Default = new AddDataRowFormatOrderCommandVisitor();

        private AddDataRowFormatOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            DataRowOrderParser.ParseForGeneral(command.ConstructParams, ((Commands.AddDataRowOrderCommand)command).DataRow, command.Tran.InstrumentId, command.Tran.AccountId, null);
            this.CreateOrder(command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            DataRowOrderParser.ParseForPhysical((PhysicalOrderConstructParams)command.ConstructParams, ((Commands.AddDataRowPhysicalOrderCommand)command).DataRow, command.Tran.InstrumentId, command.Tran.AccountId, null);
            this.CreateOrder(command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            DataRowOrderParser.ParseForBO((BOOrderConstructParams)command.ConstructParams, ((Commands.AddDataRowBOOrderCommand)command).DataRow, command.Tran.InstrumentId, command.Tran.AccountId, null);
            this.CreateOrder(command);
        }


        protected override void CreateOrderRelation(Order closeOrder, Commands.AddOrderCommandBase command)
        {
        }
    }


}
