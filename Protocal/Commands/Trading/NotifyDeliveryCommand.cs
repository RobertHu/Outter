using iExchange.Common;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Commands.Trading
{
    [ProtoContract]
    public sealed class NotifyDeliveryCommand : TradingCommand
    {
        public NotifyDeliveryCommand() { }

        public NotifyDeliveryCommand(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime)
        {
            this.DeliveryRequestStatus = DeliveryRequestStatus.Approved;
            this.AccountId = accountId;
            this.DeliveryRequestId = deliveryRequestId;
            this.ApprovedId = approvedId;
            this.ApprovedTime = approvedTime;
            this.DeliveryTime = deliveryTime;
        }

        public NotifyDeliveryCommand(Guid accountId, Guid deliveryRequestId, DateTime avalibleDeliveryTime)
        {
            this.DeliveryRequestStatus = DeliveryRequestStatus.Stocked;
            this.AccountId = accountId;
            this.DeliveryRequestId = deliveryRequestId;
            this.AvalibleDeliveryTime = avalibleDeliveryTime;
        }

        [ProtoMember(6)]
        public Guid DeliveryRequestId { get; set; }

        [ProtoMember(7)]
        public DeliveryRequestStatus DeliveryRequestStatus { get; set; }

        [ProtoMember(8)]
        public Guid? ApprovedId { get; set; }

        [ProtoMember(9)]
        public DateTime? ApprovedTime { get; set; }

        [ProtoMember(10)]
        public DateTime? DeliveryTime { get; set; }

        [ProtoMember(11)]
        public DateTime? AvalibleDeliveryTime { get; set; }
    }
}
