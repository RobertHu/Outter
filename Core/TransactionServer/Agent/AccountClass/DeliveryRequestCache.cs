using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class DeliveryRequestCache : BusinessRecordDictionary<Guid,DeliveryRequest>
    {
        internal DeliveryRequestCache(Account account, int capacity)
            : base(BusinessRecordCollectionNames.DeliveryRequests, account, capacity)
        {
        }
    }
}