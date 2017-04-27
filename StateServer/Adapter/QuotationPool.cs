using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iExchange.StateServer.Adapter
{
    internal sealed class QuotationPool
    {
        internal static readonly QuotationPool Default = new QuotationPool();

        private Queue<Protocal.OriginQ> _originQueue;
        private Queue<Protocal.OverridedQ> _overrideQueue;
        private object _mutex = new object();

        static QuotationPool() { }
        private QuotationPool()
        {
            _originQueue = new Queue<Protocal.OriginQ>(100);
            _overrideQueue = new Queue<Protocal.OverridedQ>(100);
        }

        internal void AddOriginQ(Protocal.OriginQ originQ)
        {
            lock (_mutex)
            {
                _originQueue.Enqueue(originQ);
            }
        }

        internal void AddOverideQ(Protocal.OverridedQ overrideQ)
        {
            lock (_mutex)
            {
                _overrideQueue.Enqueue(overrideQ);
            }
        }

        internal Protocal.OriginQ GetOriginQ(iExchange.Common.OriginQuotation originQuotation)
        {
            lock (_mutex)
            {
                if (_originQueue.Count > 0)
                {
                    var result = _originQueue.Dequeue();
                    result.Reset(originQuotation);
                    return result;
                }
                else
                {
                    return new Protocal.OriginQ(originQuotation);
                }
            }
        }

        internal Protocal.OverridedQ GetOverrideQ(iExchange.Common.OverridedQuotation overridedQuotation)
        {
            lock (_mutex)
            {
                if (_overrideQueue.Count > 0)
                {
                    var result = _overrideQueue.Dequeue();
                    result.Reset(overridedQuotation);
                    return result;
                }
                else
                {
                    return new Protocal.OverridedQ(overridedQuotation);
                }
            }

        }


    }
}