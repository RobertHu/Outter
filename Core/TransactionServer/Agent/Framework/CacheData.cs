using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Framework
{
    public interface IClearable
    {
        void Clear();
        void AddChild(IClearable item);
    }

    /// <summary>
    /// This class is not thread safe
    /// </summary>
    /// <typeparam name="T">Type of the data to cache</typeparam>
    public sealed class CacheData<T> : IClearable
    {
        private T value;
        private volatile bool dataCached;
        private Func<T> dataFactory;

        private List<IClearable> children;

        public CacheData(Func<T> dataFactory, IClearable parent = null)
        {
            this.dataFactory = dataFactory;
            if (parent != null) parent.AddChild(this);
        }

        public T Value
        {
            get
            {
                if (!this.dataCached)
                {
                    this.value = this.dataFactory();
                    this.dataCached = true;
                }
                return this.value;
            }
        }

        public void Clear()
        {
            this.dataCached = false;
            if (this.children != null)
            {
                foreach (IClearable item in this.children)
                {
                    item.Clear();
                }
            }
        }

        void IClearable.AddChild(IClearable child)
        {
            if (this.children == null)
            {
                this.children = new List<IClearable>();
            }
            this.children.Add(child);
        }
    }
}