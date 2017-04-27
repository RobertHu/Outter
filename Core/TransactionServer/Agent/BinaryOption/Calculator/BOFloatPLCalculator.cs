using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOFloatPLCalculator : OrderFloating
    {
        public static readonly BOFloatPLCalculator Default = new BOFloatPLCalculator();
        private BOFloatPLCalculator()
            : base(null, null) { }

        protected override bool NeedCalculateNecessary()
        {
            return false;
        }

        protected override bool NeedCalculateTradePL()
        {
            return false;
        }

        protected override bool NeedCalculateInterestAndStoragePL()
        {
            return false;
        }

    }
}
