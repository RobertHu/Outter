using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Commands
{
    internal abstract class OrderRelationCommunicationCommandBase : AddOrderRelationCommandBase
    {
        protected OrderRelationCommunicationCommandBase(Order closeOrder, Visitors.AddCommunicationOrderRelaitonVistorBase visitor, IAddOrderRelationCommandService commandService, Protocal.OrderRelationData orderRelationData)
            : base(closeOrder, visitor, commandService)
        {
            this.OrderRelationData = orderRelationData;
        }

        internal Protocal.OrderRelationData OrderRelationData { get; private set; }
    }



    internal abstract class AddCommunicationOrderRelationCommandBase : OrderRelationCommunicationCommandBase
    {
        protected AddCommunicationOrderRelationCommandBase(Order closeOrder, IAddOrderRelationCommandService commandService, Protocal.OrderRelationData orderRelationData)
            : base(closeOrder, Visitors.AddCommunicationOrderRelationCommandVisitor.Default, commandService, orderRelationData)
        {
        }


    }

    internal sealed class AddCommunicationOrderRelationCommand : AddCommunicationOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddCommunicationOrderRelationCommand));

        internal AddCommunicationOrderRelationCommand(Order closeOrder, Protocal.OrderRelationData orderRelationData)
            : base(closeOrder, AddOrderRelationCommandService.Default, orderRelationData)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal sealed class AddCommunicationPhysicalOrderRelationCommand : AddCommunicationOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddCommunicationPhysicalOrderRelationCommand));

        internal AddCommunicationPhysicalOrderRelationCommand(Physical.PhysicalOrder closeOrder, Protocal.OrderRelationData orderRelationData)
            : base(closeOrder, AddPhysicalOrderRelationCommandService.Default, orderRelationData)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal abstract class AddBookOrderRelationCommandBase : OrderRelationCommunicationCommandBase
    {
        protected AddBookOrderRelationCommandBase(Order closeOrder, IAddOrderRelationCommandService commandService, Protocal.OrderRelationBookData orderRelationData)
            : base(closeOrder, Visitors.AddBookOrderRelationCommandVisitor.Default, commandService, orderRelationData)
        {
        }
    }

    internal sealed class AddBookOrderRelationCommand : AddBookOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddBookOrderRelationCommand));
        internal AddBookOrderRelationCommand(Order closeOrder, Protocal.OrderRelationBookData orderRelationData)
            : base(closeOrder, AddOrderRelationCommandService.Default, orderRelationData)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }


    internal sealed class AddBookPhysicalOrderRelationCommand : AddBookOrderRelationCommandBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AddBookPhysicalOrderRelationCommand));
        internal AddBookPhysicalOrderRelationCommand(Physical.PhysicalOrder closeOrder, Protocal.OrderRelationBookData orderRelationData)
            : base(closeOrder, AddPhysicalOrderRelationCommandService.Default, orderRelationData)
        {
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }
    }

}
