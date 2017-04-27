using Core.TransactionServer.Agent.Framework;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass.AccountUtil
{
    internal static class AccountCalculateExtension
    {
        public static  decimal CalculateCredit(this Account account , Instrument instrument)
        {
            decimal credit =account.Setting().CreditLotD * instrument.TradePolicyDetail(null).MarginD;
            decimal creditAmount = account.Setting().CreditAmount;
            if (credit == 0)
            {
                return creditAmount;
            }
            else if (creditAmount == 0)
            {
                return credit;
            }
            else
            {
                return Math.Min(credit, creditAmount);
            }
        }

        public static decimal CaculateUnclearBalance(this Account account)
        {
            return account.UnclearDepositManager.Sum();
        }

        public static BuySellLot  CalculateLotSummary(this Account account, Transaction excludedTran)
        {
            decimal buyLot = 0m;
            decimal sellLot = 0m;
            foreach (var tran in account.Transactions)
            {
                if (tran == excludedTran) continue;
                foreach (var order in tran.Orders)
                {
                    if (order.IsOpen && order.Phase == OrderPhase.Executed)
                    {
                        if (order.IsBuy)
                        {
                            buyLot += order.LotBalance;
                        }
                        else
                        {
                            sellLot += order.LotBalance;
                        }
                    }
                }
            }
            return new BuySellLot(buyLot, sellLot);
        }

    }
}
