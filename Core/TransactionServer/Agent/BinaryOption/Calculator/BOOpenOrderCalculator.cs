using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOOpenOrderCalculator : OpenOrderCalculatorBase
    {
        internal BOOpenOrderCalculator(Order order, BOOrderSettings settings, BOOpenOrderServiceFactory factory)
            : base(order, settings, factory)
        {
        }

        public override void UpdateLotBalance(decimal lot)
        {
            _settings.LotBalance -= lot;
        }

        public override decimal CanBeClosedLot
        {
            get { return _order.LotBalance; }
        }

        public override bool IsValued()
        {
            return true;
        }

    }
}
