using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Hit
{
    internal static class SpotOrderHitter
    {
        internal static OrderHitStatus HitSpotOrder(Order order, HitOrderSettings hitOrderSettings, Quotation quotation, DateTime baseTime)
        {
            if (!order.ShouldSportOrderDelayFill && order.HitStatus.IsFinal()) return OrderHitStatus.None;
            Price marketPrice = HitCommon.CalculateMarketPrice(order.IsBuy, quotation);
            if (ShouldCancelSpotOrder(order, marketPrice, quotation.IsNormal))
            {
                return OrderHitStatus.ToCancel;
            }
            if (order.DQMaxMove <= 0) return OrderHitStatus.None;
            if (!hitOrderSettings.IncreaseHitCount()) return OrderHitStatus.None;
            Price bestPrice = CalculateBestPriceForSportOrder(order, marketPrice);
            if (bestPrice != null)
            {
                order.BestPrice = bestPrice;
                order.BestTime = baseTime;
            }
            if (order.ShouldAutoFill && !order.ShouldSportOrderDelayFill && Math.Abs(order.HitCount) >= order.Instrument().HitTimes)
            {
                return OrderHitStatus.ToAutoFill;
            }
            return OrderHitStatus.Hit;
        }

        private static Price CalculateBestPriceForSportOrder(Order order, Price marketPrice)
        {
            Price bestPrice;
            bool isBuyAndIsNormalWithSameSymbol = order.IsBuy == order.Instrument().IsNormal;
            if (isBuyAndIsNormalWithSameSymbol)
            {
                bestPrice = CalculateBestPriceForIsBuyTheSameToIsNormal(order, marketPrice);
            }
            else
            {
                bestPrice = CalculateBestPriceForIsBuyDifferentFromIsNormal(order, marketPrice);
            }
            return bestPrice;
        }

        private static Price CalculateBestPriceForIsBuyTheSameToIsNormal(Order order, Price marketPrice)
        {
            Price bestPrice = null;
            if (marketPrice <= (order.SetPrice + order.DQMaxMove))
            {
                if (order.BestPrice == null)
                {
                    bestPrice = marketPrice < order.SetPrice ? order.SetPrice : marketPrice;
                }
                else if (marketPrice > order.BestPrice)
                {
                    bestPrice = marketPrice;
                }
            }
            else
            {
                if (order.BestPrice == null)
                {
                    bestPrice = order.SetPrice + order.DQMaxMove;
                }
            }
            return bestPrice;
        }

        private static Price CalculateBestPriceForIsBuyDifferentFromIsNormal(Order order, Price marketPrice)
        {
            Price bestPrice = null;
            if (marketPrice >= order.SetPrice - order.DQMaxMove)
            {
                if (order.BestPrice == null)
                {
                    bestPrice = (marketPrice > order.SetPrice ? order.SetPrice : marketPrice);
                }
                else if (marketPrice < order.BestPrice)
                {
                    bestPrice = marketPrice;
                }
            }
            else
            {
                if (order.BestPrice == null)
                {
                    bestPrice = order.SetPrice - order.DQMaxMove;
                }
            }
            return bestPrice;
        }


        private static bool ShouldCancelSpotOrder(Order order, Price marketPrice, bool isNormal)
        {
            int sign = (isNormal ^ order.IsBuy) ? 1 : -1;
            Price setPrice = order.SetPrice - order.DQMaxMove * sign;
            if ((setPrice - marketPrice) * sign > order.Owner.DealingPolicyPayload().AcceptDQVariation)
            {
                return true;
            }
            return false;
        }
    }
}
