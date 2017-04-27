using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class CuttingOrderCalculator
    {
        internal static decimal CalculateCommission(TradePolicyDetail tradePolicyDetail, SpecialTradePolicyDetail specialTradePolicyDetail,
            CurrencyRate currencyRate, DateTime tradeDayBeginTime, Settings.Account account, Settings.Instrument instrument,
            decimal contractSize, decimal pairRelationFactor, DateTime openOrderExecuteTime, decimal closeLot, Price closePrice)
        {
            bool isDayCloseRelation = openOrderExecuteTime >= tradeDayBeginTime;
            decimal commission = pairRelationFactor * tradePolicyDetail.GetCommissionClose(isDayCloseRelation);

            if (specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionCommissionOn)
            {
                decimal fractionCommission = pairRelationFactor * specialTradePolicyDetail.GetCommissionClose(isDayCloseRelation);

                commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * commission, (int)closeLot, contractSize, closePrice, currencyRate)
                    + FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * fractionCommission, closeLot - (int)closeLot, contractSize, closePrice, currencyRate);
            }
            else
            {
                commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * commission, closeLot, contractSize, closePrice, currencyRate);
            }

            return commission;
        }

        internal static decimal CalculateLevy(TradePolicyDetail tradePolicyDetail, SpecialTradePolicyDetail specialTradePolicyDetail,
                CurrencyRate currencyRate, Settings.Account account, Settings.Instrument instrument, decimal contractSize, decimal closeLot, Price closePrice)
        {
            decimal levy = tradePolicyDetail.LevyClose;

            if (specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionLevyOn)
            {
                decimal fractionLevy = specialTradePolicyDetail.LevyClose;

                levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * levy, (int)closeLot, contractSize, closePrice, currencyRate)
                    + FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * fractionLevy, closeLot - (int)closeLot, contractSize, closePrice, currencyRate);
            }
            else
            {
                levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * levy, closeLot, contractSize, closePrice, currencyRate);
            }

            return levy;
        }

        internal static void CalculateFee(TradePolicyDetail tradePolicyDetail, SpecialTradePolicyDetail specialTradePolicyDetail,
            CurrencyRate currencyRate, DateTime tradeDayBeginTime, Settings.Account account, Settings.Instrument instrument,
            decimal contractSize, decimal pairRelationFactor, DateTime openOrderExecuteTime, decimal closedLot, Price executePrice,
            out decimal commission, out decimal levy)
        {
            bool isDayCloseRelation = openOrderExecuteTime >= tradeDayBeginTime;

            commission = pairRelationFactor * tradePolicyDetail.GetCommissionClose(isDayCloseRelation);
            levy = tradePolicyDetail.LevyClose;

            if (specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionCommissionOn)
            {
                decimal fractionCommission = pairRelationFactor * specialTradePolicyDetail.GetCommissionClose(isDayCloseRelation);

                commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * commission, (int)closedLot, contractSize, executePrice, currencyRate)
                    + FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * fractionCommission, closedLot - (int)closedLot, contractSize, executePrice, currencyRate);
            }
            else
            {
                commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * commission, closedLot, contractSize, executePrice, currencyRate);
            }

            if (specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionLevyOn)
            {
                decimal fractionLevy = specialTradePolicyDetail.LevyClose;

                levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * levy, (int)closedLot, contractSize, executePrice, currencyRate)
                    + FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * fractionLevy, closedLot - (int)closedLot, contractSize, executePrice, currencyRate);
            }
            else
            {
                levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * levy, closedLot, contractSize, executePrice, currencyRate);
            }
        }
    }
}