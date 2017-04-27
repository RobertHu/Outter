using Core.TransactionServer.Agent.BLL.AccountBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class MarginResult
    {
        internal Guid CurrencyId { get; set; }

        internal decimal Margin { get; set; }
    }

    internal sealed class AccountUsableNecessaryCalculator
    {
        internal static readonly AccountUsableNecessaryCalculator Default = new AccountUsableNecessaryCalculator();

        static AccountUsableNecessaryCalculator() { }
        private AccountUsableNecessaryCalculator() { }

        private enum BuySellType
        {
            Buy,
            Sell
        }

        private sealed class MarginData
        {
            internal Guid AccountId { get; set; }
            internal Guid CurrencyId { get; set; }
            internal Guid InstrumentId { get; set; }
            internal decimal Margin { get; set; }
            internal decimal MarginLocked { get; set; }
            internal decimal MarginNight { get; set; }
            internal decimal MarginNightLocked { get; set; }
            internal decimal MarginSell { get; set; }
            internal decimal MarginBuy { get; set; }
            internal int MarginFormula { get; set; }
            internal decimal SellLot { get; set; }
            internal decimal BuyLot { get; set; }

            internal decimal LockedLot
            {
                get
                {
                    if (this.BuyLot == 0m || this.SellLot == 0m)
                    {
                        return 0m;
                    }
                    return this.BuyLot < this.SellLot ? this.BuyLot : this.SellLot;
                }
            }

            internal decimal AverageSell
            {
                get
                {
                    return this.SellLot == 0m ? 0m : this.MarginSell / this.SellLot;
                }
            }

            internal decimal AverageBuy
            {
                get
                {
                    return this.BuyLot == 0m ? 0m : this.MarginBuy / this.BuyLot;
                }
            }

            internal Guid TradePolicyId { get; set; }
            internal decimal SellLotBalance { get; set; }
            internal decimal BuyLotBalance { get; set; }
        }

        private sealed class InstrumentLotInfo
        {
            internal Guid AccountId { get; set; }

            internal Guid InstrumentId { get; set; }

            internal decimal CurrentMonthBuy { get; set; }

            internal decimal CurrentMonthSell { get; set; }

            internal decimal FutureMonthBuy { get; set; }

            internal decimal FutureMonthSell { get; set; }
        }

        private sealed class PhysicalMarginData
        {
            internal Guid AccountId { get; set; }
            internal Guid CurrencyId { get; set; }
            internal Guid InstrumentId { get; set; }
            internal decimal MarginSell { get; set; }
            internal decimal MarginBuy { get; set; }
        }

        internal Dictionary<Guid, MarginResult> Calculate(Account account, DateTime tradeDay, InstrumentManager instrumentManager)
        {
            Dictionary<Guid, MarginResult> marginResultDict = new Dictionary<Guid, MarginResult>();
            this.CalculateMarginResultForNormal(account, tradeDay, marginResultDict, instrumentManager);
            this.CalculateMarginResultForFCPO(account, tradeDay, marginResultDict, instrumentManager);
            this.CalculateMarginResultForFKLI(account, tradeDay, marginResultDict, instrumentManager);
            return marginResultDict;
        }
        private Price GetRefPrice(UsableMarginPrice usableMarginPrice, bool isBuy, bool isNormal)
        {
            Price refPrice1 = isNormal == isBuy ? usableMarginPrice.PrivateBid : usableMarginPrice.PrivateAsk;
            Price refPrice2 = isNormal == isBuy ? usableMarginPrice.PublicBid : usableMarginPrice.PublicAsk;
            return refPrice1 ?? refPrice2;
        }


        private void CalculateMarginResultForNormal(Account account, DateTime tradeDay, Dictionary<Guid, MarginResult> marginResultDict, InstrumentManager instrumentManager)
        {
            var settingAccount = Settings.Setting.Default.GetAccount(account.Id, tradeDay);
            foreach (var eachMarginData in this.CalculateMarginData(account, tradeDay, instrumentManager))
            {
                MarginResult result = this.GetMarginResult(account.Id, eachMarginData.CurrencyId, marginResultDict);
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachMarginData.InstrumentId, tradeDay);
                var tradePolicyDetail = Settings.Setting.Default.GetTradePolicyDetail(eachMarginData.InstrumentId, settingAccount.TradePolicyId, tradeDay);
                if (settingInstrument.MarginFormula == MarginFormula.CSiPrice || settingInstrument.MarginFormula == MarginFormula.CSxPrice
                    || settingInstrument.MarginFormula == MarginFormula.CSiMarketPrice || settingInstrument.MarginFormula == MarginFormula.CSxMarketPrice)
                {
                    decimal netAverageMargin = eachMarginData.BuyLot < eachMarginData.SellLot ? eachMarginData.AverageSell * (eachMarginData.SellLot - eachMarginData.BuyLot) :
                                                                                               eachMarginData.AverageBuy * (eachMarginData.BuyLot - eachMarginData.SellLot);
                    result.Margin += netAverageMargin * eachMarginData.Margin + eachMarginData.AverageSell * eachMarginData.LockedLot * eachMarginData.MarginLocked
                                     + eachMarginData.AverageBuy * eachMarginData.LockedLot * eachMarginData.MarginLocked;
                }
                else if (settingInstrument.MarginFormula == MarginFormula.FixedAmount && tradePolicyDetail.VolumeNecessary != null)
                {
                    decimal lockedMargin = Math.Min(eachMarginData.MarginBuy, eachMarginData.MarginSell) * eachMarginData.MarginLocked;
                    decimal netLot = Math.Abs(eachMarginData.BuyLotBalance - eachMarginData.SellLotBalance);
                    decimal marginNecessary = Calculator.MarginCalculator.CalculateNecessaryMargin(Settings.Setting.Default, tradePolicyDetail.VolumeNecessaryId.Value, netLot, tradePolicyDetail.MarginO, true);
                    result.Margin += lockedMargin + marginNecessary * settingAccount.RateMarginO;
                }
                else
                {
                    result.Margin += eachMarginData.Margin * Math.Abs(eachMarginData.MarginBuy - eachMarginData.MarginSell) + Math.Min(eachMarginData.MarginBuy, eachMarginData.MarginSell) * eachMarginData.MarginLocked;
                }

            }
        }


        private void CalculateMarginResultForFCPO(Account account, DateTime tradeDay, Dictionary<Guid, MarginResult> marginResultDict, InstrumentManager instrumentManager)
        {
            this.CalculateMarginResultForInstrumentLotInfoCommon(marginResultDict, account, tradeDay, MarginFormula.FCPO, (tradePolicyDetail, currencyRate, accountCurrency, instrumentLotInfo) =>
            {
                decimal margin = instrumentLotInfo.CurrentMonthBuy * tradePolicyDetail.MarginSpot + instrumentLotInfo.CurrentMonthSell * tradePolicyDetail.MarginSpot + Math.Abs(instrumentLotInfo.FutureMonthBuy - instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginD
                                + Math.Min(instrumentLotInfo.FutureMonthBuy, instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginLockedD;
                return Calculator.MarginCalculator.GetRatedValue(margin, currencyRate.RateIn, currencyRate.RateOut, accountCurrency.Decimals);
            }, instrumentManager);
        }

        private void CalculateMarginResultForFKLI(Account account, DateTime tradeDay, Dictionary<Guid, MarginResult> marginResultDict, InstrumentManager instrumentManager)
        {
            this.CalculateMarginResultForInstrumentLotInfoCommon(marginResultDict, account, tradeDay, MarginFormula.FKLI, (tradePolicyDetail, currencyRate, accountCurrency, instrumentLotInfo) =>
                {
                    decimal margin = 0m;
                    if (instrumentLotInfo.CurrentMonthBuy == instrumentLotInfo.CurrentMonthSell)
                    {
                        margin = instrumentLotInfo.CurrentMonthBuy + tradePolicyDetail.MarginSpotSpread + Math.Min(instrumentLotInfo.FutureMonthBuy, instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginSpotSpread + Math.Abs(instrumentLotInfo.FutureMonthBuy - instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginD;
                    }
                    else if (instrumentLotInfo.CurrentMonthBuy > instrumentLotInfo.CurrentMonthSell)
                    {
                        margin += instrumentLotInfo.CurrentMonthSell;
                        margin += Math.Min(instrumentLotInfo.CurrentMonthBuy - instrumentLotInfo.CurrentMonthSell, instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginSpotSpread;
                        if (instrumentLotInfo.CurrentMonthBuy - instrumentLotInfo.CurrentMonthSell > instrumentLotInfo.FutureMonthSell)
                        {
                            margin += Math.Min(instrumentLotInfo.CurrentMonthBuy - instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.FutureMonthSell, instrumentLotInfo.FutureMonthBuy) * tradePolicyDetail.MarginLockedD;
                        }
                        else
                        {
                            margin += Math.Min(instrumentLotInfo.FutureMonthSell - (instrumentLotInfo.CurrentMonthBuy - instrumentLotInfo.CurrentMonthSell), instrumentLotInfo.FutureMonthBuy);
                        }
                        margin += (instrumentLotInfo.CurrentMonthBuy + instrumentLotInfo.FutureMonthBuy - instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginD;
                    }
                    else
                    {
                        margin += instrumentLotInfo.CurrentMonthBuy;
                        margin += Math.Min(instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.CurrentMonthBuy, instrumentLotInfo.FutureMonthBuy) * tradePolicyDetail.MarginSpotSpread;
                        if (instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.CurrentMonthBuy > instrumentLotInfo.FutureMonthBuy)
                        {
                            margin += Math.Min(instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.CurrentMonthBuy - instrumentLotInfo.FutureMonthBuy, instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginLockedD;
                        }
                        else
                        {
                            margin += Math.Min(instrumentLotInfo.FutureMonthBuy - (instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.CurrentMonthBuy), instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginLockedD;
                        }
                        margin += (instrumentLotInfo.CurrentMonthBuy + instrumentLotInfo.FutureMonthBuy - instrumentLotInfo.CurrentMonthSell - instrumentLotInfo.FutureMonthSell) * tradePolicyDetail.MarginD;
                    }
                    return Calculator.MarginCalculator.GetRatedValue(margin, currencyRate.RateIn, currencyRate.RateOut, accountCurrency.Decimals);
                }, instrumentManager);
        }


        private void CalculateMarginResultForInstrumentLotInfoCommon(Dictionary<Guid, MarginResult> marginResultDict, Account account, DateTime tradeDay, MarginFormula marginFormula, Func<Settings.TradePolicyDetail, Settings.CurrencyRate, Settings.Currency, InstrumentLotInfo, decimal> marginFunc, InstrumentManager instrumentManager)
        {
            var settingAccount = Settings.Setting.Default.GetAccount(account.Id, tradeDay);
            foreach (var eachItem in this.GetInstrumentLotInfo(account, tradeDay, marginFormula, instrumentManager))
            {
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachItem.InstrumentId, tradeDay);
                Guid currencyId = settingAccount.IsMultiCurrency ? settingInstrument.CurrencyId : settingAccount.CurrencyId;
                var tradePolicyDetail = Settings.Setting.Default.GetTradePolicyDetail(eachItem.InstrumentId, settingAccount.TradePolicyId, tradeDay);
                var currencyRate = Settings.Setting.Default.GetCurrencyRate(settingInstrument.CurrencyId, currencyId, tradeDay);
                MarginResult marginResult = this.GetMarginResult(account.Id, currencyId, marginResultDict);
                var accountCurrency = Settings.Setting.Default.GetCurrency(settingAccount.CurrencyId, tradeDay);
                marginResult.Margin += marginFunc(tradePolicyDetail, currencyRate, accountCurrency, eachItem);
            }
        }


        private MarginResult GetMarginResult(Guid accountId, Guid currencyId, Dictionary<Guid, MarginResult> marginResultDict)
        {
            MarginResult result;
            if (!marginResultDict.TryGetValue(currencyId, out result))
            {
                result = new MarginResult { CurrencyId = currencyId };
                marginResultDict.Add(currencyId, result);
            }
            return result;
        }

        private Guid GetCurrencyId(Guid accountiD, Guid instrumentId, DateTime tradeDay)
        {
            var settingAccount = Settings.Setting.Default.GetAccount(accountiD, tradeDay);
            var settingInstrument = Settings.Setting.Default.GetInstrument(instrumentId, tradeDay);
            return this.GetCurrencyId(settingAccount, settingInstrument);
        }

        private Guid GetCurrencyId(Settings.Account account, Settings.Instrument instrument)
        {
            return account.IsMultiCurrency ? instrument.CurrencyId : account.CurrencyId;
        }


        private IEnumerable<InstrumentLotInfo> GetInstrumentLotInfo(Account account, DateTime tradeDay, MarginFormula marginFormula, InstrumentManager instrumentManager)
        {
            foreach (var eachInstrument in instrumentManager.Instruments)
            {
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachInstrument.Id, tradeDay);
                if (settingInstrument.MarginFormula != marginFormula) continue;
                if (!eachInstrument.ExistsOrdersForCalculateNormal(tradeDay)) continue;

                InstrumentLotInfo item = new InstrumentLotInfo
                {
                    AccountId = account.Id,
                    InstrumentId = eachInstrument.Id
                };
                Debug.Assert(settingInstrument.SpotPaymentTime != null);
                bool isSameMonth = (tradeDay.Year - settingInstrument.SpotPaymentTime.Value.Year) * 12 + tradeDay.Month - settingInstrument.SpotPaymentTime.Value.Month == 0;
                foreach (var eachOrder in eachInstrument.GetOrders(tradeDay))
                {
                    if (eachOrder.IsForexOrderOrPayoffShortSellOrder())
                    {
                        item.CurrentMonthBuy += isSameMonth && eachOrder.Order.IsBuy ? eachOrder.Order.LotBalance : 0m;
                        item.CurrentMonthSell += isSameMonth && !eachOrder.Order.IsBuy ? eachOrder.Order.LotBalance : 0m;
                        item.FutureMonthBuy += !isSameMonth && eachOrder.Order.IsBuy ? eachOrder.Order.LotBalance : 0m;
                        item.FutureMonthSell += !isSameMonth && !eachOrder.Order.IsBuy ? eachOrder.Order.LotBalance : 0m;
                    }
                }
                yield return item;
            }

        }


        private IEnumerable<MarginData> CalculateMarginData(Account account, DateTime tradeDay, InstrumentManager instrumentManager)
        {
            var setting = Settings.Setting.Default;
            var settingAccount = setting.GetAccount(account.Id, tradeDay);
            Dictionary<Guid, MarginData> result = new Dictionary<Guid, MarginData>(account.InstrumentCount);
            foreach (var eachInstrument in instrumentManager.Instruments)
            {
                var settingInstrument = setting.GetInstrument(eachInstrument.Id, tradeDay);
                if (settingInstrument.MarginFormula == MarginFormula.FCPO || settingInstrument.MarginFormula == MarginFormula.FKLI) continue;
                if (!eachInstrument.ExistsOrdersForCalculateNormal(tradeDay)) continue;
                MarginData marginData;
                if (!result.TryGetValue(eachInstrument.Id, out marginData))
                {
                    marginData = this.CreateMarginData(setting, settingAccount, settingInstrument, tradeDay);
                    result.Add(eachInstrument.Id, marginData);
                }
                this.CalculateInstrumentMarginData(setting, settingAccount, settingInstrument, tradeDay, marginData);
                UsableMarginPrice usableMarginPrice = ResetManager.Default.GetRefPriceForUsableMargin(eachInstrument.Id, account.Id, tradeDay);
                foreach (var eachOrder in eachInstrument.GetOrders(tradeDay))
                {
                    if (!eachOrder.IsForexOrderOrPayoffShortSellOrder()) continue;
                    decimal orderMargin = this.CalculateOrderMargin(eachOrder.Order, setting, settingInstrument, settingAccount, tradeDay, usableMarginPrice);
                    marginData.MarginBuy += this.GetBuyOrSellValue(orderMargin, eachOrder.Order, BuySellType.Buy);
                    marginData.MarginSell += this.GetBuyOrSellValue(orderMargin, eachOrder.Order, BuySellType.Sell);

                    decimal quantity = eachOrder.Order.LotBalance * eachOrder.Order.Owner.ContractSize(tradeDay);
                    marginData.BuyLot += this.GetBuyOrSellValue(quantity, eachOrder.Order, BuySellType.Buy);
                    marginData.SellLot += this.GetBuyOrSellValue(quantity, eachOrder.Order, BuySellType.Sell);

                    marginData.BuyLotBalance += this.GetBuyOrSellValue(eachOrder.Order.LotBalance, eachOrder.Order, BuySellType.Buy);
                    marginData.SellLotBalance += this.GetBuyOrSellValue(eachOrder.Order.LotBalance, eachOrder.Order, BuySellType.Sell);
                }
            }
            return result.Values;
        }

        private void CalculateInstrumentMarginData(Settings.Setting setting, Settings.Account account, Settings.Instrument instrument, DateTime tradeDay, MarginData marginData)
        {
            var tradePolicyDetail = setting.GetTradePolicyDetail(instrument.Id, account.TradePolicyId, tradeDay);
            marginData.Margin += account.RateMarginO * tradePolicyDetail.MarginO;
            marginData.MarginLocked += account.RateMarginLockO * tradePolicyDetail.MarginLockedO;
            marginData.MarginNight += account.RateMarginO * tradePolicyDetail.MarginO;
            marginData.MarginNightLocked += account.RateMarginLockO * tradePolicyDetail.MarginLockedO;
        }


        private MarginData CreateMarginData(Settings.Setting setting, Settings.Account account, Settings.Instrument instrument, DateTime tradeDay)
        {
            var tradePolicyDetail = setting.GetTradePolicyDetail(instrument.Id, account.TradePolicyId, tradeDay);
            MarginData marginData = new MarginData();
            marginData.AccountId = account.Id;
            marginData.InstrumentId = instrument.Id;
            marginData.CurrencyId = account.IsMultiCurrency ? instrument.CurrencyId : account.CurrencyId;
            marginData.MarginFormula = (int)instrument.MarginFormula;
            marginData.TradePolicyId = tradePolicyDetail.TradePolicy.ID;
            return marginData;
        }


        private decimal GetBuyOrSellValue(decimal value, Order order, BuySellType buySellType)
        {
            BuySellType originType = order.IsBuy ? BuySellType.Buy : BuySellType.Sell;
            return buySellType == originType ? value : 0m;
        }


        private decimal CalculateOrderMargin(Order order, Settings.Setting setting, Settings.Instrument instrument, Settings.Account account, DateTime tradeDay, UsableMarginPrice usableMarginPrice)
        {
            Guid currencyId = this.GetCurrencyId(account, instrument);
            var currencyRate = setting.GetCurrencyRate(instrument.CurrencyId, currencyId);
            decimal? rateIn = currencyRate == null ? (decimal?)null : currencyRate.RateIn;
            decimal? rateOut = currencyRate == null ? (decimal?)null : currencyRate.RateOut;
            var currency = setting.GetCurrency(currencyId, tradeDay);
            int decimals = currency.Decimals;
            Price refPrice = this.GetRefPrice(usableMarginPrice, order.IsBuy, instrument.IsNormal);
            return Calculator.MarginCalculator.CalculateRptMargin((int)instrument.MarginFormula, order.LotBalance, order.Owner.ContractSize(tradeDay), order.ExecutePrice, rateIn, rateOut, decimals, refPrice);
        }
    }


    internal static class InstrumentResetExtension
    {
        internal static IEnumerable<ResetOrder> GetOrders(this AccountClass.Instrument instrument, DateTime tradeDay)
        {
            foreach (var eachTran in instrument.GetTransactions())
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    var orderDayHistory = ResetManager.Default.GetOrderDayHistory(eachOrder.Id, tradeDay);
                    if (eachOrder.IsOpen && eachOrder.LotBalance > 0 && eachOrder.Phase == OrderPhase.Executed && orderDayHistory != null && orderDayHistory.LotBalance > 0)
                    {
                        yield return new ResetOrder(eachOrder);
                    }
                }
            }
        }

        internal static bool ExistsOrdersForCalculateNormal(this AccountClass.Instrument instrument, DateTime tradeDay)
        {
            foreach (var eachOrder in instrument.GetOrders(tradeDay))
            {
                if (eachOrder.IsForexOrderOrPayoffShortSellOrder()) return true;
            }
            return false;
        }

        internal static bool IsForexOrderOrPayoffShortSellOrder(this ResetOrder eachOrder)
        {
            return eachOrder.PhysicalTradeSide == PhysicalTradeSide.None || (eachOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell && eachOrder.IsPayOff);
        }

    }


    internal sealed class ResetOrder
    {
        internal ResetOrder(Order order)
        {
            this.Order = order;
        }

        internal Order Order { get; private set; }

        internal bool IsBuy
        {
            get { return this.Order.IsBuy; }
        }

        internal decimal LotBalance
        {
            get
            {
                return this.Order.LotBalance;
            }
        }

        internal decimal ContractSize(DateTime? tradeDay)
        {
            return this.Order.Owner.ContractSize(tradeDay);
        }

        internal Price ExecutePrice
        {
            get
            {
                return this.Order.ExecutePrice;
            }
        }

        internal PhysicalTradeSide PhysicalTradeSide
        {
            get
            {
                if (!this.Order.IsPhysical) return iExchange.Common.PhysicalTradeSide.None;
                var physicalOrder = (Physical.PhysicalOrder)this.Order;
                return physicalOrder.PhysicalTradeSide;
            }
        }

        internal bool IsPayOff
        {
            get
            {
                if (!this.Order.IsPhysical) return true;
                var physicalOrder = this.Order as Physical.PhysicalOrder;
                Debug.Assert(physicalOrder != null);
                if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell && physicalOrder.PaidPledgeBalance != 0) return false;
                if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell && physicalOrder.PaidPledgeBalance == 0) return true;
                return physicalOrder.IsPayoff;
            }
        }
    }

}
