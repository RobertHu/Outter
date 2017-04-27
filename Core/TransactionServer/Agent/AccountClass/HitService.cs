using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Engine.iExchange;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class HitService
    {
        private Account _account;

        internal HitService(Account account)
        {
            _account = account;
        }

        internal void HitOrders(Agent.AccountClass.Instrument instrument, QuotationBulk bulk)
        {
            foreach (var eachOrder in instrument.WaitingForHitOrders)
            {
                Quotation quotation;
                if (bulk.TryGetQuotation(instrument.Id, eachOrder.Owner.SubmitorQuotePolicyProvider, out quotation))
                {
                    this.HitPlacedOrder(eachOrder, quotation);
                }
            }

            foreach (Order eachOrder in instrument.ExecutedAndHasPositionOrders)
            {
                Quotation quotation;
                if (bulk.TryGetQuotation(instrument.Id, _account, out quotation))
                {
                    this.HitExecutedOrder(eachOrder, quotation);
                }
            }
        }

        private void HitExecutedOrder(Order order, Quotation quotation)
        {
            OrderHitStatus status = order.HitAutoClosePrice(quotation, MarketManager.Now);
            Transaction closeTran = null;
            if (status == OrderHitStatus.ToAutoLimitClose)
            {
                closeTran = this.CloseByLimit(order);
            }
            else if (status == OrderHitStatus.ToAutoStopClose)
            {
                closeTran = this.CloseByStop(order);
            }

            if (closeTran != null)
            {
                iExchangeEngine.Default.Execute(new OrderExecuteEventArgs(ExecuteContext.CreateExecuteDirectly(closeTran.Owner.Id, closeTran.Id, ExecuteStatus.Filled)));
            }
        }

        internal OrderHitStatus HitPlacedOrder(Order order, Quotation quotation, bool ignoreHitTimes = false)
        {
            OrderHitStatus status = order.HitSetPrice(quotation, ignoreHitTimes,MarketManager.Now);
            if (status != OrderHitStatus.None)
            {
                order.IsHitReseted = false;
                order.Owner.Owner.SaveAndBroadcastChanges();
            }

            if (status == OrderHitStatus.Hit && order.Owner.OrderType == OrderType.Limit)
            {
                _account.AddPendingConfirmLimitOrder(order);
            }
            else if (status == OrderHitStatus.ToAutoFill)
            {
                this.AutoFillOrder(order);
            }
            return status;
        }

        private Transaction CloseByLimit(Order order)
        {
            return this.CreateCloseTransaction(order, order.AutoLimitPrice, OrderType.Limit);
        }

        private Transaction CloseByStop(Order order)
        {
            return this.CreateCloseTransaction(order, order.AutoStopPrice, OrderType.Stop);
        }

        private Transaction CreateCloseTransaction(Order order, Price closePrice, OrderType orderType)
        {
            var factory = TransactionFacade.CreateAddTranCommandFactory(orderType, order.Owner.InstrumentCategory);
            var command = factory.CreateByAutoClose(order.Owner.Owner, order, closePrice, orderType);
            command.Execute();
            return command.Result;
        }

        private void AutoFillOrder(Order order)
        {
            Price buy, sell;
            this.GetBuyAndSellPrice(order, out buy, out sell);
            var tran = order.Owner;
            Guid? executedOrderId = this.GetExecutedOrderId(order);
            OrderExecuteEventArgs eventArgs = new OrderExecuteEventArgs(new ExecuteContext(tran.Owner.Id, tran.Id, executedOrderId, ExecuteStatus.Filled, new List<OrderPriceInfo> { new OrderPriceInfo(order.Id, buy, sell) }));
            iExchangeEngine.Default.Execute(eventArgs);
        }

        private Guid? GetExecutedOrderId(Order order)
        {
            var tran = order.Owner;
            if (tran.Type == TransactionType.OneCancelOther || tran.SubType == TransactionSubType.IfDone)
            {
                return order.Id;
            }
            return null;
        }


        private void GetBuyAndSellPrice(Order order, out Price buy, out Price sell)
        {
            buy = sell = null;
            var orderType = order.Owner.OrderType;
            if (orderType == OrderType.Limit)
            {
                //Note: Limit use setprice
                buy = order.IsBuy ? order.SetPrice : null;
                sell = order.IsBuy ? null : order.SetPrice;
            }
            else if (orderType == OrderType.Market || orderType == OrderType.SpotTrade)
            {
                buy = order.IsBuy ? order.BestPrice : null;
                sell = order.IsBuy ? null : order.BestPrice;
            }
        }
    }
}
