using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Test
{
    [DataContract]
    public sealed class AccountQuotationInfo
    {
        [DataMember]
        public int QuotationCount { get; set; }

        [DataMember]
        public int SentTimeDiff { get; set; }
    }

    [DataContract]
    public sealed class ExecuteInfo
    {
        [DataMember]
        public Guid AccountId { get; set; }

        [DataMember]
        public Guid TranId { get; set; }

        [DataMember]
        public Guid ExecuteOrderId { get; set; }

        [DataMember]
        public string lot { get; set; }

        [DataMember]
        public string BuyPrice { get; set; }

        [DataMember]
        public string SellPrice { get; set; }

    }

}
