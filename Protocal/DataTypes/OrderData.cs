using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    [KnownType(typeof(BOOrderData))]
    [KnownType(typeof(Physical.PhysicalOrderData))]
    public class OrderData : OrderCommonData
    {
        [DataMember]
        public DateTime? PriceTimestamp { get; set; }

        [DataMember]
        public bool? PriceIsQuote { get; set; }
    }


    [DataContract]
    public sealed class BOOrderData : OrderData
    {
        [DataMember]
        public Guid BOBetTypeID { get; set; }

        [DataMember]
        public int BOFrequency { get; set; }

        [DataMember]
        public decimal BOOdds { get; set; }

        [DataMember]
        public long BOBetOption { get; set; }
    }



    [DataContract]
    [KnownType(typeof(OrderData))]
    [KnownType(typeof(OrderBookData))]
    public class OrderCommonData
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public bool IsOpen { get; set; }

        [DataMember]
        public bool IsBuy { get; set; }

        [DataMember]
        public string SetPrice { get; set; }

        [DataMember]
        public string SetPrice2 { get; set; }

        [DataMember]
        public decimal Lot { get; set; }

        [DataMember]
        public decimal? OriginalLot { get; set; }

        [DataMember]
        public TradeOption TradeOption { get; set; }

        [DataMember]
        public int SetPriceMaxMovePips { get; set; }

        [DataMember]
        public int DQMaxMove { get; set; }

        [DataMember]
        public string BlotterCode { get; set; }

        [DataMember]
        public List<OrderRelationData> OrderRelations { get; set; }

        [DataMember]
        public IfDoneOrderSetting IfDoneOrderSetting { get; set; }
    }


    [DataContract]
    public sealed class IfDoneOrderSetting
    {
        [DataMember]
        public string LimitPrice { get; set; }

        [DataMember]
        public string StopPrice { get; set; }
    }

    [DataContract]
    [KnownType(typeof(Physical.PhysicalOrderBookData))]
    public class OrderBookData : OrderCommonData
    {
        [DataMember]
        public string OrginCode { get; set; }

        [DataMember]
        public string ExecutePrice { get; set; }

        [DataMember]
        public decimal? CommissionSum { get; set; }

        [DataMember]
        public decimal? LevySum { get; set; }

        [DataMember]
        public decimal? OtherFeeSum { get; set; }

        [DataMember]
        public Guid? OrderBatchInstructionID { get; set; }
    }

}
