using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Framework
{
    public interface IKeyProvider<TKey>
    {
        TKey GetKey();
    }

    public abstract class BusinessRecordCollection<T> : BusinessData
        where T : BusinessRecord
    {
        private List<T> _xmlRecords;
        private CacheData<List<T>> _cache;

        public BusinessRecordCollection(string name, BusinessData parent)
            : base(name, parent)
        {
            _xmlRecords = new List<T>(10);
            _cache = new CacheData<List<T>>(() => new List<T>(this.InnerGetValues()));
        }
        public List<T> GetValues()
        {
            return _cache.Value;
        }

        protected abstract IEnumerable<T> InnerGetValues();

        public abstract int Count { get; }

        protected virtual void DoRemove(T value)
        {
            this.RemoveOrderDayHistory(value);
            _cache.Clear();
        }

        public virtual void AddItem(T child, OperationType operationType)
        {
            _xmlRecords.Add(child);
            child.Parent = this;
            if (operationType == OperationType.AsNewRecord)
            {
                child.ChangeToAdded();
            }
            _cache.Clear();
        }

        public virtual void RemoveItem(T child)
        {
            child.ChangeToDeleted();
            child.IsDeleted = true;
        }

        public virtual void Clear()
        {
            _xmlRecords.Clear();
            _cache.Clear();
        }

        public override void AcceptChanges()
        {
            this.Submit(true);
            this.Status = ChangeStatus.None;
        }

        public override void RejectChanges()
        {
            this.Submit(false);
            this.Status = ChangeStatus.None;
        }

        private void Submit(bool isCommit)
        {
            List<T> toBeRemovedItems = null;
            foreach (var eachItem in this.GetValues())
            {
                bool shouldItemBeRemoved;
                this.SubmitIndividualItem(eachItem, isCommit, out shouldItemBeRemoved);
                if (shouldItemBeRemoved)
                {
                    if (toBeRemovedItems == null)
                    {
                        toBeRemovedItems = new List<T>();
                    }
                    toBeRemovedItems.Add(eachItem);
                }
            }

            if (toBeRemovedItems != null)
            {
                foreach (var eachRemovedItem in toBeRemovedItems)
                {
                    this.DoRemove(eachRemovedItem);
                }
            }
        }

        private void SubmitIndividualItem(T item, bool isCommit, out bool shouldRemoved)
        {
            shouldRemoved = false;
            if (isCommit)
            {
                if (item.Status == ChangeStatus.Deleted)
                {
                    shouldRemoved = true;
                    _xmlRecords.Remove(item);
                }
                item.AcceptChanges();
            }
            else
            {
                if (item.Status == ChangeStatus.Added)
                {
                    shouldRemoved = true;
                    _xmlRecords.Remove(item);
                }
                item.RejectChanges();
            }
        }

        protected override void InnerWriteXml(StringBuilder xmlTag, Func<BusinessData, bool> forcely)
        {
            StringBuilder sb = StringBuilderPool.Default.Get();
            sb.AppendLine();
            sb.AppendFormat("<{0}>", _name);
            foreach (T record in _xmlRecords)
            {
                record.WriteXml(sb, forcely);
            }
            sb.AppendLine();
            sb.AppendFormat("</{0}>", _name);
            if (xmlTag != null)
            {
                xmlTag.Append(sb.ToString());
            }
            StringBuilderPool.Default.Add(sb);
        }

        private void RemoveOrderDayHistory(T value)
        {
            if (typeof(T) == typeof(Order))
            {
                var order = (Order)((object)value);
                Reset.ResetManager.Default.RemoveOrderDayHistorys(order.Id);
            }
        }

    }


    public sealed class BusinessRecordList<T> : BusinessRecordCollection<T>
        where T : BusinessRecord
    {
        private List<T> _records;

        public BusinessRecordList(string name, BusinessData parent, int capacity = 0)
            : base(name, parent)
        {
            _records = new List<T>(capacity);
        }

        public T this[int index]
        {
            get
            {
                return _records[index];
            }
        }


        public override void AddItem(T item, OperationType operationType)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            base.AddItem(item, operationType);
            _records.Add(item);
        }

        public override void Clear()
        {
            base.Clear();
            _records.Clear();
        }

        protected override void DoRemove(T value)
        {
            base.DoRemove(value);
            _records.Remove(value);
        }


        public override int Count
        {
            get { return _records.Count; }
        }

        protected override IEnumerable<T> InnerGetValues()
        {
            return _records;
        }
    }

    public class BusinessRecordDictionary<TKey, TValue> : BusinessRecordCollection<TValue>
         where TValue : BusinessRecord, IKeyProvider<TKey>
    {
        private const int DEFAULT_LOAD_FACTOR = 5;
        private Dictionary<TKey, TValue> _items;
        public BusinessRecordDictionary(string name, BusinessData parent)
            : this(name, parent, DEFAULT_LOAD_FACTOR)
        {
        }

        public BusinessRecordDictionary(string name, BusinessData parent, int capacity)
            : base(name, parent)
        {
            _items = new Dictionary<TKey, TValue>(capacity);
        }

        public override void Clear()
        {
            base.Clear();
            _items.Clear();
        }


        public override void AddItem(TValue child, OperationType operationType)
        {
            var key = ((IKeyProvider<TKey>)child).GetKey();
            if (_items.ContainsKey(key))
            {
                throw new ArgumentException(string.Format("AddItem  key = {0} already exist", key));
            }
            base.AddItem(child, operationType);
            _items.Add(key, child);
        }


        public TValue this[TKey key]
        {
            get
            {
                return _items[key];
            }
        }

        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }


        public bool TryGetValue(TKey key, out TValue value)
        {
            return _items.TryGetValue(key, out value);
        }


        public override int Count
        {
            get { return _items.Count; }
        }

        protected override void DoRemove(TValue value)
        {
            base.DoRemove(value);
            _items.Remove(((IKeyProvider<TKey>)value).GetKey());
        }

        protected override IEnumerable<TValue> InnerGetValues()
        {
            return _items.Values;
        }
    }
}