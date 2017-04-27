using Core.TransactionServer.Agent.AccountClass.InstrumentUtil;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal static class PreCheckService
    {

        internal static void CalculatePreCheckNecessary(Transaction tran,ref CalculateUnfillMarginParameters unfillParams, decimal? effectiveLot = null)
        {
            Price buy = null;
            Price sell = null;
            Account account = tran.Owner;
            Settings.Instrument settingInstrument = tran.SettingInstrument;
            AccountClass.Instrument accountInstrument = account.GetOrCreateInstrument(settingInstrument.Id);
            if (tran.OrderType == OrderType.Market || tran.OrderType == OrderType.MarketOnOpen
                || tran.OrderType == OrderType.MarketOnClose || settingInstrument.MarginFormula == MarginFormula.CSiMarketPrice
                || settingInstrument.MarginFormula == MarginFormula.CSxMarketPrice)
            {
                Quotation quotation = QuotationProvider.GetLatestQuotation(tran.InstrumentId, account);
                buy = quotation.BuyOnCustomerSide;
                sell = quotation.SellOnCustomerSide;
            }
            foreach (Order order in tran.Orders)
            {
                if (order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing)
                {
                    decimal contractSize = tran.ContractSize == 0 ? accountInstrument.TradePolicyDetail.ContractSize : tran.ContractSize;
                    var price = order.IsBuy ? sell : buy;
                    order.CalculatePreCheckNecessary(settingInstrument.MarginFormula, tran.CurrencyRate, contractSize, price, effectiveLot);
                }
            }

            foreach (Order order in tran.Orders)
            {
                if (tran.Type == TransactionType.OneCancelOther && tran.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Better) continue;
                Collect(order, tran.ContractSize, effectiveLot, ref unfillParams);
            }

        }

        private static void Collect(Order order, decimal contractSize, decimal? effectiveLot, ref CalculateUnfillMarginParameters unfillParams)
        {
            if (order.Phase != OrderPhase.Placed || order.Phase != OrderPhase.Placing) return;
            decimal lot = effectiveLot ?? order.Lot;
            decimal quantity = lot * contractSize;
            if (order.IsBuy)
            {
                if (order.IsOpen)
                {
                    unfillParams.OpenBuyMargin += order.PreCheckMargin;
                    unfillParams.OpenBuyQuantity += quantity;
                }
                else
                {
                    unfillParams.CloseBuyMargin += order.PreCheckMargin;
                    unfillParams.CloseBuyQuantity += quantity;
                }
            }
            else
            {
                if (order.IsOpen)
                {
                    unfillParams.OpenSellMargin += order.PreCheckMargin;
                    unfillParams.OpenSellQuantity += quantity;
                }
                else
                {
                    unfillParams.CloseSellMargin += order.PreCheckMargin;
                    unfillParams.CloseSellQuantity += quantity;
                }
            }
        }

    }
}
