using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Physical.InstalmentBusiness
{
    internal static class InstalmentDetailCalculator
    {
        public static int Decimals;

        public static decimal GetFortnightRate(decimal interestrate)
        {
            return (decimal)(interestrate / 26);
        }

        public static decimal GetQuarterlyRate(decimal interestrate)
        {
            return (decimal)(interestrate / 4);
        }

        public static decimal GetRepaymentAmount(decimal balance, decimal rate, int installments)
        {
            double tmp = Math.Pow(1 + (double)rate, installments);//返回指定数字的指定次幂。
            return InstalmentDetailCalculator.Round((decimal)((double)balance * (double)rate * tmp / (tmp - 1)));
        }

        public static decimal Round(decimal dec)
        {
            return RoundHelper.RoundToDecimal(dec, InstalmentDetailCalculator.Decimals);
        }

        public static string RoundToString(decimal dec)
        {
            return InstalmentDetailCalculator.Round(dec).ToString().Replace(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
        }

        ///// <summary>
        ///// 方式：等本息还款
        ///// </summary>
        ///// <param name="totalPeriod">期限</param>
        ///// <param name="balance">总金额</param>
        ///// <param name="interestRate">年利率</param>
        //public static List<InstalmentDetail> CalculateEqualInstalment(PhysicalOrder order, decimal balance, int totalPeriod, InstalmentFrequence frequence, decimal interestRate, DateTime tradeDay, int decimals)
        //{
        //    //InstalmentDetailCalculator.Decimals = decimals;
        //    //decimal principal = 0;
        //    //decimal rate0 = decimal.MinValue, instalmentAmount = decimal.MinValue;
        //    //decimal remainBalance = balance;
        //    //List<InstalmentDetail> result = new List<InstalmentDetail>();
        //    //for (int eachPeriod = 1; eachPeriod <= totalPeriod; eachPeriod++)
        //    //{
        //    //    decimal interest = InstalmentDetailCalculator.Round(remainBalance * rate);//每期要还的利息
        //    //    if (rate != 0)
        //    //    {
        //    //        if (rate0 != rate)
        //    //        {
        //    //            instalmentAmount = GetRepaymentAmount(remainBalance, rate0 = rate, totalPeriod);//计算每期要还的钱
        //    //        }
        //    //        principal = instalmentAmount - interest;//每期要还的本金
        //    //        remainBalance -= principal;//剩余本金  
        //    //        if (eachPeriod == totalPeriod && balance != 0)
        //    //        {
        //    //            principal += balance;
        //    //            interest -= balance;
        //    //            remainBalance = 0;
        //    //        }
        //    //    }
        //    //    else
        //    //    {
        //    //        instalmentAmount = principal;
        //    //    }
        //    //    var paymentDateTimeOnPlan = InstalmentCalculator.Default.CalculatePaymentDateTimeOnPlan(frequence, eachPeriod, tradeDay);
        //    //    var instalmentDetail = new InstalmentDetail(order, eachPeriod, principal, interest, 0m, paymentDateTimeOnPlan, null);
        //    //    result.Add(instalmentDetail);
        //    //}
        //    //return result;
        //}


        ///// <summary>
        ///// 方式：等本金还款
        ///// </summary>
        ///// <param name="totalPeriod">期限</param>
        ///// <param name="amount">总金额</param>
        ///// <param name="monthRate">年利率</param>
        //public static List<InstalmentDetail> CalculateEqualPrincipal(PhysicalOrder order, decimal amount, int totalPeriod, InstalmentFrequence frequence, decimal monthRate, DateTime tradeDay, int decimals)
        //{
        //    InstalmentDetailCalculator.Decimals = decimals;
        //    decimal principal = 0;
        //    decimal remainAmount = amount;
        //    principal = InstalmentDetailCalculator.Round(amount / totalPeriod);
        //    List<InstalmentDetail> result = new List<InstalmentDetail>();
        //    decimal interest = 0m;
        //    for (int eachPeriod = 1; eachPeriod <= totalPeriod; eachPeriod++)
        //    {
        //        principal = InstalmentDetailCalculator.Round(amount / totalPeriod);
        //        interest = InstalmentDetailCalculator.Round(remainAmount * monthRate);
        //        var instalmentDetail = InstalmentDetailCalculator.CalculateInstalmentDetail(order, rate, eachPeriod, frequence, tradeDay, totalPeriod, remainAmount, principal);
        //        result.Add(instalmentDetail);
        //        remainAmount = InstalmentDetailCalculator.Round(remainAmount - principal);//剩余本金                    
        //    }
        //    return result;
        //}



        private static InstalmentDetail CalculateInstalmentDetail(PhysicalOrder order, decimal rate, int instalmentPeriod, InstalmentFrequence frequence, DateTime tradeDay, int lastPeriod, decimal balance, decimal originPrincipal)
        {
            decimal interest = 0m;
            decimal principal = originPrincipal;
            if (rate != 0)
            {
                interest = InstalmentDetailCalculator.Round(balance * rate);//每期要还的利息=上个月的剩余本金×每期利率
            }
            if (instalmentPeriod != 1)
            {
                if (instalmentPeriod == lastPeriod && balance != 0)
                {
                    principal = InstalmentDetailCalculator.Round(principal + balance);
                    if (rate != 0)
                    {
                        interest = InstalmentDetailCalculator.Round(interest - balance);
                    }
                }
            }
            var instalmentAmount = InstalmentDetailCalculator.Round(principal + interest);
            DateTime paymentDateTimeOnPlan = InstalmentCalculator.Default.CalculatePaymentDateTimeOnPlan(frequence, instalmentPeriod, tradeDay);
            return new InstalmentDetail(order, instalmentPeriod, principal, interest, 0m, paymentDateTimeOnPlan, null);
        }

    }


    internal static class RoundHelper
    {
        public static decimal RoundToDecimal(decimal value, int decimals)
        {
            return decimal.Parse(Math.Round(value, decimals).ToString(string.Format("N{0}", decimals)));
        }

        public static double RoundToDouble(double value, int decimals)
        {
            return double.Parse(Math.Round(value, decimals).ToString(string.Format("N{0}", decimals)));
        }
    }


}
