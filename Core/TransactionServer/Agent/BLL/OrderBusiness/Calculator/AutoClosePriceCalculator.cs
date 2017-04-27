using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Diagnostics;
using Core.TransactionServer.Agent.Settings;

namespace Core.TransactionServer.Agent.OrderBusiness.Calculator
{
    /// <summary>
    /// 计算单子自动平仓价格
    /// </summary>
    internal static class AutoClosePriceCalculator
    {
        internal enum PriceType
        {
            Limit,
            Stop
        }

        internal static Price CalculateAutoClosePrice(this Order order, PriceType priceType)
        {
            Debug.Assert(order.IsOpen);
            SpecialTradePolicyDetail policy = order.Owner.SpecialTradePolicyDetail;
            Settings.Instrument instrument = order.Owner.SettingInstrument;

            OrderLevelRiskBase autoCloseBase = priceType == PriceType.Limit ? policy.AutoLimitBase : policy.AutoStopBase;
            decimal autoCloseThreshold = priceType == PriceType.Limit ? policy.AutoLimitThreshold : policy.AutoStopThreshold;

            if (autoCloseBase == OrderLevelRiskBase.Necessary)
            {
                return CalculateForOrderLevelRiskNecessay(order, autoCloseThreshold, instrument, priceType);
            }
            else if (autoCloseBase == OrderLevelRiskBase.OpenPrice)
            {
                return CalculateForOrderLevelOpenPrice(order, autoCloseThreshold, instrument, priceType);
            }
            else
            {
                return null;
            }
        }

        private static Price CalculateForOrderLevelRiskNecessay(Order order, decimal autoCloseThreshold, Settings.Instrument instrument, PriceType priceType)
        {
            decimal netNecessary = order.Owner.Owner.Setting.RateMarginD * order.Owner.TradePolicyDetail.MarginD * order.Necessary;
            if (priceType == PriceType.Stop) netNecessary = -netNecessary;

            decimal tradePLThreshold = netNecessary * autoCloseThreshold;
            tradePLThreshold = order.Owner.CurrencyRate.Exchange(tradePLThreshold, false);

            return Calculate(instrument.TradePLFormula, instrument.NumeratorUnit, instrument.Denominator,
                order.LotBalance, order.Owner.ContractSize, (decimal)order.ExecutePrice, order.IsBuy, tradePLThreshold);
        }

        private static Price CalculateForOrderLevelOpenPrice(Order order, decimal autoCloseThreshold, Settings.Instrument instrument, PriceType priceType)
        {
            Price basePrice = order.ExecutePrice;
            int autoClosePips = (int)autoCloseThreshold;
            int setPriceMaxMovePips = order.SetPriceMaxMovePips;
            if (setPriceMaxMovePips > 0 && setPriceMaxMovePips < autoClosePips) autoClosePips = setPriceMaxMovePips;

            bool isLimit = priceType == PriceType.Limit;
            if (order.IsBuy == isLimit)
            {
                return Price.Add(basePrice, autoClosePips, !instrument.IsNormal);
            }
            else
            {
                return Price.Subtract(basePrice, autoClosePips, !instrument.IsNormal);
            }
        }


        private static Price Calculate(TradePLFormula tradePLFormula, int numeratorUnit, int denominator, decimal lot, decimal contractSize, decimal openPrice, bool isOpenOnBuy, decimal tradePLThreshold)
        {
            decimal closePrice = 0;
            bool isGreatThan = true;

            decimal contractValue = (lot * contractSize);

            switch (tradePLFormula)
            {
                case TradePLFormula.S_BxCS:
                    if (isOpenOnBuy)
                    {
                        closePrice = openPrice + tradePLThreshold / contractValue;
                    }
                    else
                    {
                        isGreatThan = false;
                        closePrice = openPrice - tradePLThreshold / contractValue;
                    }
                    break;
                case TradePLFormula.S_BxCSiL:
                    if (isOpenOnBuy)
                    {
                        closePrice = contractValue * openPrice / (contractValue - tradePLThreshold);
                    }
                    else
                    {
                        isGreatThan = false;
                        closePrice = contractValue * openPrice / (contractValue + tradePLThreshold);
                    }
                    break;
                case TradePLFormula.Si1_Bi1xCSiL:
                    if (isOpenOnBuy)
                    {
                        isGreatThan = false;
                        closePrice = contractValue * openPrice / (contractValue + tradePLThreshold * openPrice);
                    }
                    else
                    {
                        closePrice = contractValue * openPrice / (contractValue - tradePLThreshold * openPrice);
                    }
                    break;
                case TradePLFormula.S_BxCSiO:
                    if (isOpenOnBuy)
                    {
                        closePrice = openPrice + tradePLThreshold * openPrice / contractValue;
                    }
                    else
                    {
                        isGreatThan = false;
                        closePrice = openPrice - tradePLThreshold * openPrice / contractValue;
                    }
                    break;
            }

            if (closePrice <= 0) return null;

            if (tradePLThreshold < 0) isGreatThan = !isGreatThan;

            return Price.Create((double)closePrice, numeratorUnit, denominator, isGreatThan);
        }
    }
}
