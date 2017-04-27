using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.TransactionBusiness
{
    internal static class PhysicalValueChecker
    {
        public static bool IsExceedMaxPhysicalValue(PhysicalTransaction tran, DateTime? tradeDay, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (ShouldVerifyIsExceedMaxPhysicalValue(tran.Owner))
            {
                return VerifyIsExceedMaxPhysicalValue(tran, tradeDay, out errorMsg);
            }
            return false;
        }

        private static bool ShouldVerifyIsExceedMaxPhysicalValue(Account account)
        {
            return account.Setting().MaxPhyscialValue != null;
        }


        private static bool VerifyIsExceedMaxPhysicalValue(Transaction tran, DateTime? tradeDay, out string errorMsg)
        {
            errorMsg = string.Empty;
            var account = tran.Owner;
            decimal totalMarketValue = 0m;
            bool freeCheck = true;
            decimal shortSellLot = GetAllExcuteShortSellLot(tran.InstrumentId, tran.Owner);
            totalMarketValue += CalculateMarketValue(tran, shortSellLot, tradeDay);
            freeCheck &= IsFreeCheck(tran, shortSellLot);
            if (!freeCheck)
            {
                totalMarketValue += GetAllExecuteMarketValue(tran.Owner);
                if (account.IsMultiCurrency)
                {
                    var currencyRate = tran.CurrencyRate(null);
                    totalMarketValue = currencyRate.Exchange(totalMarketValue);
                }
                bool result = totalMarketValue > account.Setting().MaxPhyscialValue.Value;
                if (result)
                {
                    errorMsg = string.Format("MaxPhyscialValue={0}, totalMarketValue={1}", account.Setting().MaxPhyscialValue.Value, totalMarketValue);
                }
                return result;
            }
            else
            {
                return false;
            }
        }

        private static decimal GetAllExecuteMarketValue(Account account)
        {
            decimal result = 0m;
            foreach (var tran in account.Transactions)
            {
                if (!tran.IsPhysical) continue;
                foreach (PhysicalOrder order in tran.Orders)
                {
                    bool isExecuted = order.Phase == OrderPhase.Executed;
                    bool isDeposit = order.PhysicalTradeSide == PhysicalTradeSide.Deposit;
                    bool isBuy = order.PhysicalTradeSide == PhysicalTradeSide.Buy;
                    if (order.IsOpen && order.IsPhysical && isExecuted && (isDeposit || isBuy))
                    {
                        result += order.MarketValue;
                    }
                }
            }
            return result;
        }

        private static decimal GetAllExcuteShortSellLot(Guid instrumentId, Account account)
        {
            decimal result = 0m;
            foreach (var tran in account.Transactions)
            {
                if (tran.InstrumentId != instrumentId) continue;
                foreach (PhysicalOrder order in tran.Orders)
                {
                    if (order.Phase == OrderPhase.Executed && order.PhysicalTradeSide == PhysicalTradeSide.ShortSell)
                    {
                        result += order.LotBalance;
                    }
                }
            }
            return result;
        }


        private static bool IsFreeCheck(Transaction tran, decimal shortSellLot)
        {
            decimal totalOpenBuyLot = 0m;
            foreach (PhysicalOrder order in tran.Orders)
            {
                if (!order.IsPhysical) continue;
                if (order.IsOpen && order.PhysicalTradeSide == PhysicalTradeSide.Buy)
                {
                    totalOpenBuyLot += order.LotBalance;
                }
            }
            return totalOpenBuyLot > shortSellLot;
        }

        private static decimal CalculateMarketValue(Transaction tran, decimal originShortSellLot, DateTime? tradeDay)
        {
            decimal totalMarketValue = 0m;
            var tradePolicyDetail = tran.TradePolicyDetail(tradeDay);
            decimal contractSize = tran.ContractSize(tradeDay) == 0 ? tradePolicyDetail.ContractSize : tran.ContractSize(tradeDay);
            decimal remainShortSellLot = originShortSellLot;
            foreach (PhysicalOrder order in tran.Orders)
            {
                if (!order.IsPhysical) continue;
                if (order.IsOpen && order.PhysicalTradeSide == PhysicalTradeSide.Buy)
                {
                    if (remainShortSellLot < order.LotBalance)
                    {
                        decimal lot = order.LotBalance - remainShortSellLot;
                        var quotation = tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
                        decimal marketValue = MarketValueCalculator.CalculateValue(tran.SettingInstrument(tradeDay).TradePLFormula,
                            lot, quotation.SellPrice, tradePolicyDetail.DiscountOfOdd, contractSize);
                        if (tran.Owner.Setting(tradeDay).IsMultiCurrency)
                        {
                            int decimals = tran.AccountInstrument.Currency(tradeDay).Decimals;
                            marketValue = Math.Round(marketValue, decimals, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            var currencyRate = tran.CurrencyRate(tradeDay);
                            marketValue = currencyRate.Exchange(marketValue);
                        }
                        totalMarketValue += marketValue;
                    }
                    remainShortSellLot -= order.LotBalance;
                    if (remainShortSellLot < 0) remainShortSellLot = 0;
                }
            }
            return totalMarketValue;
        }


    }
}
