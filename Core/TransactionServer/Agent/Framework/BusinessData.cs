using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Diagnostics;
using iExchange.Common;
using log4net;
using Core.TransactionServer.Agent.Settings;
using System.Xml.Linq;
using Protocal;
using Protocal.Physical;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.BinaryOption;

namespace Core.TransactionServer.Agent.Framework
{
    public enum ChangeStatus
    {
        None,
        Added,
        Modified,
        Deleted
    }

    public abstract class BusinessData
    {
        protected sealed class EqualCache<T2>
        {
            internal static Func<T2, T2, bool> Equal;
        }
        private BusinessData _parent;
        private ChangeStatus _status;
        protected string _name;

        static BusinessData()
        {
            EqualCache<Price>.Equal = (s1, s2) => s1 == s2;
            EqualCache<string>.Equal = (s1, s2) => s1 == s2;

            EqualCache<long>.Equal = (s1, s2) => s1 == s2;

            EqualCache<bool>.Equal = (s1, s2) => s1 == s2;
            EqualCache<bool?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<decimal>.Equal = (s1, s2) => s1 == s2;
            EqualCache<decimal?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<int>.Equal = (s1, s2) => s1 == s2;
            EqualCache<int?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<Guid>.Equal = (s1, s2) => s1 == s2;
            EqualCache<Guid?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<DateTime>.Equal = (s1, s2) => s1 == s2;
            EqualCache<DateTime?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<InstalmentFrequence>.Equal = (s1, s2) => s1 == s2;

            EqualCache<PhysicalTradeSide>.Equal = (s1, s2) => s1 == s2;

            EqualCache<CancelReason>.Equal = (s1, s2) => s1 == s2;
            EqualCache<CancelReason?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<BillType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<BillType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<BillOwnerType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<BillOwnerType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<AlertLevel>.Equal = (s1, s2) => s1 == s2;
            EqualCache<AlertLevel?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<AccountType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<AccountType?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<DeliveryRequestStatus>.Equal = (s1, s2) => s1 == s2;
            EqualCache<DeliveryRequestStatus?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<PhysicalType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<PhysicalType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<InstrumentCategory>.Equal = (s1, s2) => s1 == s2;
            EqualCache<InstrumentCategory?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<ExpireType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<ExpireType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<TransactionPhase>.Equal = (s1, s2) => s1 == s2;
            EqualCache<TransactionPhase?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<OrderType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<OrderType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<TransactionSubType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<TransactionSubType?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<TransactionType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<TransactionType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<RecalculateRateType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<RecalculateRateType?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<DownPaymentBasis>.Equal = (s1, s2) => s1 == s2;
            EqualCache<DownPaymentBasis?>.Equal = (s1, s2) => s1 == s2;


            EqualCache<InstalmentType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<InstalmentType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<TradeOption>.Equal = (s1, s2) => s1 == s2;
            EqualCache<TradeOption?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<OrderPhase>.Equal = (s1, s2) => s1 == s2;
            EqualCache<OrderPhase?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<PlacePhase>.Equal = (s1, s2) => s1 == s2;
            EqualCache<PlacePhase?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<OrderHitStatus>.Equal = (s1, s2) => s1 == s2;
            EqualCache<OrderHitStatus?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<AppType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<AppType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<AccountType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<AccountType?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<BetResult>.Equal = (s1, s2) => s1 == s2;
            EqualCache<BetResult?>.Equal = (s1, s2) => s1 == s2;

            EqualCache<Reset.ResetBillType>.Equal = (s1, s2) => s1 == s2;
            EqualCache<Reset.ResetBillType?>.Equal = (s1, s2) => s1 == s2;
        }

        protected BusinessData(string name, BusinessData parent)
        {
            _name = name;
            this.InitializeParent(parent);
            _status = ChangeStatus.Modified;
        }

        internal bool IsAttribute { get; set; }

        private void InitializeParent(BusinessData parent)
        {
            if (parent == null) return;
            this.Parent = parent;
            var businessRecord = parent as BusinessRecord;
            if (businessRecord != null)
            {
                businessRecord.AddChild(this);
            }
        }

        public BusinessData Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public ChangeStatus Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    if (this.ShouldUpdateParentStatus())
                    {
                        this.Parent.Status = ChangeStatus.Modified;
                    }
                }
            }
        }

        private bool ShouldUpdateParentStatus()
        {
            return _status != ChangeStatus.None && this.Parent != null && this.Parent.Status == ChangeStatus.None;
        }

        public virtual void AcceptChanges()
        {
            throw new NotImplementedException();
        }

        public virtual void RejectChanges()
        {
            throw new NotImplementedException();
        }


        public void WriteXml(StringBuilder xmlTag, Func<BusinessData, bool> writeForcibly)
        {
            if (this.NeedWriteXml(writeForcibly))
            {
                this.InnerWriteXml(xmlTag, writeForcibly);
            }
        }

        protected virtual bool NeedWriteXml(Func<BusinessData, bool> writeForcibly)
        {
            return writeForcibly(this) || this.Status != ChangeStatus.None;
        }

        protected abstract void InnerWriteXml(StringBuilder xmlTag, Func<BusinessData, bool> writeForcibly);

    }

    public enum PermissionFeature
    {
        /// <summary>
        /// 可以设置新值，但不会改变ChangeStatus，所以也不支持AcceptChanges/RejectChanges操作
        /// </summary>
        Dumb,
        /// <summary>
        /// 可以设置新值，由于设置新值时，会改变ChangeStatus，所以也支持AcceptChanges/RejectChanges操作
        /// </summary>
        Sound,
        /// <summary>
        /// 不可以设置新值
        /// </summary>
        ReadOnly,
        /// <summary>
        /// 不可以设置新值，但任何情况下调用WriteXml，其内容都会写入到输出的Xml中
        /// </summary>
        Volatile,
        /// <summary>
        ///值会随着parent的某些值实时变化 
        /// </summary>
        Key,

        AlwaysSound
    }

    public enum OperationType
    {
        None,
        AsNewRecord
    }

    public abstract class BusinessItem<T> : BusinessData
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BusinessItem<T>));
        private class Cache<T2>
        {
            internal static Func<T2, string> Get;
        }

        protected PermissionFeature _feature;
        protected T _value;

        static BusinessItem()
        {
            Cache<Price>.Get = s => s == null ? null : s.ToString();
            Cache<string>.Get = s => s;
            Cache<long>.Get = s => s.ToString();
            Cache<bool>.Get = s => s.ToString();
            Cache<bool?>.Get = s => s == null ? null : s.ToString();
            Cache<decimal>.Get = s => s.ToString();
            Cache<decimal?>.Get = s => s == null ? null : s.Value.ToString();
            Cache<int>.Get = s => s.ToString();
            Cache<int?>.Get = s => s == null ? null : s.ToString();
            Cache<Guid>.Get = s => s.ToString();
            Cache<Guid?>.Get = s => s.HasValue ? s.ToString() : string.Empty;
            Cache<DateTime>.Get = dt => dt.ToYYYY_MM_DDHH_MM_SSFormat();
            Cache<DateTime?>.Get = dt => dt.HasValue ? dt.Value.ToYYYY_MM_DDHH_MM_SSFormat() : string.Empty;
            Cache<InstalmentFrequence>.Get = frequence => ((int)frequence).ToString();
            Cache<PhysicalTradeSide>.Get = tradeSide => ((int)tradeSide).ToString();

            Cache<CancelReason>.Get = reason => ((int)reason).ToString();
            Cache<CancelReason?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<BillType>.Get = s => ((int)s).ToString();
            Cache<BillType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<BillOwnerType>.Get = s => ((int)s).ToString();
            Cache<BillOwnerType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<AlertLevel>.Get = s => ((int)s).ToString();
            Cache<AlertLevel?>.Get = s => s == null ? null : ((int)s).ToString();


            Cache<DeliveryRequestStatus>.Get = s => ((int)s).ToString();
            Cache<DeliveryRequestStatus?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<PhysicalType>.Get = s => ((int)s).ToString();
            Cache<PhysicalType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<InstrumentCategory>.Get = s => ((int)s).ToString();
            Cache<InstrumentCategory?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<ExpireType>.Get = s => ((int)s).ToString();
            Cache<ExpireType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<TransactionPhase>.Get = s => ((int)s).ToString();
            Cache<TransactionPhase?>.Get = s => s == null ? null : ((int)s).ToString();


            Cache<OrderType>.Get = s => ((int)s).ToString();
            Cache<OrderType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<TransactionSubType>.Get = s => ((int)s).ToString();
            Cache<TransactionSubType?>.Get = s => s == null ? null : ((int)s).ToString();


            Cache<TransactionType>.Get = s => ((int)s).ToString();
            Cache<TransactionType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<RecalculateRateType>.Get = s => ((int)s).ToString();
            Cache<RecalculateRateType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<DownPaymentBasis>.Get = s => ((int)s).ToString();
            Cache<DownPaymentBasis?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<InstalmentType>.Get = s => ((int)s).ToString();
            Cache<InstalmentType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<TradeOption>.Get = s => ((int)s).ToString();
            Cache<TradeOption?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<OrderPhase>.Get = s => ((int)s).ToString();
            Cache<OrderPhase?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<PlacePhase>.Get = s => ((int)s).ToString();
            Cache<PlacePhase?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<OrderHitStatus>.Get = s => ((int)s).ToString();
            Cache<OrderHitStatus?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<AppType>.Get = s => ((int)s).ToString();
            Cache<AppType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<AccountType>.Get = s => ((int)s).ToString();
            Cache<AccountType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<BetResult>.Get = s => ((int)s).ToString();
            Cache<BetResult?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<Reset.ResetBillType>.Get = s => ((int)s).ToString();
            Cache<Reset.ResetBillType?>.Get = s => s == null ? null : ((int)s).ToString();

            Cache<long>.Get = s => s.ToString();
            Cache<long?>.Get = s => s == null ? null : s.ToString();

        }

        protected BusinessItem(string name, T value, BusinessData parent, PermissionFeature feature)
            : base(name, parent)
        {
            this._feature = feature;
            this._value = value;
            this.IsAttribute = true;
        }

        public PermissionFeature Feature
        {
            get
            {
                return _feature;
            }
        }

        internal virtual T Value
        {
            get { return this._value; }
        }

        internal T PlainValue
        {
            get { return _value; }
        }

        public abstract void SetValue(T value);

        protected override bool NeedWriteXml(Func<BusinessData, bool> writeForcibly)
        {
            return base.NeedWriteXml(writeForcibly) || this._feature == PermissionFeature.Key;
        }

        protected override void InnerWriteXml(StringBuilder xmlTag, Func<BusinessData, bool> writeForcibly)
        {
            if (this.Value != null)
            {
                xmlTag.AppendFormat(@" {0} = ""{1}""", _name, this.ToXmlString());
            }
        }

        internal string ToXmlString()
        {
            string result = string.Empty;
            try
            {
                result = Cache<T>.Get(this.Value);
            }
            catch (Exception ex)
            {
                string msg = string.Format("type = {0} can't be convert value = {1}, error = {2}", typeof(T), this.Value, ex);
                Logger.Error(msg);
            }
            return result;
        }

        public override void RejectChanges()
        {
            this.Status = ChangeStatus.None;
        }

        public override void AcceptChanges()
        {
            this.Status = ChangeStatus.None;
        }
    }


    internal sealed class DumpBusinessItem<T> : BusinessItem<T>
    {
        private T _oldValue;

        internal DumpBusinessItem(string name, T value, BusinessData parent)
            : base(name, value, parent, PermissionFeature.Dumb)
        {
            _oldValue = default(T);
        }

        public override void SetValue(T value)
        {
            _oldValue = _value;
            _value = value;
        }

        public override void RejectChanges()
        {
            _value = _oldValue;
            _oldValue = default(T);
        }
    }


    internal abstract class SoundBusinessItemBase<T> : BusinessItem<T>
    {
        protected T _oldValue;

        protected SoundBusinessItemBase(string name, T value, BusinessData parent, PermissionFeature feature)
            : base(name, value, parent, feature)
        {
        }


        public override void AcceptChanges()
        {
            if (this.Status != ChangeStatus.None)
            {
                this._oldValue = default(T);
                this.Status = ChangeStatus.None;
            }
        }

        public override void RejectChanges()
        {
            if (this.Status != ChangeStatus.None)
            {
                _value = this._oldValue;
                this._oldValue = default(T);
                this.Status = ChangeStatus.None;
            }
        }

        protected void InnerSetValue(T value)
        {
            this._oldValue = this.Value;
            _value = value;
            this.Status = ChangeStatus.Modified;
        }

    }



    internal class TransactionalBusinessItem<T> : SoundBusinessItemBase<T>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionalBusinessItem<T>));


        internal TransactionalBusinessItem(string name, T value, BusinessData parent)
            : base(name, value, parent, PermissionFeature.Sound)
        {
        }

        public override void SetValue(T value)
        {
            try
            {
                if (!EqualCache<T>.Equal(this.Value, value))
                {
                    this.InnerSetValue(value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    internal sealed class AlwaysSoundItem<T> : SoundBusinessItemBase<T>
    {
        internal AlwaysSoundItem(string name, T value, BusinessData parent)
            : base(name, value, parent, PermissionFeature.AlwaysSound)
        {
        }

        public override void SetValue(T value)
        {
            this.InnerSetValue(value);
        }
    }


    internal sealed class VolatileBusinessItem<T> : BusinessItem<T>
    {
        private Func<T> _producer;
        internal VolatileBusinessItem(string name, Func<T> producer, BusinessData parent)
            : base(name, default(T), parent, PermissionFeature.Volatile)
        {
            _producer = producer;
        }

        internal override T Value
        {
            get
            {
                return _producer();
            }
        }

        public override void SetValue(T value)
        {
            throw new NotSupportedException("Volatile busiessItem can't be set value");
        }
    }


    internal class ReadOnlyBusinessItem<T> : BusinessItem<T>
    {
        internal ReadOnlyBusinessItem(string name, T value, BusinessData parent, bool isKey)
            : base(name, value, parent, (isKey ? PermissionFeature.Key : PermissionFeature.ReadOnly))
        {
        }

        public override void SetValue(T value)
        {
            throw new NotSupportedException("StableChangeItem can't be set value");
        }
    }


    public class BusinessRecord : BusinessData
    {
        private List<BusinessData> _items;
        private BusinessItem<bool> _isDeleted;

        public BusinessRecord(string name, int capacity)
            : base(name, null)
        {
            this._items = new List<BusinessData>(capacity);
            _isDeleted = BusinessItemFactory.Create("IsDeleted", false, PermissionFeature.Sound, this);
            this.IsAttribute = false;
        }

        public bool IsDeleted
        {
            get { return _isDeleted.Value; }
            set { _isDeleted.SetValue(value); }
        }


        internal void AddChild(BusinessData child)
        {
            _items.Add(child);
            child.Parent = this;
        }

        protected override void InnerWriteXml(StringBuilder xmlTag, Func<BusinessData, bool> writeForcibly)
        {
            StringBuilder sb = StringBuilderPool.Default.Get();
            sb.AppendLine();
            sb.AppendFormat("<{0} ", _name);
            this.WriteAttrs(sb, writeForcibly);
            sb.Append(" >");
            this.WriteInnerContent(sb, writeForcibly);
            sb.AppendFormat("</{0}>", _name);
            if (xmlTag != null)
            {
                xmlTag.Append(sb.ToString());
            }
            StringBuilderPool.Default.Add(sb);
        }

        private void WriteAttrs(StringBuilder sb, Func<BusinessData, bool> writeForcibly)
        {
            foreach (var item in this._items)
            {
                if (item.IsAttribute)
                {
                    item.WriteXml(sb, m => writeForcibly(m) || this.Status == Framework.ChangeStatus.Added);
                }
            }
        }

        private void WriteInnerContent(StringBuilder sb, Func<BusinessData, bool> writeForcibly)
        {
            foreach (var item in this._items)
            {
                if (!item.IsAttribute)
                {
                    item.WriteXml(sb, m => writeForcibly(m) || this.Status == Framework.ChangeStatus.Added);
                }
            }
        }


        public override void AcceptChanges()
        {
            if (this.Status != ChangeStatus.None)
            {
                foreach (var item in this._items)
                {
                    item.AcceptChanges();
                }
                this.Status = ChangeStatus.None;
            }
        }

        public override void RejectChanges()
        {
            if (this.Status != ChangeStatus.None)
            {
                foreach (var item in this._items)
                {
                    item.RejectChanges();
                }
                this.Status = ChangeStatus.None;
            }
        }

        internal void ChangeToAdded()
        {
            this.Status = ChangeStatus.Added;
        }

        internal virtual void ChangeToDeleted()
        {
            this.Status = ChangeStatus.Deleted;
        }
    }

    public class BillBusinessRecord : BusinessRecord
    {
        private BusinessRecordList<Bill> _bills;

        public BillBusinessRecord(string name, int capacity)
            : base(name, capacity)
        {
            _bills = new BusinessRecordList<Bill>("Bills", this);
        }

        internal IEnumerable<Bill> Bills
        {
            get { return _bills.GetValues(); }
        }


        internal void AddBill(Bill bill, OperationType operationType = OperationType.AsNewRecord)
        {
            _bills.AddItem(bill, operationType);
        }

        internal decimal GetValuedBill(BillType billType)
        {
            return this.CalculateValuedBillCommon(billType, true);
        }

        internal decimal GetNotValuedBill(BillType billType)
        {
            return this.CalculateValuedBillCommon(billType, false);
        }

        internal decimal CalculateValuedBillCommon(BillType billType, bool isValued)
        {
            decimal result = 0m;
            foreach (var eachBill in _bills.GetValues())
            {
                if (eachBill.Type != billType) continue;
                if (eachBill is PLBill)
                {
                    PLBill plBill = (PLBill)eachBill;
                    if (plBill.IsValued == isValued)
                    {
                        result += eachBill.Value;
                    }
                }
            }
            return result;
        }


        internal decimal GetBillValue(BillType billType)
        {
            decimal result = 0m;
            foreach (var eachBill in _bills.GetValues())
            {
                if (eachBill.Type == billType)
                {
                    result += eachBill.Value;
                }
            }
            return result;
        }

        internal decimal SumBillsForBalance()
        {
            decimal result = 0m;
            foreach (var eachBill in _bills.GetValues())
            {
                PLBill plBill = eachBill as PLBill;
                if (plBill != null && !plBill.IsValued) continue;
                result += eachBill.Value;
            }
            return result;
        }

    }



}