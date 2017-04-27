using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Core.TransactionServer;
using System.Threading;
using System.Diagnostics;
using iExchange.Common;
using System.Xml;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using System.Threading.Tasks.Dataflow;

namespace Core.TransactionServer.Engine.iExchange
{
    internal sealed class PriceDelayedTransactionManager
    {
        private const int ListCapacityFactor = 10;
        private const int DictCapacityFactor = 20;
        private Dictionary<Guid, List<Transaction>> _waitingForPriceTrans = new Dictionary<Guid, List<Transaction>>(DictCapacityFactor);
        private object _dictMutex = new object();
        private ActionBlock<Quotation> _quotationBlock;
        internal static readonly PriceDelayedTransactionManager Default = new PriceDelayedTransactionManager();

        private PriceDelayedTransactionManager()
        {
            _quotationBlock = new ActionBlock<Quotation>((Action<Quotation>)this.DoWork);
        }
        static PriceDelayedTransactionManager() { }

        internal void Add(Transaction tran)
        {
            lock (_dictMutex)
            {
                List<Transaction> transWithSameInstrument;
                if (!_waitingForPriceTrans.TryGetValue(tran.InstrumentId, out transWithSameInstrument))
                {
                    transWithSameInstrument = new List<Transaction>(ListCapacityFactor);
                    _waitingForPriceTrans.Add(tran.InstrumentId, transWithSameInstrument);
                }
                transWithSameInstrument.Add(tran);
            }
        }

        private void DoWork(Quotation quotation)
        {
            lock (_dictMutex)
            {
                this.InnerDoWork(quotation);
            }
        }

        private void InnerDoWork(Quotation quotation)
        {
            if (!_waitingForPriceTrans.ContainsKey(quotation.InstrumentId)) return;
            var toBeCheckedTrans = _waitingForPriceTrans[quotation.InstrumentId];
            List<Transaction> toBeRemovedTrans;
            this.CheckQuotation(quotation, toBeCheckedTrans, out toBeRemovedTrans);
            foreach (var eachTran in toBeRemovedTrans)
            {
                toBeCheckedTrans.Remove(eachTran);
            }
        }

        private void CheckQuotation(Quotation quotation, List<Transaction> toBeCheckedTrans, out List<Transaction> toBeRemovedTrans)
        {
            var baseTIme = MarketManager.Now;
            toBeRemovedTrans = new List<Transaction>();
            foreach (var eachTran in toBeCheckedTrans)
            {
                if (eachTran.EndTime <= baseTIme || eachTran.Phase == TransactionPhase.Canceled)
                {
                    toBeRemovedTrans.Add(eachTran);
                }
                else
                {
                    if (quotation.Timestamp >= eachTran.SetPriceTimestamp)
                    {
                        toBeRemovedTrans.Add(eachTran);
                        var account = eachTran.Owner;
                        //account.PlaceWhenPriceArrived(eachTran);
                    }
                }
            }
        }

    }
}
