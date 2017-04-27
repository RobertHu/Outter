using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Factory;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Commands
{
    internal abstract class AddOrderCommandServiceBase
    {
        public abstract AddOrderRelationFactoryBase OrderRelationAddFactory { get; }
        public abstract OrderConstructParams CreateConstructParams();
        public Order CreateOrder(Transaction tran, OrderConstructParams constructParams)
        {
            return this.OrderServiceFactoryBase.CreateOrder(tran, constructParams);
        }

        protected abstract Factory.OrderServiceFactoryBase OrderServiceFactoryBase { get; }

        public abstract void Accept(Visitors.AddOrderCommandVisitorBase visitor, AddOrderCommandBase command);
    }


    internal abstract class AddGernaralOrderCommandServiceBase : AddOrderCommandServiceBase
    {
        public override AddOrderRelationFactoryBase OrderRelationAddFactory
        {
            get { return AddOrderRelationFactory.Default; }
        }

        public override OrderConstructParams CreateConstructParams()
        {
            return new OrderConstructParams();
        }

        public override void Accept(Visitors.AddOrderCommandVisitorBase visitor, AddOrderCommandBase command)
        {
            visitor.VisitAddGeneralOrderCommand(command);
        }
    }



    internal sealed class AddOrderCommandService : AddGernaralOrderCommandServiceBase
    {
        internal static readonly AddOrderCommandService Default = new AddOrderCommandService();

        static AddOrderCommandService() { }
        private AddOrderCommandService() { }

        protected override Factory.OrderServiceFactoryBase OrderServiceFactoryBase
        {
            get { return Factory.GeneralOrderServiceFactory.Default; }
        }
    }



    internal sealed class AddBookOrderCommandService : AddGernaralOrderCommandServiceBase
    {
        internal static readonly AddBookOrderCommandService Default = new AddBookOrderCommandService();

        static AddBookOrderCommandService() { }
        private AddBookOrderCommandService() { }

        protected override Factory.OrderServiceFactoryBase OrderServiceFactoryBase
        {
            get { return Factory.OrderBookServiceFactory.Default; }
        }
    }


    internal abstract class AddPhysicalOrderCommandServiceBase : AddOrderCommandServiceBase
    {
        public override AddOrderRelationFactoryBase OrderRelationAddFactory
        {
            get { return AddPhysicalOrderRelationFactory.Default; }
        }

        public override OrderConstructParams CreateConstructParams()
        {
            return new PhysicalOrderConstructParams();
        }

        public override void Accept(Visitors.AddOrderCommandVisitorBase visitor, AddOrderCommandBase command)
        {
            visitor.VisitAddPhysicalOrderCommand(command);
        }
    }


    internal sealed class AddPhysicalOrderCommandService : AddPhysicalOrderCommandServiceBase
    {
        internal static readonly AddPhysicalOrderCommandService Default = new AddPhysicalOrderCommandService();

        static AddPhysicalOrderCommandService() { }
        private AddPhysicalOrderCommandService() { }

        protected override Factory.OrderServiceFactoryBase OrderServiceFactoryBase
        {
            get { return Factory.PhysicalOrderServiceFactory.Default; }
        }
    }


    internal sealed class AddBookPhysicalOrderCommandService : AddPhysicalOrderCommandServiceBase
    {
        internal static readonly AddBookPhysicalOrderCommandService Default = new AddBookPhysicalOrderCommandService();

        static AddBookPhysicalOrderCommandService() { }
        private AddBookPhysicalOrderCommandService() { }

        protected override Factory.OrderServiceFactoryBase OrderServiceFactoryBase
        {
            get { return Factory.PhysicalOrderBookServiceFactory.Default; }
        }
    }

    internal sealed class AddBOOrderCommandService : AddOrderCommandServiceBase
    {
        internal static readonly AddBOOrderCommandService Default = new AddBOOrderCommandService();

        static AddBOOrderCommandService() { }
        private AddBOOrderCommandService() { }

        public override AddOrderRelationFactoryBase OrderRelationAddFactory
        {
            get { return AddBOOrderRelationFactory.Default; }
        }

        public override OrderConstructParams CreateConstructParams()
        {
            return new BOOrderConstructParams();
        }

        public override void Accept(Visitors.AddOrderCommandVisitorBase visitor, AddOrderCommandBase command)
        {
            visitor.VisitAddBOOrderCommand(command);
        }

        protected override Factory.OrderServiceFactoryBase OrderServiceFactoryBase
        {
            get { return Factory.BOOrderServiceFactory.Default; }
        }
    }

}
