using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset.Exceptions
{
    internal sealed class OrderDayHistoryException : Exception
    {
        internal OrderDayHistoryException(Guid orderId, DateTime tradeDay)
        {
            this.OrderId = orderId;
            this.TradeDay = tradeDay;
        }

        internal Guid OrderId { get; private set; }
        internal DateTime TradeDay { get; private set; }
    }

    internal sealed class OrderConvertException : Exception
    {
        internal OrderConvertException(Guid accountId, Guid instrumentId, Guid orderId, string msg = "")
            : base(msg)
        {
            this.AccountId = accountId;
            this.InstrumentId = instrumentId;
            this.OrderId = orderId;
        }

        internal Guid AccountId { get; private set; }

        internal Guid InstrumentId { get; private set; }

        internal Guid OrderId { get; private set; }
    }

}
