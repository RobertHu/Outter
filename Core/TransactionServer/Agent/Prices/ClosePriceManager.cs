using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Prices
{
    internal sealed class ClosePriceManager : Protocal.ThreadQueueBase<Notify>
    {
        internal static readonly ClosePriceManager Default = new ClosePriceManager();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ClosePriceManager));

        static ClosePriceManager() { }
        private ClosePriceManager()
            : base(50) { }

        public override void DoWork(Notify item)
        {
            Logger.InfoFormat("begin process daily close price  {0}", item);
            ServerFacade.Default.Server.SetDailyClosePrice(item.InstrumentId, item.TradeDay, item.CloseQuotations);
        }

        public override void RecordLog(Exception ex)
        {
            Logger.Error(ex);
        }
    }

    internal sealed class Notify
    {
        internal Notify(Guid instrumentId, DateTime tradeDay, List<iExchange.Common.TradingDailyQuotation> closeQuotations)
        {
            this.InstrumentId = instrumentId;
            this.TradeDay = tradeDay;
            this.CloseQuotations = closeQuotations;
        }

        internal Guid InstrumentId { get; private set; }
        internal DateTime TradeDay { get; private set; }
        internal List<iExchange.Common.TradingDailyQuotation> CloseQuotations { get; private set; }

        public override string ToString()
        {
            return string.Format("InstrumentId = {0}, TradeDay = {1}", this.InstrumentId, this.TradeDay);
        }
    }
}
