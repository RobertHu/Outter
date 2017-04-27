using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Protocal
{
    public abstract class ThreadQueueBase<T>
    {
        protected Queue<T> _queue;
        private object _mutex;
        private AutoResetEvent _resetEvent;
        protected volatile bool _isDone = true;

        protected ThreadQueueBase(int queueSize)
        {
            _queue = new Queue<T>(queueSize);
            _mutex = new object();
            _resetEvent = new AutoResetEvent(false);
            new Thread(this.ThreadHandle)
            {
                IsBackground = true
            }.Start();
        }

        public void Add(T item)
        {
            lock (_mutex)
            {
                _isDone = false;
                _queue.Enqueue(item);
                _resetEvent.Set();
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (_mutex)
                {
                    return _queue.Count == 0;
                }
            }
        }

        public bool IsDone
        {
            get
            {
                return this.IsEmpty && _isDone;
            }
        }

        private void ThreadHandle()
        {
            while (true)
            {
                _resetEvent.WaitOne();
                while (true)
                {
                    T item;
                    lock (_mutex)
                    {
                        if (this.IsEmpty) break;
                        item = this.Dequeue();
                    }
                    try
                    {
                        this.DoWork(item);
                        _isDone = true;
                    }
                    catch (Exception ex)
                    {
                        this.RecordLog(ex);
                    }
                }
            }
        }

        protected virtual T Dequeue()
        {
            return _queue.Dequeue();
        }

        public abstract void DoWork(T item);

        public abstract void RecordLog(Exception ex);

    }

    public abstract class PoolBase<T>
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        protected PoolBase() { }

        public void Add(T item)
        {
            _queue.Enqueue(item);
        }

        protected T Get(Func<T> factory, Action<T> clearAction)
        {
            T result;
            if (_queue.TryDequeue(out result))
            {
                clearAction(result);
                return result;
            }
            return factory();
        }

    }

}
