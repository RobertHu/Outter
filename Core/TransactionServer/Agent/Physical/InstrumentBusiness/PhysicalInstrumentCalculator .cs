using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.Quotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.InstrumentBusiness
{
    internal sealed class PhysicalInstrumentCalculator : InstrumentCalculator
    {
        internal PhysicalInstrumentCalculator(PhysicalInstrument owner)
            : base(owner)
        {
        }

        public override void Calculate(DateTime baseTime, CalculateType calculateType, Quotation quotation)
        {
            base.Calculate(baseTime, calculateType, quotation);
            if (calculateType != CalculateType.CheckRiskForQuotation)
            {
                foreach (var eachTran in _owner.GetTransactions())
                {
                    foreach (var eachOrder in eachTran.Orders)
                    {
                        if (eachTran.Phase == iExchange.Common.TransactionPhase.Executed)
                        {
                            PhysicalOrder physicalOrder = (PhysicalOrder)eachOrder;
                            _riskData.TotalPaidAmount += physicalOrder.CalculatePaidAmount();
                        }
                    }
                }
            }
        }


        protected override void CalculateOrderFloatPL(Order order, ref NecessaryAndQuantity necessaryAndQuantity,Quotation quotation)
        {
            base.CalculateOrderFloatPL(order, ref necessaryAndQuantity,quotation);
        }

        public decimal CalculateBuyMarginForPartialPaymentPhysicalOrder()
        {
            return CalculateTotalMarginCommon(m => m.IsBuy && ((PhysicalOrder)m).IsPartialPaymentPhysicalOrder);
        }

        public decimal CalculateSellMarginForPartialPaymentPhysicalOrder()
        {
            return CalculateTotalMarginCommon(m => !m.IsBuy && ((PhysicalOrder)m).IsPartialPaymentPhysicalOrder);
        }

        protected override bool ShouldCalculateMargin(bool isBuy, Order order)
        {
            var physicalOrder = (PhysicalOrder)order;
            return base.ShouldCalculateMargin(isBuy, order) && !physicalOrder.IsPartialPaymentPhysicalOrder;
        }

        protected override bool ShouldCalculateLockOrderTradePLFloat(Settings.Account account, decimal buyQuantitySum, decimal sellQuantitySum)
        {
            return false;
        }

        protected override void AddUpNecessaryAndQuantity(Order order, ref NecessaryAndQuantity necessaryAndQuantity)
        {
            if (((PhysicalOrder)order).IsPartialPaymentPhysicalOrder)
            {
                necessaryAndQuantity.partialPhysicalNecessarySum += order.Necessary;
            }
            else
            {
                if (order.IsBuy)
                {
                    necessaryAndQuantity.buyNecessarySum += order.Necessary;
                    necessaryAndQuantity.buyQuantitySum += order.QuantityBalance;
                }
                else
                {
                    necessaryAndQuantity.sellNecessarySum += order.Necessary;
                    necessaryAndQuantity.sellQuantitySum += order.QuantityBalance;
                }
            }
        }

        protected override void AddUpCalculatedFloatPL(Order order)
        {
            base.AddUpCalculatedFloatPL(order);
            _riskData.ValueAsMargin += ((PhysicalOrder)order).ValueAsMargin;
        }
    }
}
