using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    internal static class NotValuedPLCalculator
    {
        internal static void Calculate(Order order, out decimal interestPL, out decimal storagePL, out decimal tradePL)
        {
            if (order.IsOpen)
            {
                CalculateOpenOrderNotValuedPL(order, out interestPL, out storagePL, out tradePL);
            }
            else
            {
                CalculateCloseOrderNotValuedPL(order, out interestPL, out storagePL, out tradePL);
            }
        }

        private static void CalculateOpenOrderNotValuedPL(Order order, out decimal interestPL, out decimal storagePL, out decimal tradePL)
        {
            interestPL = storagePL = tradePL = 0;
            order.NotValuedDayInterestAndStorage.CalculateNotValuedInterestAndStorage(order.CurrencyRate, out interestPL, out storagePL);
        }

        private static void CalculateCloseOrderNotValuedPL(Order order, out decimal interestPL, out decimal storagePL, out decimal tradePL)
        {
            interestPL = storagePL = tradePL = 0;
            if (order.IsValued) return;
            var currencyRate = order.CurrencyRate;
            foreach (var eachOrderRelation in order.OrderRelations)
            {
                interestPL += currencyRate.Exchange(eachOrderRelation.InterestPL);
                storagePL += currencyRate.Exchange(eachOrderRelation.StoragePL);
                tradePL += currencyRate.Exchange(eachOrderRelation.TradePL);
            }
        }
    }
}
