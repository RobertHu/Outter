using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.AccountBusiness
{
    internal sealed class PhysicalHelper
    {
        internal static bool HasFilledShortSellOrders(Account account, Guid instrumentId)
        {
            foreach (var eachTran in account.Transactions)
            {
                if (eachTran.IsPhysical && eachTran.InstrumentId == instrumentId)
                {
                    var physicalTran = (PhysicalTransaction)eachTran;
                    if (physicalTran.ExistsFilledShortSellOrder()) return true;
                }
            }
            return false;
        }

        internal static IEnumerable<Order> GetFilledShortSellOrders(Account account, Guid instrumentId)
        {
            foreach (var eachTran in account.Transactions)
            {
                if (eachTran.IsPhysical && eachTran.InstrumentId == instrumentId)
                {
                    foreach (PhysicalOrder eachOrder in eachTran.Orders)
                    {
                        if (eachOrder.IsExecuted && eachOrder.PhysicalTradeSide == PhysicalTradeSide.ShortSell && eachOrder.LotBalance > 0)
                        {
                            yield return eachOrder;
                        }
                    }
                }
            }
        }

    }
}
