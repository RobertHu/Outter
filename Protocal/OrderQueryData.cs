using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    public sealed class OrderQueryData
    {
        [DataMember]
        public Guid ID { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string AccountCode { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public DateTime? ExecuteTime { get; set; }

        [DataMember]
        public string InstrumentCode { get; set; }

        [DataMember]
        public int OrderType { get; set; }

        [DataMember]
        public decimal Lot { get; set; }

        [DataMember]
        public string Price { get; set; }

        [DataMember]
        public bool IsOpen { get; set; }

        [DataMember]
        public bool IsBuy { get; set; }

        [DataMember]
        public int Phase { get; set; }

        [DataMember]
        public string Remarks { get; set; }

        [DataMember]
        public int TradeOption { get; set; }

        [DataMember]
        public int TransactionType { get; set; }

        [DataMember]
        public int TransactionSubType { get; set; }

        [DataMember]
        public string ExternalExchangeCode { get; set; }

        [DataMember]
        public int InstrumentCategory { get; set; }

        [DataMember]
        public Guid? PhysicalRequestId { get; set; }

        [DataMember]
        public decimal PhysicalPaidAmount { get; set; }

        [DataMember]
        public int PhysicalTradeSide { get; set; }

        [DataMember]
        public int? PhysicalInstalmentType { get; set; }

        [DataMember]
        public decimal PhysicalOriginValue { get; set; }

        [DataMember]
        public int? RecalculateRateType { get; set; }

        [DataMember]
        public DateTime? InterestValueDate { get; set; }

        [DataMember]
        public decimal? TradePL { get; set; }

        [DataMember]
        public int? Decimals { get; set; }

        [DataMember]
        public Guid CurrencyID { get; set; }

        [DataMember]
        public string CurrencyCode { get; set; }

        [DataMember]
        public string CurrencyName { get; set; }
    }
}
