using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Data;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Commands
{
    internal sealed class AddGeneralOrderCommand : AddOrderCommandBase
    {
        internal AddGeneralOrderCommand(Transaction tran, AddOrderParameterBase addOrderParameter)
            : base(tran, addOrderParameter)
        {
        }
        protected override OrderConstructParams CreateConstructParams()
        {
            return new OrderConstructParams();
        }

        internal override Order CreateOrder()
        {
            return GeneralOrderServiceFactory.Default.CreateOrder(this.Tran, this.ConstructParams);
        }

        internal override void Accept(AddOrderCommandVisitorBase visitor)
        {
            visitor.VisitAddGeneralOrderCommand(this);
        }

        internal override AddOrderRelationFactoryBase AddOrderRelationFactory
        {
            get { return AddGeneralOrderRelationFactory.Default; }
        }

    }
}
