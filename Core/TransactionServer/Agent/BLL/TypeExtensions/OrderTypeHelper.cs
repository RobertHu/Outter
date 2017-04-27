using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TypeExtensions
{
    internal static class OrderTypeHelper
    {
        internal static bool IsPendingType(this OrderType orderType)
        {
            return orderType != OrderType.SpotTrade && orderType != OrderType.Market;
        }
    }
}
