using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Quotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalCalculateParams : CalculateParams
    {
        private bool _isPayoff;
        private decimal _discountOfOdd;
        private decimal _valueDiscountAsMargin;
        internal PhysicalCalculateParams(PhysicalOrder order)
            : base(order) { }
        internal override ChangedItem Update(Quotation qutation)
        {
            var changedItems = base.Update(qutation);
            var physicalOpenOrder = (PhysicalOrder)_owner;
            if (_isPayoff != physicalOpenOrder.IsPayoff) changedItems |= ChangedItem.IsPayoff;
            var tradePolicyDetail = MissedTradePolicyDetailManager.Get(_owner);
            if (_discountOfOdd != tradePolicyDetail.DiscountOfOdd) changedItems |= ChangedItem.DiscountOfOdd;
            if (_valueDiscountAsMargin != tradePolicyDetail.ValueDiscountAsMargin) changedItems |= ChangedItem.ValueDiscountAsMargin;
            _isPayoff = physicalOpenOrder.IsPayoff;
            _discountOfOdd = tradePolicyDetail.DiscountOfOdd;
            _valueDiscountAsMargin = tradePolicyDetail.ValueDiscountAsMargin;
            this.ChangedItem = changedItems;
            return changedItems;
        }
    }
}
