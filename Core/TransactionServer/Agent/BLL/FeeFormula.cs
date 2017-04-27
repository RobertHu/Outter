using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL
{
    public enum FeeFormula
    {
        FixedAmount = 0,
        CS = 1,
        CSDividePrice = 2,
        CSMultiplyPrice = 3,
        Pips = 4,
        PricePips = 5,
        RealizedLoss = 6,
        RealizedProfit = 7,
        RealizedPL = 8,
        SharedPL = 9
    }

    public static class FeeFormulaExtension
    {
        public static bool TakeFeeAsCost(this FeeFormula formula)
        {
            return formula == FeeFormula.PricePips;
        }

        public static bool IsDependOnPL(this FeeFormula formula)
        {
            return formula == FeeFormula.RealizedLoss || formula == FeeFormula.RealizedProfit
                || formula == FeeFormula.RealizedPL || formula == FeeFormula.SharedPL;
        }
    }

}
