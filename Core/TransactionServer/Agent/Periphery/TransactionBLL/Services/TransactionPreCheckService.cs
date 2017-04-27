using Core.TransactionServer.Agent.BLL.PreCheck;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Services
{
    internal class TransactionPreCheckService
    {
        private Transaction _tran;

        internal TransactionPreCheckService(Transaction tran)
        {
            _tran = tran;
        }

        internal MarginAndQuantityResult CalculateUnfilledMarginAndQuantity(decimal? effectiveLot = null)
        {
            var result = new MarginAndQuantityResult();
            Account account = _tran.Owner;
            Price buy, sell;
            this.CalculatePrice(out buy, out sell);
            foreach (Order eachOrder in _tran.Orders)
            {
                if (!this.ShouldCalculatePreCheckNecessary(eachOrder)) continue;
                var price = eachOrder.IsBuy ? sell : buy;
                result += eachOrder.CalculateUnfilledMarginAndQuantityForOrder(price, effectiveLot);
            }
            return result;
        }

        internal bool ShouldCalculatePreCheckNecessary(AccountClass.Instrument instrument, bool isBuy, Dictionary<Guid, decimal> unfilledLotsPerTran, out decimal? unfilledLot)
        {
            unfilledLot = null;
            if (!this.ShouldSumPlaceMargin()) return false; 
            Order order = _tran.FirstOrder;
            if (unfilledLotsPerTran != null && unfilledLotsPerTran.ContainsKey(_tran.Id))
            {
                unfilledLot = unfilledLotsPerTran[_tran.Id];
            }
            bool hasUnfilledLot = unfilledLot == null || unfilledLot > 0;
            return order.IsBuy == isBuy && order.IsOpen && hasUnfilledLot &&
                  (!instrument.IsPhysical || (instrument.IsPhysical && (!isBuy || ((PhysicalOrder)order).IsInstalment)));
        }

        internal bool ShouldSumPlaceMargin()
        {
            bool isPlaced = _tran.Phase == TransactionPhase.Placed;
            bool isNotDoneOrderPlacing = _tran.Phase == TransactionPhase.Placing && _tran.SubType != TransactionSubType.IfDone;
            bool isPairTran = _tran.Type == TransactionType.Pair;
            return (isPlaced || isNotDoneOrderPlacing) && !isPairTran;
        }

        private bool ShouldCalculatePreCheckNecessary(Order order)
        {
            bool isOCOBetterOption = _tran.Type == TransactionType.OneCancelOther && _tran.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Better;
            bool isPlacedOrPlacing = order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing;
            return !isOCOBetterOption && isPlacedOrPlacing;
        }

        private void CalculatePrice(out Price buy, out Price sell)
        {
            buy = sell = null;
            Settings.Instrument settingInstrument = _tran.SettingInstrument();
            if (_tran.OrderType == OrderType.Market || _tran.OrderType == OrderType.MarketOnOpen
                || _tran.OrderType == OrderType.MarketOnClose || settingInstrument.MarginFormula == MarginFormula.CSiMarketPrice
                || settingInstrument.MarginFormula == MarginFormula.CSxMarketPrice)
            {
                Quotation quotation = _tran.AccountInstrument.GetQuotation();
                buy = quotation.BuyPrice;
                sell = quotation.SellPrice;
            }
        }
    }
}
