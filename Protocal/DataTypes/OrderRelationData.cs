using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    [KnownType(typeof(OrderRelationBookData))]
    public class OrderRelationData
    {
        [DataMember]
        public Guid CloseOrderId { get; set; }

        [DataMember]
        public Guid OpenOrderId { get; set; }

        [DataMember]
        public decimal ClosedLot { get; set; }

        [DataMember]
        public DateTime OpenOrderExecuteTime { get; set; }

        [DataMember]
        public string OpenOrderExecutePrice { get; set; }

    }

    [DataContract]
    public class OrderRelationBookData : OrderRelationData
    {
        [DataMember]
        public DateTime CloseTime { get; set; }

        [DataMember]
        public decimal Commission { get; set; }

        [DataMember]
        public decimal Levy { get; set; }

        [DataMember]
        public decimal OtherFee { get; set; }

        [DataMember]
        public decimal InterestPL { get; set; }

        [DataMember]
        public decimal StoragePL { get; set; }

        [DataMember]
        public decimal TradePL { get; set; }

        [DataMember]
        public ValuedInfo ValuedInfo { get; set; }
    }

    [DataContract]
    public sealed class ValuedInfo
    {
        [DataMember]
        public DateTime ValueTime { get; set; }

        [DataMember]
        public int Decimals { get; set; }

        [DataMember]
        public decimal RateIn { get; set; }

        [DataMember]
        public decimal RateOut { get; set; }
    }



}
