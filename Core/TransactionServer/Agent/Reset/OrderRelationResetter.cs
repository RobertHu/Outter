using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderRelationResetter
    {
        internal static readonly OrderRelationResetter Default = new OrderRelationResetter();

        static OrderRelationResetter() { }
        private OrderRelationResetter() { }

        internal List<OrderRelation> UpdateOrderRelationValueDate(TradeDayInfo info)
        {
            List<OrderRelation> result = new List<OrderRelation>();
            if (info.Settings.ValueDate == null) return result;
            foreach (var eachOrderRelation in info.OrderRelations)
            {
                if (!this.ShouldUpdateOrderRelationValueDate(eachOrderRelation, info.Settings.ResetTime)) continue;
                var instrumentDayOpenCloseHistory = ResetManager.Default.GetInstrumentDayOpenCloseHistory(info.Instrument.Id, eachOrderRelation.CloseOrder.ExecuteTime.Value.Date);
                if (instrumentDayOpenCloseHistory.RealValueDate == info.TradeDay)
                {
                    result.Add(eachOrderRelation);
                    eachOrderRelation.ValueTime = info.Settings.ResetTime;
                    eachOrderRelation.RateIn = info.RateSetting.RateIn;
                    eachOrderRelation.RateOut = info.RateSetting.RateOut;
                    eachOrderRelation.Decimals = info.RateSetting.RoundDecimals.Common;
                }
            }
            return result;
        }

        private bool ShouldUpdateOrderRelationValueDate(OrderRelation orderRelation, DateTime resetTime)
        {
            Order closeOrder = orderRelation.CloseOrder;
            return closeOrder.Phase == OrderPhase.Executed && !closeOrder.IsOpen && closeOrder.ExecuteTime <= resetTime && orderRelation.ValueTime == null;
        }

        internal List<PhysicalOrderRelation> UpdatePhysicalOrderRelationValueMatureDate(TradeDayInfo info)
        {
            List<PhysicalOrderRelation> result = new List<PhysicalOrderRelation>();
            foreach (var eachOrderRelation in info.OrderRelations)
            {
                PhysicalOrderRelation physicalOrderRelation = eachOrderRelation as PhysicalOrderRelation;
                if (physicalOrderRelation == null) continue;
                if (physicalOrderRelation.PhysicalValueMatureDay <= info.TradeDay && physicalOrderRelation.RealPhysicalValueMatureDate == null)
                {
                    result.Add(physicalOrderRelation);
                    physicalOrderRelation.RealPhysicalValueMatureDate = DateTime.Now.Date;
                }
            }
            return result;
        }

        internal decimal CalculateValuedCloseOrderPL(List<OrderRelation> valuedOrderRelations, Exchanger exchanger)
        {
            decimal result = 0m;
            foreach (var eachOrderRelation in valuedOrderRelations)
            {
                decimal interestPL = this.CalculateValuedPL(eachOrderRelation.InterestPL, eachOrderRelation, exchanger);
                decimal storagePL = this.CalculateValuedPL(eachOrderRelation.StoragePL, eachOrderRelation, exchanger);
                decimal tradePL = this.CalculateValuedPL(eachOrderRelation.TradePL, eachOrderRelation, exchanger);
                result += (interestPL + storagePL + tradePL);
            }
            return result;
        }

        private decimal CalculateValuedPL(decimal value, OrderRelation orderRelation, Exchanger exchanger)
        {
            return exchanger.Exchange(value, orderRelation.RateIn, orderRelation.RateOut, orderRelation.Decimals);
        }


    }
}
