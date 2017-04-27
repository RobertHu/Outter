using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Service;
using log4net;
using Newtonsoft.Json;
using Protocal;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Core.TransactionServer.Agent.Prices;

namespace Core.TransactionServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal sealed class CommandCollectService : Protocal.CommandCollectServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandCollectService));

        private SettingCommandUpdater _settingCommandUpdater = new SettingCommandUpdater();

        protected override void HandleMarketCommand(MarketCommand marketCommand)
        {
            try
            {
                if (marketCommand is UpdateInstrumentTradingStatusMarketCommand)
                {
                    var command = (UpdateInstrumentTradingStatusMarketCommand)marketCommand;
                    Logger.InfoFormat("Update instrument status {0}", command);
                    Engine.EngineService.Default.UpdateInstrumentStatus(command.InstrumentStatus);
                }
                else if (marketCommand is UpdateInstrumentDayOpenCloseTimeMarketCommand)
                {
                    var command = (UpdateInstrumentDayOpenCloseTimeMarketCommand)marketCommand;
                    if (command.Records == null || command.Records.Count == 0)
                    {
                        Logger.Warn("update instrument day open close time  no records");
                        return;
                    }
                    foreach (var eachInstrumentInfo in command.Records)
                    {
                        Logger.InfoFormat("update instrument day open close time , id={0}, dayopentime={1}, dayclosetime={2}", eachInstrumentInfo.Id, eachInstrumentInfo.DayOpenTime, eachInstrumentInfo.DayCloseTime);
                    }
                    ServerFacade.Default.Server.UpdateInstrumentDayOpenCloseTime(command);
                }
                else if (marketCommand is AccountResetMarketCommand)
                {
                    var command = (AccountResetMarketCommand)marketCommand;
                    if (!command.IsReseted)
                    {
                        Logger.InfoFormat("received account reset command tradeDay = {0}", command.TradeDay);
                        InstrumentTradingStateManager.Default.UpdateLastResetDay(command.TradeDay);
                        AccountResetter.Default.Add(command.TradeDay);
                    }
                }
                else if (marketCommand is UpdateTradeDayInfoMarketCommand)
                {
                    var command = (UpdateTradeDayInfoMarketCommand)marketCommand;
                    Logger.InfoFormat("update tradeDay info, tradeDay={0}, begintime={1}, endtime={2}, istrading={3}", command.TradeDay, command.BeginTime, command.EndTime, command.IsTrading);
                    ServerFacade.Default.Server.UpdateTradeDayInfo(command);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void HandleTradingCommand(TradingCommand tradingCommand)
        {

        }

        protected override void HandleSettingCommand(SettingCommand command)
        {
            _settingCommandUpdater.Add(command);
        }

        protected override void HandleNotifyCommand(NotifyCommand command)
        {
        }

        protected override void HandleQuotationCommand(OriginQ[] originQs, OverridedQ[] overridedQs)
        {
            if (overridedQs == null) return;
            Agent.AccountClass.QuotationManager.Default.Add(overridedQs);
        }
    }

    internal sealed class SettingCommandUpdater : Protocal.ThreadQueueBase<SettingCommand>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingCommandUpdater));

        internal SettingCommandUpdater()
            : base(50) { }

        public override void DoWork(SettingCommand command)
        {
            if (!string.IsNullOrEmpty(command.Content))
            {
                Logger.Info(command.Content);
                ServerFacade.Default.Server.Update(command.AppType, XElement.Parse(command.Content));
            }
        }

        public override void RecordLog(Exception ex)
        {
            Logger.Error(ex);
        }
    }

    internal sealed class AccountResetter
    {
        internal static readonly AccountResetter Default = new AccountResetter();

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountResetter));

        private class TradeDayItem
        {
            public DateTime? TradeDay;
            public volatile bool IsProcessed;

            public bool IsValid()
            {
                return !this.IsProcessed && this.TradeDay != null;
            }

            public void Clear()
            {
                this.IsProcessed = true;
                this.TradeDay = null;
            }
        }

        private TradeDayItem _tradeDayItem;

        static AccountResetter() { }
        private AccountResetter()
        {
            _tradeDayItem = new TradeDayItem
            {
                IsProcessed = true
            };
            new Thread(this.ResetHandle)
            {
                IsBackground = true
            }.Start();
        }

        internal void Add(DateTime tradeDay)
        {
            Logger.InfoFormat("Add tradeDay = {0}", tradeDay);
            _tradeDayItem.TradeDay = tradeDay;
            _tradeDayItem.IsProcessed = false;
        }

        private void ResetHandle()
        {
            while (true)
            {
                Thread.Sleep(10000);
                try
                {
                    if (!ClosePriceManager.Default.IsDone) continue;
                    if (_tradeDayItem.IsValid())
                    {
                        Logger.InfoFormat("Begin do System reset tradeDay = {0}", _tradeDayItem.TradeDay.Value);
                        ServerFacade.Default.Server.DoSystemReset(_tradeDayItem.TradeDay.Value);
                        Logger.InfoFormat("After do System reset tradeDay = {0}", _tradeDayItem.TradeDay.Value);
                        Broadcaster.Default.Add(new AccountResetMarketCommand
                        {
                            TradeDay = _tradeDayItem.TradeDay.Value,
                            IsReseted = true
                        });
                        _tradeDayItem.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

    }



}
