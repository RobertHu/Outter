using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Threading;
using log4net;

namespace iExchange.StateServer.Adapter
{
    internal sealed class QuotationManager : Protocal.ThreadQueueBase<QuotationPair>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(QuotationManager));
        private QuotationServiceProxy _quotationServiceProxy;

        internal QuotationManager(string quotationServiceUrl)
            : base(100)
        {
            _quotationServiceProxy = new QuotationServiceProxy(quotationServiceUrl, "SystemController_QuotationService");
        }

        private Protocal.OriginQ[] CreateOriginQs(OriginQuotation[] originQs)
        {
            if (originQs == null) return null;
            Protocal.OriginQ[] result = new Protocal.OriginQ[originQs.Length];
            for (int i = 0; i < originQs.Length; i++)
            {
                result[i] = QuotationPool.Default.GetOriginQ(originQs[i]);
            }
            return result;
        }

        private Protocal.OverridedQ[] CreateOverrideQs(OverridedQuotation[] overrideQs)
        {
            if (overrideQs == null) return null;
            Protocal.OverridedQ[] result = new Protocal.OverridedQ[overrideQs.Length];
            for (int i = 0; i < overrideQs.Length; i++)
            {
                result[i] = QuotationPool.Default.GetOverrideQ(overrideQs[i]);
            }
            return result;
        }


        public override void DoWork(QuotationPair item)
        {
            _quotationServiceProxy.SetQuotation(this.CreateOriginQs(item.OriginQs), this.CreateOverrideQs(item.OverridedQs));
        }

        public override void RecordLog(Exception ex)
        {
            Logger.Error(ex);
        }
    }

    internal struct QuotationPair : IEquatable<QuotationPair>
    {
        private OriginQuotation[] _originQs;
        private OverridedQuotation[] _overridedQs;

        internal QuotationPair(OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            _originQs = originQs;
            _overridedQs = overridedQs;
        }

        internal OriginQuotation[] OriginQs
        {
            get { return _originQs; }
        }

        internal OverridedQuotation[] OverridedQs
        {
            get { return _overridedQs; }
        }

        public override bool Equals(object obj)
        {
            return this.Equals((QuotationPair)obj);
        }

        public override int GetHashCode()
        {
            if (_overridedQs == null && _originQs == null) return 0;
            if (_overridedQs == null) return _originQs.GetHashCode();
            if (_originQs == null) return _overridedQs.GetHashCode();
            return _overridedQs.GetHashCode() ^ _originQs.GetHashCode();
        }

        public bool Equals(QuotationPair other)
        {
            return this.OriginQs == other.OriginQs && this.OverridedQs == other.OverridedQs;
        }

        public static bool operator ==(QuotationPair left, QuotationPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QuotationPair left, QuotationPair right)
        {
            return !left.Equals(right);
        }

    }
}