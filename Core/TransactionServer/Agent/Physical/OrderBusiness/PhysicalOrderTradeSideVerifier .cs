using Core.TransactionServer.Agent.BLL.OrderBusiness;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalOrderTradeSideVerifier : TradeSideVerifier
    {
        public static readonly PhysicalOrderTradeSideVerifier Default = new PhysicalOrderTradeSideVerifier();
        private PhysicalOrderTradeSideVerifier() { }
        static PhysicalOrderTradeSideVerifier() { }

        protected override bool ShouldVerify(Order order)
        {
            return true;
        }

        protected override void InnerVerify(Order order)
        {
            var physicalOrder = (PhysicalOrder)order;
            var tradeSideNotAllowed = (order.Owner.TradePolicyDetail.AllowedPhysicalTradeSides & physicalOrder.PhysicalTradeSide) != physicalOrder.PhysicalTradeSide;
            if (tradeSideNotAllowed)
            {
                if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell)
                {
                    throw new TransactionServerException(TransactionError.ShortSellNotAllowed);
                }
                else
                {
                    var error = physicalOrder.OrderType == OrderType.SpotTrade ? TransactionError.PriceChangedSincePlace : TransactionError.OrderTypeIsNotAcceptable;
                    throw new TransactionServerException(error);
                }
            }
        }
    }
}
