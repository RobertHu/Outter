using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using System.Diagnostics;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class FloatingCalculator
    {
        internal static decimal CalculateOrderFloatRpt(decimal perLot, decimal lotBalance, decimal rateIn, decimal rateout, int decimals)
        {
            decimal result = perLot * lotBalance;
            return FloatingCalculator.CalculateRatedValue(result, rateIn, rateout, decimals);
        }

        private static decimal CalculateRatedValue(decimal value, decimal rateIn, decimal rateOut, int decimals)
        {
            decimal result = 0m;
            result = value * (value > 0 ? rateIn : rateOut);
            return result.MathRound(decimals);
        }

        internal static decimal CalculateOrderFloatTrade(decimal lotBalance, decimal contractSize, bool isBuy, int plTradeFormula, Price executePrice, Price livePrice, decimal rateIn, decimal rateOut, int decimals)
        {
            if (livePrice == null) return 0m;
            decimal result = lotBalance * contractSize * FloatingCalculator.CalculateFLTradePer(isBuy, plTradeFormula, executePrice, livePrice);
            result = result.MathRound(decimals);
            return FloatingCalculator.CalculateRatedValue(result, rateIn, rateOut, decimals);
        }

        private static decimal CalculateFLTradePer(bool isBuy, int plTradeFormula, Price executePrice, Price livePrice)
        {
            Price buyPrice = isBuy ? executePrice : livePrice;
            Price sellPrice = isBuy ? livePrice : executePrice;
            Debug.Assert(executePrice != null);
            Debug.Assert(livePrice != null);
            decimal result = 0m;
            switch (plTradeFormula)
            {
                case 0: result = (decimal)sellPrice - (decimal)buyPrice;
                    break;
                case 1: result = ((decimal)sellPrice - (decimal)buyPrice) / (decimal)livePrice;
                    break;
                case 2: result = (1 / (decimal)sellPrice - 1 / (decimal)buyPrice);
                    break;
                case 3: result = ((decimal)sellPrice - (decimal)buyPrice) / (decimal)executePrice;
                    break;
            }
            return result;
        }

    }

    internal static class MarketValueCalculator
    {
        internal static decimal CalculateMarkValue(decimal lot, Price livePrice, decimal discountOfOdd, int tradePLFormula, decimal contractSize)
        {
            if (livePrice == null) return 0m;
            decimal result = 0m;
            int lotIntegerPart = (int)lot;
            decimal lotRemain = lot - lotIntegerPart;
            if (tradePLFormula == 0)
            {
                result = lotIntegerPart * contractSize * (decimal)livePrice + lotRemain * contractSize * (decimal)livePrice * discountOfOdd;
            }
            else if (tradePLFormula == 1 || tradePLFormula == 3)
            {
                result = lotIntegerPart * contractSize + lotRemain * contractSize * discountOfOdd;
            }
            else if (tradePLFormula == 2)
            {
                result = lotIntegerPart * contractSize * (1 / (decimal)livePrice) + lotRemain * contractSize * (1 / (decimal)livePrice) * discountOfOdd;
            }
            return result;
        }
    }

}
