using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util.TypeExtension;

namespace Core.TransactionServer.Agent.OrderBusiness.Calculator
{
    internal static class NecessaryCalculator
    {
        internal static decimal CalculateNecessary(Order order, Quotation quotation, DateTime? tradeDay)
        {
            MarginFormula marginFormula = order.Owner.SettingInstrument().MarginFormula;
            Price price = order.ExecutePrice;
            if (marginFormula.MarketPriceInvolved())
            {
                price = order.IsBuy ? quotation.BuyPrice : quotation.SellPrice;
            }
            return NecessaryCalculator.CalculateNecessary(order, price, order.LotBalance, tradeDay);
        }

        internal static decimal CalculateNecessary(Order order, Price price, decimal lot, DateTime? tradeDay)
        {
            MarginFormula marginFormula = order.Owner.SettingInstrument().MarginFormula;
            CurrencyRate currencyRate = order.Owner.CurrencyRate(null);
            decimal contractSize = order.Owner.ContractSize(tradeDay);
            if (price == null)
            {
                price = order.ExecutePrice;
                if (marginFormula.MarketPriceInvolved())
                {
                    price = order.GetLivePriceForCalculateNecessary();
                }
            }
            return CalculateNecessary(order, marginFormula, lot, contractSize, price, currencyRate);
        }

        internal static decimal CalculateNecessary(Order order, MarginFormula marginFormula, decimal lotBalance, decimal contractSize, Price price, CurrencyRate currencyRate)
        {
            if (order.IsRisky)
            {
                decimal margin = 0;

                switch (marginFormula)
                {
                    case MarginFormula.FixedAmount:
                        margin = lotBalance;
                        margin = Math.Round(margin, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                        break;
                    case MarginFormula.CS:
                        margin = lotBalance * contractSize;
                        margin = -currencyRate.Exchange(-margin);
                        break;
                    case MarginFormula.CSiPrice:
                    case MarginFormula.CSiMarketPrice:
                        margin = lotBalance * contractSize / (decimal)price;
                        margin = -currencyRate.Exchange(-margin);
                        break;
                    case MarginFormula.CSxPrice:
                    case MarginFormula.CSxMarketPrice:
                        margin = lotBalance * contractSize * (decimal)price;
                        margin = -currencyRate.Exchange(-margin);
                        break;
                    case MarginFormula.FKLI:
                    case MarginFormula.FCPO:
                        margin = lotBalance;
                        margin = Math.Round(margin, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                        break;
                    default:
                        throw new NotSupportedException(string.Format("{0} is not a supported MarginFormula", marginFormula));
                }

                return margin;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// used from reset
        /// </summary>
        /// <param name="marginFormula"></param>
        /// <param name="lotBalance"></param>
        /// <param name="contractSize"></param>
        /// <param name="price"></param>
        /// <param name="livePrice"></param>
        /// <param name="rateIn"></param>
        /// <param name="rateOut"></param>
        /// <param name="decimals"></param>
        /// <param name="sourceDecimals"></param>
        /// <returns></returns>
    }

}