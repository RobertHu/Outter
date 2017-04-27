using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    [KnownType(typeof(TransactionBookData))]
    public class TransactionData : TransactionCommonData
    {
        [DataMember]
        public DateTime? SetPriceTimestamp { get; set; }

        [DataMember]
        public List<OrderData> Orders { get; set; }
    }


    [DataContract]
    [KnownType(typeof(TransactionData))]
    [KnownType(typeof(TransactionBookData))]
    public class TransactionCommonData
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Guid InstrumentId { get; set; }

        [DataMember]
        public Guid AccountId { get; set; }

        [DataMember]
        public TransactionType Type { get; set; }

        [DataMember]
        public TransactionSubType SubType { get; set; }

        [DataMember]
        public OrderType OrderType { get; set; }

        [DataMember]
        public ExpireType ExpireType { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public DateTime SubmitTime { get; set; }

        [DataMember]
        public Guid SubmitorId { get; set; }

        [DataMember]
        public Guid? SourceOrderId { get; set; }

        [DataMember]
        public bool PlaceByRiskMonitor { get; set; }

        [DataMember]
        public AppType AppType { get; set; }

        [DataMember]
        public bool FreePlacingPreCheck { get; set; }

        [DataMember]
        public bool DisableLmtVariation { get; set; }
    }


    [DataContract]
    public sealed class TransactionBookData : TransactionCommonData
    {
        [DataMember]
        public DateTime ExecuteTime { get; set; }

        [DataMember]
        public TransactionPhase Phase { get; set; }

        [DataMember]
        public Guid? ApproverId { get; set; }

        [DataMember]
        public bool CheckMargin { get; set; }

        [DataMember]
        public decimal ContractSize { get; set; }

        [DataMember]
        public List<OrderBookData> Orders { get; set; }

        public DateTime TradeDay { get; set; }
    }



}
