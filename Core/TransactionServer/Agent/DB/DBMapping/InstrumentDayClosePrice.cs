using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class InstrumentDayClosePrice
    {
        public DateTime TradeDay { get; set; }

        public Guid InstrumentID { get; set; }

        public Guid QuotePolicyID { get; set; }

        public string Ask { get; set; }

        public string Bid { get; set; }
    }
}
