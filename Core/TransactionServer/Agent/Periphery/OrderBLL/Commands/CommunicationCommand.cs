using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Commands
{
    internal abstract class AddCommunicationCommandBase : AddOrderCommandBase
    {
        protected AddCommunicationCommandBase(Transaction tran, Visitors.AddCommunicationOrderCommandVisitorBase visitor, AddOrderCommandServiceBase commandService, Protocal.OrderCommonData orderData, DateTime? tradeDay)
            : base(tran, visitor, commandService)
        {
            this.OrderData = orderData;
            this.TradeDay = tradeDay;
        }

        internal Protocal.OrderCommonData OrderData { get; private set; }
        internal DateTime? TradeDay { get; private set; }
    }


    internal abstract class AddCommunicationOrderCommandBase : AddCommunicationCommandBase
    {
        protected AddCommunicationOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Protocal.OrderData orderData)
            : base(tran, Visitors.AddCommunicationOrderCommandVisitor.Default, commandService, orderData, null)
        {
        }
    }


    internal sealed class AddCommunicationOrderCommand : AddCommunicationOrderCommandBase
    {
        internal AddCommunicationOrderCommand(Transaction tran, Protocal.OrderData orderData)
            : base(tran, AddOrderCommandService.Default, orderData)
        {
        }
    }


    internal sealed class AddCommunicationBOOrderCommand : AddCommunicationOrderCommandBase
    {
        internal AddCommunicationBOOrderCommand(Transaction tran, Protocal.BOOrderData orderData)
            : base(tran, AddBOOrderCommandService.Default, orderData)
        {
        }
    }




    internal sealed class AddCommunicationPhysicalOrderCommand : AddCommunicationOrderCommandBase
    {
        internal AddCommunicationPhysicalOrderCommand(Transaction tran, Protocal.Physical.PhysicalOrderData orderData)
            : base(tran, AddPhysicalOrderCommandService.Default, orderData)
        {
        }
    }


    internal abstract class AddBookWithNoCalculationOrderCommandBase : AddCommunicationCommandBase
    {
        protected AddBookWithNoCalculationOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Protocal.OrderBookData orderData, DateTime? tradeDay)
            : base(tran, Visitors.AddBookWithNoCalculationOrderCommandVisitor.Default, commandService, orderData, tradeDay)
        {
        }
    }

    internal sealed class AddBookWithNoCalculationOrderCommand : AddBookWithNoCalculationOrderCommandBase
    {
        internal AddBookWithNoCalculationOrderCommand(Transaction tran, Protocal.OrderBookData orderData, DateTime? tradeDay)
            : base(tran, AddBookOrderCommandService.Default, orderData, tradeDay)
        {
        }
    }

    internal sealed class AddBookWithNoCalculationPhysicalOrderCommand : AddBookWithNoCalculationOrderCommandBase
    {
        internal AddBookWithNoCalculationPhysicalOrderCommand(Transaction tran, Protocal.Physical.PhysicalOrderBookData orderData, DateTime? tradeDay)
            : base(tran, AddBookPhysicalOrderCommandService.Default, orderData, tradeDay)
        {
        }
    }

    internal abstract class AddBookOrderCommandBase : AddCommunicationCommandBase
    {
        protected AddBookOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Protocal.OrderBookData orderData, DateTime? tradeDay)
            : base(tran, Visitors.AddBookOrderCommandVisitor.Default, commandService, orderData, tradeDay)
        {
        }
    }


    internal sealed class AddBookOrderCommand : AddBookOrderCommandBase
    {
        internal AddBookOrderCommand(Transaction tran, Protocal.OrderBookData orderData, DateTime? tradeDay)
            : base(tran, AddOrderCommandService.Default, orderData, tradeDay)
        {
        }
    }


    internal sealed class AddBookPhysicalOrderCommand : AddBookOrderCommandBase
    {
        internal AddBookPhysicalOrderCommand(Transaction tran, Protocal.Physical.PhysicalOrderBookData orderData, DateTime? tradeDay)
            : base(tran, AddPhysicalOrderCommandService.Default, orderData, tradeDay)
        {
        }
    }


}
