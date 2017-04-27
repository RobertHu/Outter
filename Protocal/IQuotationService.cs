using iExchange.Common;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace Protocal
{
    [ProtoContract]
    [ServiceContract]
    public interface IQuotationService
    {
        [OperationContract]
        void SetQuotation(OriginQ[] originQs, OverridedQ[] overridedQs);
    }


    [ProtoContract]
    public sealed class OriginQ
    {
        public OriginQ() { }

        public OriginQ(OriginQuotation quotation)
        {
            this.Initialize(quotation);
        }


        public void Reset(OriginQuotation quotation)
        {
            this.Initialize(quotation);
        }

        private void Initialize(OriginQuotation quotation)
        {
            this.Ask = quotation.Ask;
            this.Bid = quotation.Bid;
            this.DealereID = quotation.DealereID;
            this.FromDealer = quotation.FromDealer;
            this.HasWatchOnlyQuotePolicies = quotation.HasWatchOnlyQuotePolicies;
            this.High = quotation.High;
            this.InstrumentID = quotation.InstrumentID;
            this.IsProblematic = quotation.IsProblematic;
            this.Low = quotation.Low;
            this.Origin = quotation.Origin;
            this.Timestamp = quotation.Timestamp;
            this.TotalVolume = quotation.TotalVolume;
            this.Volume = quotation.Volume;
        }

        [ProtoMember(1)]
        public string Ask { get; set; }

        [ProtoMember(2)]
        public string Bid { get; set; }

        [ProtoMember(3)]
        public Guid DealereID { get; set; }

        [ProtoMember(4)]
        public bool FromDealer { get; set; }

        [ProtoMember(5)]
        public bool HasWatchOnlyQuotePolicies { get; set; }

        [ProtoMember(6)]
        public string High { get; set; }

        [ProtoMember(7)]
        public Guid InstrumentID { get; set; }

        [ProtoMember(8)]
        public bool IsProblematic { get; set; }

        [ProtoMember(9)]
        public string Low { get; set; }

        [ProtoMember(10)]
        public string Origin { get; set; }

        [ProtoMember(11)]
        public DateTime Timestamp { get; set; }

        [ProtoMember(12)]
        public string TotalVolume { get; set; }

        [ProtoMember(13)]
        public string Volume { get; set; }
    }

    [ProtoContract]
    public sealed class OverridedQ
    {
        public OverridedQ() { }

        public OverridedQ(OverridedQuotation quotation)
        {
            this.Initialize(quotation);
        }

        public void Reset(OverridedQuotation quotation)
        {
            this.Initialize(quotation);
        }

        private void Initialize(OverridedQuotation quotation)
        {
            this.Ask = quotation.Ask;
            this.Bid = quotation.Bid;
            this.DealerID = quotation.DealerID;
            this.High = quotation.High;
            this.InstrumentID = quotation.InstrumentID;
            this.Low = quotation.Low;
            this.Origin = quotation.Origin;
            this.QuotePolicyID = quotation.QuotePolicyID;
            this.Timestamp = quotation.Timestamp;
            this.TotalVolume = quotation.TotalVolume;
            this.Volume = quotation.Volume;
        }

        [ProtoMember(1)]
        public string Ask { get; set; }

        [ProtoMember(2)]
        public string Bid { get; set; }

        [ProtoMember(3)]
        public Guid DealerID { get; set; }

        [ProtoMember(4)]
        public string High { get; set; }

        [ProtoMember(5)]
        public Guid InstrumentID { get; set; }

        [ProtoMember(6)]
        public string Low { get; set; }

        [ProtoMember(7)]
        public string Origin { get; set; }

        [ProtoMember(8)]
        public Guid QuotePolicyID { get; set; }

        [ProtoMember(9)]
        public DateTime Timestamp { get; set; }

        [ProtoMember(10)]
        public string TotalVolume { get; set; }

        [ProtoMember(11)]
        public string Volume { get; set; }
    }

    public sealed class QuotationAsyncResult : AsyncResult
    {
        public QuotationAsyncResult(OriginQ[] originQs, OverridedQ[] overridedQs, AsyncCallback callback, object asyncState)
            : base(callback, asyncState)
        {
            this.OriginQs = originQs;
            this.OverridedQs = overridedQs;
        }

        public OriginQ[] OriginQs { get; private set; }
        public OverridedQ[] OverridedQs { get; private set; }
    }


    public class AsyncResult : IAsyncResult, IDisposable
    {
        private AsyncCallback _callback;
        private object _state;
        private ManualResetEvent _manualResetEvent;

        public AsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
            _manualResetEvent = new ManualResetEvent(false);
        }

        public bool IsCompleted
        {
            get { return _manualResetEvent.WaitOne(0, false); }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return _manualResetEvent; }
        }

        public object AsyncState
        {
            get { return _state; }
        }
        public ManualResetEvent AsyncWait
        {
            get { return _manualResetEvent; }

        }
        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public void Completed()
        {
            _manualResetEvent.Set();
            if (_callback != null)
                _callback(this);
        }

        public void Dispose()
        {
            _manualResetEvent.Close();
            _manualResetEvent = null;
            _state = null;
            _callback = null;
        }
    }


}
