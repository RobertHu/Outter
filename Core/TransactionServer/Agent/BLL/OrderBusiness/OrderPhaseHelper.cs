using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.Reset;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class OrderPhaseHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderPhaseHelper));

        internal static void UpdateCloseOrderPhase(this Order order, IEnumerable<OrderRelation> orderRelations, DateTime tradeDay, Guid instrumentId, DateTime? resetTime)
        {
            if (order.ShouldUpdateCloseOrder(order.OrderRelations, resetTime) &&
                        OrderPhaseHelper.IsAllOpenOrdersShouldChangePhaseToCompleted(order.OrderRelations, tradeDay, instrumentId, resetTime))
            {
                Logger.InfoFormat("UpdateCloseOrderPhase orderId = {0}, tradeDay = {1}", order.Id, tradeDay);
                var account = order.Account;
                order.ChangeToCompleted();
                account.RemoveOrderFromCache(order);
                foreach (var eachOrderRelation in order.OrderRelations)
                {
                    var openOrder = eachOrderRelation.OpenOrder;
                    if (openOrder.Phase != OrderPhase.Completed)
                    {
                        openOrder.ChangeToCompleted();
                        account.RemoveOrderFromCache(openOrder);
                    }
                }
            }
        }

        private static bool IsAllOpenOrdersShouldChangePhaseToCompleted(IEnumerable<OrderRelation> orderRelations, DateTime tradeDay, Guid instrumentId, DateTime? resetTime)
        {
            foreach (var eachOrderRelation in orderRelations)
            {
                var openOrder = eachOrderRelation.OpenOrder;
                if (!openOrder.ShouldUpdateOpenOrder(openOrder.OrderRelations, tradeDay, instrumentId, resetTime))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ShouldUpdateOpenOrder(this Order order, IEnumerable<OrderRelation> orderRelations, DateTime tradeDay, Guid instrumentId, DateTime? resetTime = null)
        {

            return (order.Phase == OrderPhase.Executed || order.Phase ==  OrderPhase.Completed)&& order.IsOpen && order.LotBalance == 0 && order.IsExecuteTimeAfterResetTime(resetTime) && !order.ExistNotValuedOrderDayHistory(tradeDay, instrumentId)
                    && order.IsInstalmentPayOff() && !order.ExistNotMaturePhysicalValue(orderRelations) && order.IsDeliveryRequestCompleted(orderRelations);
        }

        private static bool IsExecuteTimeAfterResetTime(this Order order, DateTime? resetTime)
        {
            return resetTime == null ? true : order.ExecuteTime <= resetTime.Value;
        }

        private static bool IsInstalmentPayOff(this Order order)
        {
            PhysicalOrder physicalOrder = order as PhysicalOrder;
            if (physicalOrder == null) return true;
            if (physicalOrder.Instalment == null || physicalOrder.IsPayoff)
            {
                return true;
            }
            return false;
        }

        private static bool ExistNotValuedOrderDayHistory(this Order order, DateTime tradeDay, Guid instrumentId)
        {
            var orderDayHistorys = ResetManager.Default.GetOrderDayHistorysByOrderId(order.Id);
            if (orderDayHistorys == null) return false;
            var instrumentDayOpenCloseHistorys = ResetManager.Default.GetInstrumentDayOpenCloseHistory(instrumentId);
            foreach (var eachOrderDayHistoryPair in orderDayHistorys)
            {
                OrderDayHistory orderHistory = eachOrderDayHistoryPair.Value;
                DateTime orderDayHistoryTradeDay = eachOrderDayHistoryPair.Key;
                if (orderDayHistoryTradeDay >= tradeDay) continue;
                foreach (var eachInstrumentDayOpenCloseHistory in instrumentDayOpenCloseHistorys)
                {
                    if (eachInstrumentDayOpenCloseHistory.TradeDay == tradeDay)
                    {
                        if (eachInstrumentDayOpenCloseHistory.RealValueDate == null || eachInstrumentDayOpenCloseHistory.RealValueDate > tradeDay)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool ExistNotMaturePhysicalValue(this Order order, IEnumerable<OrderRelation> orderRelations)
        {
            if (!order.Owner.IsPhysical) return false;
            foreach (var eachOrderRelation in orderRelations)
            {
                if (eachOrderRelation.OpenOrderId != order.Id) continue;
                PhysicalOrderRelation physicalOrderRelation = (PhysicalOrderRelation)eachOrderRelation;
                if (physicalOrderRelation.PhysicalValueMatureDay != null && physicalOrderRelation.RealPhysicalValueMatureDate == null)
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsDeliveryRequestCompleted(this Order order, IEnumerable<OrderRelation> orderRelations)
        {
            if (!order.Owner.IsPhysical) return true;
            foreach (var eachOrderRelation in orderRelations)
            {
                if (eachOrderRelation.OpenOrderId == order.Id && eachOrderRelation.CloseOrder.Phase == OrderPhase.Executed)
                {
                    var closeOrder = (PhysicalOrder)eachOrderRelation.CloseOrder;
                    foreach (var eachDeliveryRequest in order.Account.DeliveryRequests.GetValues())
                    {
                        if (eachDeliveryRequest.Id == closeOrder.PhysicalRequestId && eachDeliveryRequest.DeliveryRequestStatus <= DeliveryRequestStatus.Stocked) return false;
                    }
                }
            }
            return true;
        }

        private static bool ShouldUpdateCloseOrder(this Order order, IEnumerable<OrderRelation> orderRelations, DateTime? resetTime)
        {
            return order.Phase == OrderPhase.Executed && !order.IsOpen && order.LotBalance == 0 && order.IsExecuteTimeAfterResetTime(resetTime)
                 && !order.ExistNotValuedOrderRelationForCloseOrder(orderRelations) && !order.ExistNotMaturePhysicalValueForCloseOrder(orderRelations) && !order.ExistNotReadyPhysicalRequest();
        }

        private static bool ExistNotValuedOrderRelationForCloseOrder(this Order order, IEnumerable<OrderRelation> orderRelations)
        {
            foreach (var eachOrderRelation in orderRelations)
            {
                if (eachOrderRelation.CloseOrder.Id != order.Id) continue;
                if (eachOrderRelation.ValueTime == null) return true;
            }
            return false;
        }

        private static bool ExistNotMaturePhysicalValueForCloseOrder(this Order order, IEnumerable<OrderRelation> orderRelations)
        {
            PhysicalOrder physicalOrder = order as PhysicalOrder;
            if (physicalOrder == null) return false;
            foreach (var eachOrderRelation in orderRelations)
            {
                if (eachOrderRelation.CloseOrder.Id != order.Id) continue;
                PhysicalOrderRelation physicalOrderRelation = (PhysicalOrderRelation)eachOrderRelation;
                if (eachOrderRelation.ValueTime == null ||
                    (physicalOrderRelation != null && physicalOrderRelation.PhysicalValueMatureDay != null && physicalOrderRelation.RealPhysicalValueMatureDate == null))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ExistNotReadyPhysicalRequest(this Order order)
        {
            PhysicalOrder physicalOrder = order as PhysicalOrder;
            if (physicalOrder == null) return false;
            foreach (var eachPhysicalRequest in DeliveryRequestManager.Default.DeliveryRequests)
            {
                if (eachPhysicalRequest.Id == physicalOrder.PhysicalRequestId && eachPhysicalRequest.DeliveryRequestStatus <= DeliveryRequestStatus.Stocked)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
