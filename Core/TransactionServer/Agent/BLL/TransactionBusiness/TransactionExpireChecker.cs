using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal sealed class TransactionExpireChecker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionExpireChecker));

        internal static readonly TransactionExpireChecker Default = new TransactionExpireChecker();

        private Dictionary<Guid, Transaction> _trans = new Dictionary<Guid, Transaction>(50);
        private TradingEngine _tradingEngine;

        private object _mutex = new object();

        private volatile bool _isStopped = false;

        private List<Guid> _toBeRemovedTrans = new List<Guid>(5);

        static TransactionExpireChecker() { }
        private TransactionExpireChecker() { }

        internal void Initialize(TradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
            new Thread(this.CheckHandle)
            {
                IsBackground = true
            }.Start();
        }

        internal void Stop()
        {
            _isStopped = true;
        }


        internal void Add(Transaction tran)
        {
            lock (_mutex)
            {
                if (!_trans.ContainsKey(tran.Id))
                {
                    Logger.WarnFormat("add  tran = {0}", tran);
                    _trans.Add(tran.Id, tran);
                }
                else
                {
                    Logger.WarnFormat("add already exist tran = {0}", tran);
                }
            }
        }

        internal void Remove(Guid tranId)
        {
            lock (_mutex)
            {
                if (_trans.ContainsKey(tranId))
                {
                    Logger.WarnFormat("remove  tran = {0}", tranId);
                    _trans.Remove(tranId);
                }
                else
                {
                    Logger.WarnFormat("remove not exist expired tran = {0}", tranId);
                }
            }
        }



        private void CheckHandle()
        {
            while (!_isStopped)
            {
                lock (_mutex)
                {
                    if (_trans.Count > 0)
                    {
                        this.CheckTrans();
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void CheckTrans()
        {
            this.CheckAndCollectExpiredTrans();
            this.CancelExpiredTrans();
        }

        private void CancelExpiredTrans()
        {
            if (_toBeRemovedTrans.Count > 0)
            {
                foreach (var eachTranId in _toBeRemovedTrans)
                {
                    var tran = _trans[eachTranId];
                    _trans.Remove(eachTranId);
                    _tradingEngine.Cancel(tran, CancelReason.TransactionExpired);
                }
                _toBeRemovedTrans.Clear();
            }
        }


        private void CheckAndCollectExpiredTrans()
        {
            foreach (var eachTran in _trans.Values)
            {
                if (eachTran.IsExpired(null, null))
                {
                    Logger.WarnFormat("tran = {0} expired, to be canceled", eachTran);
                    _toBeRemovedTrans.Add(eachTran.Id);
                }
            }
        }


    }
}
