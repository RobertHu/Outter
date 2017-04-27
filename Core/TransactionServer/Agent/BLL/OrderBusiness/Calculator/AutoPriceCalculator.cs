using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    internal sealed class AutoPriceCalculator
    {
        private enum PriceType
        {
            Limit,
            Stop
        }

        internal static void CalculateAutoPrice(Order order)
        {
            if (order.ShouldCalculateAutoPrice)
            {
                var tran = order.Owner;
                var settingInstrument = tran.SettingInstrument();
                var specialTradePolicyDetail = tran.SpecialTradePolicyDetail();
                if (specialTradePolicyDetail == null || settingInstrument.MarginFormula == MarginFormula.CSiMarketPrice || settingInstrument.MarginFormula == MarginFormula.CSxMarketPrice)
                {
                    order.ClearAutoPrice();
                    return;
                }

                DayQuotation dayQuotation = settingInstrument.DayQuotation;
                if ((specialTradePolicyDetail.AutoLimitBase == OrderLevelRiskBase.SettlementPrice || specialTradePolicyDetail.AutoStopBase == OrderLevelRiskBase.SettlementPrice) && dayQuotation == null)
                {
                    //AppDebug.LogEvent("TransactionServer", string.Format("CalculateAutoPrice: Instrument=={0}, AutoLimitBase=={1}, AutoStopBase=={2}", instrument.ID, specialTradePolicyDetail.AutoLimitBase, specialTradePolicyDetail.AutoStopBase), EventLogEntryType.Warning);
                    order.ClearAutoPrice();
                    return;
                }

                //AutoLimitPrice
                //Price oldAutoLimitPrice = order.AutoLimitPrice;
                order.AutoLimitPrice = CalculateAutoClosePrice(order, PriceType.Limit);
                //if (order.AutoLimitPrice != oldAutoLimitPrice)
                //{
                //    this.modifiedProperties = this.modifiedProperties.Add(Properties.AutoLimitPrice);
                //}

                //AutoStopPrice
                //Price oldAutoStopPrice = this.autoStopPrice;
                order.AutoStopPrice = CalculateAutoClosePrice(order, PriceType.Stop);
                //if (this.autoStopPrice != oldAutoStopPrice)
                //{
                //    this.modifiedProperties = this.modifiedProperties.Add(Properties.AutoStopPrice);
                //}
            }
        }

        private static Price CalculateAutoClosePrice(Order order, PriceType priceType)
        {
            Debug.Assert(order.IsOpen);
            SpecialTradePolicyDetail policy = order.Owner.SpecialTradePolicyDetail();
            Settings.Instrument instrument = order.Owner.SettingInstrument();

            OrderLevelRiskBase autoCloseBase = priceType == PriceType.Limit ? policy.AutoLimitBase : policy.AutoStopBase;
            decimal autoCloseThreshold = priceType == PriceType.Limit ? policy.AutoLimitThreshold : policy.AutoStopThreshold;
            if (autoCloseBase == OrderLevelRiskBase.None)
            {
                return null;
            }
            else if (autoCloseBase == OrderLevelRiskBase.Necessary)
            {
                return CalculateForOrderLevelRiskNecessay(order, autoCloseThreshold, instrument, priceType);
            }
            else
            {
                Price basePrice = order.ExecutePrice;
                if (autoCloseBase == OrderLevelRiskBase.SettlementPrice)
                {
                    TradeDay tradeDay = Settings.Setting.Default.GetTradeDay();
                    if (order.Owner.ExecuteTime > tradeDay.BeginTime)
                    {
                        return null;
                    }
                    else
                    {
                        basePrice = (order.IsBuy ? instrument.DayQuotation.Buy : instrument.DayQuotation.Sell);
                    }
                }
                int autoClosePips = (int)autoCloseThreshold;
                if (order.SetPriceMaxMovePips > 0 && order.SetPriceMaxMovePips < autoClosePips)
                {
                    autoClosePips = order.SetPriceMaxMovePips;
                }

                if (order.IsBuy == (priceType == PriceType.Limit))
                {
                    return Price.Add(basePrice, autoClosePips, !instrument.IsNormal);
                }
                else
                {
                    return Price.Subtract(basePrice, autoClosePips, !instrument.IsNormal);
                }
            }
        }

        private static Price CalculateForOrderLevelRiskNecessay(Order order, decimal autoCloseThreshold, Settings.Instrument instrument, PriceType priceType)
        {
            decimal netNecessary = order.Owner.Owner.Setting().RateMarginD * order.Owner.TradePolicyDetail().MarginD * order.Necessary;
            if (priceType == PriceType.Stop) netNecessary = -netNecessary;

            decimal tradePLThreshold = netNecessary * autoCloseThreshold;
            if (!order.Owner.Owner.IsMultiCurrency)
            {
                tradePLThreshold = order.Owner.CurrencyRate(null).Exchange(tradePLThreshold, false);
            }

            return Calculate(instrument.TradePLFormula, instrument.NumeratorUnit, instrument.Denominator,
                order.LotBalance, order.Owner.ContractSize(null), (decimal)order.ExecutePrice, order.IsBuy, tradePLThreshold);
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
