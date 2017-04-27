using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Caching
{
    internal sealed class CacheDataComparer : IComparer<CacheData>
    {
        internal static readonly CacheDataComparer Default = new CacheDataComparer();

        public int Compare(CacheData x, CacheData y)
        {
            return x.GetVersion().CompareTo(y.GetVersion());
        }
    }

    internal sealed class RecordManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RecordManager));
        private string _cachePath;
        private char _sequenceSeparator;
        private Dictionary<string, CacheType> _fileExtensionToCacheTypeMapping;
        private Dictionary<CacheType, string> _cacheTypeToFileExtensionMapping;

        internal RecordManager(string cachePath, char sequenceSeparator)
        {
            _cachePath = cachePath;
            _sequenceSeparator = sequenceSeparator;
            _fileExtensionToCacheTypeMapping = new Dictionary<string, CacheType>
            {
                {".rsf", CacheType.Reset},
                {".hof", CacheType.HistoryOrder},
                {".tcf", CacheType.Transaciton}
            };

            _cacheTypeToFileExtensionMapping = new Dictionary<CacheType, string>(_fileExtensionToCacheTypeMapping.Count);
            foreach (var eachPair in _fileExtensionToCacheTypeMapping)
            {
                _cacheTypeToFileExtensionMapping.Add(eachPair.Value, eachPair.Key);
            }
        }

        internal Dictionary<Guid, List<CacheData>> LoadFileContents(out List<string> inconsistentFiles)
        {
            Dictionary<Guid, List<CacheData>> accountCacheDatas = new Dictionary<Guid, List<CacheData>>(500);
            inconsistentFiles = new List<string>();

            List<string> files = new List<string>(Directory.EnumerateFiles(_cachePath));
            if (files.Count == 0) return accountCacheDatas;
            foreach (string fileName in files)
            {
                string filePath = Path.Combine(_cachePath, fileName);
                this.LoadData(filePath, accountCacheDatas, inconsistentFiles);
            }
            this.SortAccountCacheDatas(accountCacheDatas);
            return accountCacheDatas;
        }

        private void SortAccountCacheDatas(Dictionary<Guid, List<CacheData>> accountCacheDatas)
        {
            foreach (var eachCacheDatas in accountCacheDatas.Values)
            {
                eachCacheDatas.Sort(CacheDataComparer.Default);
            }
        }

        private void LoadData(string filePath, Dictionary<Guid, List<CacheData>> accountCacheDatas, List<string> inconsistentFiles)
        {
            bool isFileConsistent;
            CacheData cacheData = this.LoadDataFromFile(filePath, out isFileConsistent);
            if (!isFileConsistent)
            {
                inconsistentFiles.Add(filePath);
            }
            else
            {
                this.AddCacheData(accountCacheDatas, cacheData);
            }
        }

        private void AddCacheData(Dictionary<Guid, List<CacheData>> accountCacheDatas, CacheData cacheData)
        {
            List<CacheData> cacheDataList;
            if (!accountCacheDatas.TryGetValue(cacheData.AccountId, out cacheDataList))
            {
                cacheDataList = new List<CacheData>();
                accountCacheDatas.Add(cacheData.AccountId, cacheDataList);
            }
            cacheDataList.Add(cacheData);
        }

        private CacheData LoadDataFromFile(string filePath, out bool isFileConsistent)
        {
            isFileConsistent = true;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                string rawData = LoadCacheData(fileStream, out isFileConsistent);
                if (!isFileConsistent) return CacheData.Empty;
                var xmlContent = XElement.Parse(rawData);
                var key = xmlContent.Attribute("ID").Value;
                var cacheData = new CacheData(this.ParseSequence(filePath), Guid.Parse(key), this.GetCacheType(Path.GetExtension(filePath)), rawData, filePath);
                return cacheData;
            }
        }

        private CacheType GetCacheType(string fileExtension)
        {
            CacheType type = CacheType.None;
            _fileExtensionToCacheTypeMapping.TryGetValue(fileExtension, out type);
            return type;
        }

        internal string GetFileExtension(CacheType type)
        {
            string result = string.Empty;
            _cacheTypeToFileExtensionMapping.TryGetValue(type, out result);
            return result;
        }


        private long ParseSequence(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            var items = fileName.Split(_sequenceSeparator);
            return long.Parse(items[1]);
        }

        private static string LoadCacheData(FileStream fileStream, out bool isConsistent)
        {
            try
            {
                isConsistent = true;
                int readedCount = 0;
                byte[] hashLengthBytes = new byte[4];
                fileStream.Read(hashLengthBytes, readedCount, hashLengthBytes.Length);
                readedCount += hashLengthBytes.Length;
                int hashLength = ConvertBytesToHashLength(hashLengthBytes);
                byte[] hashData = new byte[hashLength];
                Debug.WriteLine(hashLength);
                fileStream.Read(hashData, 0, hashLength);
                readedCount += hashData.Length;

                int dataLength = (int)fileStream.Length - readedCount;
                byte[] data = new byte[dataLength];
                fileStream.Read(data, 0, dataLength);

                MD5 md5 = MD5.Create();
                byte[] currentHashData = md5.ComputeHash(data);
                if (currentHashData.IsSameWith(hashData))
                {
                    return Encoding.UTF8.GetString(data);
                }
                else
                {
                    isConsistent = false;
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                isConsistent = false;
                return null;
            }
        }

        private static int ConvertBytesToHashLength(byte[] hashBytes)
        {
            int result = 0;
            for (int i = 0; i < hashBytes.Length; i++)
            {
                result += hashBytes[i] << (i * 8);
            }
            return result;
        }

        internal void Persistent(CacheData cacheData)
        {
            var contentBytes = Encoding.UTF8.GetBytes(cacheData.RawData);
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(contentBytes);
            using (FileStream fileStream = new FileStream(cacheData.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough))
            {
                var hashLengthBytes = CalculateHashLengthBytes(hash.Length);
                fileStream.Write(hashLengthBytes, 0, hashLengthBytes.Length);
                fileStream.Write(hash, 0, hash.Length);
                fileStream.Write(contentBytes, 0, contentBytes.Length);
                fileStream.Flush();
            }
        }

        private static byte[] CalculateHashLengthBytes(int hashLength)
        {
            byte[] result = new byte[4];
            int mask = 0xFF;
            result[0] = (byte)(hashLength & mask);
            result[1] = (byte)((hashLength >> 8) & mask);
            result[2] = (byte)((hashLength >> 16) & mask);
            result[3] = (byte)((hashLength >> 24) & mask);
            return result;
        }

        internal void DeleteFileOf(string filePath)
        {
            FileHelper.DeleteSilently(filePath);
        }
    }
}
