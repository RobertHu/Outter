using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class InstrumentResetResult
    {
        public Guid AccountID { get; set; }
        public Guid InstrumentID { get; set; }
        public DateTime TradeDay { get; set; }
        public decimal ResetBalance { get; set; }
        public decimal FloatingPL { get; set; }
        public decimal InterestPLNotValued { get; set; }
        public decimal StoragePLNotValued { get; set; }
        public decimal TradePLNotValued { get; set; }
        public decimal Necessary { get; set; }
    }
}
