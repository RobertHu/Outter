using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.AccountClass.Cutting
{
    internal interface IAccountSettingFeeProvider
    {
        decimal RateOtherFee { get; }
        decimal RateCommission { get; }
        decimal RateLevy { get; }
    }

    internal interface ISpecialTradePolicyDetailForCut
    {
        decimal GetCommissionClose(DateTime executeTime);
        decimal LevyClose { get; }
        bool IsFractionCommissionOn { get; }
        bool IsFractionLevyOn { get; }
        CGSELevyCurrecyType CGSELevyCurrecyType { get; }
        decimal CGSENewLevyRemainder { get; }
        decimal CGSECloseLevyRemainder { get; }
    }

    internal interface ITradePolicyDetailForCut
    {
        decimal GetCommissionClose(DateTime executeTime);
        decimal LevyClose { get; }
        decimal OtherFeeClose { get; }
        decimal ContractSize { get; }
    }


    internal interface IInstrumentProvider
    {
        FeeFormula CommissionFormula { get; }
        FeeFormula LevyFormula { get; }
        FeeFormula OtherFeeFormula { get; }
        TradePLFormula TradePLFormula { get; }
        int Denominator { get; }
    }

    internal interface ICuttingFeeInfoProvider
    {
        bool IncludeFeeOnRiskAction { get; }
        bool CanTrade(DateTime baseTime);
        List<Order> ExecutedAndHasPositionOrders { get; }
        RiskLevelAction RiskLevelAction { get; }
        Quotation Quotation { get; }
        CurrencyRate CurrencyRate { get; }
        int CurrencyDecimals { get; }
        decimal GetCurrencyRate(Guid sourceCurrencyId, Guid targetCurrencyId);
        IAccountSettingFeeProvider AccountSettingFeeProvider { get; }
        IInstrumentProvider InstrumentProvider { get; }
        ITradePolicyDetailForCut TradePolicyDetailForCut { get; }
        ISpecialTradePolicyDetailForCut SpecialTradePolicyDetailForCut { get; }
    }
}
