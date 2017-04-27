using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Physical;

namespace Core.TransactionServer.Engine.iExchange.BLL.Physical
{
    internal sealed class ShortSellBusiness
    {
        internal static decimal GetAllExcuteShortSellLot(Guid instrumentId, Account account)
        {
            decimal result = 0m;
            foreach (var tran in account.Transactions)
            {
                if (tran.InstrumentId != instrumentId) continue;
                foreach (PhysicalOrder order in tran.Orders)
                {
                    if (order.Phase == OrderPhase.Executed && order.PhysicalTradeSide == PhysicalTradeSide.ShortSell)
                    {
                        result += order.LotBalance;
                    }
                }
            }
            return result;
        }
    }
}
