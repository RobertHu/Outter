using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.Facades
{
    internal sealed class CancelDeliveryWithShortSellOrderParam
    {
        internal bool IsOpen { get; set; }
        internal bool IsBuy { get; set; }
        internal Price SetPrice { get; set; }
        internal Price ExecutePrice { get; set; }
        internal decimal Lot { get; set; }
        internal decimal LotBalance { get; set; }
        internal TradeOption TradeOption { get; set; }
        internal Guid PhysicalRequestId { get; set; }
        internal List<OrderRelationRecord> OrderRelations { get; set; }
    }
}
