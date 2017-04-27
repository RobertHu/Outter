using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions
{
    internal static class AccountExtensions
    {
        internal static DateTime GetLastExecuteTimeForPlacing(this Account account, Guid instrumentId)
        {
            DateTime lastExecuteTime = DateTime.MinValue;
            foreach (Transaction eachTran in account.GetTransactions(instrumentId))
            {
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (eachOrder.Phase == OrderPhase.Executed && eachOrder.IsOpen
                        && eachOrder.LotBalanceReal > 0 && eachTran.ExecuteTime > lastExecuteTime)
                    {
                        lastExecuteTime = eachTran.ExecuteTime.Value;
                    }
                }
            }
            return lastExecuteTime;
        }

    }
}
