using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Xml;

namespace iExchange.StateServer.Adapter
{
    internal sealed class Mapper
    {
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);
        private Queue<XmlNode> _queue = new Queue<XmlNode>(50);
        private object _mutex = new object();
        private volatile bool _started;
        private ICommandBroadcast _broadcast;

        internal Mapper(ICommandBroadcast broadcast)
        {
            _broadcast = broadcast;
        }

        internal void Start()
        {
            if (_started) return;
            new Thread(this.ProcessMapOrder)
            {
                IsBackground = true
            }.Start();
            _started = true;
        }

        internal void Stop()
        {
            _started = false;
        }


        internal void Add(XmlNode tran)
        {
            lock (_mutex)
            {
                _queue.Enqueue(tran);
                _resetEvent.Set();
            }
        }


        private void ProcessMapOrder()
        {
            while (_started)
            {
                _resetEvent.WaitOne();
                while (true)
                {
                    XmlNode tran = null;
                    lock (_mutex)
                    {
                        if (_queue.Count > 0)
                        {
                            tran = _queue.Dequeue();
                        }
                    }
                    if (tran == null) break;
                    _broadcast.OnTranExecutedForNewVersion(tran);
                }
            }
        }

    }
}