using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class RiskChecker
    {
        internal static readonly RiskChecker Default = new RiskChecker();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RiskChecker));
        private Dictionary<Guid, DateTime> _lastRiskTimePerAccountDict;
        private TimeSpan _dueTime;
        private object _mutex;

        static RiskChecker() { }

        private RiskChecker()
        {
            _lastRiskTimePerAccountDict = new Dictionary<Guid, DateTime>(5000);
            _dueTime = TimeSpan.FromSeconds(60);
            _mutex = new object();
            new Thread(this.CheckHandle)
            {
                IsBackground = true
            }.Start();
        }

        internal void Add(Guid accountId, DateTime riskTime)
        {
            lock (_mutex)
            {
                DateTime lastCheckTime;
                if (!_lastRiskTimePerAccountDict.TryGetValue(accountId, out lastCheckTime))
                {
                    _lastRiskTimePerAccountDict.Add(accountId, riskTime);
                }
                else
                {
                    if (riskTime > lastCheckTime)
                    {
                        _lastRiskTimePerAccountDict[accountId] = riskTime;
                    }
                }

            }
        }

        internal void Remove(Guid accountId)
        {
            lock (_mutex)
            {
                if (_lastRiskTimePerAccountDict.ContainsKey(accountId))
                {
                    _lastRiskTimePerAccountDict.Remove(accountId);
                }
            }
        }


        private void CheckHandle()
        {
            while (true)
            {
                Thread.Sleep(10000);
                lock (_mutex)
                {
                    this.CheckAccounts();
                }
            }
        }

        private void CheckAccounts()
        {
            try
            {
                foreach (var eachPair in _lastRiskTimePerAccountDict)
                {
                    var accountId = eachPair.Key;
                    DateTime lastCheckTime = eachPair.Value;
                    this.CheckAccount(accountId, lastCheckTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void CheckAccount(Guid accountId, DateTime lastCheckTime)
        {
            try
            {
                var account = TradingSetting.Default.GetAccount(accountId);
                if (account == null) throw new NullReferenceException(string.Format("account id = {0}, lastCheckTime = {1}", accountId, lastCheckTime));
                if (Market.MarketManager.Now - lastCheckTime >= _dueTime)
                {
                    Task.Factory.StartNew(() => account.CheckRisk());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
