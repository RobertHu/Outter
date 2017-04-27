using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace Core.TransactionServer.Agent.Caching
{
    public enum CacheType
    {
        None,
        Transaciton,
        Reset,
        LastCode,
        HistoryOrder
    }

    public sealed class Assistant
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Assistant));
        private object _lock = new object();
        private string _cachePath;
        private string _connectionString;
        private RecordManager _recordManager;
        private Dictionary<CacheType, string> _cacheTypeToStoreProcedureMapping;
        private ResetSaver _resetSaver;
        private TradingDataSaver _tradingDataSaver;

        internal Assistant(RecordManager recordManager, string cachePath, string connectionString)
        {
            _recordManager = recordManager;
            _connectionString = connectionString;
            _cachePath = cachePath;
            _resetSaver = new ResetSaver(this);
            _tradingDataSaver = new TradingDataSaver(this);
            _cacheTypeToStoreProcedureMapping = new Dictionary<CacheType, string>
            {
                {CacheType.Reset, "Trading.SaveResetStatus"},
                {CacheType.HistoryOrder, "Trading.SaveResetStatus"},
                {CacheType.Transaciton, "Trading.UpdateTradingData"}
            };
        }

        public void Add(Account account, string rawData, CacheType cacheType)
        {
            lock (_lock)
            {
                CacheData cacheData = this.CreateCacheData(account, rawData, cacheType);
                if (cacheType == CacheType.Transaciton || cacheType == CacheType.HistoryOrder)
                {
                    _recordManager.Persistent(cacheData);
                    CacheFileManager.Copy(cacheData.FilePath);
                    this.AddForTradingData(cacheData);
                }
                else if (cacheType == CacheType.Reset)
                {
                    _recordManager.Persistent(cacheData);
                    CacheFileManager.Move(cacheData.FilePath);
                    _resetSaver.Add(cacheData);
                }
            }

        }

        private void AddForTradingData(CacheData cacheData)
        {
            _tradingDataSaver.Add(cacheData);
        }

        private void Save(string content, CacheType type)
        {
            Protocal.DB.DBRetryHelper.RetryForever(() => this.SaveToDBRetry(content, type));
        }


        private CacheData CreateCacheData(Account account, string rawData, CacheType cacheType)
        {
            long sequence = account.Version;
            string filePath = string.Format("{0}_{1}{2}", account.Id, sequence, _recordManager.GetFileExtension(cacheType));
            return new CacheData(sequence, account.Id, cacheType, rawData, Path.Combine(_cachePath, filePath));
        }


        public bool FlushToDB()
        {
            List<string> inconsistentFiles;
            var data = _recordManager.LoadFileContents(out inconsistentFiles);
            if (data.Count == 0) return true;
            var accountVersions = this.LoadAccountVersions();
            foreach (var eachPair in data)
            {
                Guid accountId = eachPair.Key;
                var accountCacheData = eachPair.Value;
                if (!this.SaveAccountCacheData(accountId, accountCacheData, accountVersions))
                {
                    return false;
                }
            }
            this.DeleteInconsistentFiles(inconsistentFiles);
            return true;
        }

        //从数据库加载账户的版本号
        private Dictionary<Guid, Int64> LoadAccountVersions()
        {
            Dictionary<Guid, Int64> accountVersions = new Dictionary<Guid, long>(1000);
            var accountVersionRecords = DB.DBRepository.Default.LoadAccountVersions();
            if (accountVersionRecords != null)
            {
                foreach (var eachRecord in accountVersionRecords)
                {
                    accountVersions.Add(eachRecord.AccountID, eachRecord.Version);
                }
            }
            return accountVersions;
        }

        private bool SaveAccountCacheData(Guid accountId, List<CacheData> accountCacheData, Dictionary<Guid, Int64> accountVersions)
        {
            Int64 accountVersion;

            //当账户的版本号不存在时，意味着该账户没未交易过， 故所有的文件全部保存
            if (!accountVersions.TryGetValue(accountId, out accountVersion))
            {
                foreach (var eachCacheData in accountCacheData)
                {
                    bool isSuccess = this.SaveToDB(eachCacheData.RawData, eachCacheData.Type);
                    if (!isSuccess) return false;
                    _recordManager.DeleteFileOf(eachCacheData.FilePath);
                }
                return true;
            }

            foreach (var eachCacheData in accountCacheData)
            {
                //当文件的版本号大于账户的当前版本号才保存
                if (eachCacheData.GetVersion() > accountVersion)
                {
                    bool isSuccess = this.SaveToDB(eachCacheData.RawData, eachCacheData.Type);
                    if (!isSuccess) return false;
                    _recordManager.DeleteFileOf(eachCacheData.FilePath);
                }
                else
                {
                    Debug.WriteLine(string.Format("SaveAccountCacheData accountId = {0}, version = {1}, accountVersion = {2}, version less than accountVersion not saved", eachCacheData.AccountId, eachCacheData.GetVersion(), accountVersion));
                    _recordManager.DeleteFileOf(eachCacheData.FilePath);
                }
            }
            return true;
        }



        private void DeleteInconsistentFiles(List<string> inconsistentFiles)
        {
            foreach (var eachFile in inconsistentFiles)
            {
                FileHelper.DeleteSilently(eachFile);
            }
        }

        private bool NeedFlushToDB()
        {
            //must get from DB, if the current TradeDay is over and not rested, return true;
            return true;
        }


        private bool SaveToDB(string cacheData, CacheType cacheType)
        {
            try
            {
                this.DoSave(cacheData, cacheType);
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("content = {0}, exception = {1}", cacheData, ex);
                return false;
            }
        }

        private void SaveToDBRetry(string cacheData, CacheType cacheType)
        {
            try
            {
                this.DoSave(cacheData, cacheType);
            }
            catch
            {
                Logger.ErrorFormat("SaveToDbRetry failed, content = {0}", cacheData);
                throw;
            }
        }


        private void DoSave(string cacheData, CacheType cacheType)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                conn.Execute(_cacheTypeToStoreProcedureMapping[cacheType], new { accountXml = cacheData }, commandType: CommandType.StoredProcedure, commandTimeout: (int)((new TimeSpan(0, 5, 0)).TotalMilliseconds));
            }
        }

        private sealed class TradingDataSaver : Protocal.ThreadQueueBase<CacheData>
        {
            private static readonly ILog Logger = LogManager.GetLogger(typeof(TradingDataSaver));

            private Assistant _assistant;

            internal TradingDataSaver(Assistant assistant)
                : base(1000)
            {
                _assistant = assistant;
            }

            public override void DoWork(CacheData m)
            {
                Logger.InfoFormat("begin save trading data to db success, accountId = {0}", m.AccountId);
                if (m.Type == CacheType.HistoryOrder)
                {
                    StringBuilder sb = Protocal.StringBuilderCache.Acquire(200);
                    sb.AppendLine("<Accounts>");
                    sb.Append(m.RawData);
                    sb.AppendLine("</Accounts>");
                    _assistant.Save(Protocal.StringBuilderCache.GetStringAndRelease(sb), m.Type);
                }
                else
                {
                    _assistant.Save(m.RawData, m.Type);
                }
                Logger.InfoFormat("save trading data to db success, content = {0}", m.RawData);
                _assistant._recordManager.DeleteFileOf(m.FilePath);
            }

            public override void RecordLog(Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private sealed class ResetSaver
        {
            private const int QUEUE_LENGTH = 1000;
            private Dictionary<Guid, Queue<CacheData>> _resetCaches;
            private object _mutex = new object();
            private Assistant _assistant;
            private StringBuilder _sb;

            internal ResetSaver(Assistant assistant)
            {
                _assistant = assistant;
                _resetCaches = new Dictionary<Guid, Queue<CacheData>>(QUEUE_LENGTH);
                _sb = new StringBuilder(40 * 1000 * 1000);
                new Thread(this.MergeResetCacheDataHandle)
                {
                    IsBackground = true
                }.Start();

            }

            internal void Add(CacheData cacheData)
            {
                lock (_mutex)
                {
                    Logger.InfoFormat("add cacheData accountId = {0}", cacheData.AccountId);
                    Queue<CacheData> accountQueue;
                    if (!_resetCaches.TryGetValue(cacheData.AccountId, out accountQueue))
                    {
                        accountQueue = new Queue<CacheData>(10);
                        _resetCaches.Add(cacheData.AccountId, accountQueue);
                    }
                    accountQueue.Enqueue(cacheData);
                }
            }

            private void MergeResetCacheDataHandle()
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    if (!_assistant._tradingDataSaver.IsDone)
                    {
                        continue;
                    }
                    lock (_mutex)
                    {
                        if (this.ExistsCacheData())
                        {
                            this.DoMergeAndSaveToDB();
                        }
                    }
                }
            }

            private bool ExistsCacheData()
            {
                if (_resetCaches.Count > 0)
                {
                    foreach (var eachAccountQueue in _resetCaches.Values)
                    {
                        if (eachAccountQueue.Count > 0) return true;
                    }
                }
                return false;
            }


            private void DoMergeAndSaveToDB()
            {
                this.Merge();
                _assistant.Save(_sb.ToString(), CacheType.Reset);
                _sb.Clear();
            }

            private void Merge()
            {
                _sb.AppendLine("<Accounts>");
                foreach (var eachAccountQueue in _resetCaches.Values)
                {
                    if (eachAccountQueue.Count > 0)
                    {
                        var item = eachAccountQueue.Dequeue();
                        _sb.AppendLine(item.RawData);
                    }
                }
                _sb.AppendLine("</Accounts>");
            }

        }

    }




    public static class HashDataHelper
    {
        public static bool IsSameWith(this byte[] left, byte[] right)
        {
            if (Object.ReferenceEquals(left, right)) return true;

            if ((left != null && right == null) || (left == null && right != null) || (left.Length != right.Length))
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index]) return false;
            }

            return true;
        }
    }

    public static class FileHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FileHelper));

        public static void ReadFully(this FileStream stream, byte[] buffer)
        {
            int remainLength = buffer.Length;
            int totalReadLength = 0;

            while (remainLength > 0)
            {
                int readLength = stream.Read(buffer, totalReadLength, remainLength);
                if (readLength == 0)
                {
                    break;
                }
                else
                {
                    totalReadLength += readLength;
                    remainLength -= readLength;
                }
            }
            if (remainLength > 0) throw new IOException();
        }

        public static void DeleteSilently(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Delete file = {0}, ex = {1}", fileName, ex);
            }
        }
    }

    internal struct CacheData : IEquatable<CacheData>
    {
        internal static readonly CacheData Empty = new CacheData(0, Guid.Empty, CacheType.None, string.Empty, string.Empty);

        private long _sequence;
        private Guid _accountId;
        private CacheType _type;
        private string _rawData;
        private string _filePath;

        internal CacheData(long sequence, Guid accountId, CacheType type, string rawData, string filePath)
        {
            _sequence = sequence;
            _accountId = accountId;
            _type = type;
            _rawData = rawData;
            _filePath = filePath;
        }

        internal Int64 GetVersion()
        {
            XElement root = XElement.Parse(_rawData);
            return Int64.Parse(root.Attribute("Version").Value);
        }


        internal long Sequence
        {
            get { return _sequence; }
        }

        internal Guid AccountId
        {
            get { return _accountId; }
        }

        internal string RawData
        {
            get { return _rawData; }
        }

        internal CacheType Type
        {
            get { return _type; }
        }

        internal string FilePath
        {
            get { return _filePath; }
        }

        public bool Equals(CacheData other)
        {
            return this.Sequence == other.Sequence && this.AccountId == other.AccountId;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((CacheData)obj);
        }

        public override int GetHashCode()
        {
            return this.Sequence.GetHashCode() ^ this.AccountId.GetHashCode();
        }

        public static bool operator ==(CacheData left, CacheData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CacheData left, CacheData right)
        {
            return !left.Equals(right);
        }
    }
}
