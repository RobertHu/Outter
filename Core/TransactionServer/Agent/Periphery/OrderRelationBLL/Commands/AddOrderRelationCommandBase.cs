using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Commands
{
    internal abstract class AddOrderRelationCommandBase : Periphery.Command
    {
        private Order _closeOrder;
        private OrderRelationConstructParams _constructParams;
        private AddOrderRelationCommandVisitorBase _visitor;
        private IAddOrderRelationCommandService _service;

        protected AddOrderRelationCommandBase(Order closeOrder, AddOrderRelationCommandVisitorBase visitor, IAddOrderRelationCommandService commandService)
        {
            _closeOrder = closeOrder;
            _visitor = visitor;
            _service = commandService;
            this.Account = _closeOrder.Owner.Owner;
        }

        internal Account Account { get; private set; }

        internal abstract ILog Logger { get; }

        internal Order CloseOrder
        {
            get { return _closeOrder; }
        }

        internal OrderRelationConstructParams ConstructParams
        {
            get
            {
                if (_constructParams == null)
                {
                    _constructParams = _service.CreateConstructParams();
                }
                return _constructParams;
            }
        }

        internal OrderRelation CreateOrderRelation()
        {
            this.Logger.InfoFormat("CreateOrderRelation accountId = {0}, closeOrderId={1}, openOrderId = {2}, closeLot= {3}", this.Account.Id, this.ConstructParams.CloseOrder.Id, this.ConstructParams.OpenOrder.Id, this.ConstructParams.ClosedLot);
            return _service.CreateOrderRelation(this.ConstructParams);
        }


        public override void Execute()
        {
            _service.Accept(_visitor, this);
        }
    }


    internal abstract class AddDataRowOrderRelationCommandBase : AddOrderRelationCommandBase
    {
        protected AddDataRowOrderRelationCommandBase(Order closeOrder, IAddOrderRelationCommandService commandService, IDBRow dr)
            : base(closeOrder, Visitors.AddDataRowFormatOrderRelationCommandVisitor.Default, commandService)
        {
            this.DataRow = dr;
        }

        internal IDBRow DataRow { get; private set; }
    }



    internal sealed class AddDataRowOrderRelationCommand : AddDataRowOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddDataRowOrderRelationCommand));

        internal AddDataRowOrderRelationCommand(Order closeOrder, IDBRow dr)
            : base(closeOrder, AddOrderRelationCommandService.Default, dr)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }


    internal sealed class AddDataRowPhysicalOrderRelationCommand : AddDataRowOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddDataRowPhysicalOrderRelationCommand));

        internal AddDataRowPhysicalOrderRelationCommand(Physical.PhysicalOrder closeOrder, IDBRow dr)
            : base(closeOrder, AddPhysicalOrderRelationCommandService.Default, dr)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal sealed class AddDataRowBOOrderRelationCommand : AddDataRowOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddDataRowBOOrderRelationCommand));

        internal AddDataRowBOOrderRelationCommand(BinaryOption.Order closeOrder, IDBRow dr)
            : base(closeOrder, AddBOOrderRelationCommandService.Default, dr)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal abstract class AddGeneralOrderRelationCommandBase : AddOrderRelationCommandBase
    {
        protected AddGeneralOrderRelationCommandBase(Order closeOrder, IAddOrderRelationCommandService commandService, Order openOrder, decimal closedLot)
            : base(closeOrder, Visitors.AddGeneralFormatOrderRelationCommandVisitor.Default, commandService)
        {
            this.OpenOrder = openOrder;
            this.ClosedLot = closedLot;
        }

        internal Order OpenOrder { get; private set; }
        internal decimal ClosedLot { get; private set; }
    }


    internal sealed class AddGeneralOrderRelationCommand : AddGeneralOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddGeneralOrderRelationCommand));

        internal AddGeneralOrderRelationCommand(Order closeOrder, Order openOrder, decimal closedLot)
            : base(closeOrder, AddOrderRelationCommandService.Default, openOrder, closedLot)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }


    internal sealed class AddGeneralPhysicalOrderRelationCommand : AddGeneralOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddGeneralPhysicalOrderRelationCommand));

        internal AddGeneralPhysicalOrderRelationCommand(Physical.PhysicalOrder closeOrder, Physical.PhysicalOrder openOrder, decimal closedLot)
            : base(closeOrder, AddPhysicalOrderRelationCommandService.Default, openOrder, closedLot)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }


    internal sealed class AddGeneralBOOrderRelationCommand : AddGeneralOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddGeneralBOOrderRelationCommand));

        internal AddGeneralBOOrderRelationCommand(BinaryOption.Order closeOrder, BinaryOption.Order openOrder, decimal closedLot)
            : base(closeOrder, AddBOOrderRelationCommandService.Default, openOrder, closedLot)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

}
