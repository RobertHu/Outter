using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass.InstrumentUtil
{
    internal struct CalculateNecessaryResult
    {
        public decimal NetNecessary;
        public decimal HedgeNecessary;
        public decimal PartialPaymentPhysicalNecessary;
    }

    internal static class InstrumentNecessaryExtension
    {
        public static CalculateNecessaryResult CalculateNecessary(this Instrument instrument, MarginAndQuantityResult necessaryParams)
        {
            var result = new CalculateNecessaryResult();
            if (necessaryParams.PartialQuantity.Sell > 0)
            {
                result.PartialPaymentPhysicalNecessary = necessaryParams.PartialMargin.Sell;
            }
            else if (necessaryParams.PartialQuantity.Buy > 0)
            {
                result.PartialPaymentPhysicalNecessary = necessaryParams.PartialMargin.Buy;
            }
            instrument.CalculateNecessaryHelper(necessaryParams.Margin.Buy, necessaryParams.Quantity.Buy,
                necessaryParams.Margin.Sell, necessaryParams.Quantity.Sell, ref result.PartialPaymentPhysicalNecessary,
                out result.NetNecessary, out result.HedgeNecessary);
            return result;
        }
    }
}
