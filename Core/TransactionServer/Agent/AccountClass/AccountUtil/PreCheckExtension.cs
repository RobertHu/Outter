using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.AccountClass.InstrumentUtil;

namespace Core.TransactionServer.Agent.AccountClass.AccountUtil
{
    internal static class PreCheckExtension
    {
        public static decimal CalculatePreCheckNecessary(this Account account, Transaction tran)
        {
            decimal result = 0m;
            foreach (var eachInstrument in account.Instruments)
            {
                var necessary = eachInstrument.CalculatePreCheckNecessary(tran);
                result +=  account.IsMultiCurrency ? eachInstrument.CurrencyRate.Exchange(necessary) : necessary;
            }
            return result;
        }


        public static decimal CalculatePreCheckBalance(this Account account)
        {
            decimal result = 0m;
            foreach (var eachTran in account.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    var balance  = eachOrder.CalculatePreCheckBalance();
                    if (account.IsMultiCurrency)
                    {
                        result += eachTran.CurrencyRate.Exchange(balance);
                    }
                    else
                    {
                        result += balance;
                    }
                }
            }
            return result;
        }
    }
}
