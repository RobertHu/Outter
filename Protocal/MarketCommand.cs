using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [DataContract]
    [KnownType(typeof(UpdateTradeDayInfoMarketCommand))]
    [KnownType(typeof(AccountResetMarketCommand))]
    [KnownType(typeof(InstrumentResetMarketCommand))]
    [KnownType(typeof(UpdateInstrumentDayOpenCloseTimeMarketCommand))]
    [KnownType(typeof(UpdateInstrumentTradingStatusMarketCommand))]
    public class MarketCommand : Command { }


    [DataContract]
    public sealed class UpdateInstrumentTradingStatusMarketCommand : MarketCommand
    {
        [DataMember]
        public Dictionary<InstrumentStatus, List<Guid>> InstrumentStatus { get; set; }

        [DataMember]
        public DateTime TradeDay { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var eachPair in this.InstrumentStatus)
            {
                StringBuilder instrumentSB = new StringBuilder();
                foreach (var eachInstrument in eachPair.Value)
                {
                    instrumentSB.Append(eachInstrument);
                    instrumentSB.Append(',');
                }
                sb.AppendLine(string.Format("instrumentStatus = {0}, instruments = {1}", eachPair.Key, instrumentSB.ToString(0, instrumentSB.Length - 1)));
            }
            return sb.ToString();
        }
    }


    [DataContract]
    public sealed class UpdateInstrumentDayOpenCloseTimeMarketCommand : MarketCommand
    {
        public List<InstrumentDayOpenCloseTimeRecord> Records { get; set; }
    }

    [DataContract]
    public sealed class InstrumentDayOpenCloseTimeRecord
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public DateTime? DayOpenTime { get; set; }

        [DataMember]
        public DateTime? DayCloseTime { get; set; }

        [DataMember]
        public DateTime? ValueDate { get; set; }

        [DataMember]
        public DateTime? NextDayOpenTime { get; set; }
    }


    [DataContract]
    public sealed class InstrumentResetMarketCommand : MarketCommand
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public DateTime TradeDay { get; set; }
    }

    [DataContract]
    public sealed class AccountResetMarketCommand : MarketCommand
    {
        [DataMember]
        public DateTime TradeDay { get; set; }
    }

    [DataContract]
    public sealed class UpdateTradeDayInfoMarketCommand : MarketCommand
    {
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
