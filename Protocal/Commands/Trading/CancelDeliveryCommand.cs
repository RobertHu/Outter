using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Commands.Trading
{
    [ProtoContract]
    public sealed class CancelDeliveryCommand : TradingCommand
    {
        public CancelDeliveryCommand() { }

        public CancelDeliveryCommand(Guid accountId, Guid deliveryRequestId, int status)
        {
            this.AccountId = accountId;
            this.DeliveryRequestId = deliveryRequestId;
            this.Status = status;
        }

        [ProtoMember(6)]
        public Guid DeliveryRequestId { get; set; }

        [ProtoMember(7)]
        public int Status { get; set; }
    }
}
