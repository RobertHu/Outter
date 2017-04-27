using Core.TransactionServer.Agent.Framework;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    internal class OrderNetVerifier
    {
        internal static readonly OrderNetVerifier Default = new OrderNetVerifier();

        static OrderNetVerifier() { }

        protected OrderNetVerifier() { }

        /// <summary>
        /// 判断当前账户中与该Order所下单的instrument相同的的单子的头寸
        /// 加上当前单子的手数后，头寸的方向是否和单子买卖的方向相同
        /// </summary>
        /// <param name="placingOrder"></param>
        /// <returns></returns>
        internal bool IsNetOpen(Order placingOrder)
        {
            decimal buyLot, sellLot;
            buyLot = sellLot = 0m;
            List<KeyValuePair<Guid, decimal>> remainCloseLotPerOpenOrderList;
            this.CalculateLotSummary(placingOrder, out remainCloseLotPerOpenOrderList, ref buyLot, ref sellLot);
            return IsNetOpenInner(placingOrder, remainCloseLotPerOpenOrderList, buyLot, sellLot);
        }

        private bool IsNetOpenInner(Order placingOrder, List<KeyValuePair<Guid, decimal>> remainCloseLotPerOpenOrderList, decimal buyLot, decimal sellLot)
        {
            decimal netLot = buyLot - sellLot;
            decimal lot = placingOrder.IsOpen ? placingOrder.Lot : this.CalculateClosedLot(placingOrder, remainCloseLotPerOpenOrderList);
            if (netLot == 0)
            {
                return placingOrder.IsOpen ? true : lot > 0;
            }
            else if (netLot > 0)
            {
                return placingOrder.IsBuy ? true : lot > netLot;
            }
            else
            {
                return !placingOrder.IsBuy ? true : lot > Math.Abs(netLot);
            }
        }

        private decimal CalculateClosedLot(Order placingOrder, List<KeyValuePair<Guid, decimal>> remainCloseLotPerOpenOrderList)
        {
            decimal closedLot = 0m;
            if (remainCloseLotPerOpenOrderList != null && remainCloseLotPerOpenOrderList.Count > 0)
            {
                decimal canBeClosedLot = 0;
                foreach (var eachPair in remainCloseLotPerOpenOrderList)
                {
                    OrderRelation relation = placingOrder.GetOrderRelation(eachPair.Key);
                    canBeClosedLot += Math.Min(eachPair.Value, relation.ClosedLot);
                }
                closedLot = Math.Min(placingOrder.Lot, canBeClosedLot);
            }
            return closedLot;
        }


        private void CalculateLotSummary(Order placingOrder, out List<KeyValuePair<Guid, decimal>> remainCloseLotPerOpenOrderList, ref decimal buyLot, ref decimal sellLot)
        {
            var account = placingOrder.Owner.Owner;
            decimal ocoLot = 0m;
            Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict = null;
            foreach (Transaction eachTran in account.GetTransactions(placingOrder.Instrument().Id))
            {
                if (!this.NeedToCalculate(eachTran, placingOrder)) continue;
                this.CalculateLotSummaryForTran(eachTran, placingOrder, ref buyLot, ref sellLot, ref ocoLot, ref closeLotsPerOpenOrderDict);
            }
            this.AddOCOLotAndCloseLot(placingOrder, closeLotsPerOpenOrderDict, ocoLot, out remainCloseLotPerOpenOrderList, ref buyLot, ref sellLot);
        }

        protected virtual bool NeedToCalculate(Transaction tran, Order placingOrder)
        {
            var amendedOrder = placingOrder.Owner.AmendedOrder;
            return tran.Id != placingOrder.Owner.Id && (amendedOrder == null || tran != amendedOrder.Owner);
        }

        private void AddOCOLotAndCloseLot(Order placingOrder, Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict, decimal ocoLot, out List<KeyValuePair<Guid, decimal>> remainCloseLotPerOpenOrderList, ref decimal buyLot, ref decimal sellLot)
        {
            remainCloseLotPerOpenOrderList = null;
            this.AddOCOLotTOLotSummary(placingOrder, ocoLot, ref buyLot, ref sellLot);
            if (closeLotsPerOpenOrderDict != null)
            {
                decimal totalCloseLot = 0;
                foreach (Order eachOpenOrder in closeLotsPerOpenOrderDict.Keys)
                {
                    decimal closeLot = GetMaxValidCloseLot(closeLotsPerOpenOrderDict[eachOpenOrder], eachOpenOrder.LotBalance);
                    totalCloseLot += closeLot;

                    if (!placingOrder.IsOpen && placingOrder.ContainsOrderRelation(eachOpenOrder.Id))
                    {
                        if (remainCloseLotPerOpenOrderList == null)
                        {
                            remainCloseLotPerOpenOrderList = new List<KeyValuePair<Guid, decimal>>();
                        }
                        remainCloseLotPerOpenOrderList.Add(new KeyValuePair<Guid, decimal>(eachOpenOrder.Id, eachOpenOrder.LotBalance - closeLot));
                    }
                }
                this.AddLotToLotSummary(placingOrder, totalCloseLot, ref buyLot, ref sellLot);
            }
        }

        private void AddOCOLotTOLotSummary(Order placingOrder, decimal ocoLot, ref decimal buyLot, ref decimal sellLot)
        {
            if (ocoLot > 0)
            {
                var lot = ocoLot / 2;
                this.AddLotToLotSummary(placingOrder, lot, ref buyLot, ref sellLot);
            }
        }

        private void CalculateLotSummaryForTran(Transaction tran, Order placingOrder, ref decimal buyLot, ref decimal sellLot, ref decimal ocoLot, ref Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict)
        {
            foreach (Order eachOrder in tran.Orders)
            {
                this.CalculateLotSummaryForOrder(eachOrder, placingOrder, tran.Type, ref buyLot, ref sellLot, ref ocoLot, ref closeLotsPerOpenOrderDict);
            }
        }

        private void CalculateLotSummaryForOrder(Order order, Order placingOrder, TransactionType transactionType, ref decimal buyLot, ref decimal sellLot, ref decimal ocoLot, ref Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict)
        {
            decimal lot = order.IsOpen ? order.LotBalance : (order.Phase == OrderPhase.Placed ? order.Lot : 0m);//lotBalance of close order is ZERO
            if (order.Phase == OrderPhase.Executed)
            {
                if (order.IsBuy) buyLot += lot;
                else sellLot += lot;
            }
            else if (order.Phase == OrderPhase.Placed && order.IsBuy == placingOrder.IsBuy)
            {
                if (!order.IsOpen)
                {
                    if (closeLotsPerOpenOrderDict == null)
                    {
                        closeLotsPerOpenOrderDict = new Dictionary<Order, List<decimal>>();
                    }
                    this.CollectLotSummaryForCloseOrder(order, closeLotsPerOpenOrderDict);
                }
                else
                {
                    if (transactionType == TransactionType.OneCancelOther)
                    {
                        ocoLot += lot;
                    }
                    else
                    {
                        this.AddLotToLotSummary(placingOrder, lot, ref buyLot, ref sellLot);
                    }
                }
            }
        }

        private void AddLotToLotSummary(Order placingOrder, decimal lot, ref decimal buyLot, ref decimal sellLot)
        {
            if (placingOrder.IsBuy)
            {
                buyLot += lot;
            }
            else
            {
                sellLot += lot;
            }
        }

        private void CollectLotSummaryForCloseOrder(Order order, Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict)
        {
            List<string> ocoCloseTrans = null;
            foreach (OrderRelation eachOrderRelation in order.OrderRelations)
            {
                this.CollectLotFromOrderRelation(eachOrderRelation, order, closeLotsPerOpenOrderDict, ref ocoCloseTrans);
            }
        }


        private void CollectLotFromOrderRelation(OrderRelation orderRelation, Order closeOrder, Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict, ref List<string> ocoCloseTrans)
        {
            if (closeOrder.Owner.Type == TransactionType.OneCancelOther)
            {
                if (ocoCloseTrans == null)
                {
                    ocoCloseTrans = new List<string>();
                }
                string ocoKey;
                if (this.IsOCOOrderCollected(closeOrder, orderRelation.OpenOrder, ocoCloseTrans, out ocoKey))
                {
                    return;
                }
                else
                {
                    ocoCloseTrans.Add(ocoKey);
                }
            }
            List<decimal> closeLotList = this.GetOrCreateCloseLotList(orderRelation.OpenOrder, closeLotsPerOpenOrderDict);
            closeLotList.Add(orderRelation.ClosedLot);
        }


        private bool IsOCOOrderCollected(Order order, Order openOrder, List<string> ocoCloseTrans, out string key)
        {
            key = string.Format("{0}-{1}", order.Owner.Id.ToString(), openOrder.Id.ToString());
            return ocoCloseTrans != null && ocoCloseTrans.Contains(key) ? true : false;
        }

        private List<decimal> GetOrCreateCloseLotList(Order openOrder, Dictionary<Order, List<decimal>> closeLotsPerOpenOrderDict)
        {
            List<decimal> result = null;
            if (!closeLotsPerOpenOrderDict.TryGetValue(openOrder, out result))
            {
                result = new List<decimal>();
                closeLotsPerOpenOrderDict[openOrder] = result;
            }
            return result;
        }



        private static decimal GetMaxValidCloseLot(List<decimal> closeLots, decimal lotBalance)
        {
            decimal result = 0;
            if (closeLots == null || closeLots.Count == 0) return result;
            if (closeLots.Count == 1)
            {
                result = Math.Min(closeLots[0], lotBalance);
            }
            else if (closeLots.Count == 2)
            {
                result = CalculateMaxValidLotWhenCloseOrderCountEqualTwo(closeLots, lotBalance);
            }
            else
            {
                result = CalculateMaxValidLotWhenCloseOrderCountExceedTwo(closeLots, lotBalance);
            }
            return result;
        }

        private static decimal CalculateMaxValidLotWhenCloseOrderCountEqualTwo(List<decimal> closeLots, decimal lotBalance)
        {
            decimal result = closeLots.Sum();
            if (result > lotBalance)
            {
                result = Math.Max(closeLots[0], closeLots[1]);
                result = Math.Min(result, lotBalance);
            }
            return result;
        }

        private static decimal CalculateMaxValidLotWhenCloseOrderCountExceedTwo(List<decimal> closeLots, decimal lotBalance)
        {
            decimal maxCloseLot = closeLots.Max();
            if (maxCloseLot >= lotBalance) return lotBalance;
            decimal sumCloseLot = closeLots.Sum();
            if (sumCloseLot <= lotBalance) return sumCloseLot;
            return CalculateMaxValidLotFromCombinations(lotBalance, closeLots);
        }

        private static decimal CalculateMaxValidLotFromCombinations(decimal lotBalance, List<decimal> closeLots)
        {
            decimal result = 0;
            for (int i = 2; i < closeLots.Count - 1; i++) // i represents the length of the combinations
            {
                Combinations<decimal> combinations = new Combinations<decimal>(closeLots, i);
                foreach (var each in combinations)
                {
                    decimal sum = each.Sum();
                    if (result < sum && sum <= lotBalance)
                    {
                        result = sum;
                    }
                    if (result == lotBalance) break;

                }
            }
            return result;
        }
    }

    internal sealed class BOOrderNetVerifier : OrderNetVerifier
    {
        internal static readonly BOOrderNetVerifier Default = new BOOrderNetVerifier();

        static BOOrderNetVerifier() { }
        private BOOrderNetVerifier() { }

        protected override bool NeedToCalculate(Transaction tran, Order placingOrder)
        {
            return false;
        }
    }


}
