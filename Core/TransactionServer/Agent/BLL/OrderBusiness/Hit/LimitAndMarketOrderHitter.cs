using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Hit
{
    internal static class LimitAndMarketOrderHitter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LimitAndMarketOrderHitter));

        internal static OrderHitStatus HitMarketAndLimitOrder(Order order, HitOrderSettings hitOrderSettings, Quotation newQuotation, DateTime baseTime, bool ignoreHitTimes, out Price bestPrice)
        {
            bestPrice = null;
            Price marketPrice = HitCommon.CalculateMarketPrice(order.IsBuy, newQuotation);
            if (!IsHitPrice(order, newQuotation, marketPrice))
            {
                return OrderHitStatus.None;
            }
            if (!hitOrderSettings.IncreaseHitCount()) return OrderHitStatus.None;
            if (ShouldUseNewHitPrice(order, newQuotation))
            {
                bestPrice = marketPrice;
            }
            else
            {
                return OrderHitStatus.None;
            }
            return order.CalculateLimitMarketHitStatus(ignoreHitTimes);
        }

        private static bool IsHitPrice(Order order, Quotation newQuotation, Price marketPrice)
        {
            if (order.Phase != OrderPhase.Placed
             || (order.OrderType != OrderType.Limit && order.OrderType != OrderType.Market)
             || marketPrice == null)
            {
                return false;
            }
            PriceCompareResult result = newQuotation.Compare(order.SetPrice, order.IsBuy);
            return order.OrderType == OrderType.Market || (order.OrderType == OrderType.Limit && (marketPrice == order.SetPrice
                || (order.TradeOption == TradeOption.Better ? result == PriceCompareResult.Better : result == PriceCompareResult.Worse)));
        }

        private static bool ShouldUseNewHitPrice(Order order, Quotation newQuotation)
        {
            Settings.Instrument instrument = order.Instrument();
            return order.HitCount == 1 || instrument.LmtAsMit || !instrument.UseBetterPriceForCompanyWhenHit
                || (instrument.UseBetterPriceForCompanyWhenHit && newQuotation.Compare(order.BestPrice, order.IsBuy) == PriceCompareResult.Worse);
        }

        internal static OrderHitStatus HitAutoClosePrice(Order order, Quotation newQuotation, DateTime baseTime)
        {
            if (order.AutoLimitPrice != null || order.AutoStopPrice != null)
            {
                bool isBuyForClose = !order.IsBuy;
                Price marketPrice = HitCommon.CalculateMarketPrice(isBuyForClose, newQuotation);
                if (order.AutoLimitPrice != null)
                {
                    int result = Price.Compare(marketPrice, order.AutoLimitPrice, !newQuotation.IsNormal);
                    if ((!isBuyForClose && result >= 0) || (isBuyForClose && result <= 0)) //Sell High Buy Low
                    {
                        return OrderHitStatus.ToAutoLimitClose;
                    }
                }

                if (order.AutoStopPrice != null)
                {
                    int result = Price.Compare(marketPrice, order.AutoStopPrice, !newQuotation.IsNormal);
                    if ((!isBuyForClose && result <= 0) || (isBuyForClose && result >= 0)) //Sell Low Buy High
                    {
                        return OrderHitStatus.ToAutoStopClose;
                    }
                }
            }
            return OrderHitStatus.None;
        }
    }
}
