using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.DataTypes
{
    [DataContract]
    public sealed class PayRecord
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Guid AccountId { get; set; }

        [DataMember]
        public Guid CurrencyId { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public BillType Type { get; set; }

        [DataMember]
        public bool IsClear { get; set; }

        [DataMember]
        public DateTime EffectedTime { get; set; }
    }

}
