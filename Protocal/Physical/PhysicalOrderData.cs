using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Physical
{
    [DataContract]
    public class PhysicalOrderData : OrderData
    {
        [DataMember]
        public PhysicalTradeSide PhysicalTradeSide { get; set; }

        [DataMember]
        public PhysicalType PhysicalType { get; set; }

        [DataMember]
        public InstalmentPart InstalmentPart { get; set; }

        [DataMember]
        public decimal AdvanceAmount { get; set; }
    }


    [DataContract]
    public sealed class PhysicalOrderBookData : OrderBookData
    {
        [DataMember]
        public Guid? PhysicalRequestId { get; set; }

        [DataMember]
        public int? PhysicalValueMatureDay { get; set; }

        [DataMember]
        public PhysicalTradeSide PhysicalTradeSide { get; set; }

        [DataMember]
        public InstalmentPart InstalmentPart { get; set; }

        [DataMember]
        public PhysicalType PhysicalType { get; set; }
    }


    [DataContract]
    public sealed class InstalmentPart
    {
        [DataMember]
        public Guid InstalmentPolicyId { get; set; }

        [DataMember]
        public decimal DownPayment { get; set; }

        [DataMember]
        public InstalmentType InstalmentType { get; set; }

        [DataMember]
        public InstalmentFrequence InstalmentFrequence { get; set; }

        [DataMember]
        public RecalculateRateType RecalculateRateType { get; set; }

        [DataMember]
        public int Period { get; set; }
    }

}
