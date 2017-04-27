using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal static class FilledCalculator
    {
        internal static decimal CalculateTotalAutoCloseLot(AccountClass.Instrument instrument, bool isBuy, decimal unfilledLot, Dictionary<Guid, decimal> remainLotsDict)
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
                        var items = FilledCalculator.CalculateOrderCanCloseLot(eachOrder, unfilledLot, remainLotsDict);
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

        internal static Tuple<decimal, decimal> CalculateOrderCanCloseLot(Order order, decimal unfilledLot, Dictionary<Guid, decimal> remainLotsDict)
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

        internal static MarginAndQuantityResult CalculateFillMarginAndQuantity(AccountClass.Instrument instrument, bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
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
}
