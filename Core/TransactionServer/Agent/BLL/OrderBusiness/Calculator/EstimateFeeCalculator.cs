using Core.TransactionServer.Engine;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    internal static class EstimateFeeCalculator
    {
        internal static void RecalculateEstimateFee(this Order openOrder, ExecuteContext context)
        {
            decimal oldComision = openOrder.EstimateCloseCommission;
            decimal oldLevy = openOrder.EstimateCloseLevy;
            var fees = openOrder.CalculateEstimateFee(context);
            openOrder.UpdateEstimateFee(fees);
            var deltaCommission = openOrder.EstimateCloseCommission - oldComision;
            var deltaLevy = openOrder.EstimateCloseLevy - oldLevy;
            openOrder.Account.AddEstimateFee(deltaCommission, deltaLevy, openOrder.EstimateCurrencyRate(context.TradeDay));
        }

        internal static void UpdateEstimateFee(this Order order, Tuple<decimal, decimal> fees)
        {
            order.EstimateCloseCommission = fees.Item1;
            order.EstimateCloseLevy = fees.Item2;
        }

        internal static Tuple<decimal, decimal> CalculateEstimateFee(this Order order, ExecuteContext context)
        {
            if (!order.IsOpen || order.Owner.OrderType == OrderType.BinaryOption)
            {
                return Tuple.Create(0m, 0m);
            }

            decimal estimateCommission = 0, estimateLevy = 0, estimateOtherFee = 0;
            var tran = order.Owner;
            var feeParameter = FeeParameter.CreateByOpenOrder(context, order);

            OrderRelation.CalculateFee(feeParameter, out estimateCommission, out estimateLevy, out estimateOtherFee);
            var instrument = tran.SettingInstrument(context.TradeDay);

            //don't support fees denpends on PL, just set the value to ZERO now
            if (instrument.CommissionFormula.IsDependOnPL())
            {
                estimateCommission = 0;
            }

            if (instrument.LevyFormula.IsDependOnPL())
            {
                estimateLevy = 0;
            }
            return Tuple.Create(estimateCommission, estimateLevy);
        }

    }
}
