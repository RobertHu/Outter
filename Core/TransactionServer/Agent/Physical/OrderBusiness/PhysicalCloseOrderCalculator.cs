using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalCloseOrderCalculator : CloseOrderCalculator
    {
        private PhysicalOrderSettings _physicalSettings;
        private PhysicalOrder _physicalOrder;
        internal PhysicalCloseOrderCalculator(PhysicalOrder order, PhysicalOrderSettings physicalSettings)
            : base(order, physicalSettings)
        {
            _physicalSettings = physicalSettings;
            _physicalOrder = order;
        }

        protected override void UpdateOpenOrder(OrderRelation orderRelation, bool isLast, ExecuteContext context)
        {
            var physicalOrderRelation = orderRelation as PhysicalOrderRelation;
            var openOrder = orderRelation.OpenOrder as PhysicalOrder;
            physicalOrderRelation.IsFullPayment = openOrder.IsPayoff;
            this.CalculateOrderRelaiton(physicalOrderRelation, isLast, context);
            this.UpdateForOpenOrder(physicalOrderRelation, openOrder);
            this.UpdateCloseOrder(physicalOrderRelation, context);
        }

        private void UpdateForOpenOrder(PhysicalOrderRelation orderRelation, PhysicalOrder openOrder)
        {
            openOrder.PhysicalOriginValueBalance += -orderRelation.ClosedPhysicalValue;
            openOrder.PaidPledgeBalance += orderRelation.PayBackPledgeOfOpenOrder;
            openOrder.UpdateLotBalance(orderRelation.ClosedLot, _physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery);
        }

        private void UpdateCloseOrder(PhysicalOrderRelation orderRelation, ExecuteContext context)
        {
            _physicalOrder.PaidPledgeBalance += orderRelation.PayBackPledgeOfCloseOrder;
            _physicalOrder.AddBill(new Bill(_physicalOrder.AccountId, _physicalOrder.CurrencyId, orderRelation.PayBackPledgeOfOpenOrder, BillType.PayBackPledge, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
        }

        private void CalculateOrderRelaiton(PhysicalOrderRelation orderRelation, bool isLast, ExecuteContext context)
        {
            orderRelation.CalculateClosedPhysicalValue(context.TradeDay);
            orderRelation.CalculatePayBackPledge(isLast, context);
        }


        internal void AssignFrozenAndUnFrozenPhysicalValue(decimal frozenFund, decimal frozenLot)
        {
            decimal unFrozenLot = _physicalOrder.Lot - frozenLot;
            decimal unFrozenFund = _physicalOrder.PhysicalOriginValue - frozenFund;
            var targetCurrencyDecimals = _physicalOrder.Owner.CurrencyRate(null).TargetCurrency.Decimals;
            foreach (PhysicalOrderRelation orderRelation in _physicalOrder.OrderRelations)
            {
                var openOrder = orderRelation.OpenOrder as PhysicalOrder;
                if (this.ShouldFrozen(openOrder))
                {
                    decimal physicalValue = Math.Round((orderRelation.ClosedLot / frozenLot) * frozenFund, targetCurrencyDecimals, MidpointRounding.AwayFromZero);
                    var tradeDay = Settings.Setting.Default.GetTradeDay();
                    DateTime frozenFundMatureDay = tradeDay.Day.AddDays(openOrder.PhysicalValueMatureDay - 1);
                    orderRelation.SetPhysicalValueAndMatureDay(physicalValue, frozenFundMatureDay);
                }
                else
                {
                    decimal physicalValue = Math.Round((orderRelation.ClosedLot / unFrozenLot) * unFrozenFund, targetCurrencyDecimals, MidpointRounding.AwayFromZero);
                    orderRelation.SetPhysicalValueAndMatureDay(physicalValue, null);
                }
            }
        }

        private bool ShouldFrozen(PhysicalOrder openOrder)
        {
            return _physicalOrder.PhysicalTradeSide != PhysicalTradeSide.Delivery && openOrder.PhysicalTradeSide == PhysicalTradeSide.Deposit && openOrder.PhysicalValueMatureDay > 0;
        }


    }
}
