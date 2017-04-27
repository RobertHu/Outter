using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands
{
    internal abstract class AddTranCommandBase : Command
    {
        private TransactionConstructParams _constructParams;
        private Visitors.AddTransactionCommandVisitorBase _visitor;
        private IAddTranCommandService _addTranCommandService;

        protected AddTranCommandBase(Account account, Visitors.AddTransactionCommandVisitorBase visitor, IAddTranCommandService addTranCommandService)
        {
            this.Account = account;
            _visitor = visitor;
            _addTranCommandService = addTranCommandService;
        }

        internal TransactionConstructParams ConstructParams
        {
            get
            {
                if (_constructParams == null)
                {
                    _constructParams = _addTranCommandService.CreateConstructParams();
                }
                return _constructParams;
            }
        }

        public Transaction Result { get; private set; }

        internal Account Account { get; private set; }

        internal AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get
            {
                return _addTranCommandService.AddOrderCommandFactory;
            }
        }

        public override void Execute()
        {
            _addTranCommandService.Accept(_visitor, this);
        }

        internal Transaction CreateTransaction()
        {
            if (this.Result != null) return this.Result;
            this.Result = _addTranCommandService.CreateTransaction(this.Account, this.ConstructParams);
            return this.Result;
        }

        internal string GenerateTransactionCode(OrderType orderType)
        {
            return TransactionCodeGenerater.Default.GenerateTranCode(this.Account.Setting().OrganizationId, orderType);
        }

    }


    internal abstract class AddDataRowTransactionCommandBase : AddTranCommandBase
    {
        protected AddDataRowTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, IDBRow dr, Framework.OperationType operationType)
            : base(account, Visitors.AddDataRowFormatTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.DataRow = dr;
            this.OperationType = operationType;
        }

        internal IDBRow DataRow { get; private set; }
        internal Framework.OperationType OperationType { get; private set; }

    }

    internal sealed class AddDataRowTransactionCommand : AddDataRowTransactionCommandBase
    {
        internal AddDataRowTransactionCommand(Account account, IDBRow dr, Framework.OperationType operationType)
            : base(account, AddTransactionCommandService.Default, dr, operationType)
        {
        }
    }


    internal sealed class AddDataRowPhysicalTransactionCommand : AddDataRowTransactionCommandBase
    {
        internal AddDataRowPhysicalTransactionCommand(Account account, IDBRow dr, Framework.OperationType operationType)
            : base(account, AddPhysicalTransactionCommandService.Default, dr, operationType)
        {
        }
    }




    internal abstract class AddAutoCloseTransactionCommandBase : AddTranCommandBase
    {
        protected AddAutoCloseTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Order openOrder, Price closePrice, OrderType orderType)
            : base(account, Visitors.AddAutoCloseTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.OpenOrder = openOrder;
            this.ClosePrice = closePrice;
            this.OrderType = orderType;
        }

        internal Order OpenOrder { get; private set; }

        internal Price ClosePrice { get; private set; }

        internal OrderType OrderType { get; private set; }
    }


    internal sealed class AddAutoCloseTransactionCommand : AddAutoCloseTransactionCommandBase
    {
        internal AddAutoCloseTransactionCommand(Account account, Order openOrder, Price closePrice, OrderType orderType)
            : base(account, AddTransactionCommandService.Default, openOrder, closePrice, orderType)
        {
        }
    }

    internal sealed class AddAutoClosePhysicalTransactionCommand : AddAutoCloseTransactionCommandBase
    {
        internal AddAutoClosePhysicalTransactionCommand(Account account, Order openOrder, Price closePrice, OrderType orderType)
            : base(account, AddPhysicalTransactionCommandService.Default, openOrder, closePrice, orderType)
        {
        }
    }


    internal abstract class AddCloseTransactionCommandBase : AddTranCommandBase
    {
        protected AddCloseTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Order openOrder)
            : base(account, Visitors.AddCloseTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.OpenOrder = openOrder;
        }

        internal Order OpenOrder { get; private set; }
    }


    internal sealed class AddCloseBOTransactionCommand : AddCloseTransactionCommandBase
    {
        internal AddCloseBOTransactionCommand(Account account, BinaryOption.Order openOrder)
            : base(account, AddBOTransactionCommandService.Default, openOrder)
        {
        }
    }



    internal abstract class AddDoneTransactionCommandBase : AddTranCommandBase
    {
        protected AddDoneTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
            : base(account, Visitors.AddDoneTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.IfTran = ifTran;
            this.SourceOrderId = sourceOrderId;
            this.LimitPrice = limitPrice;
            this.StopPrice = stopPrice;
        }

        internal Transaction IfTran { get; private set; }

        internal Guid SourceOrderId { get; private set; }

        internal Price LimitPrice { get; private set; }

        internal Price StopPrice { get; private set; }
    }


    internal sealed class AddDoneTransactionCommand : AddDoneTransactionCommandBase
    {
        internal AddDoneTransactionCommand(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
            : base(account, AddTransactionCommandService.Default, ifTran, sourceOrderId, limitPrice, stopPrice)
        {
        }
    }

    internal sealed class AddDonePhysicalTransactionCommand : AddDoneTransactionCommandBase
    {
        internal AddDonePhysicalTransactionCommand(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
            : base(account, AddPhysicalTransactionCommandService.Default, ifTran, sourceOrderId, limitPrice, stopPrice)
        {
        }
    }


    internal abstract class AddCutTransactionCommandBase : AddTranCommandBase
    {
        protected AddCutTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
            : base(account, Visitors.AddCutTransactionCommandVisitor.Default, addTranCommandService)
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

    internal sealed class AddCutTransactionCommand : AddCutTransactionCommandBase
    {
        internal AddCutTransactionCommand(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
            : base(account, AddTransactionCommandService.Default, instrument, lotBalanceSum, setPrice, isBuy)
        {
        }
    }

    internal sealed class AddCutPhysicalTransactionCommand : AddCutTransactionCommandBase
    {
        internal AddCutPhysicalTransactionCommand(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
            : base(account, AddPhysicalTransactionCommandService.Default, instrument, lotBalanceSum, setPrice, isBuy)
        {
        }
    }

    internal sealed class AddPhysicalInstalmentTransactionCommand : AddTranCommandBase
    {
        internal AddPhysicalInstalmentTransactionCommand(Account account, Transaction oldTran, Guid sourceOrderId, Physical.PhysicalOrder oldOrder, bool isBuy, bool isOpen, decimal lot)
            : base(account, Visitors.AddPhysicalInstalmentTransactionCommandVisitor.Default, AddPhysicalTransactionCommandService.Default)
        {
            this.OldTran = oldTran;
            this.SourceOrderId = sourceOrderId;
            this.OldOrder = oldOrder;
            this.IsBuy = isBuy;
            this.IsOpen = isOpen;
            this.Lot = lot;
        }

        internal Transaction OldTran { get; private set; }
        internal Guid SourceOrderId { get; private set; }
        internal Physical.PhysicalOrder OldOrder { get; private set; }
        internal bool IsBuy { get; private set; }
        internal bool IsOpen { get; private set; }
        internal decimal Lot { get; private set; }
        internal DateTime? BaseTime { get; set; }

    }



    internal abstract class AddMultipleCloseTranCommandBase : AddTranCommandBase
    {
        protected AddMultipleCloseTranCommandBase(Account account, IAddTranCommandService commandService, Guid instrumentId, decimal contractSize, Guid submitorId)
            : base(account, Visitors.AddMultipleCloseTranCommandVisitor.Default, commandService)
        {
            this.InstrumentId = instrumentId;
            this.ContractSize = contractSize;
            this.SubmitorId = submitorId;
        }

        internal Guid InstrumentId { get; private set; }
        internal decimal ContractSize { get; private set; }
        internal Guid SubmitorId { get; private set; }
    }

    internal class AddMultipleCloseTranCommand : AddMultipleCloseTranCommandBase
    {
        internal AddMultipleCloseTranCommand(Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
            : base(account, AddTransactionCommandService.Default, instrumentId, contractSize, submitorId)
        {
        }
    }


    internal class AddMultipleClosePhysicalTranCommand : AddMultipleCloseTranCommandBase
    {
        internal AddMultipleClosePhysicalTranCommand(Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
            : base(account, AddPhysicalTransactionCommandService.Default, instrumentId, contractSize, submitorId)
        {
        }
    }


    internal sealed class AddCancelDeliveryWithShortSellTranCommand : AddTranCommandBase
    {
        internal AddCancelDeliveryWithShortSellTranCommand(Account account, Guid instrumentId, decimal contractSize)
            : base(account, Visitors.AddCancelDeliveryWithShortSellTransactionCommandVisitor.Default, AddPhysicalTransactionCommandService.Default)
        {
            this.InstrumentId = instrumentId;
            this.ContractSize = contractSize;
        }

        internal Guid InstrumentId { get; private set; }
        internal decimal ContractSize { get; private set; }
    }



}
