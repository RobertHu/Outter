using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalOrderPlaceVerifier : OrderPlaceVerifierBase
    {
        public static readonly PhysicalOrderPlaceVerifier Default = new PhysicalOrderPlaceVerifier();

        private PhysicalOrderPlaceVerifier() { }
        static PhysicalOrderPlaceVerifier() { }

        protected override TradeSideVerifierBase CreateTradeSideVerifier()
        {
            return PhysicalOrderTradeSideVerifier.Default;
        }

        protected override void InnerVerify(Order order)
        {
            base.InnerVerify(order);
            this.VerifyPlaceSettings(order);
        }

        protected override bool ShouldVerifyMaxLot(Order order)
        {
            return true;
        }
    }
}
