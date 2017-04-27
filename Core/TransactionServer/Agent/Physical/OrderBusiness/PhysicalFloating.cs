using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Quotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalFloating : OrderFloating
    {
        private BusinessItem<decimal> _marketValue;
        private BusinessItem<decimal> _valueAsMargin;
        internal PhysicalFloating(PhysicalOrder order, PhysicalCalculateParams calculateParams)
            : base(order, calculateParams)
        {
            _marketValue = BusinessItemFactory.Create("MarketValue", 0m, PermissionFeature.Dumb, order);
            _valueAsMargin = BusinessItemFactory.Create("ValueAsMargin", 0m, PermissionFeature.Dumb, order);
        }

        internal decimal MarketValue
        {
            get { return _marketValue.Value; }
            private set { _marketValue.SetValue(value); }
        }

        internal decimal ValueAsMargin
        {
            get { return _valueAsMargin.Value; }
            private set { _valueAsMargin.SetValue(value); }
        }

        protected override void InnerCalculate(Quotation quotation)
        {
            base.InnerCalculate(quotation);
            if (this.NeedCalculateMarketValue())
            {
                this.CalculateMarketValue(quotation);
            }
        }

        private void CalculateMarketValue(Quotation quotation)
        {
            if (this.NeedCalculateMarketValue())
            {
                decimal valueAsMargin = 0;
                this.MarketValue = MarketValueCalculator.CaculatePhysicalOrderMarketValue(_owner, quotation, null, out valueAsMargin);
                this.ValueAsMargin = valueAsMargin;
            }
        }

        private bool NeedCalculateMarketValue()
        {
            bool result = false;
            var changedItems = _calculateParams.ChangedItem;
            result |= this.NeedCalculateCommon();
            result |= changedItems.Include(ChangedItem.Quotation);
            result |= changedItems.Include(ChangedItem.IsPayoff);
            result |= changedItems.Include(ChangedItem.DiscountOfOdd);
            result |= changedItems.Include(ChangedItem.ValueDiscountAsMargin);
            return result;
        }

        protected override bool NeedCalculateTradePL()
        {
            var isPayOffChanged = _calculateParams.ChangedItem.Include(ChangedItem.IsPayoff);
            return base.NeedCalculateTradePL() || isPayOffChanged;
        }

        protected override bool NeedCalculateNecessary()
        {
            var isPayOffChanged = _calculateParams.ChangedItem.Include(ChangedItem.IsPayoff);
            return base.NeedCalculateNecessary() || isPayOffChanged;
        }

    }
}
