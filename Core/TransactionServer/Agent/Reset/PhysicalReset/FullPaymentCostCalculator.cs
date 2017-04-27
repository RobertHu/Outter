using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset.PhysicalReset
{
    internal static class FullPaymentCostCalculator
    {
        internal static decimal Calculate(PhysicalOrder physicalOrder)
        {
            decimal result = 0m;
            if (physicalOrder.IsOpen)
            {
                result = physicalOrder.IsPayoff && physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Buy ? -physicalOrder.PhysicalOriginValue : 0m;
            }
            else
            {
                foreach (PhysicalOrderRelation eachOrderRelation in physicalOrder.OrderRelations)
                {
                    if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Deposit || (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Sell && physicalOrder.IsPayoff && eachOrderRelation.PhysicalValueMatureDay == null))
                    {
                        result += eachOrderRelation.PhysicalValue;
                    }
                }
            }
            return result;
        }
    }
}
