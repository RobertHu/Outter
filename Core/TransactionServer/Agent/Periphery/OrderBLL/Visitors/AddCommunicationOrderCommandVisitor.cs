using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddCommunicationOrderCommandVisitor : AddCommunicationOrderCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddCommunicationOrderCommandVisitor));
        public static readonly AddCommunicationOrderCommandVisitor Default = new AddCommunicationOrderCommandVisitor();

        private AddCommunicationOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParseCommon((AddCommunicationOrderCommandBase)command);
            this.CreateOrder(command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParseCommon((AddCommunicationOrderCommandBase)command);
            this.CreateOrder(command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParseCommon((AddCommunicationOrderCommandBase)command);
            this.CreateOrder(command);
        }


        private void ParseCommon(AddCommunicationOrderCommandBase command)
        {
            var constructParams = command.ConstructParams;
            var orderData = (Protocal.OrderData)command.OrderData;
            constructParams.Phase = OrderPhase.Placing;
            constructParams.FillOrderCommonData(orderData, command.InstrumentId, command.TradeDay);
            constructParams.Code = command.GenerateOrderCode();
            constructParams.BlotterCode = base.ParseBlotterCode(command);
            var isQuote = orderData.PriceIsQuote;
            if (isQuote != null && !isQuote.Value && orderData.PriceTimestamp != null)
            {
                constructParams.PriceTimestamp = orderData.PriceTimestamp;
            }

            if (constructParams.IsOpen && orderData.OrderRelations != null && orderData.OrderRelations.Count > 0) // open order don't have order relations
            {
                throw new TransactionServerException(TransactionError.InvalidRelation);
            }

            if (!constructParams.IsOpen)
            {
                this.ValidateForCloseOrder(orderData, constructParams);
            }
        }

        protected override OrderRelationBLL.Commands.AddOrderRelationCommandBase CreateAddOrderRelationCommand(OrderRelationBLL.Factory.AddOrderRelationFactoryBase factory, Order closeOrder, object orderRelationData)
        {
            return factory.Create(closeOrder, (Protocal.OrderRelationData)orderRelationData);
        }
    }
}
