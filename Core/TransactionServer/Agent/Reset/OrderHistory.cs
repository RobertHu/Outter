using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderHistory
    {
        internal Guid Id { get; set; }
        internal Guid AccountId { get; set; }
        internal Guid InstrumentId { get; set; }
    }

}
