using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.DB;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OpenOrderCloser
    {
        internal static OpenOrderCloser Default = new OpenOrderCloser();

        static OpenOrderCloser() { }
        private OpenOrderCloser() { }


        internal void Close(DateTime tradeDay)
        {
#if TEST
#else
            List<Guid> openOrders = this.GetOpenOrderExceedOpenDays(Settings.Setting.Default.Instruments.Values, tradeDay);
           TradingSetting.Default.DoParallelForAccounts(account => account.CloseForOpenOrderExceedOpenDays(openOrders, tradeDay));
#endif
        }

        private List<Guid> GetOpenOrderExceedOpenDays(IEnumerable<Settings.Instrument> instruments, DateTime tradeDay)
        {
            List<Guid> result = new List<Guid>();
            XElement instrumentXml = new XElement("Instruments");
            foreach (var eachInstrument in instruments)
            {
                instrumentXml.Add(new XElement("Instrument", eachInstrument.Id));
            }

            DataSet dataSet = DBRepository.Default.GetOpenOrderIdsExceedOpenDays(instrumentXml, tradeDay);

            foreach (DataRow dataRow in dataSet.Tables[0].Rows)
            {
                result.Add((Guid)dataRow["Id"]);
            }
            return result;
        }
    }
}
