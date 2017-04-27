using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Engine;
using Protocal;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    public abstract class OrderExecuteService
    {
        protected Order _order;
        protected Account _account;
        protected OrderSettings _settings;

        protected OrderExecuteService(Order order, OrderSettings settings)
        {
            _order = order;
            _account = order.Owner.Owner;
            _settings = settings;
        }

        public void Execute(ExecuteContext context)
        {
            if (this.ShouldVerifyPendingConfirmLimitOrderLot())
            {
                this.VerifyPendingConfirmLimitOrderLot();
            }
            this.Calculate(context);
            this.UpdateInterestValueDate();
            if (_order.OrderType == OrderType.MultipleClose) //Special handle, prevent from changing report
            {
                _settings.Lot = 0;
            }
            AutoPriceCalculator.CalculateAutoPrice(_order);
        }

        private void Calculate(ExecuteContext context)
        {
            if (_order.IsOpen)
            {
                this.CalculateForOpenOrder(context);
            }
            else
            {
                this.CalculateForCloseOrder(context);
            }
            this.CalculateBalance();
        }

        protected abstract void CalculateForOpenOrder(ExecuteContext context);

        protected abstract void CalculateForCloseOrder(ExecuteContext context);


        public void PartialExecute(decimal executeLot, bool cancelRemain, XmlDocument toExecuteXmlTran, XmlNode toExecuteTranNode, List<OrderRelation> toBeRemovedOrderRelations)
        {
            OrderPartialExecutor.Execute(_order, _settings, executeLot, cancelRemain, toExecuteXmlTran, toExecuteTranNode, toBeRemovedOrderRelations);
        }

        private void VerifyPendingConfirmLimitOrderLot()
        {
            if (this.IsCloseOrderExceedPendingConfirLimitOrderLot())
            {
                throw new TransactionServerException(TransactionError.ExistPendingLimitCloseOrder, "closeLot plus pendingConfirmLimitOrderLot exceed related open order' s balance");
            }
        }

        protected virtual bool ShouldVerifyPendingConfirmLimitOrderLot()
        {
            return !_order.IsOpen && !_order.Owner.IsPending;
        }

        private bool IsCloseOrderExceedPendingConfirLimitOrderLot()
        {
            Debug.Assert(!_order.IsOpen);
            foreach (var eachOrderRelation in _order.OrderRelations)
            {
                var pendingConfirmClosedLot = this.GetPendingConfirmClosedLot(eachOrderRelation.OpenOrder);
                if (pendingConfirmClosedLot + eachOrderRelation.ClosedLot > eachOrderRelation.OpenOrder.LotBalance)
                {
                    return true;
                }
            }
            return false;
        }


        private decimal GetPendingConfirmClosedLot(Order openOrder)
        {
            var ocoTrans = new List<Transaction>();
            var closeOrders = openOrder.GetAllCloseOrderAndClosedLot();
            var pendingConfirmClosedLot = 0m;
            foreach (var closeOrderAndLot in closeOrders)
            {
                var closeOrder = closeOrderAndLot.Key;
                if (ocoTrans.Contains(closeOrder.Owner)) continue;
                var closedLot = closeOrderAndLot.Value;
                var pendingConfirmLimitOrders = _account.GetPendingConfirmLimitOrders();
                if (closeOrder.Owner.OrderType.IsPendingType() && pendingConfirmLimitOrders.Contains(closeOrder))
                {
                    pendingConfirmClosedLot += closedLot;
                }
                if (closeOrder.Owner.Type == TransactionType.OneCancelOther)
                {
                    ocoTrans.Add(closeOrder.Owner);
                }
            }
            return pendingConfirmClosedLot;
        }

        private void UpdateInterestValueDate()
        {
            var tradePolicyDetail = _order.Owner.TradePolicyDetail;
            int interestValueDay = _order.IsBuy ? tradePolicyDetail.BuyInterestValueDay : tradePolicyDetail.SellInterestValueDay;
            var accountId = _order.Owner.Owner.Id;
            var tradeDay = Settings.SettingManager.Default.Setting.GetTradeDay();
            _settings.InterestValueDate = tradeDay.Day.AddDays(interestValueDay);
        }

        private void CalculateBalance()
        {
            Debug.Assert(_order.IsExecuted);
            var deltaBalance = _order.SumBillsForBalance();
            var account = _order.Owner.Owner;
            var currencyId = _order.Owner.CurrencyId;
            decimal accountBalance = account.IsMultiCurrency? account.GetOrCreateFund(currencyId).Balance: account.SumFund.Balance;
            if (accountBalance < 0 || accountBalance + deltaBalance < 0)
            {
                throw new OrderExecuteAccountBalanceNotEnoughException(account.Id, accountBalance);
            }
            _order.Owner.Owner.AddBalance(_order.Owner.CurrencyId, deltaBalance);
        }

    }

    internal sealed class OrderExecuteAccountBalanceNotEnoughException : Exception
    {
        internal OrderExecuteAccountBalanceNotEnoughException(Guid accountId, decimal balance)
        {
            this.AccountId = accountId;
            this.Balance = balance;
        }

        internal Guid AccountId { get; private set; }
        internal decimal Balance { get; private set; }

    }


    public class GeneralOrderExecuteService : OrderExecuteService
    {
        internal GeneralOrderExecuteService(Order order, OrderSettings settings)
            : base(order, settings) { }

        protected override void CalculateForOpenOrder(ExecuteContext context)
        {
            if (context.IsTryExecute || context.ShouldCancelExecute) return;
            _order.CalculateFee();
        }

        protected override void CalculateForCloseOrder(ExecuteContext context)
        {
            _order.UpdateOpenOrder();
            if (!context.IsTryExecute && !context.ShouldCancelExecute)
            {
                _order.CalculateFee();
            }
            if (!context.ShouldCancelExecute)
            {
                _order.CalculateValuedPL();
            }
        }
    }

    public sealed class GeneralOrderBookExecuteService : GeneralOrderExecuteService
    {
        internal GeneralOrderBookExecuteService(Order order, OrderSettings settings)
            : base(order, settings) { }

        protected override bool ShouldVerifyPendingConfirmLimitOrderLot()
        {
            return false;
        }

    }


    public static class OrderPartialExecutor
    {
        internal static void Execute(Order order, OrderSettings orderSettings, decimal executeLot, bool cancelRemain, XmlDocument toExecuteXmlTran, XmlNode toExecuteTranNode, List<OrderRelation> toBeRemovedOrderRelations)
        {
            XmlElement toExecuteOrderNode = null;
            if (!cancelRemain)
            {
                toExecuteOrderNode = AppendNewOrder(order, toExecuteXmlTran, toExecuteTranNode, executeLot);
            }
            decimal remainLot = order.Lot - executeLot;
            orderSettings.Lot = cancelRemain ? executeLot : remainLot;
            if (order.IsOpen)
            {
                orderSettings.LotBalance = order.Lot;
            }
            else
            {
                PartialExecuteCloseOrder(order, cancelRemain, toExecuteXmlTran, toExecuteOrderNode, toBeRemovedOrderRelations);
            }
        }

        private static void PartialExecuteCloseOrder(Order order, bool cancelRemain, XmlDocument toExecuteXmlTran, XmlNode toExecuteOrderNode, List<OrderRelation> toBeRemovedOrderRelations)
        {
            List<OrderRelation> orderRelations = new List<OrderRelation>(order.OrderRelations.Count());
            foreach (OrderRelation orderRelation in order.OrderRelations)
            {
                orderRelations.Add(orderRelation);
            }
            orderRelations.Sort(cancelRemain ? OrderRelation.OpenOrderExecuteTimeAscendingComparer : OrderRelation.OpenOrderExecuteTimeDescendingComparer);
            decimal remainLot = order.Lot;
            foreach (OrderRelation orderRelation in orderRelations)
            {
                orderRelation.ExecuteService.PartialExecute(ref remainLot, cancelRemain, toExecuteXmlTran, toExecuteOrderNode);
                if (remainLot <= 0)
                {
                    toBeRemovedOrderRelations.Add(orderRelation);
                }
            }

        }

        private static XmlElement AppendNewOrder(Order order, XmlDocument toExecuteXmlTran, XmlNode toExecuteTranNode, decimal executeLot)
        {
            XmlElement toExecuteOrderNode = toExecuteXmlTran.CreateElement("Order");
            toExecuteTranNode.AppendChild(toExecuteOrderNode);
            toExecuteOrderNode.SetAttribute("ID", XmlConvert.ToString(Guid.NewGuid()));
            toExecuteOrderNode.SetAttribute("Phase", XmlConvert.ToString((int)order.Phase));
            toExecuteOrderNode.SetAttribute("TradeOption", XmlConvert.ToString((int)order.TradeOption));
            toExecuteOrderNode.SetAttribute("IsOpen", XmlConvert.ToString(order.IsOpen));
            toExecuteOrderNode.SetAttribute("IsBuy", XmlConvert.ToString(order.IsBuy));
            toExecuteOrderNode.SetAttribute("Lot", XmlConvert.ToString(executeLot));
            toExecuteOrderNode.SetAttribute("OriginalLot", XmlConvert.ToString(order.OriginalLot));
            toExecuteOrderNode.SetAttribute("LotBalance", XmlConvert.ToString(executeLot));
            if (order.SetPrice != null) toExecuteOrderNode.SetAttribute("SetPrice", (string)order.SetPrice);
            if (order.SetPrice2 != null) toExecuteOrderNode.SetAttribute("SetPrice2", (string)order.SetPrice2);
            if (order.SetPriceMaxMovePips != 0) toExecuteOrderNode.SetAttribute("SetPriceMaxMovePips", XmlConvert.ToString(order.SetPriceMaxMovePips));
            if (order.DQMaxMove != 0) toExecuteOrderNode.SetAttribute("DQMaxMove", XmlConvert.ToString(order.DQMaxMove));
            if (order.ExecutePrice != null) toExecuteOrderNode.SetAttribute("ExecutePrice", (string)order.ExecutePrice);
            if (order.AutoLimitPrice != null) toExecuteOrderNode.SetAttribute("AutoLimitPrice", (string)order.AutoLimitPrice);
            if (order.AutoStopPrice != null) toExecuteOrderNode.SetAttribute("AutoStopPrice", (string)order.AutoStopPrice);
            return toExecuteOrderNode;
        }
    }

}
