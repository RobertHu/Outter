using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Engine;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL
{
    public interface IFees
    {
        decimal CommissionSum { get; }
        decimal LevySum { get; }
        decimal OtherFee { get; }
    }

    public abstract class OrderFeeCalculatorBase
    {
        protected enum OpenCloseType
        {
            Open,
            Close
        }

        protected Order _order;
        protected OrderSettings _settings;

        protected OrderFeeCalculatorBase(Order order, OrderSettings settings)
        {
            _order = order;
            _settings = settings;
        }

        internal Transaction Tran
        {
            get { return _order.Owner; }
        }

        public IFees Calculate(ExecuteContext context)
        {
            if (this.ShouldCalculateFees())
            {
                return this.DoCalculate(context);
            }
            return this.CalculateWhenNotNeeded();
        }

        public void CalculateFeeAsCost(ExecuteContext context)
        {
            if (this.ShouldCalculateFees())
            {
                this.AdjustPrice(context);
            }
        }

        protected abstract IFees CalculateWhenNotNeeded();

        protected abstract IFees DoCalculate(ExecuteContext context);

        private void AdjustPrice(ExecuteContext context)
        {
            var openCloseType = _order.IsOpen ? OpenCloseType.Open : OpenCloseType.Close;
            this.AdjustPricePipsByCommission(openCloseType, context);
            this.AdjustPricePipsByLevy(openCloseType, context);
            this.AdjustPricePipsByOtherFee(openCloseType, context);
        }

        protected abstract decimal CalculateCommission(ExecuteContext context);
        protected abstract decimal CalculateLevy(ExecuteContext context);

        protected abstract decimal CalculateOtherFee(ExecuteContext context);

        protected virtual bool ShouldCalculateFees()
        {
            return _order.OrderType != OrderType.MultipleClose;
        }

        protected void AdjustPricePipsByCommission(OpenCloseType openCloseType, ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            if (setting.Item1.CommissionFormula.TakeFeeAsCost()) //Adjust PricePips
            {
                decimal commission = setting.Item2.RateCommission * (openCloseType == OpenCloseType.Open ? setting.Item3.CommissionOpen : setting.Item3.CommissionCloseD);
                _settings.ExecutePrice += (int)Math.Round(commission * (_order.IsBuy == setting.Item1.IsNormal ? 1 : -1), 0, MidpointRounding.AwayFromZero);
            }
        }

        internal Tuple<Settings.Instrument, Settings.Account, Settings.TradePolicyDetail, Settings.SpecialTradePolicyDetail, Settings.CurrencyRate> GetHistorySettings(ExecuteContext context)
        {
            var instrument = this.Tran.SettingInstrument(context.TradeDay);
            var account = this.Tran.Owner.Setting(context.TradeDay);
            var tradePolicyDetail = this.Tran.TradePolicyDetail(context.TradeDay);
            var specialTradePolicyDetail = this.Tran.SpecialTradePolicyDetail(context.TradeDay);
            var currencyRate = this.Tran.CurrencyRate(context.TradeDay);
            return Tuple.Create(instrument, account, tradePolicyDetail, specialTradePolicyDetail, currencyRate);
        }


        protected void AdjustPricePipsByLevy(OpenCloseType openCloseType, ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            if (setting.Item1.LevyFormula.TakeFeeAsCost())//Adjust PricePips
            {
                decimal levy = setting.Item2.RateLevy * (openCloseType == OpenCloseType.Open ? setting.Item3.LevyOpen : setting.Item3.LevyClose);
                _settings.ExecutePrice += (int)Math.Round(levy * (_order.IsBuy == setting.Item1.IsNormal ? 1 : -1), 0, MidpointRounding.AwayFromZero);
            }
        }

        protected void AdjustPricePipsByOtherFee(OpenCloseType openCloseType, ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            if (setting.Item1.OtherFeeFormula.TakeFeeAsCost())//Adjust PricePips
            {
                decimal otherFee = setting.Item2.RateOtherFee * (openCloseType == OpenCloseType.Open ? setting.Item3.OtherFeeOpen : setting.Item3.OtherFeeClose);
                _settings.ExecutePrice += (int)Math.Round(otherFee * (_order.IsBuy == setting.Item1.IsNormal ? 1 : -1), 0, MidpointRounding.AwayFromZero);
            }

        }


    }

    public abstract class CloseOrderFeeBookCalculatorBase : OrderFeeCalculatorBase
    {
        private enum FeeType
        {
            Commission,
            Levy,
            OtherFee
        }

        protected CloseOrderFeeBookCalculatorBase(Order order, OrderSettings settings)
            : base(order, settings)
        {
        }

        protected override bool ShouldCalculateFees()
        {
            return base.ShouldCalculateFees() && !_order.IsOpen;
        }

        protected override decimal CalculateCommission(ExecuteContext context)
        {
            return this.CalculateHelper(FeeType.Commission);
        }

        protected override decimal CalculateLevy(ExecuteContext context)
        {
            return this.CalculateHelper(FeeType.Levy);
        }

        protected override decimal CalculateOtherFee(ExecuteContext context)
        {
            return this.CalculateHelper(FeeType.OtherFee);
        }

        private decimal CalculateHelper(FeeType feeType)
        {
            decimal result = 0m;
            foreach (var eachOrderRelation in _order.OrderRelations)
            {
                if (feeType == FeeType.Commission)
                {
                    result += eachOrderRelation.Commission;
                }
                else if (feeType == FeeType.Levy)
                {
                    result += eachOrderRelation.Levy;
                }
                else if (feeType == FeeType.OtherFee)
                {
                    result += eachOrderRelation.OtherFee;
                }
            }
            return result;
        }


    }

    public sealed class CloseOrderFeeBookCalculator : CloseOrderFeeBookCalculatorBase
    {
        internal CloseOrderFeeBookCalculator(Order order, OrderSettings settings)
            : base(order, settings)
        {
        }


        protected override IFees DoCalculate(ExecuteContext context)
        {
            return new OrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context));
        }

        protected override IFees CalculateWhenNotNeeded()
        {
            return OrderFees.Empty;
        }
    }

    public sealed class PhysicalCloseOrderFeeBookCalculator : CloseOrderFeeBookCalculatorBase
    {
        internal PhysicalCloseOrderFeeBookCalculator(PhysicalOrder order, Physical.OrderBusiness.PhysicalOrderSettings settings)
            : base(order, settings)
        {
        }

        protected override IFees DoCalculate(ExecuteContext context)
        {
            return new PhysicalOrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context), 0m);
        }

        protected override IFees CalculateWhenNotNeeded()
        {
            return PhysicalOrderFees.Empty;
        }
    }


    public sealed class OrderFees : IFees
    {
        public static readonly OrderFees Empty = new OrderFees(0m, 0m, 0m);
        internal OrderFees(decimal commissionSum, decimal levySum, decimal otherFee)
        {
            this.CommissionSum = commissionSum;
            this.LevySum = levySum;
            this.OtherFee = otherFee;
        }

        public decimal CommissionSum { get; private set; }

        public decimal LevySum { get; private set; }

        public decimal OtherFee { get; private set; }
    }


    internal class OpenOrderFeeCalculator : OrderFeeCalculatorBase
    {
        internal OpenOrderFeeCalculator(Order order, OrderSettings settings)
            : base(order, settings)
        {
        }

        protected override IFees CalculateWhenNotNeeded()
        {
            return OrderFees.Empty;
        }

        protected override decimal CalculateCommission(ExecuteContext context)
        {
            decimal result = 0m;
            var setting = this.GetHistorySettings(context);
            var instrument = setting.Item1;
            var account = setting.Item2;
            var currencyRate = setting.Item5;
            var tradePolicyDetail = setting.Item3;
            if (instrument.CommissionFormula.TakeFeeAsCost()) return result;
            var specialTradePolicyDetail = setting.Item4;
            decimal contractSize = !context.ShouldUseHistorySettings ? this.Tran.ContractSize(context.TradeDay) : tradePolicyDetail.ContractSize;

            if (!instrument.CommissionFormula.IsDependOnPL() && specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionCommissionOn)
            {
                result = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * tradePolicyDetail.CommissionOpen, (int)_order.Lot, contractSize, _order.ExecutePrice, currencyRate)
                    + FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * specialTradePolicyDetail.CommissionOpen, _order.Lot - (int)_order.Lot, contractSize, _order.ExecutePrice, currencyRate);
            }
            else
            {
                result = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * tradePolicyDetail.CommissionOpen, _order.Lot, contractSize, _order.ExecutePrice, currencyRate);
            }

            if (result >= 0)
            {
                result = Math.Max(result, tradePolicyDetail.MinCommissionOpen);
            }
            return result;
        }


        protected override decimal CalculateLevy(ExecuteContext context)
        {
            decimal result = 0m;
            var setting = this.GetHistorySettings(context);
            var instrument = setting.Item1;
            var account = setting.Item2;
            var currencyRate = setting.Item5;
            var tradePolicyDetail = setting.Item3;
            decimal contractSize = !context.ShouldUseHistorySettings ? this.Tran.ContractSize(context.TradeDay) : tradePolicyDetail.ContractSize;
            if (instrument.LevyFormula.TakeFeeAsCost()) return result;
            var specialTradePolicyDetail = setting.Item4;
            if (!instrument.LevyFormula.IsDependOnPL() && specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionLevyOn)
            {
                result = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * tradePolicyDetail.LevyOpen, (int)_order.Lot, contractSize, _order.ExecutePrice, currencyRate)
                    + FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * specialTradePolicyDetail.LevyOpen, _order.Lot - (int)_order.Lot, contractSize, _order.ExecutePrice, currencyRate);
            }
            else
            {
                result = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * tradePolicyDetail.LevyOpen, _order.Lot, contractSize, _order.ExecutePrice, currencyRate);
            }

            if (!instrument.LevyFormula.IsDependOnPL() && specialTradePolicyDetail != null)
            {
                var cgseLevyCurrencyRate = specialTradePolicyDetail.GetCGSELevyCurrencyRate(account, instrument, currencyRate, context);
                result += FeeCalculator.CalculateCGSELevy(_order.Lot, true, specialTradePolicyDetail, cgseLevyCurrencyRate);
            }
            return result;
        }

        protected override decimal CalculateOtherFee(ExecuteContext context)
        {
            decimal result = 0m;
            var setting = this.GetHistorySettings(context);
            var instrument = setting.Item1;
            var account = setting.Item2;
            var currencyRate = setting.Item5;
            var tradePolicyDetail = setting.Item3;
            decimal contractSize = !context.ShouldUseHistorySettings ? this.Tran.ContractSize(context.TradeDay) : tradePolicyDetail.ContractSize;
            if (instrument.OtherFeeFormula.TakeFeeAsCost()) return result;
            result = FeeCalculator.CalculateLevy(instrument.OtherFeeFormula, instrument.TradePLFormula, account.RateOtherFee * tradePolicyDetail.OtherFeeOpen, _order.Lot, contractSize, _order.ExecutePrice, currencyRate);
            return result;
        }

        protected override IFees DoCalculate(ExecuteContext context)
        {
            return new OrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context));
        }
    }

    internal abstract class CloseOrderFeeCalculatorBase : OrderFeeCalculatorBase
    {
        protected CloseOrderFeeCalculatorBase(Order order, OrderSettings settings)
            : base(order, settings) { }

        protected override IFees DoCalculate(ExecuteContext context)
        {
            foreach (OrderRelation eachOrderRelation in _order.OrderRelations)
            {
                eachOrderRelation.CalculateFee(context);
            }
            return this.GetResult(context);
        }

        protected abstract IFees GetResult(ExecuteContext context);

        protected override decimal CalculateCommission(ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            var account = _order.Owner.Owner;
            var tran = _order.Owner;
            var instrument = setting.Item1;
            var tradePolicyDetail = setting.Item3;
            var currencyRate = setting.Item5;
            decimal result = 0m;
            if (instrument.CommissionFormula.TakeFeeAsCost()) return result;
            foreach (OrderRelation eachOrderRelation in _order.OrderRelations)
            {
                result += eachOrderRelation.Commission;
            }

            if (result >= 0 && result < tradePolicyDetail.MinCommissionClose)
            {
                decimal oldCommissionSum = result;
                result = tradePolicyDetail.MinCommissionClose;
                this.ReassignCommissionForOrderRelations(_order, oldCommissionSum, currencyRate.TargetCurrency.Decimals, result);
            }
            return result;
        }

        private void ReassignCommissionForOrderRelations(Order order, decimal oldCommissionSum, int decimals, decimal commissionSum)
        {
            int relationCount = order.OrderRelations.Count();
            decimal remainCommission = commissionSum;
            foreach (OrderRelation eachOrderRelation in order.OrderRelations)
            {
                relationCount--;
                if (relationCount == 0)
                {
                    eachOrderRelation.Commission += remainCommission;
                }
                else
                {
                    decimal commission = 0m;
                    if (oldCommissionSum != 0m)
                    {
                        commission = Math.Round(commissionSum * eachOrderRelation.Commission / oldCommissionSum, decimals, MidpointRounding.AwayFromZero);
                    }
                    eachOrderRelation.Commission += commission;
                    remainCommission -= commission;
                }
            }
        }

        protected override decimal CalculateLevy(ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            var account = _order.Owner.Owner;
            var tran = _order.Owner;
            var instrument = setting.Item1;
            decimal result = 0m;
            if (instrument.LevyFormula.TakeFeeAsCost()) return result;
            foreach (OrderRelation eachOrderRelation in _order.OrderRelations)
            {
                result += eachOrderRelation.Levy;
            }
            return result;
        }

        protected override decimal CalculateOtherFee(ExecuteContext context)
        {
            var setting = this.GetHistorySettings(context);
            var account = _order.Owner.Owner;
            var tran = _order.Owner;
            var instrument = setting.Item1;
            decimal result = 0m;
            if (instrument.OtherFeeFormula.TakeFeeAsCost()) return result;
            foreach (OrderRelation eachOrderRelation in _order.OrderRelations)
            {
                result += eachOrderRelation.OtherFee;
            }
            return result;
        }
    }

    internal sealed class CloseOrderFeeCalculator : CloseOrderFeeCalculatorBase
    {
        internal CloseOrderFeeCalculator(Order order, OrderSettings settings)
            : base(order, settings) { }

        protected override IFees CalculateWhenNotNeeded()
        {
            return OrderFees.Empty;
        }

        protected override IFees GetResult(ExecuteContext context)
        {
            return new OrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context));
        }
    }

    internal sealed class PhysicalOrderFees : IFees
    {
        public static readonly PhysicalOrderFees Empty = new PhysicalOrderFees(0m, 0m, 0m, 0m);
        internal PhysicalOrderFees(decimal commissionSum, decimal levySum, decimal otherFee, decimal instalmentAdministrationFee)
        {
            this.CommissionSum = commissionSum;
            this.LevySum = levySum;
            this.OtherFee = otherFee;
            this.InstalmentAdministrationFee = instalmentAdministrationFee;
        }

        internal decimal InstalmentAdministrationFee { get; private set; }

        public decimal CommissionSum { get; private set; }

        public decimal LevySum { get; private set; }

        public decimal OtherFee { get; private set; }
    }


    internal sealed class PhysicalOpenOrderFeeCalculator : OpenOrderFeeCalculator
    {
        internal PhysicalOpenOrderFeeCalculator(PhysicalOrder order, OrderSettings settings)
            : base(order, settings) { }

        protected override IFees CalculateWhenNotNeeded()
        {
            return PhysicalOrderFees.Empty;
        }

        protected override IFees DoCalculate(ExecuteContext context)
        {
            var commissionSum = this.CalculateCommission(context);
            var levySum = this.CalculateLevy(context);
            var otherFee = this.CalculateOtherFee(context);
            var instalmentAdministationFee = this.CalculateInstalmentAdministrationFee(context);
            return new PhysicalOrderFees(commissionSum, levySum, otherFee, instalmentAdministationFee);
        }

        internal decimal CalculateInstalmentAdministrationFee(ExecuteContext context)
        {
            var phsyicalOrder = _order as PhysicalOrder;
            return phsyicalOrder.CalculateInstalmentAdministrationFee(context);
        }
    }

    internal sealed class PhysicalCloseOrderFeeCalculator : CloseOrderFeeCalculatorBase
    {
        internal PhysicalCloseOrderFeeCalculator(PhysicalOrder order, OrderSettings settings)
            : base(order, settings) { }


        protected override IFees GetResult(ExecuteContext context)
        {
            var phsyicalOrder = _order as PhysicalOrder;
            var instalmentAdministrationFee = phsyicalOrder.CalculateInstalmentAdministrationFee(context);
            return new PhysicalOrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context), instalmentAdministrationFee);
        }

        protected override IFees CalculateWhenNotNeeded()
        {
            return PhysicalOrderFees.Empty;
        }
    }

    internal sealed class BOOrderFeeCalculator : OrderFeeCalculatorBase
    {
        internal BOOrderFeeCalculator(BinaryOption.Order order, BOOrderSettings settings)
            : base(order, settings) { }

        protected override IFees CalculateWhenNotNeeded()
        {
            return OrderFees.Empty;
        }

        protected override decimal CalculateCommission(ExecuteContext context)
        {
            if (!_order.IsOpen) return 0m;
            var boOrder = (BinaryOption.Order)_order;
            decimal result = 0m;
            var tradePolicyDetail = _order.Owner.TradePolicyDetail();
            var key = new BinaryOption.BOPolicyDetailKey(tradePolicyDetail.BinaryOptionPolicyID.Value, boOrder.BetTypeId, boOrder.Frequency);
            BinaryOption.BOPolicyDetail binaryOptionPolicyDetail;
            BinaryOption.BOPolicyDetailRepository.Default.TryGet(key, out binaryOptionPolicyDetail);
            result = Math.Max(binaryOptionPolicyDetail.CommissionOpen * boOrder.Lot, binaryOptionPolicyDetail.MinCommissionOpen);
            return result;
        }

        protected override decimal CalculateLevy(ExecuteContext context)
        {
            return 0m;
        }

        protected override decimal CalculateOtherFee(ExecuteContext context)
        {
            return 0m;
        }


        protected override IFees DoCalculate(ExecuteContext context)
        {
            return new OrderFees(this.CalculateCommission(context), this.CalculateLevy(context), this.CalculateOtherFee(context));
        }
    }
}
