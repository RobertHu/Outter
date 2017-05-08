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
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class OrderPhaseHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderPhaseHelper));

        internal static void UpdateCloseOrderPhase(this Order order, DateTime tradeDay, Guid instrumentId, DateTime? resetTime)
        {
            if (order.ShouldUpdateOrderPhase(tradeDay, instrumentId, resetTime, new SortedSet<Guid>()))
            {
                order.InnerUpdateCloseOrderPhase(tradeDay, instrumentId, resetTime, new SortedSet<Guid>());
            }
        }

        private static void InnerUpdateCloseOrderPhase(this Order order, DateTime tradeDay, Guid instrumentId, DateTime? resetTime, ISet<Guid> visited)
        {
            Logger.InfoFormat("UpdateCloseOrderPhase orderId = {0}, tradeDay = {1}", order.Id, tradeDay);
            var account = order.Account;
            if (!visited.Contains(order.Id))
            {
                visited.Add(order.Id);
            }
            order.ChangeToCompletedAndRemove();
            foreach (var eachOrderRelation in order.OrderRelations)
            {
                var openOrder = eachOrderRelation.OpenOrder;
                openOrder.ChangeToCompletedAndRemove();
                ChangeAllCloseOrdersToCompleted(openOrder, order, resetTime, tradeDay, instrumentId, visited);
            }
        }


        private static bool ShouldUpdateOrderPhase(this Order order, DateTime tradeDay, Guid instrumentId, DateTime? resetTime, ISet<Guid> visited)
        {
            Logger.InfoFormat("ShouldUpdateOrderPhase orderId = {0}, visite.count = {1}", order.Id, visited.Count);
            if (order.ShouldUpdateCloseOrder(order.OrderRelations, resetTime) &&
                        OrderPhaseHelper.IsAllOpenOrdersShouldChangePhaseToCompleted(order.OrderRelations, tradeDay, instrumentId, resetTime))
            {
                visited.Add(order.Id);
                foreach (var eachOrderRelation in order.OrderRelations)
                {
                    var openOrder = eachOrderRelation.OpenOrder;
                    foreach (var eachOrderRelationForOpenOrder in openOrder.GetAllOrderRelations())
                    {
                        if (eachOrderRelationForOpenOrder != eachOrderRelation)
                        {
                            var closeOrder = eachOrderRelationForOpenOrder.CloseOrder;
                            if (closeOrder != order && !visited.Contains(closeOrder.Id))
                            {
                                if (!closeOrder.ShouldUpdateOrderPhase(tradeDay, instrumentId, resetTime, visited))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }



        private static void ChangeToCompletedAndRemove(this Order order)
        {
            if (order.Phase != OrderPhase.Completed)
            {
                order.ChangeToCompleted();
                order.Account.RemoveOrderFromCache(order);
            }
        }



        private static void ChangeAllCloseOrdersToCompleted(Order openOrder, Order originalCloseOrder, DateTime? resetTime, DateTime tradeDay, Guid instrumentId, ISet<Guid> visited)
        {
            var orderRelations = openOrder.GetAllOrderRelations();
            if (orderRelations.Count == 0) return;
            foreach (var eachOrderRelation in orderRelations)
            {
                var closeOrder = eachOrderRelation.CloseOrder;
                if (closeOrder != originalCloseOrder && !visited.Contains(closeOrder.Id))
                {
                    closeOrder.InnerUpdateCloseOrderPhase(tradeDay, instrumentId, resetTime, visited);
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

            return (order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed) && order.IsOpen && order.LotBalance == 0 && order.IsExecuteTimeAfterResetTime(resetTime) && !order.ExistNotValuedOrderDayHistory(tradeDay, instrumentId)
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

