using log4net;
using ProtoBuf;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Protocal
{
    [ProtoContract]
    [ServiceContract]
    public interface ICommandCollectService
    {
        [OperationContract(AsyncPattern = false, IsOneWay = true)]
        void AddCommand(Command command);

        [OperationContract]
        string Test();
    }



    public abstract class CommandCollectServiceBase : ICommandCollectService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandCollectServiceBase));

        public void AddCommand(Command command)
        {
            try
            {
                var quotationCommand = command as QuotationCommand;
                if (quotationCommand != null)
                {
                    this.HandleQuotationCommand(quotationCommand.OriginQs, quotationCommand.OverridedQs);
                }
                else if (command is MarketCommand)
                {
                    this.HandleMarketCommand((MarketCommand)command);
                }
                else if (command is TradingCommand)
                {
                    var tradingCommand = (TradingCommand)command;
                    if (this.ShouldProcessTradingCommand())
                    {
                        Commands.TradingCommandManager.Default.Add(tradingCommand);
                    }
                    this.HandleTradingCommand(tradingCommand);
                }
                else if (command is SettingCommand)
                {
                    this.HandleSettingCommand((SettingCommand)command);
                }
                else if (command is NotifyCommand)
                {
                    this.HandleNotifyCommand((NotifyCommand)command);
                }
            }
            catch (Exception ex)
            {
                Logger.Error((command is TradingCommand ? ((TradingCommand)command).Content : string.Empty), ex);
            }
        }

        protected virtual bool ShouldProcessTradingCommand()
        {
            return true;
        }

        protected abstract void HandleQuotationCommand(OriginQ[] originQs, OverridedQ[] overridedQs);

        protected abstract void HandleMarketCommand(MarketCommand command);

        protected abstract void HandleTradingCommand(TradingCommand tradingCommand);

        protected abstract void HandleSettingCommand(SettingCommand command);

        protected abstract void HandleNotifyCommand(NotifyCommand command);

        public string Test()
        {
            return "OK";
        }
    }


}