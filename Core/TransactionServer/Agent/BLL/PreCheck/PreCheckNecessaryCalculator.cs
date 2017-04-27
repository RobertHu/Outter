using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal static class PreCheckNecessaryCalculator
    {
        internal static decimal CalculatePreCheckNecessary(this AccountClass.Instrument instrument, Transaction tran)
        {
            Account account = tran.Owner;
            Settings.TradePolicyDetail tradePolicyDetail = instrument.TradePolicyDetail();
            if (tran.AccountInstrument == instrument)
            {
                return InnerCalculatePreCheckNecessary(instrument, tran, tran.FirstOrder.IsBuy);
            }
            decimal buyPreCheckNecessary = InnerCalculatePreCheckNecessary(instrument, tran, true);
            decimal sellPreCheckNecessary = InnerCalculatePreCheckNecessary(instrument, tran, false);
            return Math.Max(buyPreCheckNecessary, sellPreCheckNecessary);
        }

        private static decimal InnerCalculatePreCheckNecessary(AccountClass.Instrument instrument, Transaction tran, bool isBuy)
        {
            MarginAndQuantityResult unfilledArgs = new MarginAndQuantityResult();
            Dictionary<Guid, decimal> remainFilledLotPerOrderDict = new Dictionary<Guid, decimal>();
            if (tran.IsPending)
            {
                unfilledArgs = UnfilledCalculator.CalculateUnfilledMarginArgsForPlacePendingOrder(instrument, tran, isBuy, remainFilledLotPerOrderDict);
            }
            MarginAndQuantityResult filledArgs = new MarginAndQuantityResult();
            MarginAndQuantityResult marginArgs = new MarginAndQuantityResult();
            instrument.InitializeFilledAndMarginArgs(isBuy, unfilledArgs, filledArgs, marginArgs);
            filledArgs += FilledCalculator.CalculateFillMarginAndQuantity(instrument, isBuy, remainFilledLotPerOrderDict);
            return CalculateNecessary(instrument, isBuy, marginArgs, filledArgs);
        }

        private static decimal CalculateNecessary(AccountClass.Instrument instrument, bool isBuy, MarginAndQuantityResult marginArgs, MarginAndQuantityResult filledArgs)
        {
            MarginAndQuantityResult necessaryParams = new MarginAndQuantityResult();
            necessaryParams.Add(isBuy, marginArgs, filledArgs);
            decimal netNecessary = 0m;
            decimal hedgeNecessary = 0m;
            decimal partialPaymentPhysicalNecessary = 0m;

            if (necessaryParams.PartialQuantity.Sell > 0)
            {
                partialPaymentPhysicalNecessary = necessaryParams.PartialMargin.Sell;
            }
            else if (necessaryParams.PartialQuantity.Buy > 0)
            {
                partialPaymentPhysicalNecessary = necessaryParams.PartialMargin.Buy;
            }
            instrument.CalculateNetAndHedgeNecessary(necessaryParams.Margin.Buy, necessaryParams.Margin.Sell,
                necessaryParams.Quantity.Buy, necessaryParams.Quantity.Sell, partialPaymentPhysicalNecessary,
                out netNecessary, out hedgeNecessary);
            return netNecessary + hedgeNecessary;
        }


        private static void InitializeFilledAndMarginArgs(this AccountClass.Instrument instrument, bool isBuy, MarginAndQuantityResult unfilledArgs, MarginAndQuantityResult filledArgs, MarginAndQuantityResult marginArgs)
        {
            var physicalInstrument = instrument as Physical.PhysicalInstrument;
            BuySellPair margin, quantity, partialMargin, partialQuantity;
            margin = new BuySellPair(instrument.TotalBuyMargin, instrument.TotalSellMargin);
            quantity = new BuySellPair(instrument.TotalBuyQuantity, instrument.TotalSellQuantity);
            if (physicalInstrument != null)
            {
                partialMargin = new BuySellPair(physicalInstrument.TotalBuyMarginForPartialPaymentPhysicalOrder, physicalInstrument.TotalSellMarginForPartialPaymentPhysicalOrder);
                partialQuantity = new BuySellPair(physicalInstrument.TotalBuyLotBalanceForPartialPaymentPhysicalOrder, physicalInstrument.TotalSellLotBalanceForPartialPaymentPhysicalOrder);
            }
            else
            {
                partialMargin = BuySellPair.Empty;
                partialQuantity = BuySellPair.Empty;
            }
            filledArgs.Add(isBuy, margin, quantity, partialMargin, partialQuantity);
            marginArgs.Add(isBuy, unfilledArgs);
            marginArgs.Add(isBuy, margin, quantity, partialMargin, partialQuantity);
        }
    }
}
