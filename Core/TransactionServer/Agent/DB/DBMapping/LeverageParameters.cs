using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    internal sealed class LeverageParameters
    {
        internal Guid AccountId { get; set; }

        internal decimal Leverage { get; set; }

        internal decimal RateMarginO { get; set; }

        internal decimal RateMarginD { get; set; }

        internal decimal RateMarginLockO { get; set; }

        internal decimal RateMarginLockD { get; set; }
    }
}
