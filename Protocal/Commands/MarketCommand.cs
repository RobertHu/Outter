using ProtoBuf;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal
{
    [ProtoContract]
    [ProtoInclude(44, typeof(UpdateTradeDayInfoMarketCommand))]
    [ProtoInclude(43, typeof(AccountResetMarketCommand))]
    [ProtoInclude(42, typeof(UpdateInstrumentDayOpenCloseTimeMarketCommand))]
    [ProtoInclude(41, typeof(UpdateInstrumentTradingStatusMarketCommand))]
    public class MarketCommand : Command { }


    [ProtoContract]
    public sealed class UpdateInstrumentTradingStatusMarketCommand : MarketCommand
    {
        [ProtoMember(3)]
        public Dictionary<InstrumentStatus, List<InstrumentStatusInfo>> InstrumentStatus { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(200);
            foreach (var eachPair in this.InstrumentStatus)
            {
                StringBuilder instrumentSB = new StringBuilder();
                InstrumentStatus status = eachPair.Key;
                List<InstrumentStatusInfo> instruments = eachPair.Value;
                if (eachPair.Value.Count == 0) continue;
                foreach (var eachInstrument in instruments)
                {
                    instrumentSB.Append(eachInstrument);
                    instrumentSB.Append(',');
                }
                sb.AppendLine(string.Format("instrumentStatus = {0}, count= {2} instruments = {1}", status, instrumentSB.ToString(0, instrumentSB.Length - 1), instruments.Count));
            }
            return sb.ToString();
        }
    }


    [ProtoContract]
    public sealed class InstrumentStatusInfo
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public DateTime? TradeDay { get; set; }

        public override string ToString()
        {
            return string.Format("Id ={0}, tradeDay ={1} ", this.Id, this.TradeDay);
        }
    }




    [ProtoContract]
    public sealed class UpdateInstrumentDayOpenCloseTimeMarketCommand : MarketCommand
    {
        [ProtoMember(3)]
        public List<InstrumentDayOpenCloseTimeRecord> Records { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(200);
            foreach (var eachRecord in this.Records)
            {
                sb.AppendLine(eachRecord.ToString());
            }
            return sb.ToString();
        }
    }

    [ProtoContract]
    public sealed class InstrumentDayOpenCloseTimeRecord
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        [ProtoMember(2)]
        public DateTime TradeDay { get; set; }

        [ProtoMember(3)]
        public DateTime? DayOpenTime { get; set; }

        [ProtoMember(4)]
        public DateTime? DayCloseTime { get; set; }

        [ProtoMember(5)]
        public DateTime? ValueDate { get; set; }

        [ProtoMember(6)]
        public DateTime? NextDayOpenTime { get; set; }

        [ProtoMember(7)]
        public DateTime? RealValueDate { get; set; }

        public override string ToString()
        {
            return string.Format("id = {0}, DayOpenTime = {1}, DayCloseTime  = {2}, ValueDate  = {3}, NextDayOpenTime  = {4}", this.Id, this.DayOpenTime, this.DayCloseTime, this.ValueDate, this.NextDayOpenTime);
        }
    }


    [ProtoContract]
    public sealed class AccountResetMarketCommand : MarketCommand
    {
        [ProtoMember(3)]
        public DateTime TradeDay { get; set; }

        [ProtoMember(4)]
        public bool IsReseted { get; set; }
    }



    [ProtoContract]
    public sealed class UpdateTradeDayInfoMarketCommand : MarketCommand
    {
        [ProtoMember(3)]
        public DateTime TradeDay { get; set; }

        [ProtoMember(4)]
        public DateTime BeginTime { get; set; }

        [ProtoMember(5)]
        public DateTime EndTime { get; set; }

        [ProtoMember(6)]
        public bool IsTrading { get; set; }
    }

}
