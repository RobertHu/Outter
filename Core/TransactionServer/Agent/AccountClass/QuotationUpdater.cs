using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Core.TransactionServer.Agent.Quotations;
using log4net;
using System.Runtime.InteropServices;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using iExchange.Common;
using Protocal;
using Core.TransactionServer.Agent.Market;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal sealed class QuotationManager : ThreadQueueBase<OverridedQ[]>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(QuotationManager));

        internal static readonly QuotationManager Default = new QuotationManager();
        private QuotationMerger _merger = new QuotationMerger();
        static QuotationManager() { }
        private QuotationManager()
            : base(100)
        {
        }

        protected override OverridedQ[] Dequeue()
        {
            OverridedQ[] quotation;
            if (_queue.Count < 4)
            {
                quotation = _queue.Dequeue();
            }
            else
            {
                quotation = _merger.MergeAndGetQuotationToBroadcast(_queue);
                _queue.Clear();
            }
            return quotation;
        }


        public override void DoWork(OverridedQ[] item)
        {
            QuotationBulk bulk = new QuotationBulk(item);
            TradingSetting.Default.DoParallelForAccounts(a => a.UpdateQuotation(bulk));
            TradingSetting.Default.DoParallelForAccounts(a => a.Hit(bulk));
            MarketManager.Default.UpdateQuotation(bulk);
            PriceAlert.Manager.Default.SetQuotation(item);
        }

        public override void RecordLog(Exception ex)
        {
            Logger.Error(ex);
        }
    }




    internal sealed class QuotationMerger
    {
        private Dictionary<InstrumentQuotePolicyKey, MergedQuotation> mergedQuotaions = new Dictionary<InstrumentQuotePolicyKey, MergedQuotation>(20);

        internal OverridedQ[] MergeAndGetQuotationToBroadcast(Queue<OverridedQ[]> pendingQuotations)
        {
            if (pendingQuotations.Count > 0)
            {
                foreach (OverridedQ[] pendingQuotation in pendingQuotations)
                {
                    if (pendingQuotation == null) continue;

                    foreach (OverridedQ overridedQuotation in pendingQuotation)
                    {
                        if (string.IsNullOrEmpty(overridedQuotation.Ask) || string.IsNullOrEmpty(overridedQuotation.Bid)) continue;

                        InstrumentQuotePolicyKey mergedQuotationKey = new InstrumentQuotePolicyKey(overridedQuotation.InstrumentID, overridedQuotation.QuotePolicyID);

                        MergedQuotation mergedQuotation = null;
                        if (!this.mergedQuotaions.TryGetValue(mergedQuotationKey, out mergedQuotation))
                        {
                            mergedQuotation = new MergedQuotation(overridedQuotation.InstrumentID, overridedQuotation.QuotePolicyID);
                            this.mergedQuotaions.Add(mergedQuotationKey, mergedQuotation);
                        }
                        mergedQuotation.Merge(overridedQuotation);
                    }
                }
            }

            List<OverridedQ> overridedQuotations = null;
            foreach (MergedQuotation mergedQuotation in this.mergedQuotaions.Values)
            {
                QuotationPair pair = mergedQuotation.Fetch();
                if (!pair.IsEmpty)
                {
                    if (overridedQuotations == null) overridedQuotations = new List<OverridedQ>();
                    overridedQuotations.Add(pair.OverridedQuotation);
                }
            }
            return overridedQuotations.ToArray();
        }
    }


    internal struct InstrumentQuotePolicyKey : IEquatable<InstrumentQuotePolicyKey>
    {
        private Guid _instrumentId;
        private Guid _quotePolicyId;

        internal InstrumentQuotePolicyKey(Guid instrumentId, Guid quotePolicyId)
        {
            _instrumentId = instrumentId;
            _quotePolicyId = quotePolicyId;
        }

        internal Guid InstrumentId
        {
            get { return _instrumentId; }
        }

        internal Guid QuotePolicyId
        {
            get { return _quotePolicyId; }
        }

        public bool Equals(InstrumentQuotePolicyKey other)
        {
            return this.InstrumentId == other.InstrumentId && this.QuotePolicyId == other.QuotePolicyId;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((InstrumentQuotePolicyKey)obj);
        }

        public override int GetHashCode()
        {
            return this.InstrumentId.GetHashCode() ^ this.QuotePolicyId.GetHashCode();

        }
    }



    internal class MergedQuotation
    {
        private Guid InstrumentId;
        private Guid QuotePolicyId;

        private QuotationPair High = new QuotationPair();
        private QuotationPair Low = new QuotationPair();
        private QuotationPair Last = new QuotationPair();

        internal MergedQuotation(Guid instrumentId, Guid quotePolicyId)
        {
            this.InstrumentId = instrumentId;
            this.QuotePolicyId = quotePolicyId;
        }

        internal QuotationPair Fetch()
        {
            QuotationPair result = QuotationPair.Empty;
            if (!this.High.IsEmpty)
            {
                if (this.Low.IsEmpty || this.High.OverridedQuotation.Timestamp < this.Low.OverridedQuotation.Timestamp)
                {
                    result = this.High;
                    this.High = QuotationPair.Empty;
                }
                else
                {
                    result = this.Low;
                    this.Low = QuotationPair.Empty;
                }
            }
            else if (!this.Low.IsEmpty)
            {
                result = this.Low;
                this.Low = QuotationPair.Empty;
            }
            else if (!this.Last.IsEmpty)
            {
                result = this.Last;
                this.Last = QuotationPair.Empty;
            }

            return result;
        }

        internal void Merge(OverridedQ overridedQuotation)
        {
            if (this.High.IsEmpty || decimal.Parse(overridedQuotation.Bid) >= decimal.Parse(this.High.OverridedQuotation.Bid))
            {
                if (!this.High.IsEmpty && this.Low.IsEmpty)
                {
                    this.Low = new QuotationPair(this.High);
                }

                this.High = new QuotationPair(overridedQuotation);
                if (!this.Last.IsEmpty && overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
                {
                    this.Last = QuotationPair.Empty;
                }
            }
            else if (this.Low.IsEmpty || decimal.Parse(overridedQuotation.Bid) <= decimal.Parse(this.Low.OverridedQuotation.Bid))
            {
                this.Low = new QuotationPair(overridedQuotation);
                if (!this.Last.IsEmpty && overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
                {
                    this.Last = QuotationPair.Empty;
                }
            }
            else if (this.Last.IsEmpty || overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
            {
                this.Last = new QuotationPair(overridedQuotation);
            }
        }
    }

    internal struct QuotationPair
    {
        internal static readonly QuotationPair Empty = new QuotationPair(null);

        private OverridedQ _quotation;

        internal QuotationPair(OverridedQ quotation)
        {
            _quotation = quotation;
        }

        internal QuotationPair(QuotationPair pair)
        {
            _quotation = pair.OverridedQuotation;
        }

        internal OverridedQ OverridedQuotation
        {
            get
            {
                return _quotation;
            }
        }

        internal bool IsEmpty { get { return this.OverridedQuotation == null; } }
    }

}