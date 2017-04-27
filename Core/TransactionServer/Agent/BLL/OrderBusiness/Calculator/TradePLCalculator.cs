using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    internal static class TradePLCalculator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradePLCalculator));

        internal static decimal Calculate(Order order, Quotation quotation, DateTime? tradeDay)
        {
            try
            {
                if (order.IsRisky)
                {
                    decimal buyPrice, sellPrice, closePrice;
                    if (order.IsBuy)
                    {
                        buyPrice = (decimal)order.ExecutePrice;
                        sellPrice = closePrice = (decimal)quotation.BuyPrice;
                    }
                    else
                    {
                        buyPrice = closePrice = (decimal)quotation.SellPrice;
                        sellPrice = (decimal)order.ExecutePrice;
                    }

                    TradePLFormula tradePLFormula = order.Owner.SettingInstrument().TradePLFormula;
                    decimal lot = order.LotBalance;
                    decimal contractSize = order.Owner.ContractSize(tradeDay);
                    return Calculate(tradePLFormula, lot * contractSize, buyPrice, sellPrice, closePrice, order.Owner.CurrencyRate(null));
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("order id = {0}, executePrice = {1}, buyPrice = {2}, sellPrice = {3}, tradeDay = {4}, order.Phase = {5}, tran.Id = {6}, account.Id = {7} ,error= {8}", order.Id, order.ExecutePrice, quotation.BuyPrice, quotation.SellPrice, tradeDay,order.Phase,order.Owner.Id, order.Owner.AccountId, ex);
                return 0m;
            }
        }


        internal static decimal Calculate(TradePLFormula tradePLFormula, decimal quantity, decimal buyPrice,
            decimal sellPrice, decimal closePrice, CurrencyRate currencyRate)
        {
            var tradePL = Calculate(tradePLFormula, quantity, buyPrice, sellPrice, closePrice);
            return currencyRate.Exchange(tradePL);
        }


        internal static decimal Calculate(TradePLFormula tradePLFormula, decimal lot, decimal contractSize, decimal buyPrice, decimal sellPrice, decimal closePrice, int decimals)
        {
            decimal result = TradePLCalculator.Calculate(tradePLFormula, lot * contractSize, buyPrice, sellPrice, closePrice);
            return Math.Round(result, decimals, MidpointRounding.AwayFromZero);
        }

        internal static decimal Calculate(TradePLFormula tradePLFormula, decimal quantity, decimal buyPrice, decimal sellPrice, decimal closePrice)
        {
            try
            {
                decimal tradePL = 0;
                switch (tradePLFormula)
                {
                    case TradePLFormula.S_BxCS:
                        tradePL = quantity * (sellPrice - buyPrice);
                        break;
                    case TradePLFormula.S_BxCSiL:
                        tradePL = quantity * (sellPrice - buyPrice) / closePrice;
                        break;
                    case TradePLFormula.Si1_Bi1xCSiL:
                        tradePL = quantity * (1 / sellPrice - 1 / buyPrice);
                        break;
                    case TradePLFormula.S_BxCSiO:
                        decimal openPrice = (closePrice == buyPrice) ? sellPrice : buyPrice;
                        tradePL = quantity * (sellPrice - buyPrice) / openPrice;
                        break;
                    default:
                        throw new NotSupportedException(string.Format("{0} is not a supported TradePLFormula", tradePLFormula));
                }
                return tradePL;
            }
            catch (System.DivideByZeroException)
            {
                Logger.ErrorFormat("buyPrice = {0}, sellPrice= {1}, closePrice = {2}", buyPrice, sellPrice, closePrice);
                throw;
            }
        }
    }
}
