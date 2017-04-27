using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class CurrencyExtension
    {
        internal static Guid GetCurrencyId(this Order order)
        {
            var account = order.Account;
            if (account.IsMultiCurrency)
            {
                return order.Owner.CurrencyId;
            }
            else
            {
                return account.Setting().CurrencyId;
            }
        }
    }
}
