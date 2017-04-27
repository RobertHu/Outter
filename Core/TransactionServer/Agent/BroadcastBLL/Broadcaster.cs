using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Xml;
using iExchange.Common;
using log4net;
using System.ServiceModel;
using Core.TransactionServer.Engine.iExchange.Common;
using Core.TransactionServer.Agent.BroadcastBLL;
using System.Threading.Tasks.Dataflow;

namespace Core.TransactionServer.Agent
{
    public sealed class Broadcaster
    {
        public static readonly Broadcaster Default = new Broadcaster();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Broadcaster));
        private ActionBlock<Protocal.Command> _msgBlock;

        private Service.BroadcasterProxy _proxy;

        static Broadcaster() { }

        private Broadcaster()
        {

            _proxy = new Service.BroadcasterProxy(Settings.ExternalSettings.Default.BroadcastServiceUrl, "broadcastor");
            _msgBlock = new ActionBlock<Protocal.Command>(command =>
            {
                try
                {
                    _proxy.Broadcast(command);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    _msgBlock.Post(command);
                }
            });
        }


        public void Add(Guid accountId, string content)
        {
#if NOBROADCAST
#else
            var command = CommandFactory.CreateContentChangedCommand(accountId, content);
            this.Add(command);
#endif
        }

        internal void Add(Protocal.TradingCommand command)
        {
#if NOBROADCAST
#else
            command.SourceType = AppType.TransactionServer;
            _msgBlock.Post(command);
#endif
        }

        internal void Add(Protocal.MarketCommand command)
        {
            command.SourceType = AppType.TransactionServer;
            _msgBlock.Post(command);
        }

        internal void Add(Protocal.SettingCommand command)
        {
            command.SourceType = AppType.TransactionServer;
            _msgBlock.Post(command);
        }


        private Protocal.IBroadcastService DoCreateChannel()
        {
            return Protocal.ChannelFactory.CreateChannelByName<Protocal.IBroadcastService>("broadcaster", new EndpointAddress(Settings.ExternalSettings.Default.BroadcastServiceUrl));
        }


    }

}