using Core.TransactionServer.Agent.AccountClass.InstrumentUtil;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.BLL.OrderBusiness.TypeExtension;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Physical;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.TypeExtension
{
    internal static class PlacePreCheckExtension
    {
        internal static MarginAndQuantityResult CalculateUnfilledMarginAndQuantity(this Transaction tran, decimal? effectiveLot = null)
        {
            var result = new MarginAndQuantityResult();
            Account account = tran.Owner;
            Price buy, sell;
            tran.CalculatePrice(out buy, out sell);
            foreach (Order eachOrder in tran.Orders)
            {
                if (!tran.ShouldCalculatePreCheckNecessary(eachOrder)) continue;
                var price = eachOrder.IsBuy ? sell : buy;
                eachOrder.CalculatePreCheckNecessary(tran.SettingInstrument.MarginFormula, tran.CurrencyRate, tran.ContractSize, price, effectiveLot);
                result += eachOrder.CalculateUnfilledMarginAndQuantity(tran.ContractSize, effectiveLot);
            }
            return result;
        }

        internal static bool ShouldCalculatePreCheckNecessary(this Transaction tran, Instrument instrument, bool isBuy, Dictionary<Guid, decimal> unfilledLotsPerTran, out decimal? unfilledLot)
        {
            unfilledLot = null;
            if (!tran.ShouldSumPlaceMargin()) return false; ;
            Order order = tran.FirstOrder;
            if (unfilledLotsPerTran.ContainsKey(tran.Id))
            {
                unfilledLot = unfilledLotsPerTran[tran.Id];
            }
            bool hasUnfilledLot = unfilledLot == null || unfilledLot > 0;
            return order.IsBuy == isBuy && order.IsOpen && hasUnfilledLot &&
                  (!instrument.IsPhysical || (instrument.IsPhysical && (!isBuy || ((PhysicalOrder)order).IsInstalment)));
        }


        private static bool ShouldCalculatePreCheckNecessary(this Transaction tran, Order order)
        {
            bool isOCOBetterOption = tran.Type == TransactionType.OneCancelOther && tran.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Better;
            bool isPlacedOrPlacing = order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing;
            return !isOCOBetterOption && isPlacedOrPlacing;
        }


        private static void CalculatePrice(this Transaction tran, out Price buy, out Price sell)
        {
            buy = sell = null;
            Settings.Instrument settingInstrument = tran.SettingInstrument;
            if (tran.OrderType == OrderType.Market || tran.OrderType == OrderType.MarketOnOpen
                || tran.OrderType == OrderType.MarketOnClose || settingInstrument.MarginFormula == MarginFormula.CSiMarketPrice
                || settingInstrument.MarginFormula == MarginFormula.CSxMarketPrice)
            {
                Quotation quotation = tran.TradingInstrument.Quotation;
                buy = quotation.BuyOnCustomerSide;
                sell = quotation.SellOnCustomerSide;
            }
        }


        internal static bool ShouldSumPlaceMargin(this Transaction tran)
        {
            bool isPlaced = tran.Phase == TransactionPhase.Placed;
            bool isNotDoneOrderPlacing = tran.Phase == TransactionPhase.Placing && tran.SubType != TransactionSubType.IfDone;
            bool isPairTran = tran.Type == TransactionType.Pair;
            return (isPlaced || isNotDoneOrderPlacing) && !isPairTran;
        }

        internal static BuySellLot CalculateLotBalanceForPreCheck(this Transaction tran, bool asRisk = false)
        {
            BuySellLot result = BuySellLot.Empty;
            foreach (var eachOrder in tran.Orders)
            {
                if (tran.ShouldCalculateOrderLotBalance(eachOrder, asRisk))
                {
                    result += eachOrder.CalculateLotBalance();
                }
            }
            return result;
        }

        private static bool ShouldCalculateOrderLotBalance(this Transaction tran, Order order, bool asRisk)
        {
            bool isBetterOption = tran.Type == TransactionType.OneCancelOther && tran.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Better;
            return !isBetterOption && (asRisk || order.IsRisky);
        }

    }
}
