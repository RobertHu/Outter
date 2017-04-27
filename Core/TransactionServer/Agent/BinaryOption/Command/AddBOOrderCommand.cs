using Core.TransactionServer.Agent.BLL.OrderBusiness.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using iExchange.Common;

namespace Core.TransactionServer.Agent.BinaryOption.Command
{
    internal sealed class AddBOOrderCommand : AddOrderCommandBase
    {
        internal AddBOOrderCommand(BOTransaction tran, AddOrderParameterBase orderParam)
            : base(tran, orderParam) { }

        internal override void Accept(AddOrderCommandVisitorBase visitor)
        {
            visitor.VisitAddBOOrderCommand(this);
        }

        protected override BLL.OrderBusiness.OrderConstructParams CreateConstructParams()
        {
            return new BOOrderConstructParams();
        }

        internal override BLL.OrderRelationBusiness.AddOrderRelationFactoryBase AddOrderRelationFactory
        {
            get { return Factory.AddBOOrderRelationFactory.Default; }
        }

        internal override Agent.Order CreateOrder()
        {
            return BOOrderServiceFactory.Default.CreateOrder(this.Tran, this.ConstructParams);
        }
    }
}
