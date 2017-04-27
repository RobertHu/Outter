using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL.Visitors
{
    internal abstract class AddOrderRelationCommandVisitorBase
    {
        internal abstract void VisitGeneralOrderRelationCommand(Commands.AddOrderRelationCommandBase command);
        internal abstract void VisitPhysicalOrderRelationCommand(Commands.AddOrderRelationCommandBase command);
        internal abstract void VisitBOOrderRelationCommand(Commands.AddOrderRelationCommandBase command);
    }


    internal abstract class AddCommunicationOrderRelaitonVistorBase : AddOrderRelationCommandVisitorBase
    {
        protected void Visit(Commands.OrderRelationCommunicationCommandBase command)
        {
            this.ValidateCommon(command);
            this.ValidateLotBalanceCommon(command);
            command.ConstructParams.Fill(command.Account, command.OrderRelationData);
            command.CreateOrderRelation();
        }

        private void ValidateCommon(Commands.OrderRelationCommunicationCommandBase command)
        {
            var openOrder = command.Account.GetOrder(command.OrderRelationData.OpenOrderId);
            if (openOrder == null)
            {
                throw new TransactionServerException(TransactionError.OpenOrderNotExists);
            }
            bool isSameAccount = command.Account == openOrder.Owner.Owner;
            bool isSameInstrument = command.CloseOrder.Owner.SettingInstrument() == openOrder.Owner.SettingInstrument();
            bool isSameDirection = command.CloseOrder.IsBuy == openOrder.IsBuy;
            if (!isSameAccount || !isSameInstrument || !openOrder.IsOpen || isSameDirection)
            {
                StringBuilder sb = new StringBuilder();
                if (!isSameAccount)
                {
                    sb.AppendFormat("is not the same account, close order account id = {0}, open order account id = {1}", command.Account.Id, openOrder.Owner.Owner.Id);
                }

                if (!isSameInstrument)
                {
                    var closeInstrument = command.CloseOrder.Owner.SettingInstrument();
                    var openInstrument = openOrder.Owner.SettingInstrument();
                    sb.AppendFormat("is not the same instrument, close order instrument id = {0}, open order instrument id = {1}", closeInstrument.Id, openInstrument.Id);
                }

                if (!openOrder.IsOpen)
                {
                    sb.AppendFormat("open order is not open id = {0}", openOrder.Id);
                }

                if (isSameDirection)
                {
                    sb.AppendFormat("open order and close order is in the same directory  isbuy = {0}", openOrder.IsBuy);
                }
                throw new TransactionServerException(TransactionError.InvalidOrderRelation, sb.ToString());
            }
        }

        private void ValidateLotBalanceCommon(Commands.OrderRelationCommunicationCommandBase command)
        {
            var openOrder = command.Account.GetOrder(command.OrderRelationData.OpenOrderId);
            var closedLot = command.OrderRelationData.ClosedLot;
            var physicalCloseOrder = command.CloseOrder as Physical.PhysicalOrder;
            if (physicalCloseOrder != null && physicalCloseOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery)
            {
                var physicalOpenOrder = openOrder as Physical.PhysicalOrder;
                Debug.Assert(physicalCloseOrder != null);
                if (physicalOpenOrder.DeliveryLockLot < closedLot)
                {
                    string errorDetail = string.Format("openOrder={0}, closeOrder={1}, DeliveryLockLot = {2}, closedLot = {3}", openOrder.Id, physicalCloseOrder.Id, physicalOpenOrder.DeliveryLockLot, closedLot);
                    throw new TransactionServerException(TransactionError.ExceedOpenLotBalance, errorDetail);
                }
            }
            else
            {
                if (openOrder.LotBalance < closedLot)
                {
                    string errorDetail = string.Format("openOrder={0}, closeOrder={1}, lotBalance = {2}, closedLot = {3}", openOrder.Id, command.CloseOrder.Id, openOrder.LotBalance, closedLot);
                    throw new TransactionServerException(TransactionError.ExceedOpenLotBalance, errorDetail);
                }
            }
        }


    }



}
