using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal static class OrderPlacePreCheckExtension
    {
        internal static decimal CalculatePreCheckNecessary(this Order order, Price preCheckPrice, decimal? effectiveLot)
        {
            decimal result = 0m;
            if (preCheckPrice != null)
            {
                var lot = effectiveLot.HasValue ? effectiveLot.Value : order.Lot;
                result = NecessaryCalculator.CalculateNecessary(order, preCheckPrice, lot);
            }
            else
            {
                var price = order.SetPrice == null ? order.ExecutePrice : order.SetPrice;
                result = NecessaryCalculator.CalculateNecessary(order, price, order.Lot);
            }
            return result;
        }

        internal static MarginAndQuantityResult CalculateFilledMarginAndQuantity(this Order order, bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            var result = new MarginAndQuantityResult();
            decimal margin, quantity;
            var physicalOrder = order as PhysicalOrder;
            bool isPartialPaymentPhysicalOrder = physicalOrder != null && physicalOrder.IsPartialPaymentPhysicalOrder;
            order.CalculateMarginAndQuantity(isPartialPaymentPhysicalOrder, remainFilledLotPerOrderDict, out margin, out quantity);
            result.AddMarginAndQuantity(order.IsBuy, isPartialPaymentPhysicalOrder, margin, quantity);
            return result;
        }

        private static void CalculateMarginAndQuantity(this Order order, bool isPartialPaymentPhysicalOrder, Dictionary<Guid, decimal> remainFilledLotPerOrderDict, out decimal margin, out decimal quantity)
        {
            margin = quantity = 0m;
            decimal remainLot;
            if (remainFilledLotPerOrderDict != null && remainFilledLotPerOrderDict.TryGetValue(order.Id, out remainLot))
            {
                if (remainLot > 0)
                {
                    margin = NecessaryCalculator.CalculateNecessary(order, null, remainLot);
                    quantity = isPartialPaymentPhysicalOrder ? remainLot : remainLot * order.Owner.ContractSize;
                }
            }
            else
            {
                margin = order.Necessary;
                quantity = isPartialPaymentPhysicalOrder ? order.LotBalance : order.LotBalance * order.Owner.ContractSize;
            }
        }


        internal static bool ShouldCalculateFilledMarginAndQuantity(this Order order, bool isBuy)
        {
            return order.IsBuy != isBuy && order.Phase == OrderPhase.Executed && order.IsOpen && order.LotBalance > 0 && order.IsRisky;
        }

        internal static BuySellLot CalculateLotBalance(this Order order)
        {
            decimal buyLotBalance = 0;
            decimal sellLotBalance = 0;
            if (order.Phase == OrderPhase.Executed)
            {
                if (order.IsBuy)
                {
                    buyLotBalance = order.LotBalance;
                }
                else
                {
                    sellLotBalance = order.LotBalance;
                }
            }
            else if (order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing)
            {
                if (order.IsOpen)
                {
                    if (order.IsBuy)
                    {
                        buyLotBalance = order.LotBalance;
                    }
                    else
                    {
                        sellLotBalance = order.LotBalance;
                    }
                }
                else //will close executed orders
                {
                    if (order.IsBuy)
                    {
                        sellLotBalance = -order.Lot;
                    }
                    else
                    {
                        buyLotBalance = -order.Lot;
                    }
                }
            }
            return new BuySellLot(buyLotBalance, sellLotBalance);
        }
    }
}
