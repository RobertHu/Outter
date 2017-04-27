using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using System.Data;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class AddPhysicalOrderCommand : AddOrderCommandBase
    {
        internal AddPhysicalOrderCommand(PhysicalTransaction tran, AddOrderParameterBase orderParam)
            :base(tran, orderParam)
        {
        }

        internal override void Accept(AddOrderCommandVisitorBase visitor)
        {
            visitor.VisitAddPhysicalOrderCommand(this);
        }

        protected override OrderConstructParams CreateConstructParams()
        {
            return new PhysicalOrderConstructParams();
        }

        internal override BLL.OrderRelationBusiness.AddOrderRelationFactoryBase AddOrderRelationFactory
        {
            get { return OrderRelationBusiness.AddPhysicalOrderRelationFactory.Default; }
        }

        internal override Order CreateOrder()
        {
            return PhysicalOrderServiceFactory.Default.CreateOrder(this.Tran, this.ConstructParams);
        }
    }
}
