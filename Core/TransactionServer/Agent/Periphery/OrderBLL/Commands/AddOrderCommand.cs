using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using System.Data;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using System.Diagnostics;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.Util.Code;
using Protocal;
using log4net;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Factory;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Periphery.Facades;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Commands
{
    internal abstract class AddOrderCommandBase : Command
    {
        private OrderConstructParams _constructParams;
        private AddOrderCommandServiceBase _commandService;
        private AddOrderCommandVisitorBase _visitor;

        protected AddOrderCommandBase(Transaction tran, AddOrderCommandVisitorBase visitor, AddOrderCommandServiceBase commandService)
        {
            this.Tran = tran;
            _visitor = visitor;
            _commandService = commandService;
        }

        internal Order Result { get; private set; }

        internal Transaction Tran { get; private set; }

        internal Guid InstrumentId
        {
            get { return this.Tran.InstrumentId; }
        }

        internal OrderConstructParams ConstructParams
        {
            get
            {
                if (_constructParams == null)
                {
                    _constructParams = _commandService.CreateConstructParams();
                }
                return _constructParams;
            }
        }

        internal AddOrderRelationFactoryBase AddOrderRelationFactory
        {
            get { return _commandService.OrderRelationAddFactory; }
        }

        internal Order CreateOrder()
        {
            if (this.Result == null)
            {
                this.Result = _commandService.CreateOrder(this.Tran, this.ConstructParams);
            }
            return this.Result;
        }

        internal string GenerateOrderCode()
        {
            return TransactionCodeGenerater.Default.GenerateOrderCode(this.Tran.Owner.Setting().OrganizationId);
        }

        public override void Execute()
        {
            _commandService.Accept(_visitor, this);
        }
    }


    internal abstract class AddDataRowOrderCommandBase : AddOrderCommandBase
    {
        protected AddDataRowOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, IDBRow dr)
            : base(tran, Visitors.AddDataRowFormatOrderCommandVisitor.Default, commandService)
        {
            this.DataRow = dr;
        }

        internal IDBRow  DataRow { get; private set; }
    }


    internal sealed class AddDataRowOrderCommand : AddDataRowOrderCommandBase
    {
        internal AddDataRowOrderCommand(Transaction tran, IDBRow  dr)
            : base(tran, AddOrderCommandService.Default, dr)
        {
        }
    }

    internal sealed class AddDataRowPhysicalOrderCommand : AddDataRowOrderCommandBase
    {
        internal AddDataRowPhysicalOrderCommand(Transaction tran, IDBRow  dr)
            : base(tran, AddPhysicalOrderCommandService.Default, dr)
        {
        }
    }

    internal sealed class AddDataRowBOOrderCommand : AddDataRowOrderCommandBase
    {
        internal AddDataRowBOOrderCommand(Transaction tran,  IDBRow dr)
            : base(tran, AddBOOrderCommandService.Default, dr)
        {
        }
    }


    internal abstract class AddAutoCloseOrderCommandBase : AddOrderCommandBase
    {
        protected AddAutoCloseOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, Visitors.AddAutoCloseOrderCommandVisitor.Default, commandService)
        {
            this.OpenOrder = openOrder;
            this.ClosePrice = closePrice;
            this.TradeOption = tradeOption;
        }

        internal Order OpenOrder { get; private set; }

        internal Price ClosePrice { get; private set; }

        internal TradeOption TradeOption { get; private set; }
    }



    internal sealed class AddAutoCloseOrderCommand : AddAutoCloseOrderCommandBase
    {
        internal AddAutoCloseOrderCommand(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, AddOrderCommandService.Default, openOrder, closePrice, tradeOption)
        {
        }
    }

    internal sealed class AddAutoClosePhysicalOrderCommand : AddAutoCloseOrderCommandBase
    {
        internal AddAutoClosePhysicalOrderCommand(Transaction tran, PhysicalOrder openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, AddOrderCommandService.Default, openOrder, closePrice, tradeOption)
        {
        }
    }


    internal sealed class AddBOCloseOrdeCommand : AddOrderCommandBase
    {
        internal AddBOCloseOrdeCommand(Transaction tran, BinaryOption.Order openOrder)
            : base(tran, Visitors.AddCloseOrderCommandVisitor.Default, AddBOOrderCommandService.Default)
        {
            this.OpenOrder = openOrder;
        }

        internal BinaryOption.Order OpenOrder { get; private set; }
    }


    internal abstract class AddDoneOrderCommandBase : AddOrderCommandBase
    {
        protected AddDoneOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, Visitors.AddDoneOrderCommandVisitor.Default, commandService)
        {
            this.OpenOrder = openOrder;
            this.ClosePrice = closePrice;
            this.TradeOption = tradeOption;
        }

        internal Order OpenOrder { get; private set; }

        internal Price ClosePrice { get; private set; }

        internal TradeOption TradeOption { get; private set; }
    }


    internal sealed class AddDoneOrderCommand : AddDoneOrderCommandBase
    {
        internal AddDoneOrderCommand(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, AddOrderCommandService.Default, openOrder, closePrice, tradeOption)
        {
        }
    }


    internal sealed class AddPhysicalDoneOrderCommand : AddDoneOrderCommandBase
    {
        internal AddPhysicalDoneOrderCommand(Transaction tran, Physical.PhysicalOrder openOrder, Price closePrice, TradeOption tradeOption)
            : base(tran, AddPhysicalOrderCommandService.Default, openOrder, closePrice, tradeOption)
        {
        }
    }


    internal abstract class AddCutOrderCommandBase : AddOrderCommandBase
    {
        protected AddCutOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, bool isBuy, decimal lotBalance, Price setPrice)
            : base(tran, Visitors.AddCutOrderCommandVisitor.Default, commandService)
        {
            this.IsBuy = isBuy;
            this.LotBalance = lotBalance;
            this.SetPrice = setPrice;
        }

        internal bool IsBuy { get; private set; }
        internal decimal LotBalance { get; private set; }
        internal Price SetPrice { get; private set; }
    }


    internal sealed class AddCutOrderCommand : AddCutOrderCommandBase
    {
        internal AddCutOrderCommand(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice)
            : base(tran, AddOrderCommandService.Default, isBuy, lotBalance, setPrice)
        {
        }
    }


    internal sealed class AddPhysicalCutOrderCommand : AddCutOrderCommandBase
    {
        internal AddPhysicalCutOrderCommand(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice)
            : base(tran, AddPhysicalOrderCommandService.Default, isBuy, lotBalance, setPrice)
        {
        }
    }


    internal sealed class AddInstalmentOrderOrderCommand : AddOrderCommandBase
    {
        internal AddInstalmentOrderOrderCommand(Transaction tran, PhysicalOrder oldOrder, decimal lot, bool isOpen, bool isBuy)
            : base(tran, Visitors.AddPhysicalInstalmentOrderCommandVisitor.Default, AddPhysicalOrderCommandService.Default)
        {
            this.OldOrder = oldOrder;
            this.Lot = lot;
            this.IsOpen = isOpen;
            this.IsBuy = isBuy;
        }

        internal PhysicalOrder OldOrder { get; private set; }
        internal decimal Lot { get; private set; }
        internal bool IsOpen { get; private set; }
        internal bool IsBuy { get; private set; }
    }

    internal abstract class AddMultipleCloseOrderCommandBase : AddOrderCommandBase
    {
        protected AddMultipleCloseOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
            : base(tran, AddMultipleCloseOrderCommandVisitor.Default, commandService)
        {
            this.ClosedLot = closedLot;
            this.ExecutePrice = executePrice;
            this.IsBuy = isBuy;
            this.OrderRelations = orderRelations;
        }

        internal decimal ClosedLot { get; private set; }
        internal Price ExecutePrice { get; private set; }
        internal bool IsBuy { get; private set; }
        internal List<OrderRelationRecord> OrderRelations { get; private set; }
    }


    internal sealed class AddMultipleCloseOrderCommand : AddMultipleCloseOrderCommandBase
    {
        internal AddMultipleCloseOrderCommand(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
            : base(tran, AddOrderCommandService.Default, closedLot, executePrice, isBuy, orderRelations)
        {
        }
    }


    internal sealed class AddMultipleClosePhysicalOrderCommand : AddMultipleCloseOrderCommandBase
    {
        internal AddMultipleClosePhysicalOrderCommand(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
            : base(tran, AddPhysicalOrderCommandService.Default, closedLot, executePrice, isBuy, orderRelations)
        {
        }
    }

    internal sealed class AddCancelDeliveryWithShortSellOrderCommand : AddOrderCommandBase
    {
        internal AddCancelDeliveryWithShortSellOrderCommand(Transaction tran, CancelDeliveryWithShortSellOrderParam param)
            : base(tran, Visitors.AddCancelDeliveryWithShortSellOrderCommandVisitor.Default, AddPhysicalOrderCommandService.Default)
        {
            this.IsOpen = param.IsOpen;
            this.IsBuy = param.IsBuy;
            this.SetPrice = param.SetPrice;
            this.ExecutePrice = param.ExecutePrice;
            this.Lot = param.Lot;
            this.LotBalance = param.LotBalance;
            this.PhysicalRequestId = param.PhysicalRequestId;
            this.OrderRelations = param.OrderRelations;
            this.TradeOption = param.TradeOption;
        }
        internal bool IsOpen { get; private set; }
        internal bool IsBuy { get; private set; }
        internal Price SetPrice { get; private set; }
        internal Price ExecutePrice { get; private set; }
        internal decimal Lot { get; private set; }
        internal decimal LotBalance { get; private set; }
        internal TradeOption TradeOption { get; private set; }
        internal Guid PhysicalRequestId { get; private set; }
        internal List<OrderRelationRecord> OrderRelations { get; private set; }
    }


}
