using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Protocal;
using System.Data;
using log4net;
using Protocal.TypeExtensions;

namespace SystemController.Persistence
{
    internal sealed class DBRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DBRepository));
        private object _mutex = new object();
        internal static readonly DBRepository Default = new DBRepository();

        static DBRepository() { }
        private DBRepository() { }


        internal string ConnectionString
        {
            get
            {
                return SettingManager.Default.DBConnectionString;
            }

        }

        internal DataSet LoadAndGenerateInstrumentTradingTime(XElement instrumentXml)
        {
            lock (_mutex)
            {
                DataSet result = null;
                Dictionary<string, object> sqlParams = new Dictionary<string, object>
                {
                    {"@instrumentXml", instrumentXml.ToString()}
                };
                Protocal.DB.DBRetryHelper.RetryForever(() =>
                {
                    result = DataBaseHelper.GetData("Trading.GenerateInstrumentTradingTime", this.ConnectionString, new string[] { "InstrumentDayOpenCloseTime", "TradingTime" }, sqlParams);
                });
                return result;
            }
        }


        internal DataSet GenerateTradeDay(DateTime tradeDay)
        {
            lock (_mutex)
            {
                DataSet ds = null;
                Protocal.DB.DBRetryHelper.RetryForever(() =>
                    {
                        Dictionary<string, object> sqlParams = new Dictionary<string, object>
                            {
                                {"@tradeDay", tradeDay}
                            };
                        Logger.InfoFormat("GenerateTradeDay, tradeDay = {0}", tradeDay);
                        ds = DataBaseHelper.GetData("Trading.[GenerateTradeDayInfo]", this.ConnectionString, new string[] { "TradeDayHistory", "InstrumentDayOpenCloseTime", "TradingTime" }, sqlParams);
                    });
                return ds;
            }
        }

        internal void GenerateTradingTimeAndInstrumentDayOpenCloseHistory()
        {
            lock (_mutex)
            {
                Protocal.DB.DBRetryHelper.RetryForever(() =>
                    {
                        DataBaseHelper.GetData("Trading.[RefreshTradingTime]", this.ConnectionString, null, (Dictionary<string, object>)null);
                    });
            }
        }

    }
}
