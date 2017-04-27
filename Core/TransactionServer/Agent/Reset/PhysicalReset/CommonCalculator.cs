using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset.PhysicalReset
{
    internal static class InstalmentCalculator
    {
        internal static decimal CalculateInstalmentInterest(TradeDayInfo data, PhysicalOrder physicalOrder, out decimal interestRate, out decimal remainsAmount)
        {
            decimal result = 0m;
            interestRate = 0m;
            remainsAmount = 0m;
            if (data.Instrument.Category == InstrumentCategory.Physical)
            {
                remainsAmount = physicalOrder.PhysicalOriginValueBalance - Math.Abs(physicalOrder.PaidPledgeBalance);
                if (physicalOrder.Instalment != null)
                {
                    interestRate = CalculateInstalmentInterestRate(physicalOrder, data.Settings);
                    if (physicalOrder.InterestValueDate <= data.TradeDay)
                    {
                        result = remainsAmount * (interestRate / data.Instrument.InterestYearDays) * data.Settings.InterestMultiple;
                    }
                }
            }
            return result;
        }


        private static decimal CalculateInstalmentInterestRate(PhysicalOrder order, InstrumentTradeDaySetting settings)
        {
            decimal result = 0;
            if (order.Instalment.Period == 1 || order.PhysicalTradeSide == PhysicalTradeSide.ShortSell)
            {
                result = order.IsBuy ? settings.InstalmentInterestRateBuy : settings.InstalmentInterestRateSell;
            }
            return result;
        }

    }
}
