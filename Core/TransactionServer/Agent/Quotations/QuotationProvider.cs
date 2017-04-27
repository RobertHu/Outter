using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Quotations
{
    internal static class QuotationProvider
    {
        internal static Quotation GetLatestQuotationAndQuotePolicyId(Guid instrumentId, IQuotePolicyProvider quotePolicyProvider, out Guid? quotePolicyId)
        {
            return Market.MarketManager.Default[instrumentId].GetQuotationAndQuotePolicyId(quotePolicyProvider, out quotePolicyId);
        }

        internal static bool HasTrdingQuotation(Guid instrumentId, IQuotePolicyProvider quotePolicyProvider)
        {
            return Market.MarketManager.Default[instrumentId].HasTrdingQuotation(quotePolicyProvider);
        }
    }
}
