using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    internal sealed class OrderPlacePriceVerifier
    {
        internal static readonly OrderPlacePriceVerifier Default = new OrderPlacePriceVerifier();

        static OrderPlacePriceVerifier() { }
        private OrderPlacePriceVerifier() { }

        internal bool IsBuy(Order order)
        {
            return order.Owner.SettingInstrument.IsNormal ? order.IsBuy : !order.IsBuy;
        }

        internal void Verify(Order order)
        {
            if (this.ShouldVerify(order))
            {
                this.InnerVerify(order);
            }
        }

        private void InnerVerify(Order order)
        {
            DateTime priceTimestamp;
            Price marketPrice = order.GetMarketPrice(out priceTimestamp);
            if (order.IsSpotOrder)
            {
                this.VerifyPriceForSportOrder(order, marketPrice, priceTimestamp);
            }
            else if (order.IsLimitOrder)
            {
                this.VerifyPriceForLimitOrder(order, marketPrice, priceTimestamp);
            }
        }

        private void VerifyPriceForLimitOrder(Order order, Price marketPrice, DateTime priceTimestamp)
        {
            bool isNetDirectionTheSame = order.Owner.PlacedByRiskMonitor ? false : OrderNetVerifier.Default.IsNetOpen(order);//Order placed by RiskMonitor use CloseVariation temporarily
            var acceptLimitVariation = isNetDirectionTheSame ? order.Owner.DealingPolicyPayload.AcceptLmtVariation : order.Owner.DealingPolicyPayload.AcceptCloseLmtVariation;
            var absPriceDiff = Math.Abs(order.SetPrice - marketPrice);
            if (!order.DisableAcceptLmtVariation && absPriceDiff < acceptLimitVariation)
            {
                order.JudgePrice = marketPrice;
                order.JudgePriceTimestamp = priceTimestamp;
                string errorDetail = string.Format("marketPrice = {0}, setPrice = {1}, acceptLimitVariation={2}, absPriceDiff = {3}", marketPrice, order.SetPrice, acceptLimitVariation, absPriceDiff);
                throw new TransactionServerException(TransactionError.SetPriceTooCloseToMarket, errorDetail);
            }
            this.VerifyPriceWithTradeOption(order, marketPrice, priceTimestamp);
        }

        private void VerifyPriceWithTradeOption(Order order, Price marketPrice, DateTime priceTimestamp)
        {
            bool isBuy = order.Owner.SettingInstrument.IsNormal ? order.IsBuy : !order.IsBuy;
            string message = string.Format("isBuy={0}, setPrice={1},comparePrice={2}", isBuy, order.SetPrice, marketPrice);
            if (this.IsSetPriceBetter(order, isBuy, marketPrice))
            {
                if (order.TradeOption != TradeOption.Better)
                {
                    order.JudgePrice = marketPrice;
                    order.JudgePriceTimestamp = priceTimestamp;
                    throw new TransactionServerException(TransactionError.InvalidPrice, message);
                }
            }
            else
            {
                if (order.TradeOption != TradeOption.Stop)
                {
                    order.JudgePrice = marketPrice;
                    order.JudgePriceTimestamp = priceTimestamp;
                    throw new TransactionServerException(TransactionError.InvalidPrice, message);
                }
            }
        }


        private bool IsSetPriceBetter(Order order, bool isBuy, Price marketPrice)
        {
            bool betterSell = !isBuy && order.SetPrice > marketPrice;
            bool betterBuy = isBuy && order.SetPrice <= marketPrice;
            return betterBuy || betterSell;
        }


        private void VerifyPriceForSportOrder(Order order, Price marketPrice, DateTime priceTimestamp)
        {
            int sign = (order.Owner.SettingInstrument.IsNormal ^ order.IsBuy) ? 1 : -1;
            Price setPrice = order.SetPrice - order.DQMaxMove * sign;
            int variation = (setPrice - marketPrice) * sign;
            if (variation > order.Owner.DealingPolicyPayload.AcceptDQVariation)
            {
                order.JudgePrice = marketPrice;
                order.JudgePriceTimestamp = priceTimestamp;
                string errorDetail = string.Format("comparePrice={0}, setPrice={1}, acceptDQVariation={2}, sign={3}, variation={4}",
                    marketPrice, setPrice, order.Owner.DealingPolicyPayload.AcceptDQVariation, sign, variation);
                throw new TransactionServerException(TransactionError.OutOfAcceptDQVariation, errorDetail);
            }
        }

        private bool ShouldVerify(Order order)
        {
            Order amendedOrder = order.Owner.AmendedOrder;
            if (amendedOrder == null) return true;
            if (this.IsTheSameTradeOptionAndSetPrice(order, amendedOrder))
            {
                return false;
            }

            if (order.Owner.Type == TransactionType.OneCancelOther && amendedOrder.Owner.IsOrderCountEqualOCO)
            {
                Order theOtherOrder = amendedOrder.Owner.GetTheOtherOrderOfOCO(amendedOrder);
                if (this.IsTheSameTradeOptionAndSetPrice(order, theOtherOrder))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsTheSameTradeOptionAndSetPrice(Order order, Order amendedOrder)
        {
            return order.TradeOption == amendedOrder.TradeOption && order.SetPrice == amendedOrder.SetPrice;
        }

    }
}
