using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
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
    internal abstract class AddOrderCommandVisitorBase
    {
        protected AddOrderCommandVisitorBase()
        {
        }

        public abstract void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command);

        public abstract void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command);

        public abstract void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command);

        protected void CreateOrder(AddOrderCommandBase command)
        {
            Order order = command.CreateOrder();
            if (!order.IsOpen)
            {
                this.CreateOrderRelation(order, command);
            }
        }

        protected abstract void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command);
    }

    internal abstract class AddCommunicationOrderCommandVisitorBase : AddOrderCommandVisitorBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AddCommunicationOrderCommandVisitorBase));

        protected string ParseBlotterCode(AddCommunicationCommandBase command)
        {
            try
            {
                return string.IsNullOrEmpty(command.OrderData.BlotterCode) ?
                        Settings.Setting.Default.SettingInfo.GetBlotter(command.Tran.Owner.Setting().BlotterID.Value).Code :
                       command.OrderData.BlotterCode;
            }
            catch
            {
                return null;
            }
        }

        protected void ValidateForCloseOrder(OrderCommonData orderData, OrderConstructParams constructParams)
        {
            Logger.InfoFormat("closeOrderId = {0}, totalClosedLot = {1}, lot = {2}", orderData.Id, constructParams.LotBalance, orderData.Lot);
            foreach (var eachOrderRelation in orderData.OrderRelations)
            {
                var closedLot = eachOrderRelation.ClosedLot;
                constructParams.LotBalance -= closedLot;
            }
            if (constructParams.LotBalance != 0)
            {
                throw new TransactionServerException(TransactionError.InvalidLotBalance, string.Format("closeOrderId = {0}, leftLotBalance = {1}, lot = {2}", orderData.Id, constructParams.LotBalance, orderData.Lot));
            }
        }

        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
            var orderData = ((AddCommunicationCommandBase)command).OrderData;
            foreach (var eachOrderRelation in orderData.OrderRelations)
            {
                Logger.InfoFormat("begin create order relation openOrderId={0}, closeOrderId={1}, closedLot={2}", eachOrderRelation.OpenOrderId, eachOrderRelation.CloseOrderId, eachOrderRelation.ClosedLot);
                OrderRelationBLL.Commands.AddOrderRelationCommandBase addCommand = this.CreateAddOrderRelationCommand(command.AddOrderRelationFactory, closeOrder, eachOrderRelation);
                addCommand.Execute();
            }
        }

        protected abstract OrderRelationBLL.Commands.AddOrderRelationCommandBase CreateAddOrderRelationCommand(OrderRelationBLL.Factory.AddOrderRelationFactoryBase factory, Order closeOrder, object orderRelationData);

    }

}
