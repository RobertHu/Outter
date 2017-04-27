using System;
using System.Collections.Generic;
using System.Web;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using iExchange.Common;
using iExchange.StateServer.ExchangeMapping;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace iExchange.StateServer
{
    /// <summary>
    /// Responsible for send the executed transaction data to head office.
    /// </summary>
    class FilialeTransactionSwitcher
    {
        private static readonly string PacketFileFullName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packet_Filiale.dat");
        private const int SleepSecondsWhileSendFailed = 10;

        private SyncQueue<FilialeTransaction> _FailedQueue = new SyncQueue<FilialeTransaction>();
        private AutoResetEvent _FailedQueueAddedEvent = new AutoResetEvent(false);
        private BinaryFormatter _Formatter = new BinaryFormatter();
        private iExchangeMappingService _iExchangeMappingService = new iExchangeMappingService();
        private object _Lock = new object();

        private static FilialeTransactionSwitcher _Instance;
        private static object _LockForSingleton = new object();

        private FilialeTransactionSwitcher()
        {
        }

        public static FilialeTransactionSwitcher GetInstance()
        {
            if (_Instance == null)
            {
                lock (_LockForSingleton)
                {
                    if (_Instance == null)
                    {
                        FilialeTransactionSwitcher instance = new FilialeTransactionSwitcher();
                        instance.Initialize();
                        _Instance = instance;
                    }
                }
            }
            return _Instance;
        }

        /// <summary>
        /// Send the executed transaction data to head office, if failed, save the data to file and re-send it later.
        /// </summary>
        public void AddFilialeTransaction(FilialeTransaction filialeTransaction)
        {
            lock (_Lock)
            {
                if (this._FailedQueue.Count > 0)
                {
                    this.AppendFailedTransactionToFile(filialeTransaction);
                    this._FailedQueue.Enqueue(filialeTransaction);
                    this._FailedQueueAddedEvent.Set();
                }
                else
                {
                    try
                    {
                        this.SendFilialeTransactionToExchangeSwitch(filialeTransaction);
                    }
                    catch (Exception exception)
                    {
                        AppDebug.LogEvent("StateServer", String.Format("{0}\r\n{1}", exception, filialeTransaction), EventLogEntryType.Error);

                        this.AppendFailedTransactionToFile(filialeTransaction);
                        this._FailedQueue.Enqueue(filialeTransaction);
                        this._FailedQueueAddedEvent.Set();
                    }
                }
            }
        }
        
        #region private methods

        private void Initialize()
        {
            this._iExchangeMappingService.Url = ConfigurationManager.AppSettings["iExchange.StateServer.iExchangeMappingService"];
            try
            {
                this.LoadFailedTransactionsFromFile();
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", String.Format("LoadFailedTransactionsFromFile error: {0}", exception), EventLogEntryType.Error);
                throw;
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.SendFailedTransactions));
        }
        
        private void SendFilialeTransactionToExchangeSwitch(FilialeTransaction filialeTransaction)
        {
            string filialeCode = ConfigurationManager.AppSettings["FilialeCode"];
            if (String.IsNullOrEmpty(filialeCode))
            {
                throw new Exception("FilialeCode not found in appSettings.");
            }

            this._iExchangeMappingService.FilialeAddTransaction(filialeCode, filialeTransaction.Id, filialeTransaction.XmlTran);
        }

        private void SendFailedTransactions(object state)
        {
            while (true)
            {
                while (this._FailedQueue.Count > 0)
                {
                    FilialeTransaction filialeTransaction = this._FailedQueue.Peek();
                    try
                    {
                        this.SendFilialeTransactionToExchangeSwitch(filialeTransaction);

                        this._FailedQueue.Dequeue();
                        if (this._FailedQueue.Count == 0 && File.Exists(PacketFileFullName))
                        {
                            File.Delete(PacketFileFullName);
                        }
                    }
                    catch (Exception exception)
                    {
                        AppDebug.LogEvent("StateServer", String.Format("{0}\r\n{1}", exception, filialeTransaction), EventLogEntryType.Error);
                        Thread.Sleep(TimeSpan.FromSeconds(SleepSecondsWhileSendFailed));
                    }
                }

                this._FailedQueueAddedEvent.WaitOne();
            }
        }
        
        private void LoadFailedTransactionsFromFile()
        {
            if (File.Exists(PacketFileFullName))
            {
                using (FileStream stream = File.Open(PacketFileFullName, FileMode.Open))
                {
                    while (stream.Position < stream.Length)
                    {
                        FilialeTransaction item = (FilialeTransaction)this._Formatter.Deserialize(stream);
                        this._FailedQueue.Enqueue(item);
                    }
                }
            }
        }

        private void AppendFailedTransactionToFile(FilialeTransaction filialeTransaction)
        {
            using (FileStream stream = File.Open(PacketFileFullName, FileMode.OpenOrCreate))
            {
                stream.Position = stream.Length;
                this._Formatter.Serialize(stream, filialeTransaction);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a queue wrapper that is synchronized (thread safe).
    /// </summary>
    public class SyncQueue<T>
    {
        private Queue<T> _Queue = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (this._Queue)
            {
                this._Queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            lock (this._Queue)
            {
                return this._Queue.Dequeue();
            }
        }

        public T Peek()
        {
            lock (this._Queue)
            {
                return this._Queue.Peek();
            }
        }

        public int Count
        {
            get
            {
                lock (this._Queue)
                {
                    return this._Queue.Count;
                }
            }
        }
    }
}
