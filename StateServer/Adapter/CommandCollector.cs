using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Protocal;
using System.ServiceModel;
using log4net;

namespace iExchange.StateServer.Adapter
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    internal class CommandCollector : CommandCollectServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandCollector));

        protected override bool ShouldProcessTradingCommand()
        {
            return false;
        }

        protected override void HandleMarketCommand(MarketCommand command)
        {
            try
            {
                if (command is AccountResetMarketCommand)
                {
                    var resetCommand = (AccountResetMarketCommand)command;
                    if (resetCommand.IsReseted)
                    {
                        FaxEmailServices.FaxEmailEngine.Default.NotifyTradeDayReset(resetCommand.TradeDay);
                    }
                }
                else if (command is UpdateInstrumentTradingStatusMarketCommand)
                {
                    CommandManager.Default.ProcessMarketCommand(command);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        protected override void HandleSettingCommand(SettingCommand command)
        {
            try
            {
                Logger.InfoFormat("received settingCommand content = {0}", command.Content);
                CommandManager.Default.BroadcastSettingCommand(command.Content);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void HandleTradingCommand(TradingCommand tradingCommand)
        {
            try
            {
                CommandManager.Default.ProcessTradingCommand(tradingCommand);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void HandleNotifyCommand(NotifyCommand command)
        {
        }

        protected override void HandleQuotationCommand(OriginQ[] originQs, OverridedQ[] overridedQs)
        {
        }
    }
}