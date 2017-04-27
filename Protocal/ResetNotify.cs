using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    public sealed class ResetNotify
    {
        [DataMember]
        public DateTime TradeDay { get; set; }
    }
}
