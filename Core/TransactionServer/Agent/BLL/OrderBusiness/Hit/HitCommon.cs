using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Hit
{
    internal static class HitCommon
    {
        internal static Price CalculateMarketPrice(bool isBuy, Quotation newQuotation)
        {
            return isBuy ? newQuotation.SellPrice : newQuotation.BuyPrice;
        }
    }
}
