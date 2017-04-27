using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Core.TransactionServer.Agent.Physical
{
    //internal static class PhysicalValueCalculater
    //{
    //    public static bool ExceedMaxPhysicalValue(Account account, Transaction tran,DateTime? tradeDay ,out string errorDetail)
    //    {
    //        bool freeCheck = true;
    //        decimal totalMarketValue = 0;
    //        errorDetail = null;
    //        if (tran.Owner.Setting(tradeDay).MaxPhyscialValue != null && tran.IsPhysical)
    //        {
    //            TradePolicyDetail tradePolicyDetail = tran.TradePolicyDetail(tradeDay);
    //            decimal contractSize = tran.ContractSize(tradeDay);
    //            Price sell = null;
    //            decimal shortSellLot = AddUpShortSellLot(account,tran.SettingInstrument(tradeDay),tradeDay);
    //            foreach (Order order in tran.Orders)
    //            {
    //                if (!order.IsOpen) continue;
    //                var PhysicalOpenOrder = order as PhysicalOrder;

    //                if (PhysicalOpenOrder.PhysicalTradeSide == PhysicalTradeSide.Buy)
    //                {
    //                    if (shortSellLot < order.LotBalance)
    //                    {
    //                        freeCheck = false;
    //                        decimal lot = order.LotBalance - shortSellLot;
    //                        if (sell == null)
    //                        {
    //                            var quotation = tran.AccountInstrument.Quotation;
    //                            sell = quotation.SellPrice;
    //                        }
    //                        decimal marketValue = MarketValueCalculator.CalculateValue(tran.SettingInstrument(tradeDay).TradePLFormula,
    //                            lot, sell, tradePolicyDetail.DiscountOfOdd, contractSize);
    //                        if (account.Setting().IsMultiCurrency)
    //                        {
    //                            var currency = Settings.Setting.Default.GetCurrency(tran.InstrumentId,tradeDay);
    //                            int decimals = currency.Decimals;
    //                            marketValue = Math.Round(marketValue, decimals, MidpointRounding.AwayFromZero);
    //                        }
    //                        else
    //                        {
    //                            CurrencyRate currencyRate = tran.CurrencyRate(tradeDay);
    //                            marketValue = currencyRate.Exchange(marketValue);
    //                        }
    //                        totalMarketValue += marketValue;
    //                    }
    //                    shortSellLot -= order.LotBalance;
    //                }
    //            }
    //        }

    //        if (!freeCheck)
    //        {
    //            totalMarketValue += AddUpMarketValue(account,tradeDay);
    //            if (account.Setting().IsMultiCurrency)
    //            {
    //                CurrencyRate currencyRate = tran.CurrencyRate(tradeDay);
    //                totalMarketValue = currencyRate.Exchange(totalMarketValue);
    //            }

    //            bool result = totalMarketValue > account.Setting(tradeDay).MaxPhyscialValue.Value;
    //            if (!result)
    //            {
    //                errorDetail = string.Format("MaxPhyscialValue={0}, totalMarketValue={1}", account.Setting(tradeDay).MaxPhyscialValue.Value, totalMarketValue);
    //            }
    //            return result;
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //    }

    //    private static decimal AddUpMarketValue(Account account,DateTime? tradeDay)
    //    {
    //        decimal totalMarketValue = 0;
    //        foreach (var fund in account.Funds)
    //        {
    //            totalMarketValue += fund.CalculateMarketValue(tradeDay);
    //        }
    //        return totalMarketValue;
    //    }


    //    private static decimal AddUpShortSellLot(Account account,Instrument instrument,DateTime? tradeDay)
    //    {
    //        decimal shortSellLot = 0;
    //        foreach (var fund in account.Funds)
    //        {
    //            if (fund.CurrencyId == instrument.CurrencyId)
    //            {
    //                shortSellLot = fund.CalculateShotSellLot(instrument.Id,tradeDay);
    //            }
    //        }
    //        return shortSellLot;
    //    }

    //}
}


namespace Core.TransactionServer
{
    internal sealed class AccountEqualityComparer : IEqualityComparer<Agent.Account>
    {
        public static readonly AccountEqualityComparer Default = new AccountEqualityComparer();
        private AccountEqualityComparer() { }

        public bool Equals(Agent.Account x, Agent.Account y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Agent.Account obj)
        {
            return obj.Id.GetHashCode();
        }
    }

}
