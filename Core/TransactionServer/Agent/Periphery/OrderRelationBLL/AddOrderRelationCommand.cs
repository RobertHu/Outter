using Core.TransactionServer.Agent.BinaryOption.Command;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
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

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL
{
    internal sealed class OpenOrderNotFoundException : Exception
    {
        internal OpenOrderNotFoundException(Guid openOrderId, Guid closeOrderId)
        {
            this.OpenOrderId = openOrderId;
            this.CloseOrderId = closeOrderId;
        }
        internal Guid OpenOrderId { get; private set; }
        internal Guid CloseOrderId { get; private set; }
    }


    public abstract class AddOrderRelationCommandBase : Periphery.Command
    {
        private Order _closeOrder;
        private XElement _xmlOrderRelation;
        private DataRow _dataRowOrderRelation;
        private OrderRelationConstructParams _constructParams;
        private AddOrderRelationCommandVisitorBase _visitor;

        protected AddOrderRelationCommandBase(Order closeOrder, XElement xmlOrderRelation, AddOrderRelationCommandVisitorBase visitor)
            : this(closeOrder, visitor)
        {
            _xmlOrderRelation = xmlOrderRelation;
        }

        protected AddOrderRelationCommandBase(Order closeOrder, Protocal.OrderRelationData orderRelationData, AddOrderRelationCommandVisitorBase visitor)
            : this(closeOrder, visitor)
        {
            this.OrderRelaitonData = orderRelationData;
        }

        protected AddOrderRelationCommandBase(Order closeOrder, DataRow dataRowOrderRelation, AddOrderRelationCommandVisitorBase visitor)
            : this(closeOrder, visitor)
        {
            _dataRowOrderRelation = dataRowOrderRelation;
        }

        protected AddOrderRelationCommandBase(Order closeOrder, Order openOrder, decimal closedLot, AddOrderRelationCommandVisitorBase visitor)
            : this(closeOrder, visitor)
        {
            this.OpenOrder = openOrder;
            this.ClosedLot = closedLot;
        }


        private AddOrderRelationCommandBase(Order closeOrder, AddOrderRelationCommandVisitorBase visitor)
        {
            _closeOrder = closeOrder;
            this.Account = _closeOrder.Owner.Owner;
            _visitor = visitor;
        }

        internal Account Account { get; private set; }

        internal Protocal.OrderRelationData OrderRelaitonData { get; private set; }

        internal Order CloseOrder
        {
            get { return _closeOrder; }
        }

        internal Order OpenOrder { get; private set; }

        internal decimal ClosedLot { get; private set; }


        internal XElement XmlOrderRelation
        {
            get { return _xmlOrderRelation; }
        }

        internal DataRow DataRowOrderRelation
        {
            get { return _dataRowOrderRelation; }
        }

        internal OrderRelationConstructParams ConstructParams
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

        protected abstract OrderRelationConstructParams CreateConstructParams();

        internal abstract OrderRelation CreateOrderRelation();

        protected abstract void Accept(AddOrderRelationCommandVisitorBase visitor);

        public override void Execute()
        {
            this.Accept(_visitor);
        }
    }

    public abstract class AddOrderRelationCommandVisitorBase
    {
        internal abstract void VisitGeneralOrderRelationCommand(AddGeneralOrderRelationCommand command);
        internal abstract void VisitPhysicalOrderRelationCommand(AddPhysicalOrderRelationCommand command);
        internal abstract void VisitBOOrderRelationCommand(AddBOOrderRelationCommand command);
    }

    public sealed class AddCommunicationOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        public static readonly AddCommunicationOrderRelationCommandVisitor Default = new AddCommunicationOrderRelationCommandVisitor();

        private AddCommunicationOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(AddGeneralOrderRelationCommand command)
        {
            this.VisitCommon(command);
        }

        internal override void VisitPhysicalOrderRelationCommand(AddPhysicalOrderRelationCommand command)
        {
            this.VisitCommon(command);
        }

        internal override void VisitBOOrderRelationCommand(AddBOOrderRelationCommand command)
        {
            throw new NotImplementedException();
        }

        private void VisitCommon(AddOrderRelationCommandBase command)
        {
            this.ValidateCommon(command);
            this.ValidateLotBalanceCommon(command);
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }


        private void ValidateCommon(AddOrderRelationCommandBase command)
        {
            var openOrder = command.Account.GetOrder(command.OrderRelaitonData.OpenOrderId);
            if (openOrder == null)
            {
                throw new TransactionServerException(TransactionError.OpenOrderNotExists);
            }
            bool isSameAccount = command.Account == openOrder.Owner.Owner;
            bool isSameInstrument = command.CloseOrder.Owner.SettingInstrument == openOrder.Owner.SettingInstrument;
            bool isSameDirection = command.CloseOrder.IsBuy == openOrder.IsBuy;
            if (!isSameAccount || !isSameInstrument || !openOrder.IsOpen || isSameDirection)
            {
                StringBuilder sb = new StringBuilder();
                if (!isSameAccount)
                {
                    sb.AppendFormat("is not the same account, close order account id = {0}, open order account id = {1}", command.Account.Id, openOrder.Owner.Owner.Id);
                }

                if (!isSameInstrument)
                {
                    var closeInstrument = command.CloseOrder.Owner.SettingInstrument;
                    var openInstrument = openOrder.Owner.SettingInstrument;
                    sb.AppendFormat("is not the same instrument, close order instrument id = {0}, open order instrument id = {1}", closeInstrument.Id, openInstrument.Id);
                }

                if (!openOrder.IsOpen)
                {
                    sb.AppendFormat("open order is not open id = {0}", openOrder.Id);
                }

                if (isSameDirection)
                {
                    sb.AppendFormat("open order and close order is in the same directory  isbuy = {0}", openOrder.IsBuy);
                }
                throw new TransactionServerException(TransactionError.InvalidOrderRelation, sb.ToString());
            }
        }

        private void ValidateLotBalanceCommon(AddOrderRelationCommandBase command)
        {
            var openOrder = command.Account.GetOrder(command.OrderRelaitonData.OpenOrderId);
            var closedLot = command.OrderRelaitonData.ClosedLot;
            if (openOrder.LotBalance < closedLot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
            }
        }

        private void ParseCommon(AddOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.OpenOrder = command.Account.GetOrder(command.OrderRelaitonData.OpenOrderId);
            constructParams.ClosedLot = command.OrderRelaitonData.ClosedLot;
            constructParams.CloseOrder = command.CloseOrder;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
        }

    }

    public sealed class AddXmlFormatOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        internal static readonly AddXmlFormatOrderRelationCommandVisitor Default = new AddXmlFormatOrderRelationCommandVisitor();
        private AddXmlFormatOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(AddGeneralOrderRelationCommand command)
        {
            this.ValidateCommon(command);
            this.ValidateLotBalanceCommon(command);
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        internal override void VisitPhysicalOrderRelationCommand(AddPhysicalOrderRelationCommand command)
        {
            this.ValidateCommon(command);
            this.ValidatePhysicalLotBalance(command);
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        internal override void VisitBOOrderRelationCommand(AddBOOrderRelationCommand command)
        {
            throw new NotImplementedException();
        }

        private void ValidatePhysicalLotBalance(AddOrderRelationCommandBase command)
        {
            var openOrder = this.ParseOpenOrder(command);
            var closedLot = this.ParseClosedLot(command);
            var physicalOpenOrder = (PhysicalOrder)openOrder;
            if (physicalOpenOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery)
            {
                if (physicalOpenOrder.DeliveryLockLot < closedLot)
                {
                    throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
                }
            }
            else
            {
                if (physicalOpenOrder.LotBalance < closedLot)
                {
                    throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
                }
            }
        }


        private void ValidateCommon(AddOrderRelationCommandBase command)
        {
            var openOrder = this.ParseOpenOrder(command);
            if (openOrder == null)
            {
                throw new TransactionServerException(TransactionError.OpenOrderNotExists);
            }
            bool isSameAccount = command.Account == openOrder.Owner.Owner;
            bool isSameInstrument = command.CloseOrder.Owner.SettingInstrument == openOrder.Owner.SettingInstrument;
            bool isSameDirection = command.CloseOrder.IsBuy == openOrder.IsBuy;
            if (!isSameAccount || !isSameInstrument || !openOrder.IsOpen || isSameDirection)
            {
                throw new TransactionServerException(TransactionError.InvalidOrderRelation);
            }
        }

        private void ValidateLotBalanceCommon(AddOrderRelationCommandBase command)
        {
            var openOrder = this.ParseOpenOrder(command);
            var closedLot = this.ParseClosedLot(command);
            if (openOrder.LotBalance < closedLot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
            }
        }


        private void ParseCommon(AddOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.OpenOrder = this.ParseOpenOrder(command);
            constructParams.ClosedLot = this.ParseClosedLot(command);
            constructParams.CloseOrder = command.CloseOrder;
            if (command.XmlOrderRelation.HasAttribute("CloseTime"))
            {
                constructParams.CloseTime = XmlConvert.ToDateTime(command.XmlOrderRelation.Attribute("CloseTime").Value, DateTimeFormat.Xml);
            }
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
        }

        private Order ParseOpenOrder(AddOrderRelationCommandBase command)
        {
            var openOrderId = command.XmlOrderRelation.AttrToGuid("OpenOrderID");
            return command.Account.GetOrder(openOrderId);
        }

        private decimal ParseClosedLot(AddOrderRelationCommandBase command)
        {
            return command.XmlOrderRelation.AttrToDecimal("ClosedLot");
        }

    }

    public sealed class AddDataRowFormatOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddDataRowFormatOrderRelationCommandVisitor));
        internal static readonly AddDataRowFormatOrderRelationCommandVisitor Default = new AddDataRowFormatOrderRelationCommandVisitor();

        private AddDataRowFormatOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(AddGeneralOrderRelationCommand command)
        {
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        internal override void VisitPhysicalOrderRelationCommand(AddPhysicalOrderRelationCommand command)
        {
            this.ParsePhysical(command);
            command.CreateOrderRelation();
        }

        internal override void VisitBOOrderRelationCommand(AddBOOrderRelationCommand command)
        {
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        private void ParsePhysical(AddOrderRelationCommandBase command)
        {
            this.ParseCommon(command);
            var physicalConstructParams = (PhysicalOrderRelationConstructParams)command.ConstructParams;
            var dataRowOrderRelation = command.DataRowOrderRelation;
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

        private void ParseCommon(AddOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            var dataRowOrderRelation = command.DataRowOrderRelation;
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
            constructParams.OperationType = Framework.OperationType.None;
        }
    }


    public sealed class AddGeneralFormatOrderRelationCommandVisitor : AddOrderRelationCommandVisitorBase
    {
        internal static readonly AddGeneralFormatOrderRelationCommandVisitor Default = new AddGeneralFormatOrderRelationCommandVisitor();

        private AddGeneralFormatOrderRelationCommandVisitor() { }

        internal override void VisitGeneralOrderRelationCommand(AddGeneralOrderRelationCommand command)
        {
            this.ParseOrderRelationCommon(command);
        }

        internal override void VisitPhysicalOrderRelationCommand(AddPhysicalOrderRelationCommand command)
        {
            this.ParseOrderRelationCommon(command);
        }

        internal override void VisitBOOrderRelationCommand(AddBOOrderRelationCommand command)
        {
            this.ParseOrderRelationCommon(command);
        }

        private void ParseOrderRelationCommon(AddOrderRelationCommandBase command)
        {
            this.ParseCommon(command);
            command.CreateOrderRelation();
        }

        private void ParseCommon(AddOrderRelationCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.OpenOrder = command.OpenOrder;
            constructParams.CloseOrder = command.CloseOrder;
            constructParams.ClosedLot = command.ClosedLot;
            constructParams.OperationType = Framework.OperationType.AsNewRecord;
        }
    }


}
