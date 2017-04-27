using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.InstrumentBusiness
{
    internal abstract class LotCalculator
    {
        protected AccountClass.Instrument _owner;
        protected LotCalculator(AccountClass.Instrument owner)
        {
            _owner = owner;
        }

        public abstract decimal CalculateBuyLotBalance();

        public abstract decimal CalculateSellLotBalance();

        public decimal CalculateBuyQuantity()
        {
            return this.CalculateTotalQuantity(TradeDirection.Buy);
        }

        public decimal CalculateSellQuantity()
        {
            return this.CalculateTotalQuantity(TradeDirection.Sell);
        }

        protected decimal CalculateTotalLotBalance(Predicate<Order> predicate)
        {
            decimal result = 0m;
            foreach (var eachOrder in _owner.ExecutedAndHasPositionOrders)
            {
                if (predicate(eachOrder) && eachOrder.IsRisky)
                {
                    result += eachOrder.LotBalance;
                }
            }
            return result;
        }

        protected decimal CalculateTotalQuantity(TradeDirection direction)
        {
            decimal result = 0m;
            foreach (var eachOrder in _owner.ExecutedAndHasPositionOrders)
            {
                if (direction.SameAs(eachOrder.IsBuy) && eachOrder.IsRisky)
                {
                    result += eachOrder.QuantityBalance;
                }
            }
            return result;
        }
    }

    internal sealed class GeneralLotCalculator : LotCalculator
    {
        internal GeneralLotCalculator(AccountClass.Instrument owner)
            : base(owner) { }

        public override decimal CalculateBuyLotBalance()
        {
            return this.CalculateTotalLotBalance(m => m.IsBuy);
        }

        public override decimal CalculateSellLotBalance()
        {
            return this.CalculateTotalLotBalance(m => !m.IsBuy);
        }
    }
}
