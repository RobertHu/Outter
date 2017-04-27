using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Core.TransactionServer.Agent.Caching
{
    internal struct CacheItem
    {
        private Guid _accountId;
        private string _rawData;
        private CacheType _type;

        internal CacheItem(Guid accountId, string rawData, CacheType type)
        {
            _accountId = accountId;
            _rawData = rawData;
            _type = type;
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
    }

    public sealed class CacheCenter
    {
        private Assistant _assistant;

        public static readonly CacheCenter Default = new CacheCenter();

        static CacheCenter() { }
        private CacheCenter() { }

        public void Initialize(string cachePath, string connectionString)
        {
            _assistant = new Assistant(new RecordManager(cachePath, '_'), cachePath, connectionString);
        }

        public void Add(Account account, string rawData, CacheType cacheType)
        {
            _assistant.Add(account, rawData, cacheType);
        }

        public bool FlushToDB()
        {
            return _assistant.FlushToDB();
        }

    }
}
