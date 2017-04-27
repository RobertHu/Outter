using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Framework
{
    internal enum BillOwnerType
    {
        None,
        Order,
        OrderRelation,
        Account
    }

    public sealed class PLBill : Bill
    {
        private BusinessItem<DateTime?> _valueTime;
        private BusinessItem<bool> _isValued;
        internal PLBill(Guid accountId, Guid currencyId, decimal value, BillType type, BillOwnerType ownerType, DateTime? valueTime, bool isValued, DateTime updateTime)
            : base(accountId, currencyId, value, type, ownerType, updateTime)
        {
            this.Initialize(valueTime, isValued);
        }

        internal PLBill(IDBRow  dr)
            : base(dr)
        {
            DateTime? valueTime = null;
            if (dr["ValueTime"] != DBNull.Value)
            {
                valueTime = (DateTime)dr["ValueTime"];
            }
            bool isValued = (bool)dr["IsValued"];
            this.Initialize(valueTime, isValued);
        }

        private void Initialize(DateTime? valueTime, bool isValued)
        {
            _valueTime = BusinessItemFactory.Create("ValueTime", valueTime, PermissionFeature.Sound, this);
            _isValued = BusinessItemFactory.Create("IsValued", isValued, PermissionFeature.Sound, this);
        }


        internal bool IsValued { get { return _isValued.Value; } }

    }


    public class Bill : BusinessRecord
    {
        private const int ItemCapacityFactor = 4;
        private BusinessItem<Guid> _id;
        private BusinessItem<decimal> _value;
        private BusinessItem<BillType> _type;
        private BusinessItem<Guid> _accountId;
        private BusinessItem<BillOwnerType> _ownerType;
        private BusinessItem<DateTime> _updateTime;
        private BusinessItem<Guid> _currencyId;

        internal static Bill CreateForOrder(Guid accountId, Guid currencyId, decimal value, BillType type)
        {
            return new Bill(accountId, currencyId, value, type, BillOwnerType.Order);
        }

        internal static Bill CreateForAccount(Guid accountId, Guid currencyId, decimal value, BillType type)
        {
            return new Bill(accountId, currencyId, value, type, BillOwnerType.Account);
        }

        internal Bill(Guid accountId, Guid currencyId, decimal value, BillType type, BillOwnerType ownerType)
            : this(Guid.NewGuid(), accountId, currencyId, value, type, ownerType, DateTime.Now)
        {
        }

        internal Bill(Guid accountId, Guid currencyId, decimal value, BillType type, BillOwnerType ownerType, DateTime updateTime)
            : this(Guid.NewGuid(), accountId, currencyId, value, type, ownerType, updateTime)
        {
        }


        internal Bill(IDBRow  dr)
            : base("Bill", ItemCapacityFactor)
        {
            Guid id = (Guid)dr["ID"];
            Guid accountId = (Guid)dr["AccountID"];
            decimal value = (decimal)dr["Value"];
            BillType type = (BillType)dr.GetColumn<int>("Type");
            BillOwnerType ownerType = (BillOwnerType)dr.GetColumn<int>("OwnerType");
            DateTime updateTime = dr.GetColumn<DateTime>("UpdateTime");
            Guid currencyId = dr.GetColumn<Guid>("CurrencyID");
            this.Initialize(id, accountId, currencyId, value, type, ownerType, updateTime);
        }

        private Bill(Guid id, Guid accountId, Guid currencyId, decimal value, BillType type, BillOwnerType ownerType, DateTime updateTime)
            : base("Bill", ItemCapacityFactor)
        {
            this.Initialize(id, accountId, currencyId, value, type, ownerType, updateTime);
        }

        private void Initialize(Guid id, Guid accountId, Guid currencyId, decimal value, BillType type, BillOwnerType ownerType, DateTime updateTime)
        {
            _id = this.CreateBusinessItem("ID", id, PermissionFeature.Key);
            _accountId = this.CreateBusinessItem("AccountID", accountId);
            _value = this.CreateBusinessItem("Value", value, PermissionFeature.Sound);
            _type = this.CreateBusinessItem("Type", type);
            _ownerType = this.CreateBusinessItem("OwnerType", ownerType);
            _updateTime = this.CreateBusinessItem("UpdateTime", updateTime, PermissionFeature.Sound);
            _currencyId = this.CreateBusinessItem("CurrencyID", currencyId, PermissionFeature.ReadOnly);
        }


        internal Guid Id
        {
            get { return _id.Value; }
        }

        internal Guid AccountId
        {
            get { return _accountId.Value; }
        }

        internal Guid CurrencyID
        {
            get { return _currencyId.Value; }
        }

        internal BillOwnerType OwnerType
        {
            get { return _ownerType.Value; }
        }


        protected BusinessItem<T> CreateBusinessItem<T>(string name, T value, PermissionFeature feature = PermissionFeature.ReadOnly)
        {
            return BusinessItemFactory.Create(name, value, feature, this);
        }

        internal decimal Value
        {
            get
            {
                return _value.Value;
            }
            set
            {
                _value.SetValue(value);
            }
        }

        internal BillType Type
        {
            get
            {
                return _type.Value;
            }
        }

        internal DateTime UpdateTime
        {
            get { return _updateTime.Value; }
            private set { _updateTime.SetValue(value); }
        }

        public static decimal operator +(Bill left, Bill right)
        {
            return left.Value + right.Value;
        }

        public static decimal operator +(decimal left, Bill right)
        {
            return left + right.Value;
        }

        public static decimal operator +(Bill left, decimal right)
        {
            return left.Value + right;
        }

        public static Bill operator -(Bill bill)
        {
            return new Bill(Guid.NewGuid(), bill.AccountId, bill.CurrencyID, -bill.Value, bill.Type, bill.OwnerType, bill.UpdateTime);
        }

    }

}
