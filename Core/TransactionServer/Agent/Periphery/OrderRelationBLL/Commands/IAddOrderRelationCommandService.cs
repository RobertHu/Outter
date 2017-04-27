using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Commands
{
    internal interface IAddOrderRelationCommandService
    {
        OrderRelationConstructParams CreateConstructParams();

        OrderRelation CreateOrderRelation(OrderRelationConstructParams constructParams);

        void Accept(AddOrderRelationCommandVisitorBase visitor, AddOrderRelationCommandBase command);
    }


    internal sealed class AddOrderRelationCommandService : IAddOrderRelationCommandService
    {
        internal static readonly AddOrderRelationCommandService Default = new AddOrderRelationCommandService();

        static AddOrderRelationCommandService() { }
        private AddOrderRelationCommandService() { }

        public OrderRelationConstructParams CreateConstructParams()
        {
            return new OrderRelationConstructParams();
        }

        public OrderRelation CreateOrderRelation(OrderRelationConstructParams constructParams)
        {
            return new OrderRelation(constructParams);
        }

        public void Accept(AddOrderRelationCommandVisitorBase visitor, AddOrderRelationCommandBase command)
        {
            visitor.VisitGeneralOrderRelationCommand(command);
        }
    }


    internal sealed class AddPhysicalOrderRelationCommandService : IAddOrderRelationCommandService
    {
        internal static readonly AddPhysicalOrderRelationCommandService Default = new AddPhysicalOrderRelationCommandService();

        static AddPhysicalOrderRelationCommandService() { }
        private AddPhysicalOrderRelationCommandService() { }

        public OrderRelationConstructParams CreateConstructParams()
        {
            return new PhysicalOrderRelationConstructParams();
        }

        public OrderRelation CreateOrderRelation(OrderRelationConstructParams constructParams)
        {
            return new Physical.PhysicalOrderRelation((PhysicalOrderRelationConstructParams)constructParams);
        }

        public void Accept(AddOrderRelationCommandVisitorBase visitor, AddOrderRelationCommandBase command)
        {
            visitor.VisitPhysicalOrderRelationCommand(command);
        }
    }


    internal sealed class AddBOOrderRelationCommandService : IAddOrderRelationCommandService
    {
        internal static readonly AddBOOrderRelationCommandService Default = new AddBOOrderRelationCommandService();

        static AddBOOrderRelationCommandService() { }
        private AddBOOrderRelationCommandService() { }

        public OrderRelationConstructParams CreateConstructParams()
        {
            return new OrderRelationConstructParams();
        }

        public OrderRelation CreateOrderRelation(OrderRelationConstructParams constructParams)
        {
            return new BinaryOption.BOOrderRelation(constructParams);
        }

        public void Accept(AddOrderRelationCommandVisitorBase visitor, AddOrderRelationCommandBase command)
        {
            visitor.VisitBOOrderRelationCommand(command);
        }
    }

}
