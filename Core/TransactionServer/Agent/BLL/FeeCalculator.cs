using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Engine;

namespace Core.TransactionServer.Agent.BLL
{
    public static class FeeCalculator
    {
        internal static decimal CalculateLevy(FeeFormula levyFormula, TradePLFormula tradePLFormula, decimal unitLevy, decimal lot, decimal contractSize, Price price, CurrencyRate currencyRate, decimal tradePL = 0)
        {
            if (unitLevy > 1)
            {
                unitLevy = Math.Round(unitLevy, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
            }

            decimal levy = 0;
            switch (levyFormula)
            {
                case FeeFormula.FixedAmount:
                    levy = unitLevy * lot;
                    levy = Math.Round(levy, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
                case FeeFormula.CS:
                    levy = unitLevy * lot * contractSize;
                    levy = -currencyRate.Exchange(-levy);
                    break;
                case FeeFormula.CSDividePrice:
                    levy = unitLevy * lot * contractSize / (decimal)price;
                    levy = -currencyRate.Exchange(-levy);
                    break;
                case FeeFormula.CSMultiplyPrice:
                    levy = unitLevy * lot * contractSize * (decimal)price;
                    levy = -currencyRate.Exchange(-levy);
                    break;
                case FeeFormula.Pips:
                    Price buyPrice, sellPrice;
                    if ((int)tradePLFormula != 2)
                    {
                        buyPrice = price;
                        sellPrice = price + (int)unitLevy;
                    }
                    else
                    {
                        buyPrice = price + (int)unitLevy;
                        sellPrice = price;
                    }
                    Price closePrice = price;

                    levy = TradePLCalculator.Calculate(tradePLFormula, lot, contractSize, (decimal)buyPrice, (decimal)sellPrice, (decimal)closePrice, currencyRate.SourceCurrency.Decimals);
                    levy = -currencyRate.Exchange(-levy);
                    break;
                case FeeFormula.PricePips:
                    levy = 0;
                    break;

                case FeeFormula.RealizedProfit:
                    levy = tradePL > 0 ? unitLevy * tradePL : 0;
                    break;
                case FeeFormula.RealizedLoss:
                    levy = tradePL < 0 ? unitLevy * tradePL : 0;
                    break;
                case FeeFormula.RealizedPL:
                    levy = unitLevy * Math.Abs(tradePL);
                    break;
                case FeeFormula.SharedPL:
                    levy = unitLevy * tradePL;
                    break;
            }

            return levy;
        }

        internal static decimal CalculateCommission(FeeFormula commissionFormula, TradePLFormula tradePLFormula, decimal unitCommission, decimal lot, decimal contractSize, Price price, CurrencyRate currencyRate, decimal tradePL = 0)
        {
            if (unitCommission > 1)
            {
                unitCommission = Math.Round(unitCommission, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
            }

            decimal commission = 0;
            switch (commissionFormula)
            {
                case FeeFormula.FixedAmount:
                    commission = unitCommission * lot;
                    commission = Math.Round(commission, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
                case FeeFormula.CS:
                    commission = unitCommission * lot * contractSize;
                    commission = -currencyRate.Exchange(-commission);
                    break;
                case FeeFormula.CSDividePrice:
                    commission = unitCommission * lot * contractSize / (decimal)price;
                    commission = -currencyRate.Exchange(-commission);
                    break;
                case FeeFormula.CSMultiplyPrice:
                    commission = unitCommission * lot * contractSize * (decimal)price;
                    commission = -currencyRate.Exchange(-commission);
                    break;
                case FeeFormula.Pips:
                    Price buyPrice, sellPrice;
                    if ((int)tradePLFormula != 2)
                    {
                        buyPrice = price;
                        sellPrice = price + (int)unitCommission;
                    }
                    else
                    {
                        buyPrice = price + (int)unitCommission;
                        sellPrice = price;
                    }
                    Price closePrice = price;

                    commission = TradePLCalculator.Calculate(tradePLFormula, lot, contractSize, (decimal)buyPrice, (decimal)sellPrice, (decimal)closePrice, currencyRate.SourceCurrency.Decimals);
                    commission = -currencyRate.Exchange(-commission);
                    break;
                case FeeFormula.PricePips:
                    commission = 0;
                    break;

                case FeeFormula.RealizedProfit:
                    commission = tradePL > 0 ? unitCommission * tradePL : 0;
                    break;
                case FeeFormula.RealizedLoss:
                    commission = tradePL < 0 ? unitCommission * tradePL : 0;
                    break;
                case FeeFormula.RealizedPL:
                    commission = unitCommission * Math.Abs(tradePL);
                    break;
                case FeeFormula.SharedPL:
                    commission = unitCommission * tradePL;
                    break;
            }

            return commission;
        }

        internal static CurrencyRate GetCGSELevyCurrencyRate(Settings.Account account, Settings.Instrument instrument, SpecialTradePolicyDetail specialTradePolicyDetail, CurrencyRate defaultCurrencyRate, ExecuteContext context)
        {
            if (specialTradePolicyDetail.CGSELevyCurrecyType == CGSELevyCurrecyType.UseInstrumentCurrencyType)
            {
                return defaultCurrencyRate;
            }
            else
            {
                Guid sourceCurrencyId = account.CurrencyId;
                Guid targetCurrencyId = account.IsMultiCurrency ? instrument.CurrencyId : account.CurrencyId;

                return context != null && context.ShouldUseHistorySettings ? Settings.Setting.Default.GetCurrencyRate(sourceCurrencyId, targetCurrencyId, context.TradeDay)
                                                                            : Settings.Setting.Default.GetCurrencyRate(sourceCurrencyId, targetCurrencyId);
            }
        }

        internal static decimal CalculateCGSELevy(decimal lot, bool isOpen, SpecialTradePolicyDetail policy, CurrencyRate currencyRate)
        {
            int roundValue = (int)Math.Round(lot, MidpointRounding.AwayFromZero);
            decimal levy = roundValue * (isOpen ? policy.CGSENewLevyMultipler : policy.CGSECloseLevyMultipler);
            if (lot > roundValue)
            {
                levy += (isOpen ? policy.CGSENewLevyRemainder : policy.CGSECloseLevyRemainder);
            }
            return -currencyRate.Exchange(-levy);
        }

        internal static decimal CaculateInstalmentAdministrationFee(decimal value, decimal lot, InstalmentPolicyDetail instalmentPolicyDetail, CurrencyRate currencyRate)
        {
            decimal result = 0;
            switch (instalmentPolicyDetail.InstalmentFeeType)
            {
                case InstalmentFeeType.FixedAmountPerLot:
                    result = lot * instalmentPolicyDetail.InstalmentFeeRate;
                    result = Math.Round(result, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
                case InstalmentFeeType.ValueProportion:
                    result = value * instalmentPolicyDetail.InstalmentFeeRate;
                    result = -currencyRate.Exchange(-result);
                    break;
                case InstalmentFeeType.FixedAmount:
                    result = instalmentPolicyDetail.InstalmentFeeRate;
                    result = Math.Round(result, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
                default:
                    result = 0;
                    break;
            }
            return result;
        }

    }
}
