using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class ResetExtension
    {
        internal static decimal MathRound(this decimal amount, int decimals)
        {
            return Math.Round(amount, decimals, MidpointRounding.AwayFromZero);
        }

        internal static decimal Exchange(this decimal amount, decimal rateIn, decimal rateOut, int decimals, int? sourceDecimals = null)
        {
            decimal result = 0;
            decimal rate = amount > 0 ? rateIn : rateOut;
            if (sourceDecimals == null)
            {
                result = (amount * rate).MathRound(decimals);
            }
            else
            {
                result = (amount.MathRound(sourceDecimals.Value) * rate).MathRound(decimals);
            }
            return result;
        }

    }
}
