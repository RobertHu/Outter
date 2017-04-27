using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Physical
{
    [DataContract]
    public class OrderInstalmentData
    {
        [DataMember]
        public Guid OrderId { get; set; }

        [DataMember]
        public int Sequence { get; set; }

        public Guid AccountId { get; set; }

        public Guid InstrumentId { get; set; }

        [DataMember]
        public decimal InterestRate { get; set; }

        [DataMember]
        public decimal Principal { get; set; }

        [DataMember]
        public decimal Interest { get; set; }

        [DataMember]
        public decimal DebitInterest { get; set; }

        [DataMember]
        public DateTime? PaymentDateTimeOnPlan { get; set; }

        [DataMember]
        public DateTime? PaidDateTime { get; set; }

        [DataMember]
        public Guid? UpdatePersonId { get; set; }

        [DataMember]
        public DateTime? UpdateTime { get; set; }

        [DataMember]
        public decimal? LotBalance { get; set; }
    }
}
