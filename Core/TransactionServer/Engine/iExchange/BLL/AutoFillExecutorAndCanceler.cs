using Core.TransactionServer;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Protocal;
using Core.TransactionServer.Agent;

namespace Core.TransactionServer.Engine.iExchange.BLL
{
    internal sealed class AutoFillExecutorAndCanceler
    {
        internal static AutoFillExecutorAndCanceler Default = new AutoFillExecutorAndCanceler();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AutoFillExecutorAndCanceler));
        static AutoFillExecutorAndCanceler() { }
        private AutoFillExecutorAndCanceler() { }

        internal void ExecuteOrders(List<DeferredAutoFillInfo> items)
        {
            foreach (var eachItem in items)
            {
                List<OrderPriceInfo> orderInfos;
                if (eachItem.Transaction.OrderType == OrderType.BinaryOption)
                {
                    orderInfos = this.GetPricesForBO(eachItem);
                }
                else
                {
                    orderInfos = this.GetBuySellPrice(eachItem);
                }
                var executeRequest = new OrderExecuteEventArgs(new ExecuteContext(eachItem.Account.Id, eachItem.Transaction.Id, ExecuteStatus.Filled, orderInfos));
                iExchangeEngine.Default.Execute(executeRequest);
            }
        }

        private List<OrderPriceInfo> GetBuySellPrice(DeferredAutoFillInfo item)
        {
            List<OrderPriceInfo> result = new List<OrderPriceInfo>();
            var tran = item.Transaction;
            foreach (var eachOrder in tran.Orders)
            {
                Price buyPrice = null, sellPrice = null;
                if (eachOrder.IsBuy)
                {
                    this.SetPrice(ref buyPrice, tran, eachOrder);
                }
                else
                {
                    this.SetPrice(ref sellPrice, tran, eachOrder);
                }
                result.Add(new OrderPriceInfo(eachOrder.Id, buyPrice, sellPrice));
            }
            return result;
        }

        private void SetPrice(ref Price target, Transaction tran, Order order)
        {
            target = order.SetPrice;
            Price bestPrice;
            if (this.TryGetBestPrice(tran, out bestPrice))
            {
                target = bestPrice;
            }
        }


        private bool TryGetBestPrice(Transaction tran, out Price bestPrice)
        {
            bestPrice = null;
            if (tran.FirstOrder.DQMaxMove > 0 && tran.FirstOrder.BestPrice != null)
            {
                bestPrice = tran.FirstOrder.BestPrice;
                return true;
            }
            return false;
        }


        private List<OrderPriceInfo> GetPricesForBO(DeferredAutoFillInfo item)
        {
            Price buyPrice = null;
            Price sellPrice = null;
            var tran = item.Transaction;
            var order = tran.FirstOrder;
            if (order.BestPrice != null)
            {
                buyPrice = sellPrice = order.BestPrice;
            }
            else
            {
                var quotation = tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
                buyPrice = quotation.BuyPrice;
                sellPrice = quotation.SellPrice;
            }
            return this.CreateOrderPriceInfos(tran, buyPrice, sellPrice);
        }


        private List<OrderPriceInfo> CreateOrderPriceInfos(Transaction tran, Price buyPrice, Price sellPrice)
        {
            List<OrderPriceInfo> result = new List<OrderPriceInfo>();
            foreach (var eachOrder in tran.Orders)
            {
                result.Add(new OrderPriceInfo(eachOrder.Id, buyPrice, sellPrice));
            }
            return result;
        }







        internal void CancelInvalidAutoFillTransactions(List<DeferredAutoFillInfo> items)
        {
            foreach (var item in items)
            {
                this.CancelInvalidAutoFillTransaction(item);
            }
        }

        internal void CancelInvalidAutoFillTransaction(DeferredAutoFillInfo entity, int dealay = 0)
        {
            Action action = () =>
            {
                if (dealay > 0) Thread.Sleep(dealay);
                try
                {
                    iExchangeEngine.Default.Cancel(entity.Transaction, CancelReason.OtherReason);
                }
                catch (TransactionServerException tranException)
                {
                    Logger.Warn(tranException.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            };
            Task.Factory.StartNew(action);
        }

    }

}
