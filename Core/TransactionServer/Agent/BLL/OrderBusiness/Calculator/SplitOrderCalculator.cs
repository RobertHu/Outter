using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Util.Code;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.OrderBusiness.Calculator
{
    public sealed class OrderSplitCalculator
    {
        private Order _order;
        private OrderSettings _settings;

        internal OrderSplitCalculator(Order order, OrderSettings settings)
        {
            _order = order;
            _settings = settings;
        }

        public void Split(Dictionary<Guid, decimal> openOrderPerClosedLotDict, bool isForCut)
        {
            this.InnerSplit(openOrderPerClosedLotDict, isForCut);
            this.ProcessAfterSplitComplete();
        }

        private void InnerSplit(Dictionary<Guid, decimal> openOrderPerClosedLotDict, bool isForCut)
        {
            if (_order.LotBalance == 0) return;
            var toBeClosedTrans = GetToBeClosedTransactions(_order.Owner);
            this.CloseTrans(openOrderPerClosedLotDict, toBeClosedTrans, isForCut);
        }

        private void CloseTrans(Dictionary<Guid, decimal> openOrderPerClosedLotDict, List<Transaction> toBeClosedTrans, bool isForCut)
        {
            foreach (var eachTran in toBeClosedTrans)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (_order.LotBalance == 0) return;
                    if (!eachOrder.IsExecuted || (isForCut && !eachOrder.IsRisky)) continue;
                    decimal closedLotForOpenOrder = this.GetClosedLotForOpenOrder(eachOrder.Id, openOrderPerClosedLotDict);
                    if (eachOrder.IsBuy != _order.IsBuy && eachOrder.LotBalance > closedLotForOpenOrder)
                    {
                        decimal closedLot = Math.Min(eachOrder.LotBalance - closedLotForOpenOrder, _order.LotBalance);
                        if (_order.IsOpen)
                        {
                            _settings.IsOpen = false;
                        }
                        _settings.LotBalance -= closedLot;
                        if (_order.Owner.IsPhysical && !_order.IsBuy)
                        {
                            ((PhysicalOrderSettings)_settings).PhysicalTradeSide = PhysicalTradeSide.Sell;
                        }
                        this.CreateOrderRelation(_order, eachOrder, closedLot);
                    }
                }
            }
        }

        private decimal GetClosedLotForOpenOrder(Guid orderId, Dictionary<Guid, decimal> openOrderPerClosedLotDict)
        {
            decimal closedLot = 0m;
            if (openOrderPerClosedLotDict == null) return closedLot;
            openOrderPerClosedLotDict.TryGetValue(orderId, out closedLot);
            return closedLot;
        }


        private void CreateOrderRelation(Order closeOrder, Order openOrder, decimal closedLot)
        {
            var factory = OrderRelationFacade.Default.GetAddOrderRelationFactory(closeOrder);
            var command = factory.Create(openOrder, closeOrder, closedLot);
            command.Execute();
        }


        private void ProcessAfterSplitComplete()
        {
            if (_order.LotBalance == 0) return;
            if (_order.LotBalance < _order.Lot)
            {
                var order = this.CreateOrder();
                order.Phase = OrderPhase.Executed;
                order.ExecutePrice = _order.ExecutePrice;
                order.Code = TransactionCodeGenerater.Default.GenerateOrderCode(_order.Account.Setting().OrganizationId);
                if (_order.Owner.IsPhysical && !order.IsBuy)
                {
                    ((PhysicalOrder)order).PhysicalTradeSide = PhysicalTradeSide.ShortSell;
                }
            }
            else
            {
                Debug.Assert(_settings.Lot == _settings.LotBalance);
                if (_order.Owner.IsPhysical && !_order.IsBuy)
                {
                    ((PhysicalOrderSettings)_settings).PhysicalTradeSide = PhysicalTradeSide.ShortSell;
                }
            }
        }


        private Order CreateOrder()
        {
            var orderConstuctParams = _order.ConstructParams.Copy();
            orderConstuctParams.Lot = _order.LotBalance;
            orderConstuctParams.LotBalance = _order.LotBalance;
            orderConstuctParams.IsOpen = true;
            _settings.Lot -= _order.LotBalance;
            _settings.LotBalance = 0;
            return _order.Owner.IsPhysical ? new PhysicalOrder(_order.Owner, (Agent.Periphery.OrderBLL.PhysicalOrderConstructParams)orderConstuctParams, _order.ServiceFactory) : new Order(_order.Owner, orderConstuctParams, _order.ServiceFactory);
        }


        private List<Transaction> GetToBeClosedTransactions(Transaction targetTran)
        {
            var account = targetTran.Owner;
            List<Transaction> trans = new List<Transaction>(account.Transactions.Count());
            foreach (var eachTran in targetTran.AccountInstrument.GetTransactions())
            {
                if (!eachTran.CanBeClosedBySplit(targetTran)) continue;
                trans.Add(eachTran);
            }
            trans.Sort(Transaction.AutoCloseComparer);
            return trans;
        }
    }
}