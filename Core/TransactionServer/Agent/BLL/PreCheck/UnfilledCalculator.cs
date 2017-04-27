using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Services;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal static class UnfilledCalculator
    {
        internal static MarginAndQuantityResult CalculateUnfilledMarginArgsForPlacePendingOrder(AccountClass.Instrument instrument, Transaction tran, bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            MarginAndQuantityResult result = new MarginAndQuantityResult();
            bool isAutoClose = tran.Owner.IsAutoClose || instrument.IsPhysical;
            decimal canAutoCloseLot = CalculateUnfilledAutoCloseLot(instrument, tran, isBuy, isAutoClose, remainFilledLotPerOrderDict);
            Dictionary<Guid, decimal> unfilledLotPerTran = null;
            if (isAutoClose && canAutoCloseLot > 0)
            {
                decimal totalAutoCloseLot = FilledCalculator.CalculateTotalAutoCloseLot(instrument, isBuy, canAutoCloseLot, remainFilledLotPerOrderDict);
                unfilledLotPerTran = CalculateRemainUnfilledLotPerTransactionInSameDirection(instrument, isBuy, totalAutoCloseLot);
            }
            result = CalculateUnfilledMarginAndQuantity(instrument, isBuy, unfilledLotPerTran);
            return result;
        }


        private static decimal CalculateUnfilledAutoCloseLot(AccountClass.Instrument instrument, Transaction tran, bool isBuy, bool isAutoClose, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
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
                        CalculateRemainFilledLotPerOpenOrder(eachOrder, remainFilledLotPerOrderDict);
                    }
                    else if (isAutoClose)
                    {
                        result += eachOrder.Lot;
                    }
                }
            }
            return result;
        }

        private static void CalculateRemainFilledLotPerOpenOrder(Order order, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
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

        private static Dictionary<Guid, decimal> CalculateRemainUnfilledLotPerTransactionInSameDirection(AccountClass.Instrument instrument, bool isBuy, decimal totalAutoCloseLot)
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

        private static MarginAndQuantityResult CalculateUnfilledMarginAndQuantity(AccountClass.Instrument instrument, bool isBuy, Dictionary<Guid, decimal> unfilledLotsPerTran)
        {
            MarginAndQuantityResult result = new MarginAndQuantityResult();
            foreach (Transaction eachTran in instrument.GetTransactions())
            {
                if (eachTran.OrderCount == 0) continue;
                decimal? unfilledLot;
                if (eachTran.ShouldCalculatePreCheckNecessary(instrument, isBuy, unfilledLotsPerTran, out unfilledLot))
                {
                    result += eachTran.CalculateUnfilledMarginAndQuantity(unfilledLot);
                }
            }
            return result;
        }


    }
}
