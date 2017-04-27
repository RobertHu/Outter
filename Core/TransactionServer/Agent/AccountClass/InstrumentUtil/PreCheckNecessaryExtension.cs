using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.TypeExtension;
using Core.TransactionServer.Agent.BLL.OrderBusiness.TypeExtension;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.Agent.AccountClass.InstrumentUtil
{
    internal sealed class UnfilledCalculator
    {
        internal static readonly UnfilledCalculator Default = new UnfilledCalculator();
        static UnfilledCalculator() { }
        private UnfilledCalculator() { }

        internal MarginAndQuantityResult CalculateUnfilledMarginArgsForPlacePendingOrder(Instrument instrument, Transaction tran, bool isBuy, out Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            MarginAndQuantityResult result = new MarginAndQuantityResult();
            bool isAutoClose = tran.Owner.IsAutoClose || instrument.IsPhysical;
            decimal totalUnfilledLot = this.CalculateUnfilledLot(instrument, tran, isBuy, isAutoClose, out remainFilledLotPerOrderDict);
            if (isAutoClose && totalUnfilledLot > 0)
            {
                decimal totalAutoCloseLot = FilledCalculator.Default.CalculateTotalAutoCloseLot(instrument, isBuy, totalUnfilledLot, remainFilledLotPerOrderDict);
                var unfilledLotPerTran = this.CalculateUnfilledLotPerTransaction(instrument, isBuy, totalAutoCloseLot);
                result = this.CalculateUnfilledMarginAndQuantity(instrument, isBuy, unfilledLotPerTran);
            }
            return result;
        }

        private decimal CalculateUnfilledLot(Instrument instrument, Transaction tran, bool isBuy, bool isAutoClose, out Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            remainFilledLotPerOrderDict = null;
            decimal result = 0m;
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                if (!eachTran.ShouldSumPlaceMargin()) continue;
                bool isHandledOCOTran = false;
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (isHandledOCOTran) continue;
                    isHandledOCOTran = eachTran.Type == TransactionType.OneCancelOther;
                    if (eachOrder.IsBuy != isBuy) continue;
                    if (!eachOrder.IsOpen)
                    {
                        if (remainFilledLotPerOrderDict == null)
                        {
                            remainFilledLotPerOrderDict = new Dictionary<Guid, decimal>();
                        }
                        this.CalculateRemainFilledLotPerOrder(eachOrder, remainFilledLotPerOrderDict);
                    }
                    else if (isAutoClose)
                    {
                        result += eachOrder.Lot;
                    }
                }
            }
            return result;
        }

        private void CalculateRemainFilledLotPerOrder(Order order, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            foreach (OrderRelation eachOrderRelation in order.OrderRelations)
            {
                decimal remainLot = 0m;
                var openOrder = eachOrderRelation.OpenOrder;
                if (!remainFilledLotPerOrderDict.TryGetValue(openOrder.Id, out remainLot))
                {
                    remainLot = openOrder.LotBalance;
                }
                remainLot -= eachOrderRelation.ClosedLot;
                remainFilledLotPerOrderDict[openOrder.Id] = remainLot;
            }
        }

        private Dictionary<Guid, decimal> CalculateUnfilledLotPerTransaction(Instrument instrument, bool isBuy, decimal totalAutoCloseLot)
        {
            Dictionary<Guid, decimal> result = null;
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                if (totalAutoCloseLot <= 0) break;
                if (!eachTran.ShouldSumPlaceMargin()) continue;
                bool isHandledOCOTran = false;
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (isHandledOCOTran) continue;
                    isHandledOCOTran = eachTran.Type == TransactionType.OneCancelOther;
                    if (eachOrder.IsBuy == isBuy && eachOrder.IsOpen)
                    {
                        decimal canFilledLot = Math.Min(totalAutoCloseLot, eachOrder.Lot);
                        decimal unfilledLot = eachOrder.Lot - canFilledLot;
                        if (result == null)
                        {
                            result = new Dictionary<Guid, decimal>();
                        }
                        result[eachTran.Id] = unfilledLot;
                        totalAutoCloseLot -= canFilledLot;
                        if (totalAutoCloseLot <= 0) break;
                    }
                }
            }
            return result;
        }

        private MarginAndQuantityResult CalculateUnfilledMarginAndQuantity(Instrument instrument, bool isBuy, Dictionary<Guid, decimal> unfilledLotsPerTran)
        {
            MarginAndQuantityResult result = new MarginAndQuantityResult();
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                decimal? unfilledLot;
                if (eachTran.ShouldCalculatePreCheckNecessary(instrument, isBuy, unfilledLotsPerTran, out unfilledLot))
                {
                    result += eachTran.CalculateUnfilledMarginAndQuantity(unfilledLot);
                }
            }
            return result;
        }


    }

    internal sealed class FilledCalculator
    {
        internal static readonly FilledCalculator Default = new FilledCalculator();
        static FilledCalculator() { }
        private FilledCalculator() { }

        internal decimal CalculateTotalAutoCloseLot(Instrument instrument, bool isBuy, decimal unfilledLot, Dictionary<Guid, decimal> remainLotsDict)
        {
            decimal result = 0m;
            decimal remainUnfilledLot = unfilledLot;
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                if (remainUnfilledLot <= 0) break;
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsBuy != isBuy && eachOrder.IsOpen && eachOrder.Phase == OrderPhase.Executed)
                    {
                        var items = this.CalculateOrderCanCloseLot(eachOrder, unfilledLot, remainLotsDict);
                        decimal canClosedLot = items.Item1;
                        decimal remainLot = items.Item2;
                        remainLotsDict[eachOrder.Id] = remainLot - canClosedLot;
                        remainUnfilledLot -= canClosedLot;
                        result += canClosedLot;
                        if (remainUnfilledLot <= 0) break;
                    }
                }
            }
            return result;
        }

        internal Tuple<decimal, decimal> CalculateOrderCanCloseLot(Order order, decimal unfilledLot, Dictionary<Guid, decimal> remainLotsDict)
        {
            decimal canClosedLot = 0m;
            decimal remainLot;
            if (!remainLotsDict.TryGetValue(order.Id, out remainLot))
            {
                remainLot = order.LotBalance;
            }
            if (remainLot > 0)
            {
                canClosedLot = Math.Min(remainLot, unfilledLot);
            }
            return Tuple.Create(canClosedLot, remainLot);
        }

        internal MarginAndQuantityResult CalculateFillMarginAndQuantity(Instrument instrument, bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            var result = new MarginAndQuantityResult();
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (eachOrder.ShouldCalculateFilledMarginAndQuantity(isBuy))
                    {
                        result += eachOrder.CalculateFilledMarginAndQuantity(isBuy, remainFilledLotPerOrderDict);
                    }
                }
            }
            return result;
        }

    }

    internal static class PreCheckNecessaryExtension
    {
        internal static decimal CalculatePreCheckNecessary(this Instrument instrument, Transaction tran)
        {
            Account account = tran.Owner;
            TradePolicyDetail tradePolicyDetail = instrument.TradePolicyDetail;
            int decimals = Math.Min(instrument.Currency.Decimals, tradePolicyDetail.NecessaryRound);
            if (tran.TradingInstrument == instrument)
            {
                return InnerCalculatePreCheckNecessary(instrument, tran, tran.FirstOrder.IsBuy);
            }
            decimal buyPreCheckNecessary = InnerCalculatePreCheckNecessary(instrument, tran, true);
            decimal sellPreCheckNecessary = InnerCalculatePreCheckNecessary(instrument, tran, false);
            return Math.Max(buyPreCheckNecessary, sellPreCheckNecessary);
        }

        private static decimal InnerCalculatePreCheckNecessary(Instrument instrument, Transaction tran, bool isBuy)
        {
            TradePolicyDetail tradePolicyDetail = instrument.TradePolicyDetail;
            MarginAndQuantityResult unfilledArgs = new MarginAndQuantityResult();
            Dictionary<Guid, decimal> remainFilledLotPerOrderDict = null;
            if (tran.IsPending)
            {
                unfilledArgs = UnfilledCalculator.Default.CalculateUnfilledMarginArgsForPlacePendingOrder(instrument, tran, isBuy, out remainFilledLotPerOrderDict);
            }
            MarginAndQuantityResult filledArgs = new MarginAndQuantityResult();
            MarginAndQuantityResult marginArgs = new MarginAndQuantityResult();
            instrument.InitializeFilledAndMarginArgs(isBuy, unfilledArgs, filledArgs, marginArgs);
            filledArgs += FilledCalculator.Default.CalculateFillMarginAndQuantity(instrument, isBuy, remainFilledLotPerOrderDict);
            return CalculateNecessary(instrument, isBuy, marginArgs, filledArgs);
        }

        private static decimal CalculateNecessary(Instrument instrument, bool isBuy, MarginAndQuantityResult marginArgs, MarginAndQuantityResult filledArgs)
        {
            MarginAndQuantityResult necessaryParams = new MarginAndQuantityResult();
            necessaryParams.Add(isBuy, marginArgs, filledArgs);
            var result = instrument.CalculateNecessary(necessaryParams);
            return result.NetNecessary + result.HedgeNecessary;
        }


        private static void InitializeFilledAndMarginArgs(this Instrument instrument, bool isBuy, MarginAndQuantityResult unfilledArgs, MarginAndQuantityResult filledArgs, MarginAndQuantityResult marginArgs)
        {
            var physicalInstrument = instrument as PhysicalInstrument;
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

    internal sealed class MarginAndQuantityResult
    {
        internal MarginAndQuantityResult()
        {
            this.Margin = BuySellPair.Empty;
            this.Quantity = BuySellPair.Empty;
            this.PartialMargin = BuySellPair.Empty;
            this.PartialQuantity = BuySellPair.Empty;
        }

        internal BuySellPair Margin { get; private set; }
        internal BuySellPair Quantity { get; private set; }
        internal BuySellPair PartialMargin { get; private set; }
        internal BuySellPair PartialQuantity { get; private set; }

        internal void Add(bool isBuy, MarginAndQuantityResult unfilledResult, MarginAndQuantityResult filledResult)
        {
            if (isBuy)
            {
                this.Margin += new BuySellPair(unfilledResult.Margin.Buy, filledResult.Margin.Sell);
                this.Quantity += new BuySellPair(unfilledResult.Quantity.Buy, filledResult.Quantity.Sell);
                this.PartialMargin += new BuySellPair(unfilledResult.PartialMargin.Buy, filledResult.PartialMargin.Sell);
                this.PartialQuantity += new BuySellPair(unfilledResult.PartialQuantity.Buy, filledResult.PartialQuantity.Sell);
            }
            else
            {
                this.Margin += new BuySellPair(filledResult.Margin.Buy, unfilledResult.Margin.Sell);
                this.Quantity += new BuySellPair(filledResult.Quantity.Buy, unfilledResult.Margin.Sell);
                this.PartialMargin += new BuySellPair(filledResult.PartialMargin.Buy, unfilledResult.PartialMargin.Sell);
                this.PartialQuantity += new BuySellPair(filledResult.PartialQuantity.Buy, unfilledResult.PartialQuantity.Sell);
            }
        }

        internal void AddMarginAndQuantity(bool isBuy, bool isPartialPaymentPhysicalOrder, decimal margin, decimal quantity)
        {
            if (isBuy)
            {
                if (isPartialPaymentPhysicalOrder)
                {
                    this.PartialMargin.AddBuy(margin);
                    this.PartialQuantity.AddBuy(quantity);
                }
                else
                {
                    this.Margin.AddBuy(margin);
                    this.Quantity.AddBuy(quantity);
                }
            }
            else
            {
                if (isPartialPaymentPhysicalOrder)
                {
                    this.PartialMargin.AddSell(margin);
                    this.PartialQuantity.AddSell(quantity);
                }
                else
                {
                    this.Margin.AddSell(margin);
                    this.Quantity.AddSell(quantity);
                }
            }
        }

        internal void Add(bool isBuy, BuySellPair margin, BuySellPair quantity, BuySellPair partialMargin, BuySellPair partialQuantity)
        {
            if (isBuy)
            {
                this.Margin.AddBuy(margin);
                this.Quantity.AddBuy(quantity);
                this.PartialMargin.AddBuy(partialMargin);
                this.PartialQuantity.AddBuy(partialQuantity);
            }
            else
            {
                this.Margin.AddSell(margin);
                this.Quantity.AddSell(quantity);
                this.PartialMargin.AddSell(partialMargin);
                this.PartialQuantity.AddSell(partialQuantity);
            }
        }

        internal void Add(bool isBuy, MarginAndQuantityResult other)
        {
            if (isBuy)
            {
                this.Margin.AddBuy(other.Margin);
                this.Quantity.AddBuy(other.Quantity);
                this.PartialMargin.AddBuy(other.PartialMargin);
                this.PartialQuantity.AddBuy(other.PartialQuantity);
            }
            else
            {
                this.Margin.AddSell(other.Margin);
                this.Quantity.AddSell(other.Quantity);
                this.PartialMargin.AddSell(other.PartialMargin);
                this.PartialQuantity.AddSell(other.PartialQuantity);
            }
        }

        internal MarginAndQuantityResult Add(MarginAndQuantityResult other)
        {
            this.Margin += other.Margin;
            this.Quantity += other.Quantity;
            this.PartialMargin += other.PartialMargin;
            this.PartialQuantity += other.PartialQuantity;
            return this;
        }

        public static MarginAndQuantityResult operator +(MarginAndQuantityResult left, MarginAndQuantityResult right)
        {
            return left.Add(right);
        }

    }
}
