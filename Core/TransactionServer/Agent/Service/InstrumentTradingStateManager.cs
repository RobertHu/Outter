using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Core.TransactionServer.Agent.Service
{
    internal sealed class InstrumentTradingStateManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentTradingStateManager));

        internal static readonly InstrumentTradingStateManager Default = new InstrumentTradingStateManager();

        private int _getInterval;
        private Int64 _lastResetDay;

        static InstrumentTradingStateManager() { }
        private InstrumentTradingStateManager() { }


        private GatewayProxy Proxy
        {
            get
            {
                return ServerFacade.Default.GatewayProxy;
            }
        }


        internal void UpdateLastResetDay(DateTime tradeDay)
        {
            Int64 lastTicks = Interlocked.Read(ref _lastResetDay);
            Logger.InfoFormat("UpdateLastResetDay lastResetDay = {0}, tradeDay = {1}", lastTicks == 0 ? string.Empty : (new DateTime(lastTicks)).ToString(), tradeDay);
            Interlocked.Exchange(ref _lastResetDay, tradeDay.Ticks);
        }

        internal void Start()
        {
            _getInterval = (int)(TimeSpan.FromHours(1).TotalMilliseconds);
            new Thread(this.GetStateHandle)
            {
                IsBackground = true
            }.Start();
        }

        private void GetStateHandle()
        {
            while (true)
            {
                Thread.Sleep(_getInterval);
                try
                {
                    this.GetTradingStatus();
                    this.GetInstrumentDayOpenCloseTime();
                    this.GetTradeDayInfo();
                    this.GetResetNotify();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private void GetResetNotify()
        {
            var resetNotify = this.Proxy.GetResetNotify();
            if (resetNotify == null) return;
            if (resetNotify.TradeDay.Ticks > _lastResetDay)
            {
                Logger.InfoFormat("GetResetNotify, lastResetDay = {0}, resetDay = {1}", new DateTime(_lastResetDay), new DateTime(resetNotify.TradeDay.Ticks));
                AccountResetter.Default.Add(resetNotify.TradeDay);
                Interlocked.Exchange(ref _lastResetDay, resetNotify.TradeDay.Ticks);
            }
        }

        private void GetTradingStatus()
        {
            var instrumentTradingStatusCommand = this.Proxy.GetTradingInstrumentStatusCommand();
            if (instrumentTradingStatusCommand != null)
            {
                ServerFacade.Default.Server.UpdateInstrumentsTradingStatus(instrumentTradingStatusCommand.InstrumentStatus);
            }
        }

        private void GetInstrumentDayOpenCloseTime()
        {
            var instrumentDayOpenCloseCommand = this.Proxy.GetInstrumentDayOpenCloseTimeCommand();
            if (instrumentDayOpenCloseCommand != null)
            {
                ServerFacade.Default.Server.UpdateInstrumentDayOpenCloseTime(instrumentDayOpenCloseCommand);
            }
        }

        private void GetTradeDayInfo()
        {
            var command = this.Proxy.GetTradeDayInfoCommand();
            if (command != null)
            {
                ServerFacade.Default.Server.UpdateTradeDayInfo(command);
            }
        }



    }
}
