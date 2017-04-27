using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.TradingInstrument
{
    [DataContract]
    public class InstrumentTradingTime
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public List<TradingSession> Sessions { get; set; }
    }


    [DataContract]
    public class TradingInfo
    {
        [DataMember]
        public TradeDayInfo TradeDayInfo { get; set; }

        [DataMember]
        public List<InstrumentTradingTime> TradingTimeList { get; set; }

    }

    [DataContract]
    public class TradeDayInfo
    {
        public TradeDayInfo() { }

        public TradeDayInfo(DataRow dr)
        {
            this.TradeDay = (DateTime)dr["TradeDay"];
            this.BeginTime = (DateTime)dr["BeginTime"];
            this.EndTime = (DateTime)dr["EndTime"];
            this.IsTrading = (bool)dr["IsTrading"];
        }

        [DataMember]
        public DateTime TradeDay { get; set; }

        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }

        [DataMember]
        public bool IsTrading { get; set; }
    }


}
