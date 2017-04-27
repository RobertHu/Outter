using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal;
using System.Data;
using Protocal.TradingInstrument;
using SystemController.Persistence;
using Protocal.TypeExtensions;
using SystemController.InstrumentBLL;
using System.Threading;

namespace SystemController
{
    internal sealed class TradeDayManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradeDayManager));
        internal static readonly TradeDayManager Default = new TradeDayManager();

        private TradeDayInfo _tradeDayInfo;

        static TradeDayManager() { }
        private TradeDayManager() { }

        internal TradeDayInfo TradeDayInfo
        {
            get
            {
                return _tradeDayInfo;
            }
        }

        internal DateTime TradeDay
        {
            get { return _tradeDayInfo.TradeDay; }
        }


        internal void Generate(DateTime tradeDay)
        {
            try
            {
                DataSet ds = DBRepository.Default.GenerateTradeDay(tradeDay);
                DataRow dr = ds.Tables[0].Rows[0];
                TradeDayInfo newDayInfo = new TradeDayInfo(dr);
                Interlocked.Exchange(ref _tradeDayInfo, newDayInfo);
                this.LoadInstrumentDayOpenCloseHistoryRecords(ds.Tables[1]);
                this.ParseAndAddInstrumentCheckTime(ds);
                InstrumentBLL.InstrumentManager.Default.ResetDayCloseQuotationState();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("GenerateTradeDay tradeDay = {0}", tradeDay), ex);
            }
        }

        private void ParseAndAddInstrumentCheckTime(DataSet ds)
        {
            Logger.InfoFormat("ParseAndAddInstrumentCheckTime, instrumentCount = {0}", InstrumentManager.Default.Count);
            if (InstrumentManager.Default.Count > 0)
            {
                Logger.Info("LoadInstumentTradingTimeCommon");
                var instrumentTradingTimes = TradingTimeParser.ParseInstrumentTradingTimes(ds);
                if (instrumentTradingTimes != null)
                {
                    InstrumentBLL.Manager.Default.LoadInstrumentTradingTimes(instrumentTradingTimes);
                }
            }
        }

        private void LoadInstrumentDayOpenCloseHistoryRecords(DataTable table)
        {
            Logger.InfoFormat("LoadInstrumentDayOpenCloseHistoryRecords recordCount = {0}", table.Rows.Count);
            if (table.Rows.Count == 0) return;
            foreach (DataRow eachRow in table.Rows)
            {
                InstrumentDayOpenCloseTimeRecord record = new InstrumentDayOpenCloseTimeRecord();
                record.Id = (Guid)eachRow["InstrumentID"];
                record.TradeDay = (DateTime)eachRow["TradeDay"];
                record.DayOpenTime = eachRow.GetColumn<DateTime?>("DayOpenTime");
                record.DayCloseTime = eachRow.GetColumn<DateTime?>("DayCloseTime");
                record.ValueDate = eachRow.GetColumn<DateTime?>("ValueDate");
                record.NextDayOpenTime = eachRow.GetColumn<DateTime?>("NextDayOpenTime");
                record.RealValueDate = eachRow.GetColumn<DateTime?>("RealValueDate");
                InstrumentBLL.InstrumentDayOpenCloseTimeKeeper.Default.AddInstrumentDayOpenCloseTimeRecordByDB(record);
                InstrumentManager.Default.UpdateInstrument(record.Id, record.TradeDay, record.DayOpenTime, record.DayCloseTime);
            }
        }


    }
}
