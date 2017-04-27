using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SystemController
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal sealed class BroadcastService : Protocal.IBroadcastService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BroadcastService));
        public void Broadcast(Protocal.Command command)
        {
            try
            {
                Logger.WarnFormat("Receive command, type={0}", command.GetType());
                var tradingCommand = command as Protocal.TradingCommand;
                if (tradingCommand != null)
                {
                    Logger.WarnFormat("Receive tradingCommand, content = {0}", tradingCommand.Content);
                }

                Broadcaster.Default.AddCommand(command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public bool Test()
        {
            return true;
        }
    }
}
