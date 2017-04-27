using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Physical.InstalmentBusiness
{
    internal sealed class InstalmentCalculator
    {
        internal static readonly InstalmentCalculator Default = new InstalmentCalculator();
        static InstalmentCalculator() { }
        private InstalmentCalculator() { }

        internal DateTime CalculatePaymentDateTimeOnPlan(InstalmentFrequence frequence, int period, DateTime tradeDay)
        {
            DateTime result = tradeDay;
            if (frequence == InstalmentFrequence.Month)
            {
                result = tradeDay.AddMonths(period);
            }
            else if (frequence == InstalmentFrequence.Season)
            {
                result = tradeDay.AddMonths(period * 3);
            }
            else if (frequence == InstalmentFrequence.TwoWeek)
            {
                result = tradeDay.AddDays(period * 2 * 7);
            }
            else if (frequence == InstalmentFrequence.Year)
            {
                result = tradeDay.AddYears(period);
            }
            return result;
        }


        internal int CalculateInstalmentQuantity(int period, InstalmentFrequence frequence)
        {
            int result = period;
            if (frequence == InstalmentFrequence.TwoWeek)
            {
                result = (int)Math.Ceiling(period * (52.0 / 12.0 / 2.0));
            }
            else if (frequence == InstalmentFrequence.Season)
            {
                result = (int)Math.Ceiling(period * (4.0 / 12.0));
            }
            return result;
        }

    }
}
