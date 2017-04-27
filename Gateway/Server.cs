using SystemController.Config;
using log4net;
using Newtonsoft.Json;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using SystemController.InstrumentBLL;
using SystemController.Factory;
using SystemController.Persistence;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace SystemController
{
    internal sealed class Server
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Server));
        internal static readonly Server Default = new Server();
        private ResetManager _resetManager = new ResetManager();
        private static readonly TimeSpan Max_Wait_FOR_DAY_CLOSE_QUOTATION_Time = TimeSpan.FromMinutes(10);

        private Server() { }

        internal ResetNotify ResetNotify
        {
            get { return _resetManager.Notify; }
        }

        internal void Start()
        {
            try
            {
                SettingManager.Default.LoadSettings();
                DBRepository.Default.GenerateTradingTimeAndInstrumentDayOpenCloseHistory();
                DateTime tradeDay = DateTime.Now.Date;
                TradeDayManager.Default.Generate(tradeDay);
                Broadcaster.Default.ClientManager.ClientConnected += InstrumentBLL.InstrumentTradingStatusKeeper.Default.ClientConnectedHanlde;
                InstrumentTradingStatusKeeper.Default.DayCloseQuotationReceived += InstrumentBLL.InstrumentManager.Default.DayCloseQuotationReceivedEventHandle;
                InstrumentBLL.Manager.Default.LoadInstrumentTradingTimeFromDB();
                Hoster.Default.Start();
                new Thread(this.DoReset) { IsBackground = true }.Start();
                Logger.Info("Start up");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        private void DoReset(object state)
        {
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    var tradeDayInfo = TradeDayManager.Default.TradeDayInfo;
                    if (tradeDayInfo.EndTime <= DateTime.Now)
                    {
                        Logger.InfoFormat("begin do system reset tradeDay={0}", tradeDayInfo.TradeDay);
                        this.WaitForAllInstrumentsDayCloseQuotationReceived(tradeDayInfo.TradeDay);
                        DateTime nextTradeDay = DateTime.Now.Date;
                        Logger.InfoFormat("lastTradeDay = {0}, currentTradeDay = {1}", tradeDayInfo.TradeDay, nextTradeDay);
                        TradeDayManager.Default.Generate(nextTradeDay);
                        this.BroadcastResetCommand(tradeDayInfo.TradeDay);
                        _resetManager.Add(tradeDayInfo.TradeDay);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private void WaitForAllInstrumentsDayCloseQuotationReceived(DateTime tradeDay)
        {
            DateTime baseTime = DateTime.Now;
            while (!InstrumentBLL.InstrumentManager.Default.IsAllInstrumentsReceivedDayCloseQuotation(tradeDay))
            {
                Thread.Sleep(1000);
                if (DateTime.Now - baseTime >= Max_Wait_FOR_DAY_CLOSE_QUOTATION_Time)
                {
                    Logger.WarnFormat("WaitForAllInstrumentsDayCloseQuotationReceived tradeDay = {0}, exceed max wait time ", tradeDay);
                    break;
                }
            }
        }


        private void BroadcastResetCommand(DateTime tradeDay)
        {
            var command = MarketCommandFactory.CreateAccountResetCommand(tradeDay);
            Broadcaster.Default.AddCommand(command);
            var updateTradeDayInfoCommand = MarketCommandFactory.CreateUpdateTradeDayInfoCommand();
            Broadcaster.Default.AddCommand(updateTradeDayInfoCommand);
        }

        internal void Stop()
        {
            try
            {
                Hoster.Default.Stop();
                Logger.Info("Server Stop");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    internal sealed class ResetManager
    {
        private ResetNotify _notify;

        public ResetNotify Notify
        {
            get { return _notify; }
        }

        public void Add(DateTime tradeDay)
        {
            if (_notify == null || _notify.TradeDay < tradeDay)
            {
                var newNotify = new ResetNotify { TradeDay = tradeDay };
                Interlocked.Exchange(ref _notify, newNotify);
            }
        }

    }

}
