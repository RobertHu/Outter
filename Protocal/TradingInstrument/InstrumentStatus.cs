using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.TradingInstrument
{
    public enum InstrumentStatus
    {
        None = 0,
        DayOpen,
        DayClose,
        SessionOpen,
        SessionClose,
        StopPlace,
        NotInTrading,
        DayOpening,
        DayClosing,
        SessionOpening,
        SessionClosing,
        MOOOpen,
        MOOClosed,
        MOCOpen,
        MOCClosed,
        DayCloseQuotationReceived
    }

    [DataContract]
    public class InstrumentDayOpenCloseParams : IEqualityComparer<InstrumentDayOpenCloseParams>
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public DateTime TradeDay { get; set; }

        [DataMember]
        public DateTime? DayOpenTime { get; set; }

        [DataMember]
        public DateTime? DayCloseTime { get; set; }

        [DataMember]
        public int LastAcceptTimeSpan { get; set; }

        [DataMember]
        public DateTime? RealValueDate { get; set; }

        [DataMember]
        public DateTime? ValueDate { get; set; }

        [DataMember]
        public DateTime? NextDayOpenTime { get; set; }

        [DataMember]
        public bool IsTrading { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public List<TradingSession> TradingSessions { get; set; }

        public void AddSession(TradingSession session)
        {
            if (this.TradingSessions == null)
            {
                this.TradingSessions = new List<TradingSession>();
            }
            this.TradingSessions.Add(session);
        }


        public bool Equals(InstrumentDayOpenCloseParams x, InstrumentDayOpenCloseParams y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(InstrumentDayOpenCloseParams obj)
        {
            return obj.Id.GetHashCode();
        }
    }

    [DataContract]
    public class TradingSession
    {
        [DataMember]
        public DateTime BeginTime { get; set; }

        [DataMember]
        public DateTime AcceptEndTime { get; set; }

        [DataMember]
        public DateTime EndTime { get; set; }
    }
}
