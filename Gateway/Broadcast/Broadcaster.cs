using SystemController.Config;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SystemController.Broadcast;

namespace SystemController
{
    internal sealed class Broadcaster
    {
        private enum RequestType
        {
            None,
            OnConnect
        }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Broadcaster));
        internal static readonly Broadcaster Default = new Broadcaster();

        private ClientManager _clientManager;

        static Broadcaster() { }

        private Broadcaster()
        {
            _clientManager = new ClientManager();
            _clientManager.Start();
        }

        internal ClientManager ClientManager { get { return _clientManager; } }


        internal void AddClient(string address, iExchange.Common.AppType appType)
        {
            _clientManager.AddClient(address, appType);
        }

        internal void AddCommand(Command command)
        {
            _clientManager.AddCommand(command);
        }

    }


    internal sealed class ClientManager
    {
        private sealed class ClientInfo
        {
            internal ClientInfo(iExchange.Common.AppType appType)
            {
                this.AppType = appType;
                this.TryConnectedCount = 0;
            }

            internal iExchange.Common.AppType AppType { get; private set; }
            internal int TryConnectedCount { get; set; }
        }

        private sealed class DisconnectedClientManager
        {
            private static readonly ILog Logger = LogManager.GetLogger(typeof(DisconnectedClientManager));
            private Dictionary<string, ClientInfo> _clients = new Dictionary<string, ClientInfo>(4);
            private object _mutex = new object();

            internal int Count
            {
                get
                {
                    lock (_mutex)
                    {
                        return _clients.Count;
                    }
                }
            }

            internal KeyValuePair<string, ClientInfo>[] Clients
            {
                get
                {
                    lock (_mutex)
                    {
                        return _clients.ToArray();
                    }
                }
            }

            internal void Add(string url, iExchange.Common.AppType appType)
            {
                lock (_mutex)
                {
                    if (!_clients.ContainsKey(url))
                    {
                        _clients.Add(url, new ClientInfo(appType));
                    }
                }
            }


            internal void Print()
            {
                lock (_mutex)
                {
                    Logger.InfoFormat("Check disconnected clients count = {0}", _clients.Count);
                    foreach (var eachClientUrl in _clients.Keys)
                    {
                        Logger.InfoFormat("Disconnected Url = {0}", eachClientUrl);
                    }
                }
            }

            internal void Remove(string url)
            {
                lock (_mutex)
                {
                    if (_clients.ContainsKey(url))
                    {
                        _clients.Remove(url);
                    }
                }
            }

            internal void LoadClientUrls()
            {
                lock (_mutex)
                {
                    try
                    {
                        Logger.Info("Load Client urls");
                        var urls = CommandUrlSection.GetConfig().CommandUrls;
                        for (int i = 0; i < urls.Count; i++)
                        {
                            var item = urls[i];
                            Logger.InfoFormat("url={0}, appType={1}", item.Url, item.AppType);
                            var appType = (iExchange.Common.AppType)Enum.Parse(typeof(iExchange.Common.AppType), item.AppType);
                            _clients.Add(item.Url, new ClientInfo(appType));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }

            internal bool Contains(string url)
            {
                lock (_mutex)
                {
                    return _clients.ContainsKey(url);
                }
            }


        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ClientManager));
        private Dictionary<string, ClientBase> _clientDict = new Dictionary<string, ClientBase>(4);
        private object _mutex = new object();
        internal event ClientConnectedHandle ClientConnected;
        private DisconnectedClientManager _disconnectedClientManager = new DisconnectedClientManager();
        private const int MAX_TRY_CONNECT_COUNT = 10;

        internal void Start()
        {
            lock (_mutex)
            {
                this.LoadClientUrls();
                Thread thread = new Thread(this.DisconnectedClientHandle);
                thread.IsBackground = true;
                thread.Start();
            }
        }


        internal void AddCommand(Command command)
        {
            lock (_mutex)
            {
                if (command == null) return;
                foreach (var pair in _clientDict)
                {
                    pair.Value.Send(command);
                }
            }
        }

        internal void AddClient(string address, iExchange.Common.AppType appType)
        {
            lock (_mutex)
            {
                if (_disconnectedClientManager.Contains(address) && appType == iExchange.Common.AppType.TransactionServer) return;
                _disconnectedClientManager.Remove(address);
                if (_clientDict.ContainsKey(address)) return;
                try
                {
                    var client = CreateClient(address, appType);
                    if (client == null) return;
                    Logger.InfoFormat("add client url = {0}", address);
                    this.AddClient(client);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private void AddClient(ClientBase client)
        {
            lock (_mutex)
            {
                if (!_clientDict.ContainsKey(client.ServiceUrl))
                {
                    _clientDict.Add(client.ServiceUrl, client);
                    this.OnClientConnected(client);
                }
            }
        }


        private static ClientBase CreateClient(string address, iExchange.Common.AppType appType)
        {
            try
            {
                var channel = CreateChannel(address);
                return ClientFactory.Create(channel, address, appType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private static ClientBase CreateClientAndTest(string address, iExchange.Common.AppType appType)
        {
            try
            {
                var channel = CreateChannel(address);
                channel.Test();
                return ClientFactory.Create(channel, address, appType);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("CreateClientAndTest address = {0}, appType = {1}, error= {2}", address, appType, ex);
                return null;
            }
        }


        internal static ICommandCollectService CreateChannel(string address)
        {
            return Protocal.ChannelFactory.CreateChannelByName<ICommandCollectService>("CommandCollector", new EndpointAddress(address));
        }



        private void LoadClientUrls()
        {
            _disconnectedClientManager.LoadClientUrls();
        }

        private void DisconnectedClientHandle()
        {
            while (true)
            {
                try
                {
                    this.CheckDisconnectedClients();
                    this.CheckConnectedClients();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                Thread.Sleep(20000);
            }
        }

        private void CheckDisconnectedClients()
        {
            _disconnectedClientManager.Print();
            if (_disconnectedClientManager.Count != 0)
            {
                Logger.InfoFormat("Begin try reconnect clients , count = {0}", _disconnectedClientManager.Count);
                this.ReconnectClient();
            }
        }

        private void ReconnectClient()
        {
            foreach (var eachDisconnectedClient in _disconnectedClientManager.Clients)
            {
                var clientInfo = eachDisconnectedClient.Value;
                ClientBase client = CreateClientAndTest(eachDisconnectedClient.Key, clientInfo.AppType);
                clientInfo.TryConnectedCount++;
                string serviceUrl = eachDisconnectedClient.Key;
                if (client != null)
                {
                    Logger.InfoFormat("client reconnect success url={0}, appType = {1}", serviceUrl, client.AppType);
                    _disconnectedClientManager.Remove(serviceUrl);
                    this.AddClient(client);
                }
                else
                {
                    if (clientInfo.TryConnectedCount >= MAX_TRY_CONNECT_COUNT)
                    {
                        Logger.InfoFormat("client url = {0}, exceed max try connect count = {1} , removed", serviceUrl, clientInfo.TryConnectedCount);
                        _disconnectedClientManager.Remove(serviceUrl);
                    }
                }
            }

        }

        private void CheckConnectedClients()
        {
            ClientBase[] clients = null;
            lock (_mutex)
            {
                if (_clientDict.Count == 0) return;
                clients = _clientDict.Values.ToArray();
            }
            Logger.InfoFormat("Check connected clients count = {0}", _clientDict.Count);
            foreach (var eachClient in clients)
            {
                if (!eachClient.IsCommunicationOK())
                {
                    Logger.InfoFormat("discovery disconnected client  url = {0}, appType = {1}", eachClient.ServiceUrl, eachClient.AppType);
                    lock (_mutex)
                    {
                        if (_clientDict.ContainsKey(eachClient.ServiceUrl))
                        {
                            _clientDict.Remove(eachClient.ServiceUrl);
                        }
                    }
                    _disconnectedClientManager.Add(eachClient.ServiceUrl, eachClient.AppType);
                }
            }

        }

        private void OnClientConnected(ClientBase client)
        {
            var handle = this.ClientConnected;
            if (handle != null)
            {
                handle(client);
            }
        }
    }


    internal delegate void AgentCommunicateFailedHandle(ClientBase agent);

    internal delegate void ClientConnectedHandle(ClientBase client);
}

