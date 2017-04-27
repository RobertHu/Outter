using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL
{
    internal abstract class ValuedPLCalculatorBase
    {
        protected Order _order;
        protected ValuedPLCalculatorBase(Order order)
        {
            Debug.Assert(!order.IsOpen);
            _order = order;
        }

        internal PLValue Calculate(ExecuteContext context)
        {
            var result = PLValue.Empty;
            foreach (var eachOrderRelation in _order.OrderRelations)
            {
                if (this.ShouldRecalculatePL())
                {
                    eachOrderRelation.CalculatePL(context);
                }
                if (this.ShouldDoValue(eachOrderRelation,context))
                {
                    eachOrderRelation.DoValue(context.TradeDay);
                }
                result += this.GetValuedPL(eachOrderRelation);
            }
            return result;
        }


        protected virtual bool ShouldRecalculatePL()
        {
            return true;
        }



        protected virtual bool ShouldDoValue(OrderRelation orderRelation, ExecuteContext context)
        {
            return !orderRelation.IsValued;
        }

        protected bool IsInValueDate(ExecuteContext context)
        {
            return _order.Owner.SettingInstrument(context.TradeDay).IsInValueDate(_order.Owner.Owner.Id, context);
        }

        private PLValue GetValuedPL(OrderRelation orderRelation)
        {
            if (!orderRelation.IsValued) return PLValue.Empty;
            return orderRelation.CalculateValuedPL();
        }
    }


    internal class ValuedPLCalculator : ValuedPLCalculatorBase
    {
        internal ValuedPLCalculator(Order order)
            : base(order) { }

        protected override bool ShouldDoValue(OrderRelation orderRelation, ExecuteContext context)
        {
            return base.ShouldDoValue(orderRelation,context) && this.IsInValueDate(context);
        }
    }

    internal sealed class ValuedPLBookCalculator : ValuedPLCalculator
    {
        internal ValuedPLBookCalculator(Order order)
            : base(order)
        {
        }

        protected override bool ShouldRecalculatePL()
        {
            return false;
        }
    }


    internal class PhysicalValuedPLCalculator : ValuedPLCalculatorBase
    {
        internal PhysicalValuedPLCalculator(PhysicalOrder order)
            : base(order) { }
    }

    internal sealed class PhysicalValuedPLBookCalculator : ValuedPLCalculatorBase
    {
        internal PhysicalValuedPLBookCalculator(PhysicalOrder order)
            : base(order) { }

        protected override bool ShouldRecalculatePL()
        {
            return false;
        }
    }


    internal class BOValuedPLCalculator : ValuedPLCalculatorBase
    {
        internal BOValuedPLCalculator(BinaryOption.Order order)
            : base(order)
        {
        }

        protected override bool ShouldDoValue(OrderRelation orderRelation, ExecuteContext context)
        {
            return base.ShouldDoValue(orderRelation,context) && this.IsInValueDate(context);
        }
    }


    internal sealed class BOValuedPLBookCalculator : BOValuedPLCalculator
    {
        internal BOValuedPLBookCalculator(BinaryOption.Order order)
            : base(order)
        {
        }

        protected override bool ShouldRecalculatePL()
        {
            return false;
        }

    }




}
