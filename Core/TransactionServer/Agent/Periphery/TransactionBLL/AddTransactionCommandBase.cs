using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BinaryOption.Command;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.Commands;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Util.Code;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL
{
    public sealed class CutTransactionParams
    {
        internal CutTransactionParams(Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
        {
            this.Instrument = instrument;
            this.LotBalanceSum = lotBalanceSum;
            this.SetPrice = setPrice;
            this.IsBuy = isBuy;
        }

        internal Instrument Instrument { get; private set; }
        internal decimal LotBalanceSum { get; private set; }
        internal Price SetPrice { get; private set; }
        internal bool IsBuy { get; private set; }
    }

    public abstract class AddTransactionCommandBase : Command
    {
        private TransactionConstructParams _constructParams;
        private AddTransactionCommandVisitorBase _visitor;

        protected AddTransactionCommandBase(Account account, XElement xmlTran, bool placeByRiskMonitor, AddTransactionCommandVisitorBase visitor)
            : this(account, placeByRiskMonitor, visitor)
        {
            if (!xmlTran.HasElements) throw new TransactionServerException(TransactionError.HasNoOrders);
            this.XmlTran = xmlTran;
        }

        protected AddTransactionCommandBase(Account account, Protocal.TransactionData tran, AddTransactionCommandVisitorBase visitor)
            : this(account, tran.PlaceByRiskMonitor, visitor)
        {
            this.TransactionData = tran;
        }


        protected AddTransactionCommandBase(Account account, DataRow dataRowTran, AddTransactionCommandVisitorBase visitor)
            : this(account, false, visitor)
        {
            this.DataRowTran = dataRowTran;
        }

        protected AddTransactionCommandBase(Account account, Order openOrder, Price closePrice, OrderType orderType, AddTransactionCommandVisitorBase visitor)
            : this(account, false, visitor)
        {
            this.OpenOrder = openOrder;
            this.ClosePrice = closePrice;
            this.OrderType = orderType;
        }

        protected AddTransactionCommandBase(Account account, Order openOrder, AddTransactionCommandVisitorBase visitor)
            : this(account, false, visitor)
        {
            this.OpenOrder = openOrder;
        }

        protected AddTransactionCommandBase(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice, AddTransactionCommandVisitorBase visitor)
            : this(account, false, visitor)
        {
            this.IfTran = ifTran;
            this.SourceOrderId = sourceOrderId;
            this.LimitPrice = limitPrice;
            this.StopPrice = stopPrice;
        }

        protected AddTransactionCommandBase(Account account, CutTransactionParams cutTransactionParams, AddTransactionCommandVisitorBase visitor)
            : this(account, false, visitor)
        {
            this.CutTransactionParams = cutTransactionParams;
        }


        protected AddTransactionCommandBase(Account account, bool placeByRiskMonitor, AddTransactionCommandVisitorBase visitor)
        {
            this.Account = account;
            this.PlaceByRiskMonitor = placeByRiskMonitor;
            _visitor = visitor;
        }

        internal Protocal.TransactionData TransactionData { get; private set; }

        internal Account Account { get; private set; }

        internal DataRow DataRowTran { get; private set; }

        internal XElement XmlTran { get; private set; }

        internal bool PlaceByRiskMonitor { get; private set; }

        internal Order OpenOrder { get; private set; }

        internal Price ClosePrice { get; private set; }

        internal OrderType OrderType { get; private set; }

        internal Transaction IfTran { get; private set; }

        internal Guid SourceOrderId { get; private set; }

        internal Price LimitPrice { get; private set; }

        internal Price StopPrice { get; private set; }

        internal CutTransactionParams CutTransactionParams { get; private set; }

        internal TransactionConstructParams ConstructParams
        {
            get
            {
                if (_constructParams == null)
                {
                    _constructParams = this.CreateConstructParams();
                }
                return _constructParams;
            }
        }

        public Transaction Result { get; set; }

        internal abstract AddOrderCommandFactoryBase AddOrderCommandFactory { get; }

        public override void Execute()
        {
            this.Accept(_visitor);
        }

        protected abstract TransactionConstructParams CreateConstructParams();


        internal abstract Transaction CreateTransaction();

        internal abstract void Accept(AddTransactionCommandVisitorBase visitor);
    }

    public abstract class AddTransactionCommandVisitorBase
    {
        internal abstract void VisitAddGeneralTransactionCommand(AddGeneralTransactionCommand command);
        internal abstract void VisitAddPhysicalTransactionCommand(AddPhysicalTransactionCommand command);
        internal abstract void VisitAddBOTransactionCommand(AddBOTransactionCommand command);
    }

    public sealed class AddCommunicationTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        public static readonly AddCommunicationTransactionCommandVisitor Default = new AddCommunicationTransactionCommandVisitor();

        private AddCommunicationTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(AddGeneralTransactionCommand command)
        {
            this.ParseTransaction(command);
        }

        internal override void VisitAddPhysicalTransactionCommand(AddPhysicalTransactionCommand command)
        {
            this.ParseTransaction(command);
        }

        internal override void VisitAddBOTransactionCommand(AddBOTransactionCommand command)
        {
            throw new NotImplementedException();
        }


        private void ParseTransaction(AddTransactionCommandBase command)
        {
            this.ParseCommon(command);
            var tran = command.CreateTransaction();
            command.Result = tran;
            this.CreateOrders(command);
            if (tran.SubType == TransactionSubType.IfDone)
            {
                IfDoneTransactionParser.Default.FillDoneTrans(tran, command.TransactionData);
            }
            else if (tran.SubType == TransactionSubType.Amend && this.IsChangeToIfDone(command.TransactionData))
            {
                IfDoneTransactionParser.Default.FillDoneTrans(tran, command.TransactionData);
                tran.SubType = TransactionSubType.IfDone;
            }
            TransactionCodeGenerater.Default.FillTranAndOrderCode(tran);
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

        private void CreateOrders(AddTransactionCommandBase command)
        {
            var tranData = command.TransactionData;
            if (tranData.Orders == null || tranData.Orders.Count == 0) return;
            var tran = command.Result;
            foreach (var eachOrder in tranData.Orders)
            {
                var addOrderCommand = command.AddOrderCommandFactory.CreateByCommunication(tran, new AddCommunicationOrderParameter(eachOrder,tranData.TradeDay));
                addOrderCommand.Execute();
            }

            if (command.PlaceByRiskMonitor)
            {
                this.SetOrderWhenPlaceByRiskMonitor(tran);
            }
        }

        private void SetOrderWhenPlaceByRiskMonitor(Transaction tran)
        {
            foreach (var eachOrder in tran.Orders)
            {
                eachOrder.PlacedByRiskMonitor = true;
                eachOrder.DisableAcceptLmtVariation = tran.FreeLmtVariationCheck;
            }
        }


        private void ParseCommon(AddTransactionCommandBase command)
        {
            TransactionConstructParams constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
            var transactionData = command.TransactionData;
            constructParams.Id = transactionData.Id;
            constructParams.Code = transactionData.Code;
            constructParams.InstrumentId = transactionData.InstrumentId;
            constructParams.Type = transactionData.Type;
            constructParams.SubType = transactionData.SubType;
            constructParams.Phase = TransactionPhase.Placing;
            constructParams.OrderType = transactionData.OrderType;
            constructParams.FreePlacingPreCheck = transactionData.FreePlacingPreCheck;
            constructParams.FreeLmtVariationCheck = transactionData.FreeLmtVariationCheck;
            constructParams.BeginTime = transactionData.BeginTime;
            constructParams.EndTime = transactionData.EndTime;
            constructParams.ExpireType = transactionData.ExpireType;
            constructParams.SubmitorId = transactionData.SubmitorId;
            constructParams.SubmitTime = transactionData.SubmitTime;
            constructParams.SourceOrderId = transactionData.SourceOrderId;
        }

    }




    public sealed class AddDataRowFormatTransactionCommandVisitor : AddTransactionCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddDataRowFormatTransactionCommandVisitor));

        internal static readonly AddDataRowFormatTransactionCommandVisitor Default = new AddDataRowFormatTransactionCommandVisitor();

        private AddDataRowFormatTransactionCommandVisitor() { }

        internal override void VisitAddGeneralTransactionCommand(AddGeneralTransactionCommand command)
        {
            this.ParseTransactionCommon(command);
        }

        internal override void VisitAddPhysicalTransactionCommand(AddPhysicalTransactionCommand command)
        {
            this.ParseTransactionCommon(command);
        }

        internal override void VisitAddBOTransactionCommand(AddBOTransactionCommand command)
        {
            this.ParseTransactionCommon(command);
        }

        private void ParseTransactionCommon(AddTransactionCommandBase command)
        {
            this.ParseCommon(command);
            var tran = command.CreateTransaction();
            command.Result = tran;
        }

        private void ParseCommon(AddTransactionCommandBase command)
        {
            var dataRowTran = command.DataRowTran;
            var constructParams = command.ConstructParams;
            constructParams.OperationType = Framework.OperationType.None;
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
            constructParams.SourceOrderId = dataRowTran.GetColumn<Guid?>("AssigningOrderID");
        }
    }
}
