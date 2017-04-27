using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class MultipleCloser
    {
        private class OpenOrderInfo
        {
            public Guid ID;
            public decimal Lot;

            public OpenOrderInfo(Guid id, decimal lot)
            {
                this.ID = id;
                this.Lot = lot;
            }
        }

        internal static Transaction Close(Account account, Guid[] orderIds)
        {
            account.Verify(orderIds);
            return MultipleClose(account, orderIds);
        }

        private static Transaction MultipleClose(Account account, Guid[] orderIds)
        {
            Order firstOrder = account.GetOrder(orderIds[0]);
            Transaction tran = TransactionFacade.CreateMultipleCloseTran(firstOrder.Owner.SettingInstrument().Category, account, firstOrder.Instrument().Id, firstOrder.Owner.ContractSize(null), account.Customer.Id);
            bool isOpenByBuy = false;
            List<OpenOrderInfo> openOrders = new List<OpenOrderInfo>();
            foreach (Guid eachOrderId in orderIds)
            {
                Order order = account.GetOrder(eachOrderId);
                if (openOrders.Count == 0)
                {
                    isOpenByBuy = order.IsBuy;
                    openOrders.Add(new OpenOrderInfo(order.Id, order.LotBalance));
                }
                else if (order.IsBuy == isOpenByBuy)
                {
                    openOrders.Add(new OpenOrderInfo(order.Id, order.LotBalance));
                }
                else
                {
                    tran.CloseOrder(account, order, openOrders, isOpenByBuy);
                }
            }
            tran.ExecuteDirectly( Engine.ExecuteContext.CreateExecuteDirectly(account.Id, tran.Id, Engine.ExecuteStatus.Filled));
            return tran;
        }

        private static void CloseOrder(this Transaction tran, Account account, Order order, List<OpenOrderInfo> openOrders, bool isOpenByBuy)
        {
            decimal totalOpenLot = 0;
            foreach (OpenOrderInfo openOrder in openOrders)
            {
                totalOpenLot += openOrder.Lot;
            }
            decimal openToCloseLot = Math.Min(totalOpenLot, order.LotBalance);

            List<OrderRelationRecord> orderRelationRecords = new List<OrderRelationRecord>();
            decimal remainLot = openToCloseLot;
            foreach (OpenOrderInfo openOrder in openOrders)
            {
                decimal relationCloseLot = Math.Min(openOrder.Lot, remainLot);
                orderRelationRecords.Add(new OrderRelationRecord(account.GetOrder(openOrder.ID), relationCloseLot));
                openOrder.Lot -= relationCloseLot;
                remainLot -= relationCloseLot;
                if (remainLot == 0) break;
            }

            OrderFacade.Default.CreateMultipleCloseOrder(tran, openToCloseLot, order.ExecutePrice, !isOpenByBuy, orderRelationRecords);

            OrderFacade.Default.CreateMultipleCloseOrder(tran, openToCloseLot, order.ExecutePrice, !order.IsBuy, new List<OrderRelationRecord>() { new OrderRelationRecord(order, openToCloseLot) });

            if (order.LotBalance > openToCloseLot) //Change open direction with the remain lot
            {
                openOrders.Clear();

                isOpenByBuy = order.IsBuy;
                openOrders.Add(new OpenOrderInfo(order.Id, order.LotBalance - openToCloseLot));
            }
            else
            {
                //Remove all orders have been full closed
                for (int i = openOrders.Count - 1; i >= 0; i--)
                {
                    if (openOrders[i].Lot == 0)
                    {
                        openOrders.RemoveAt(i);
                    }
                }
            }
        }


        private static void Verify(this Account account, Guid[] orderIds)
        {
            decimal contractSize = 0;
            Guid accountId = default(Guid);
            Guid instrumentId = default(Guid);
            string previousOrderCode = null;
            bool hasBuy = false;
            bool hasSell = false;
            TransactionError error = TransactionError.OK;
            string errorInfo = "TransactionServer.MultipleClose:";
            foreach (Guid eachOrderId in orderIds)
            {
                errorInfo += "{" + eachOrderId.ToString() + "} ";

                Order order = account.GetOrder(eachOrderId);
                if (order == null)
                {
                    error = TransactionError.MultipleCloseOrderNotFound;
                }
                else if (order.Phase != OrderPhase.Executed)
                {
                    error = TransactionError.MultipleCloseOnlyExecutedOrderAllowed;
                }
                else if (!order.IsOpen)
                {
                    error = TransactionError.MultipleCloseOnlyOpenOrderAllowed;
                }
                else if (order.LotBalance == 0)
                {
                    error = TransactionError.MultipleCloseHasNoLotBalance;
                }
                else if (contractSize == 0)
                {
                    contractSize = order.Owner.ContractSize(null);
                    accountId = order.Owner.AccountId;
                    instrumentId = order.Owner.InstrumentId;
                }
                else if (contractSize != order.Owner.ContractSize(null))
                {
                    error = TransactionError.MultipleCloseOnlySameContractSizeAllowed;
                }
                else if (accountId != order.Owner.AccountId)
                {
                    error = TransactionError.MultipleCloseOnlySameAccountAllowed;
                }
                else if (instrumentId != order.Owner.InstrumentId)
                {
                    error = TransactionError.MultipleCloseOnlySameInstrumentAllowed;
                }
                else if (string.Compare(previousOrderCode, order.Code, StringComparison.InvariantCulture) > 0)
                {
                    error = TransactionError.MultipleCloseNotSortByCode;
                }

                if (error != TransactionError.OK)
                {
                    throw new TransactionServerException(error, errorInfo);
                }

                if (order.IsBuy)
                {
                    hasBuy = true;
                }
                else
                {
                    hasSell = true;
                }

                previousOrderCode = order.Code;
            }
            if (!(hasBuy && hasSell))
            {
                error = TransactionError.MultipleCloseOppositeNotFound;
            }

            if (error != TransactionError.OK)
            {
                throw new TransactionServerException(error, errorInfo);
            }
        }

    }
}
