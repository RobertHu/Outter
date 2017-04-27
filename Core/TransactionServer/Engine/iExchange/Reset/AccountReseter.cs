using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Engine.iExchange.Reset
{
    internal sealed class AccountReseter
    {
        internal Guid Id { get; private set; }

        internal DateTime LastTradeDay { get; private set; }

        internal void DoReset(Guid instrumentId)
        {
        }

        internal void CalculateBalance()
        {
        }

    }
}
