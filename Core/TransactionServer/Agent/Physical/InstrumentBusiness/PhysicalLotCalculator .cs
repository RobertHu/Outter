using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.InstrumentBusiness
{
    internal sealed class PhysicalLotCalculator : LotCalculator
    {
        internal PhysicalLotCalculator(PhysicalInstrument owner)
            : base(owner) { }

        public override decimal CalculateBuyLotBalance()
        {
            return this.CalculateTotalLotBalance(m => m.IsBuy);
        }

        public override decimal CalculateSellLotBalance()
        {
            return this.CalculateTotalLotBalance(m => !m.IsBuy);
        }

        private bool IsPartialPaymentPhysicalOrder(Order order)
        {
            Debug.Assert(order.IsPhysical);
            return ((PhysicalOrder)order).IsPartialPaymentPhysicalOrder;
        }

        public decimal CalculateBuyLotBalanceForPartialPaymentPhysicalOrder()
        {
            return this.CalculateTotalLotBalance(m => this.IsPartialPaymentPhysicalOrder(m) && m.IsBuy);
        }

        public decimal CalculateSellLotBalanceForPartialPaymentPhysicalOrder()
        {
            return this.CalculateTotalLotBalance(m => this.IsPartialPaymentPhysicalOrder(m) && !m.IsBuy);
        }
    }
}
