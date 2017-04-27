using log4net;
using Protocal;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SystemController.InstrumentBLL
{
    internal sealed class TradingTimeFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradingTimeFactory));
        private Dictionary<Guid, List<TradingSession>> _InstrumentTradingSessionDict = new Dictionary<Guid, List<TradingSession>>();
        internal static readonly TradingTimeFactory Default = new TradingTimeFactory();

        static TradingTimeFactory() { }
        private TradingTimeFactory() { }

        internal List<TradingSession> GetTradingSessions(Guid instrumentId)
        {
            List<TradingSession> result = null;
            _InstrumentTradingSessionDict.TryGetValue(instrumentId, out result);
            return result;
        }

        internal List<Protocal.TradingInstrument.InstrumentDayOpenCloseParams> Create()
        {
            var ds = this.LoadData();
            InstrumentManager.Default.InitializeInstrument(ds);
            Logger.InfoFormat("ds table count = {0}", ds.Tables.Count);
            var result = this.ParseTradingTime(ds).Values.ToList();
            this.BuildInstrumentTradingSessions(result);
            return result;
        }

        private void BuildInstrumentTradingSessions(List<Protocal.TradingInstrument.InstrumentDayOpenCloseParams> instrumentParams)
        {
            foreach (var eachInstrumentParams in instrumentParams)
            {
                List<TradingSession> sessions;
                if (!_InstrumentTradingSessionDict.TryGetValue(eachInstrumentParams.Id, out sessions))
                {
                    sessions = new List<TradingSession>();
                    _InstrumentTradingSessionDict.Add(eachInstrumentParams.Id, sessions);
                }
                foreach (var eachSession in eachInstrumentParams.TradingSessions)
                {
                    TradingSession session = new TradingSession
                    {
                        BeginTime = eachSession.BeginTime,
                        EndTime = eachSession.EndTime
                    };
                    sessions.Add(session);
                }
            }
        }

        private DataSet LoadData()
        {
            DataSet result = null;
            Protocal.DB.DBRetryHelper.RetryForever(() =>
            {
                result = DataBaseHelper.GetData("Trading.[GetInstrumentTradingTime]", SettingManager.Default.DBConnectionString, new string[] { "Instrument", "TradingTime", "TradeDay" }, new Dictionary<string, object>());
            });
            return result;
        }

        private Dictionary<Guid, Protocal.TradingInstrument.InstrumentDayOpenCloseParams> ParseTradingTime(DataSet ds)
        {
            var result = new Dictionary<Guid, Protocal.TradingInstrument.InstrumentDayOpenCloseParams>();
            var table = ds.Tables["TradingTime"];
            foreach (DataRow dr in table.Rows)
            {
                this.ParseIndividualTradingTime(dr, result);
            }
            return result;
        }

        private void ParseIndividualTradingTime(DataRow dr, Dictionary<Guid, Protocal.TradingInstrument.InstrumentDayOpenCloseParams> instrumentParamDict)
        {
            Guid instrumentId = (Guid)dr["InstrumentID"];
            Protocal.TradingInstrument.InstrumentDayOpenCloseParams instrumentParams;
            var instrument = InstrumentManager.Default.GetInstrument(instrumentId);
            if (instrument.TradeDay == null) return;
            if (!instrumentParamDict.TryGetValue(instrumentId, out instrumentParams))
            {
                instrumentParams = new Protocal.TradingInstrument.InstrumentDayOpenCloseParams
                {
                    Id = instrumentId,
                    TradeDay = instrument.TradeDay.Value,
                    DayOpenTime = instrument.DayOpenTime,
                    DayCloseTime = instrument.DayCloseTime,
                    LastAcceptTimeSpan = instrument.LastAcceptTimeSpan,
                    IsTrading = true
                };
                instrumentParamDict.Add(instrumentId, instrumentParams);
            }
            DateTime beginTime = (DateTime)dr["BeginTime"];
            DateTime endTime = (DateTime)dr["EndTime"];
            var session = new Protocal.TradingInstrument.TradingSession
            {
                BeginTime = beginTime,
                EndTime = endTime
            };
            instrumentParams.AddSession(session);
        }
    }

    internal sealed class Instrument
    {
        internal Instrument(Guid id)
        {
            this.Id = id;
        }

        internal Instrument(DataRow dr)
        {
            this.Id = (Guid)dr["ID"];
            if (dr["DayOpenTime"] != DBNull.Value)
            {
                this.DayOpenTime = (DateTime)dr["DayOpenTime"];
            }

            if (dr["DayCloseTime"] != DBNull.Value)
            {
                this.DayCloseTime = (DateTime)dr["DayCloseTime"];
            }

            if (dr["LastAcceptTimeSpan"] != DBNull.Value)
            {
                this.LastAcceptTimeSpan = (int)dr["LastAcceptTimeSpan"];
            }

            if (dr["TradeDay"] != DBNull.Value)
            {
                this.TradeDay = (DateTime)dr["TradeDay"];
            }
        }


        internal Guid Id { get; private set; }

        internal DateTime? TradeDay { get; set; }

        internal DateTime? DayOpenTime { get; set; }

        internal DateTime? DayCloseTime { get; set; }

        internal int LastAcceptTimeSpan { get; private set; }

        internal bool DayCloseQuotationReceived { get; set; }


        public override string ToString()
        {
            return string.Format("id = {0}, tradeDay = {1}, dayOpenTime = {2}, dayCloseTime = {3}", this.Id, this.GetStringRep(this.TradeDay), this.GetStringRep(this.DayOpenTime), this.GetStringRep(this.DayCloseTime));
        }

        private string GetStringRep(DateTime? dt)
        {
            return dt == null ? string.Empty : dt.Value.ToString();
        }

    }
}
