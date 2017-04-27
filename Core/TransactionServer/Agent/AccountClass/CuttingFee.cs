using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using log4net;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class CuttingFee
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CuttingFee));

        private sealed class CuttingItem
        {
            internal CuttingItem(bool isBuy, decimal contractSize, decimal closeLot, DateTime executeTime, Price executePrice)
            {
                this.IsBuy = isBuy;
                this.ContractSize = contractSize;
                this.CuttingLot = closeLot;
                this.ExecuteTime = executeTime;
                this.ExecutePrice = executePrice;
            }

            internal bool IsBuy { get; private set; }
            internal decimal ContractSize { get; private set; }
            internal decimal CuttingLot { get; private set; }
            internal DateTime ExecuteTime { get; private set; }
            internal Price ExecutePrice { get; private set; }
        }

        private Instrument _owner;

        internal CuttingFee(Instrument owner)
        {
            this._owner = owner;
        }

        internal decimal Calculate()
        {
            if (!Settings.Setting.Default.SystemParameter.IncludeFeeOnRiskAction || !_owner.Trading.CanTrade(Market.MarketManager.Now, PlaceContext.Empty))
            {
                return 0m;
            }
            return this.InnerCalculate();
        }

        private decimal InnerCalculate()
        {
            decimal buyLotSum = 0, sellLotSum = 0;
            List<Order> openBuyOrders = null, openSellOrders = null;
            List<CuttingItem> openBuyItems = null, openSellItems = null;
            Settings.Account account = this._owner.Owner.Setting();
            if (account.RiskLevelAction == RiskLevelAction.CloseAll)
            {
                openBuyItems = new List<CuttingItem>();
                openSellItems = new List<CuttingItem>();
            }
            else
            {
                openBuyOrders = new List<Order>();
                openSellOrders = new List<Order>();
            }

            List<Order> orders = new List<Order>(this._owner.ExecutedAndHasPositionOrders);
            orders.Sort(AutoCloseOrderComparer.Default);

            foreach (Order eachOrder in orders)
            {
                if (!eachOrder.IsRisky) continue;

                if (eachOrder.Phase == OrderPhase.Executed && eachOrder.LotBalance > 0)
                {
                    if (eachOrder.IsBuy)
                    {
                        buyLotSum += eachOrder.LotBalance;
                        if (openBuyOrders != null) openBuyOrders.Add(eachOrder);
                        else openBuyItems.Add(new CuttingItem(eachOrder.IsBuy, eachOrder.Owner.ContractSize(null), eachOrder.LotBalance, eachOrder.Owner.ExecuteTime.Value, eachOrder.ExecutePrice));
                    }
                    else
                    {
                        sellLotSum += eachOrder.LotBalance;
                        if (openSellOrders != null) openSellOrders.Add(eachOrder);
                        else openSellItems.Add(new CuttingItem(eachOrder.IsBuy, eachOrder.Owner.ContractSize(null), eachOrder.LotBalance, eachOrder.Owner.ExecuteTime.Value, eachOrder.ExecutePrice));
                    }
                }
            }

            if (account.RiskLevelAction == RiskLevelAction.CloseAll)
            {
                return this.CaculateFeeForCuttingAll(buyLotSum, openBuyItems, sellLotSum, openSellItems);
            }
            else
            {
                return this.CaculateFeeForCuttingNet(buyLotSum, openBuyOrders, sellLotSum, openSellOrders);
            }
        }

        private decimal CaculateFeeForCuttingAll(decimal buyLotSum, ICollection<CuttingItem> openBuyOrders,
            decimal sellLotSum, ICollection<CuttingItem> openSellOrders)
        {
            decimal fee = 0;
            var quotation = _owner.GetQuotation();
            if (quotation == null) return fee;
            Price buy = quotation.BuyPrice, sell = quotation.SellPrice;
            if (sellLotSum > 0)
            {
                fee += this.CaculateFeeForCutting(sellLotSum, sell, openSellOrders);
            }

            if (buyLotSum > 0)
            {
                fee += this.CaculateFeeForCutting(buyLotSum, buy, openBuyOrders);
            }
            return fee;
        }

        private decimal CaculateFeeForCuttingNet(decimal buyLotSum, ICollection<Order> openBuyOrders, decimal sellLotSum, ICollection<Order> openSellOrders)
        {
            bool isBuy = (buyLotSum < sellLotSum);
            decimal lotBalanceSum = Math.Abs(buyLotSum - sellLotSum);
            if (lotBalanceSum == 0) return 0m;

            List<CuttingItem> cuttingItems = new List<CuttingItem>();
            decimal remainLotBalanceSum = lotBalanceSum;
            ICollection<Order> openOrders = isBuy ? openSellOrders : openBuyOrders;
            foreach (Order eachOpenOrder in openOrders)
            {
                if (remainLotBalanceSum <= 0) break;
                decimal closedLot = Math.Min(eachOpenOrder.LotBalance, remainLotBalanceSum);
                cuttingItems.Add(new CuttingItem(eachOpenOrder.IsBuy, eachOpenOrder.Owner.ContractSize(null), closedLot, eachOpenOrder.Owner.ExecuteTime.Value, eachOpenOrder.ExecutePrice));
                remainLotBalanceSum -= closedLot;
            }


            var quotation = _owner.GetQuotation();
            if (quotation == null) return 0m;
            Price buyPrice = quotation.BuyPrice, sellPrice = quotation.SellPrice;
            Price cutPrice = (isBuy ? sellPrice : buyPrice);

            return this.CaculateFeeForCutting(lotBalanceSum, cutPrice, cuttingItems);
        }

        private decimal CaculateFeeForCutting(decimal closedLot, Price cutPrice, ICollection<CuttingItem> cuttingItems)
        {
            if ((decimal)cutPrice == 0) return 0m;
            decimal commission = 0m, levy = 0m, otherFee = 0m;
            Settings.Instrument instrument = _owner.Setting;
            Settings.Account account = _owner.Owner.Setting();
            TradePolicyDetail tradePolicyDetail = _owner.TradePolicyDetail();
            SpecialTradePolicyDetail specialTradePolicyDetail = _owner.SpecialTradePolicyDetail(null);
            decimal contractSize = tradePolicyDetail.ContractSize;
            CurrencyRate currencyRate = _owner.CurrencyRate(null);

            if (instrument.ExchangeSystem == ExchangeSystem.Local
                && (account.RiskLevelAction == RiskLevelAction.CloseNetPosition || account.RiskLevelAction == RiskLevelAction.CloseAll))
            {
                if (!instrument.CommissionFormula.TakeFeeAsCost() || !instrument.LevyFormula.TakeFeeAsCost() || !instrument.OtherFeeFormula.TakeFeeAsCost())
                {
                    foreach (CuttingItem eachCuttingItem in cuttingItems)
                    {
                        Price buyPrice, sellPrice, closePrice;
                        if (eachCuttingItem.IsBuy)
                        {
                            buyPrice = cutPrice;
                            sellPrice = eachCuttingItem.ExecutePrice;
                        }
                        else
                        {
                            sellPrice = cutPrice;
                            buyPrice = eachCuttingItem.ExecutePrice;
                        }
                        closePrice = cutPrice;
                        decimal subCommission = 0m, subLevy = 0m, subOtherFee = 0m;
                        decimal tradePL = TradePLCalculator.Calculate(instrument.TradePLFormula, eachCuttingItem.CuttingLot, eachCuttingItem.ContractSize, (decimal)buyPrice, (decimal)sellPrice, (decimal)closePrice, _owner.Currency(null).Decimals);
                        var feeParameter = new FeeParameter()
                        {
                            Account = account,
                            TradePolicyDetail = tradePolicyDetail,
                            SpecialTradePolicyDetail = specialTradePolicyDetail,
                            Instrument = instrument,
                            CurrencyRate = currencyRate,
                            ContractSize = contractSize,
                            OpenOrderExecuteTime = eachCuttingItem.ExecuteTime,
                            ClosedLot = eachCuttingItem.CuttingLot,
                            ExecutePrice = cutPrice,
                            TradePL = tradePL
                        };
                        OrderRelation.CalculateFee(feeParameter, out subCommission, out subLevy, out subOtherFee);
                        commission += subCommission;
                        levy += subLevy;
                        otherFee += subOtherFee;
                    }
                }

                if (instrument.LevyFormula.TakeFeeAsCost())
                {
                    levy = this.CalculateFeeCommon(account.RateLevy, tradePolicyDetail.LevyClose, closedLot, contractSize);
                }

                if (instrument.OtherFeeFormula.TakeFeeAsCost())
                {
                    otherFee = this.CalculateFeeCommon(account.RateOtherFee, tradePolicyDetail.OtherFeeClose, closedLot, contractSize);
                }

                if (instrument.CommissionFormula.TakeFeeAsCost())
                {
                    commission = this.CalculateFeeCommon(account.RateCommission, tradePolicyDetail.CommissionCloseD, closedLot, contractSize);
                }
                else
                {
                    if (commission >= 0) commission = Math.Max(commission, tradePolicyDetail.MinCommissionClose);
                }
            }
            else
            {
                if (instrument.CommissionFormula.TakeFeeAsCost()) //Adjust PricePips
                {
                    commission = this.CalculateFeeCommon(account.RateCommission, tradePolicyDetail.CommissionOpen, closedLot, contractSize);
                }
                else
                {
                    if (!instrument.CommissionFormula.IsDependOnPL() && specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionCommissionOn)
                    {
                        commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * tradePolicyDetail.CommissionOpen,
                            (int)closedLot, contractSize, cutPrice, currencyRate) +
                            FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * tradePolicyDetail.CommissionOpen, closedLot - (int)closedLot,
                            contractSize, cutPrice, currencyRate);
                    }
                    else
                    {
                        commission = FeeCalculator.CalculateCommission(instrument.CommissionFormula, instrument.TradePLFormula, account.RateCommission * tradePolicyDetail.CommissionOpen, closedLot, contractSize, cutPrice, currencyRate);
                    }

                    if (commission >= 0)
                    {
                        commission = Math.Max(commission, tradePolicyDetail.MinCommissionOpen);
                    }
                }

                if (instrument.LevyFormula.TakeFeeAsCost()) //Adjust PricePips
                {
                    levy = this.CalculateFeeCommon(account.RateLevy, tradePolicyDetail.LevyOpen, closedLot, contractSize);
                }
                else
                {
                    if (!instrument.LevyFormula.IsDependOnPL() && specialTradePolicyDetail != null && specialTradePolicyDetail.IsFractionCommissionOn)
                    {
                        levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * tradePolicyDetail.LevyOpen, (int)closedLot
                            , contractSize, cutPrice, currencyRate) +
                         FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * tradePolicyDetail.LevyOpen, closedLot - (int)closedLot
                            , contractSize, cutPrice, currencyRate);
                    }
                    else
                    {
                        levy = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateLevy * tradePolicyDetail.LevyOpen, closedLot, contractSize, cutPrice, currencyRate);
                    }

                    if (!instrument.LevyFormula.IsDependOnPL() && specialTradePolicyDetail != null)
                    {
                        CurrencyRate cgseLevyCurrencyRate = FeeCalculator.GetCGSELevyCurrencyRate(account, instrument, specialTradePolicyDetail, currencyRate, null);
                        levy += FeeCalculator.CalculateCGSELevy(closedLot, true, specialTradePolicyDetail, cgseLevyCurrencyRate);
                    }
                }

                if (instrument.OtherFeeFormula.TakeFeeAsCost())
                {
                    otherFee = this.CalculateFeeCommon(account.RateOtherFee, tradePolicyDetail.OtherFeeOpen, closedLot, contractSize);
                }
                else
                {
                    otherFee = FeeCalculator.CalculateLevy(instrument.LevyFormula, instrument.TradePLFormula, account.RateOtherFee * tradePolicyDetail.OtherFeeOpen, closedLot, contractSize, cutPrice, currencyRate);
                }

            }

            return commission + levy + otherFee; ;
        }

        private decimal CalculateFeeCommon(decimal rate, decimal value, decimal lot, decimal contractSize)
        {
            decimal adjustPrice = (rate * value) / _owner.Setting.Denominator;
            return adjustPrice * lot * contractSize;
        }

    }
}