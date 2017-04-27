using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class CloseOrderPLCalculator
    {
        internal static void Calculate(TradeDayInfo tradeDayInfo, Exchanger exchanger, Dictionary<Guid, OrderResetResult> resetOrderDict, List<Guid> affectedOrders)
        {
            CalculateNotValuedOrderRelationPL(tradeDayInfo, exchanger, resetOrderDict, affectedOrders);
            CalculateValuedOrderRelationPL(tradeDayInfo, exchanger, resetOrderDict, affectedOrders);
        }

        private static void CalculateValuedOrderRelationPL(TradeDayInfo tradeDayInfo, Exchanger exchanger, Dictionary<Guid, OrderResetResult> resetOrderDict, List<Guid> affectedOrders)
        {
            foreach (var eachOrderRelation in tradeDayInfo.OrderRelations)
            {
                Order closeOrder = eachOrderRelation.CloseOrder;
                if (!closeOrder.ShouldCalculate(affectedOrders)) continue;
                if (closeOrder.Phase == OrderPhase.Executed && !closeOrder.IsOpen && closeOrder.ExecuteTime <= tradeDayInfo.Settings.ResetTime && eachOrderRelation.ValueTime > tradeDayInfo.Settings.BeginTime && eachOrderRelation.ValueTime <= tradeDayInfo.Settings.ResetTime)
                {
                    var resetResult = CreateOrderResetResult(closeOrder, tradeDayInfo, resetOrderDict);
                    var valuedPL = eachOrderRelation.CalculateValuedPL();
                    resetResult.ValuedPL += new InterestStorage(valuedPL.InterestPL, valuedPL.StoragePL);
                    resetResult.TradePLValued += valuedPL.TradePL;
                    PhysicalOrderRelation physicalOrderRelation = eachOrderRelation as PhysicalOrderRelation;
                    if (physicalOrderRelation != null)
                    {
                        resetResult.PhysicalTradePLValued += CalculatePhysicalTradePL(physicalOrderRelation, exchanger);
                    }
                }
            }
        }


        private static void CalculateNotValuedOrderRelationPL(TradeDayInfo tradeDayInfo, Exchanger exchanger, Dictionary<Guid, OrderResetResult> resetOrderDict, List<Guid> affectedOrders)
        {
            foreach (var eachOrderRelation in tradeDayInfo.OrderRelations)
            {
                Order closeOrder = eachOrderRelation.CloseOrder;
                if (!closeOrder.ShouldCalculate(affectedOrders)) continue;
                PhysicalOrder physicalCloseOrder = closeOrder as PhysicalOrder;
                PhysicalOrderRelation physicalOrderRelation = eachOrderRelation as PhysicalOrderRelation;
                if (closeOrder.Phase == OrderPhase.Executed && !closeOrder.IsOpen && closeOrder.ExecuteTime <= tradeDayInfo.Settings.ResetTime && eachOrderRelation.ValueTime == null)
                {
                    OrderResetResult resetResult = CreateOrderResetResult(closeOrder, tradeDayInfo, resetOrderDict);
                    resetResult.NotValuedPL += new InterestStorage(exchanger.ExchangeByCommonDecimals(eachOrderRelation.InterestPL), exchanger.ExchangeByCommonDecimals(eachOrderRelation.StoragePL));
                    resetResult.TradePLNotValued += exchanger.ExchangeByCommonDecimals(eachOrderRelation.TradePL);
                    if (physicalOrderRelation != null)
                    {
                        resetResult.PhysicalTradePLNotValued += CalculatePhysicalTradePL(physicalOrderRelation, exchanger);
                    }
                }
            }
        }


        private static OrderResetResult CreateOrderResetResult(Order closeOrder, TradeDayInfo tradeDayInfo, Dictionary<Guid, OrderResetResult> resetOrderDict)
        {
            OrderResetResult resetResult;
            if (!resetOrderDict.TryGetValue(closeOrder.Id, out resetResult))
            {
                resetResult = new OrderResetResult();
                CalculateOrderRelationPLCommon(closeOrder, resetResult, tradeDayInfo);
                resetOrderDict.Add(closeOrder.Id, resetResult);
            }
            return resetResult;
        }


        private static decimal CalculatePhysicalTradePL(PhysicalOrderRelation orderRelation, Exchanger exchanger)
        {
            decimal result = 0m;
            if (orderRelation.PhysicalValueMatureDay == null || orderRelation.RealPhysicalValueMatureDate == orderRelation.PhysicalValueMatureDay)
            {
                result = exchanger.ExchangeByCommonDecimals(orderRelation.TradePL);
            }
            return result;
        }

        private static void CalculateOrderRelationPLCommon(Order closeOrder, OrderResetResult resetResult, TradeDayInfo tradeDayInfo)
        {
            PhysicalOrder physicalCloseOrder = closeOrder as PhysicalOrder;
            resetResult.TradeDay = tradeDayInfo.TradeDay;
            resetResult.CurrencyId = tradeDayInfo.RateSetting.CurrencyId;
            resetResult.OrderId = closeOrder.Id;
            resetResult.PhysicalPaidAmount = physicalCloseOrder != null ? physicalCloseOrder.PaidAmount : 0;
            resetResult.PaidPledgeBalance = physicalCloseOrder != null ? physicalCloseOrder.PaidPledgeBalance : 0;
            resetResult.PhysicalOriginValueBalance = physicalCloseOrder != null ? physicalCloseOrder.PhysicalOriginValueBalance : 0;
        }
    }
}
