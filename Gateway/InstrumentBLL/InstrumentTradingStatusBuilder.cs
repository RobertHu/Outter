using log4net;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemController.InstrumentBLL
{
    public interface IInstrumentTradingStatusBuilder
    {
        Dictionary<InstrumentStatus, List<Guid>> StatusDict { get; }
        bool ExistsStatus();
        void Add(Guid instrumentId, InstrumentStatus status, DateTime checkTime, DateTime tradeDay);
    }



    public sealed class InstrumentTradingStatusBuilder : IInstrumentTradingStatusBuilder
    {
        private struct LastTime : IEquatable<LastTime>, IComparable<LastTime>
        {
            private DateTime _checkTime;
            private DateTime _tradeDay;

            internal LastTime(DateTime checkTime, DateTime tradeDay)
            {
                _checkTime = checkTime;
                _tradeDay = tradeDay;
            }

            internal DateTime CheckTime
            {
                get { return _checkTime; }
            }

            internal DateTime TradeDay
            {
                get { return _tradeDay; }
            }


            public int CompareTo(LastTime other)
            {
                int value = this.TradeDay.CompareTo(other.TradeDay);
                if (value != 0) return value;
                else return this.CheckTime.CompareTo(other.CheckTime);
            }

            public static bool operator >(LastTime left, LastTime right)
            {
                return left.CompareTo(right) == 1;
            }

            public static bool operator >=(LastTime left, LastTime right)
            {
                return left.CompareTo(right) >= 0;
            }

            public static bool operator <(LastTime left, LastTime right)
            {
                return left.CompareTo(right) == -1;
            }

            public static bool operator <=(LastTime left, LastTime right)
            {
                return left.CompareTo(right) <= 0;
            }

            public static bool operator ==(LastTime left, LastTime right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(LastTime left, LastTime right)
            {
                return !left.Equals(right);
            }

            public override int GetHashCode()
            {
                return this.CheckTime.GetHashCode() ^ this.TradeDay.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this.Equals((LastTime)obj);
            }

            public bool Equals(LastTime other)
            {
                return this.CheckTime == other.CheckTime && this.TradeDay == other.TradeDay;
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentTradingStatusBuilder));

        private Dictionary<Guid, LastTime> _instrumentLastTimeDict = new Dictionary<Guid, LastTime>();

        public InstrumentTradingStatusBuilder()
        {
            this.StatusDict = new Dictionary<InstrumentStatus, List<Guid>>();
        }

        public Dictionary<InstrumentStatus, List<Guid>> StatusDict { get; private set; }

        public bool ExistsStatus()
        {
            return this.StatusDict.Count > 0;
        }


        public void Add(Guid instrumentId, InstrumentStatus status, DateTime checkTime, DateTime tradeDay)
        {
            Logger.InfoFormat("addInstrumentStatus id = {0}, status = {1}, checkTime = {2}, tradeDay = {3}", instrumentId, status, checkTime, tradeDay);
            if (!_instrumentLastTimeDict.ContainsKey(instrumentId))
            {
                _instrumentLastTimeDict.Add(instrumentId, new LastTime(checkTime, tradeDay));
                this.AddToStatusDict(instrumentId, status);
            }
            else
            {
                this.AddForInstrumentExists(instrumentId, status, checkTime, tradeDay);
            }
        }

        private void AddForInstrumentExists(Guid instrumentId, InstrumentStatus status, DateTime checkTime, DateTime tradeDay)
        {
            var lastCheckTime = _instrumentLastTimeDict[instrumentId];
            var lastStatus = this.GetLastStatus(instrumentId);
            if (lastStatus == null)
            {
                Logger.ErrorFormat("last status can't be found, instrumentId = {0}, status = {1}, checkTime = {2}", instrumentId, status, checkTime);
                return;
            }
            if (status == InstrumentStatus.DayCloseQuotationReceived && lastStatus != InstrumentStatus.DayClose)
            {
                Logger.WarnFormat("AddForInstrumentExists when receveid daycloseQuotation, last status should be dayClose instrumentId = {0}, status = {1}, lastStatus = {2}, checkTime = {3}, tradeDay = {4}", instrumentId, status, lastStatus
                    , checkTime, tradeDay);
                return;
            }
            LastTime current = new LastTime(checkTime, tradeDay);
            if (_instrumentLastTimeDict[instrumentId] < current || (lastCheckTime == current && status == InstrumentStatus.DayClose))
            {
                this.DeleteFromStatusDict(instrumentId);
                this.AddToStatusDict(instrumentId, status);
                _instrumentLastTimeDict[instrumentId] = current;
            }
        }

        private InstrumentStatus? GetLastStatus(Guid instrumentId)
        {
            foreach (var eachPair in this.StatusDict)
            {
                var status = eachPair.Key;
                var instruments = eachPair.Value;
                foreach (var eachInstrument in instruments)
                {
                    if (eachInstrument == instrumentId) return status;
                }
            }
            return null;
        }

        private void AddToStatusDict(Guid instrumentId, InstrumentStatus status)
        {
            List<Guid> instrumentIds;
            if (!this.StatusDict.TryGetValue(status, out instrumentIds))
            {
                instrumentIds = new List<Guid>();
                this.StatusDict.Add(status, instrumentIds);
            }
            instrumentIds.Add(instrumentId);
        }

        private void DeleteFromStatusDict(Guid instrumentId)
        {
            List<InstrumentStatus> toBeRemoveStatusList = new List<InstrumentStatus>();
            foreach (var eachPair in this.StatusDict)
            {
                InstrumentStatus status = eachPair.Key;
                List<Guid> instruments = eachPair.Value;

                if (instruments.Contains(instrumentId))
                {
                    instruments.Remove(instrumentId);
                }

                if (instruments.Count == 0)
                {
                    toBeRemoveStatusList.Add(status);
                }
            }

            foreach (var eachStatus in toBeRemoveStatusList)
            {
                this.StatusDict.Remove(eachStatus);
            }

        }
    }

    public sealed class InstrumentTradingStatusBuilderProxy : IInstrumentTradingStatusBuilder
    {
        private InstrumentTradingStatusBuilder _builder;

        public InstrumentTradingStatusBuilderProxy()
        {
            _builder = new InstrumentTradingStatusBuilder();
        }

        public Dictionary<InstrumentStatus, List<Guid>> StatusDict
        {
            get { return _builder.StatusDict; }
        }

        public bool ExistsStatus()
        {
            return _builder.ExistsStatus();
        }

        public void Add(Guid instrumentId, InstrumentStatus status, DateTime checkTime, DateTime tradeDay)
        {
            _builder.Add(instrumentId, status, checkTime, tradeDay);
            InstrumentTradingStatusKeeper.Default.AddInstrumentStatus(instrumentId, status, checkTime, tradeDay);
        }
    }

}
