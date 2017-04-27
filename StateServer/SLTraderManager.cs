using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using iExchange.Common;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace iExchange.StateServer
{
    internal struct CommandAndToken
    {
        private Token token;
        private Command command;

        internal CommandAndToken(Token token, Command command)
        {
            this.token = token;
            this.command = command;
        }

        internal Token Token
        {
            get { return this.token; }
        }

        internal Command Command
        {
            get { return this.command; }
        }
    }

    internal class SLTraderManager
    {
        private static TimeSpan checkSuspectInterval = TimeSpan.FromSeconds(15);
        private object lockObj = new object();
       
        private Dictionary<string, ICommandCollectService> silverLightTradingConsoles = new Dictionary<string,ICommandCollectService>();
        private List<string> suspectUrls = new List<string>();
        private Thread checkSuspectUrlThread = null;
        private int retryCount = 0;
        private Dictionary<string, LinkedList<CommandAndToken>> commandQueue = new Dictionary<string, LinkedList<CommandAndToken>>();
        private Dictionary<string, Thread> sendCommandThreads = new Dictionary<string, Thread>();
        private Dictionary<string, AutoResetEvent> commandEvents = new Dictionary<string, AutoResetEvent>();

        internal void BroadcastCommand(Token token, Command command)
        {
            lock (this.lockObj)
            {
                foreach (string url in this.silverLightTradingConsoles.Keys)
                {
                    LinkedList<CommandAndToken> queue = null;
                    if (!this.commandQueue.TryGetValue(url, out queue))
                    {
                        queue = new LinkedList<CommandAndToken>();
                        this.commandQueue.Add(url, queue);
                    }

                    if (!this.sendCommandThreads.ContainsKey(url) || !this.sendCommandThreads[url].IsAlive)
                    {
                        Thread thread = new Thread(this.SendCommand);
                        this.sendCommandThreads[url] = thread;
                        this.commandEvents[url] = new AutoResetEvent(false);

                        thread.IsBackground = true;
                        thread.Start(url);
                    }
                    
                    queue.AddLast(new CommandAndToken(token, command));
                    this.commandEvents[url].Set();
                }
            }
        }

        private void SendCommand(object state)
        {
            ICommandCollectService commandCollecter = null;
            AutoResetEvent commandEvent = null;
            LinkedList<CommandAndToken> queue = null;

            string url = (string)state;
            lock (this.lockObj)
            {
                if (!this.silverLightTradingConsoles.ContainsKey(url)) return;

                commandCollecter = this.silverLightTradingConsoles[url];
                commandEvent = this.commandEvents[url];
                queue = this.commandQueue[url];
            }

            while (true)
            {
                commandEvent.WaitOne();

                while (true)
                {
                    CommandAndToken? commandAndToken = null;
                    lock (this.lockObj)
                    {
                        if (queue.Count > 0)
                        {
                            commandAndToken = queue.First.Value;
                        }
                    }

                    if (commandAndToken == null)
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            commandCollecter.AddCommand(commandAndToken.Value.Token, commandAndToken.Value.Command);
                            lock (this.lockObj)
                            {
                                queue.RemoveFirst();
                            }
                        }
                        catch (Exception e)
                        {
                            AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n Token = {1},TargetApp = {2}\n Command = {3} url={4}", e, commandAndToken.Value.Token, AppType.TradingConsoleSilverLight, commandAndToken.Value.Command, url), EventLogEntryType.Warning);
                            if (e is EndpointNotFoundException || e is TimeoutException)
                            {
                                lock (this.lockObj)
                                {   
                                    this.commandQueue[url].Clear();
                                    this.silverLightTradingConsoles.Remove(url);
                                    this.HandleSuspectUrl(url);
                                    return;
                                }
                            }
                            else //if (e is FaultException || e is CommunicationObjectFaultedException || e is ProtocolException)
                            {
                                AppDebug.LogEvent("StateServer", "Try to recover channel to trader sl: " + url, EventLogEntryType.Warning);                                
                                this.CreateChannelForTraderSL(url, true);
                                lock (this.lockObj)
                                {
                                    if (!this.silverLightTradingConsoles.TryGetValue(url, out commandCollecter))
                                    {
                                        commandCollecter = null;
                                    }
                                }

                                if (commandCollecter == null) Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
        }

        private void HandleSuspectUrl(string url)
        {
            this.retryCount = 0;
            if (!this.suspectUrls.Contains(url))
            {
                AppDebug.LogEvent("StaterServer", "Add " + url + " into suspect collection", EventLogEntryType.Warning);
                this.suspectUrls.Add(url);

                if (this.checkSuspectUrlThread == null || !this.checkSuspectUrlThread.IsAlive)
                {
                    this.checkSuspectUrlThread = new Thread(this.CheckSuspectUrl);
                    this.checkSuspectUrlThread.IsBackground = true;
                    this.checkSuspectUrlThread.Start();
                }
            }
        }

        private void CheckSuspectUrl()
        {
            List<string> urls = new List<string>();
            while (true)
            {
                lock (this.lockObj)
                {
                    if (this.suspectUrls.Count == 0)
                    {
                        this.checkSuspectUrlThread = null;
                        return;
                    }
                    else
                    {
                        urls.Clear();
                        urls.InsertRange(0, this.suspectUrls);
                    }
                }

                this.retryCount++;
                this.retryCount = Math.Max(20, this.retryCount);
                Thread.Sleep(checkSuspectInterval);
                foreach (string url in urls)
                {
                    if (this.TrySuspectUrl(url))
                    {
                        AppDebug.LogEvent("StaterServer", "Recorve " + url + " from suspect collection to normal collection", EventLogEntryType.Warning);
                        lock (this.lockObj)
                        {
                            this.suspectUrls.Remove(url);
                            this.CreateChannelForTraderSL(url, false);
                        }
                    }
                }
            }
        }

        private bool TrySuspectUrl(string url)
        {
            Guid idToTry = new Guid("11111111-2222-3333-4444-555555555555");
            try
            {
                ICommandCollectService commandCollectService = CreateCommandCollectService(url, 10);
                commandCollectService.KickoutPredecessor(idToTry);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ICommandCollectService CreateCommandCollectService(string url, int timeoutInSecond = 30)
        {
            EndpointAddress address = new EndpointAddress(url);
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.MaxBufferPoolSize = binding.MaxReceivedMessageSize = binding.MaxBufferSize = 16 * 1024 * 1024;
            binding.SendTimeout = TimeSpan.FromSeconds(timeoutInSecond);
            binding.OpenTimeout = TimeSpan.FromSeconds(timeoutInSecond);
            ChannelFactory<ICommandCollectService> factory = new ChannelFactory<ICommandCollectService>(binding, address);
            ICommandCollectService commandCollectService = factory.CreateChannel();
            return commandCollectService;
        }

        public bool KickoutPredecessor(Guid userId)
        {
            List<string> trderServerSLUrls = null;
            lock (this.lockObj)
            {
                trderServerSLUrls = new List<string>(this.silverLightTradingConsoles.Keys);
            }

            try
            {
                Parallel.ForEach(trderServerSLUrls, slTradingConsoleUrl =>
                {
                    try
                    {
                        ICommandCollectService commandCollecter = null;

                        lock (this.lockObj)
                        {
                            this.silverLightTradingConsoles.TryGetValue(slTradingConsoleUrl, out commandCollecter);
                        }

                        if(commandCollecter != null) commandCollecter.KickoutPredecessor(userId);
                    }
                    catch (Exception exception)
                    {
                        lock (this.lockObj)
                        {
                            LinkedList<CommandAndToken> queue = null;
                            if(this.commandQueue.TryGetValue(slTradingConsoleUrl, out queue))
                            {
                                queue.Clear();
                            }
                            this.silverLightTradingConsoles.Remove(slTradingConsoleUrl);
                            this.HandleSuspectUrl(slTradingConsoleUrl);
                        }
                        AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Warning);
                    }
                });
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Warning);
                return false;
            }
            return true;
        }

        internal void KickoutAll()
        {
            this.KickoutPredecessor(Guid.Empty);//Guid.Empty means All user
        }

        internal string CreateChannelForTraderSL(string urls, bool removeExist = false)
        {
            urls = urls.ToLower();

            lock (this.lockObj)
            {
                string[] urlArray = urls.Split(';');
                foreach (string url in urlArray)
                {
                    try
                    {
                        if (this.silverLightTradingConsoles.ContainsKey(url))
                        {
                            if (removeExist)
                            {
                                this.silverLightTradingConsoles.Remove(url);
                            }
                            else
                            {
                                continue;
                            }
                        }

                        ICommandCollectService commandCollectService = CreateCommandCollectService(url);
                        this.silverLightTradingConsoles.Add(url, commandCollectService);
                    }
                    catch (Exception exception)
                    {
                        AppDebug.LogEvent("StaterServer", "Failed to CreateChannelForTraderSL with " + url
                            + Environment.NewLine + exception.ToString(), EventLogEntryType.Warning);
                        continue;
                    }
                }
            }
            return urls;
        }
    }
}