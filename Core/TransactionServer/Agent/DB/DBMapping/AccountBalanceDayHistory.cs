using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class AccountBalanceDayHistory
    {
        public DateTime TradeDay { get; set; }

        public Guid AccountID { get; set; }

        public Guid CurrencyID { get; set; }

        public decimal Balance { get; set; }
    }
}
