using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal static class MarketValueCalculator
    {
        internal static decimal CaculatePhysicalOrderMarketValue(Order order, Quotation quotation, DateTime? tradeDay, out decimal valueAsMargin)
        {
            Price price = order.IsBuy ? quotation.SellPrice : quotation.BuyPrice;
            return CalculateMarketValue((PhysicalOrder)order, price, tradeDay, out valueAsMargin);
        }


        internal static decimal CalculateMarketValue(PhysicalOrder order, Price price, DateTime? tradeDay, out decimal valueAsMargin)
        {
            valueAsMargin = 0;
            decimal result = 0m;
            if (!order.IsPhysical) return result;
            var physicalOpenOrder = order as PhysicalOrder;
            if ((physicalOpenOrder.PhysicalTradeSide == PhysicalTradeSide.Buy && physicalOpenOrder.IsPayoff) || physicalOpenOrder.PhysicalTradeSide == PhysicalTradeSide.Deposit)
            {
                TradePLFormula tradePLFormula = order.Owner.SettingInstrument(tradeDay).TradePLFormula;
                decimal lot = order.LotBalance, contractSize = order.Owner.ContractSize(tradeDay);
                decimal discountOfOdd = order.Owner.TradePolicyDetail(tradeDay).DiscountOfOdd;
                decimal marketValue = CalculateValue(tradePLFormula, lot, price, discountOfOdd, contractSize);
                valueAsMargin = order.Owner.CurrencyRate(tradeDay).Exchange(marketValue * order.Owner.TradePolicyDetail(tradeDay).ValueDiscountAsMargin);
                return order.Owner.CurrencyRate(tradeDay).Exchange(marketValue);
            }
            return result;
        }



        public static decimal CalculateValue(TradePLFormula tradePLFormula, decimal lot, Price price, decimal discountOfOdd, decimal contractSize)
        {
            decimal lotInteger = Math.Truncate(lot);
            decimal lotRemainder = lot - lotInteger;

            decimal marketValue = 0;
            decimal livePrice = (decimal)price;
            switch (tradePLFormula)
            {
                case TradePLFormula.S_BxCS:
                    marketValue = lotInteger * contractSize * livePrice
                        + lotRemainder * contractSize * livePrice * discountOfOdd;
                    break;
                case TradePLFormula.S_BxCSiL:
                case TradePLFormula.S_BxCSiO:
                    marketValue = lotInteger * contractSize
                        + lotRemainder * contractSize * discountOfOdd;
                    break;
                case TradePLFormula.Si1_Bi1xCSiL:
                    livePrice = 1 / livePrice;
                    marketValue = lotInteger * contractSize * livePrice
                        + lotRemainder * contractSize * livePrice * discountOfOdd;
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} is not a supported TradePLFormula", tradePLFormula));
            }
            return marketValue;
        }
    }
}
