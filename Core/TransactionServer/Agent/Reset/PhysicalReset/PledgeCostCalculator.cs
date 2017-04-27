using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset.PhysicalReset
{
    internal static class PledgeCostCalculator
    {
        internal static decimal Calculate(PhysicalOrder physicalOrder)
        {
            return physicalOrder.IsOpen ? CalculateForOpenOrder(physicalOrder) : CalculateForCloseOrder(physicalOrder);
        }

        private static decimal CalculateForOpenOrder(PhysicalOrder physicalOrder)
        {
            if (!physicalOrder.IsOpen) return 0m;
            decimal result = 0m;
            if (physicalOrder.IsPayoff && physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Buy)
            {
                result = 0m;
            }
            else
            {
                result = physicalOrder.PaidPledgeBalance;
                if (physicalOrder.Instalment != null)
                {
                    foreach (var eachInstalmentDetail in physicalOrder.Instalment.InstalmentDetails)
                    {
                        if (eachInstalmentDetail.PaidDateTime != null)
                        {
                            result += eachInstalmentDetail.Principal + eachInstalmentDetail.Interest + eachInstalmentDetail.DebitInterest;
                        }
                    }
                }
            }
            return result;
        }

        private static decimal CalculateForCloseOrder(PhysicalOrder physicalOrder)
        {
            if (physicalOrder.IsOpen) return 0m;
            decimal result = 0m;
            if (physicalOrder.IsPayoff && physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Sell)
            {
                return result;
            }
            foreach (PhysicalOrderRelation eachOrderRelation in physicalOrder.OrderRelations)
            {
                result += eachOrderRelation.PayBackPledge;
            }
            return result;
        }

    }
}
