using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using iExchange.Common;
using System.Data.SqlClient;
using System.Data;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.AccountClass;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.Util.Code;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Physical.Delivery
{
    internal sealed class DeliveryRequest : BusinessRecord, IKeyProvider<Guid>
    {
        private sealed class DeliveryRequestConstructParams
        {
            internal Guid Id { get; set; }
            internal Guid AccountId { get; set; }
            internal Guid InstrumentId { get; set; }
            internal string Code { get; set; }
            internal string PrintingCode { get; set; }
            internal string Ask { get; set; }
            internal string Bid { get; set; }
            internal DeliveryRequestStatus DeliveryRequestStatus { get; set; }
            internal decimal RequireQuantity { get; set; }
            internal decimal RequireLot { get; set; }
            internal DateTime SubmitTime { get; set; }
            internal Guid SubmitorId { get; set; }
            internal DateTime? DeliveryTime { get; set; }
            internal decimal Charge { get; set; }
            internal Guid? ChargeCurrencyId { get; set; }
            internal Guid? DeliveryAddressId { get; set; }
        }

        private const int DEFAULT_ITEMS_FACTOR = 12;
        private Account _owner;
        private BusinessItem<Guid> _id;
        private BusinessItem<Guid> _accountId;
        private BusinessItem<Guid> _instrumentId;
        private BusinessItem<string> _code;
        private BusinessItem<string> _printingCode;
        private BusinessItem<decimal> _requireQuantity;
        private BusinessItem<DateTime> _submitTime;
        private BusinessItem<DateTime?> _availableDeliveryTime;
        private BusinessItem<Guid> _submitorId;
        private BusinessItem<DateTime?> _deliveryTime;
        private BusinessItem<decimal> _requireLot;
        private BusinessItem<DeliveryRequestStatus> _deliveryRequestStatus;
        private BusinessItem<Guid?> _chargeCurrencyId;
        private BusinessItem<decimal> _charge;
        private BusinessItem<Guid?> _deliveryAddressId;
        private BusinessItem<string> _ask;
        private BusinessItem<string> _bid;
        private BusinessRecordDictionary<Guid,DeliveryRequestOrderRelation> _deliveryRequestOrderRelations;
        private BusinessRecordList<DeliveryRequestSpecification> _specifications;

        internal Guid Id
        {
            get { return this._id.Value; }
        }

        internal Account Owner
        {
            get { return this._owner; }
        }

        internal Guid AccountId
        {
            get { return this._accountId.Value; }
        }

        internal Guid InstrumentId
        {
            get { return this._instrumentId.Value; }
        }

        internal string Code
        {
            get { return this._code.Value; }
        }

        internal decimal RequireQuantity
        {
            get { return this._requireQuantity.Value; }
        }

        internal DateTime SubmitTime
        {
            get { return this._submitTime.Value; }
        }

        internal DateTime? DeliveryTime
        {
            get { return this._deliveryTime.Value; }
        }

        internal DateTime? AvailableDeliveryTime
        {
            get { return _availableDeliveryTime.Value; }
            set { _availableDeliveryTime.SetValue(value); }
        }

        internal decimal RequireLot
        {
            get { return this._requireLot.Value; }
        }

        internal DeliveryRequestStatus DeliveryRequestStatus
        {
            get { return this._deliveryRequestStatus.Value; }
            set { this._deliveryRequestStatus.SetValue(value); }
        }

        internal Guid? ChargeCurrencyId
        {
            get { return this._chargeCurrencyId.Value; }
        }

        internal decimal Charge
        {
            get { return this._charge.Value; }
        }

        internal Guid? DeliveryAddressId
        {
            get { return this._deliveryAddressId.Value; }
        }

        internal String Ask
        {
            get { return this._ask.Value; }
        }

        internal String Bid
        {
            get { return this._bid.Value; }
        }

        internal IEnumerable<DeliveryRequestOrderRelation> DeliveryRequestOrderRelations
        {
            get { return _deliveryRequestOrderRelations.GetValues(); }
        }


        internal bool TryGet(Guid openOrderId, out DeliveryRequestOrderRelation relation)
        {
            return this._deliveryRequestOrderRelations.TryGetValue(openOrderId, out relation);
        }

        internal DeliveryRequest(Account owner, IDBRow dataRow)
            : base(BusinessRecordNames.DeliveryRequest, DEFAULT_ITEMS_FACTOR)
        {
            this._owner = owner;
            var constructParams = this.Parse(dataRow);
            this.Initialize(constructParams);
            owner.AddDeliveryRequest(this, OperationType.None);
        }

        internal DeliveryRequest(Account owner, Protocal.Physical.DeliveryRequestData node)
            : base(BusinessRecordNames.DeliveryRequest, DEFAULT_ITEMS_FACTOR)
        {
            this._owner = owner;
            var constructParams = this.Parse(node);
            this.Initialize(constructParams);
            owner.AddDeliveryRequest(this, OperationType.AsNewRecord);
        }


        private DeliveryRequestConstructParams Parse(IDBRow  dr)
        {
            DeliveryRequestConstructParams result = new DeliveryRequestConstructParams();
            result.Id = (Guid)dr["Id"];
            result.AccountId = (Guid)dr["AccountId"];
            result.InstrumentId = (Guid)dr["InstrumentId"];
            result.Code = (string)dr["Code"];
            result.PrintingCode = (string)dr["PrintingCode"];
            result.Ask = (string)dr["Ask"];
            result.Bid = (string)dr["Bid"];
            result.DeliveryRequestStatus = (DeliveryRequestStatus)((byte)dr["Status"]);
            result.RequireQuantity = (decimal)dr["RequireQuantity"];
            result.RequireLot = (decimal)dr["RequireLot"];
            result.SubmitTime = (DateTime)dr["SubmitTime"];
            result.SubmitorId = (Guid)dr["SubmitorId"];
            if (dr["DeliveryTime"] != DBNull.Value)
            {
                result.DeliveryTime = (DateTime)dr["DeliveryTime"];
            }
            return result;
        }

        private DeliveryRequestConstructParams Parse(Protocal.Physical.DeliveryRequestData node)
        {
            var codeAndPritingCode = DeliveryCodeGenerator.Default.Create();
            DeliveryRequestConstructParams result = new DeliveryRequestConstructParams();
            result.Id = node.Id;
            result.Code = codeAndPritingCode.Item1;
            result.PrintingCode = codeAndPritingCode.Item2;
            result.AccountId = node.AccountId;
            result.InstrumentId = node.InstrumentId;
            result.RequireQuantity = node.RequireQuantity;
            result.RequireLot = node.RequireLot;
            result.SubmitTime = DateTime.Now;
            result.SubmitorId = node.SubmitorId;
            result.DeliveryTime = node.DeliveryTime;
            result.DeliveryRequestStatus = DeliveryRequestStatus.Accepted;
            result.Charge = node.Charge;
            result.ChargeCurrencyId = node.ChargeCurrencyId;
            result.DeliveryAddressId = node.DeliveryAddressId;
            return result;
        }

        private void Initialize(DeliveryRequestConstructParams constructParams)
        {
            _id = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Id, constructParams.Id, PermissionFeature.Key, this);
            _accountId = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.AccountId, constructParams.AccountId, PermissionFeature.ReadOnly, this);
            _instrumentId = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.InstrumentId, constructParams.InstrumentId, PermissionFeature.ReadOnly, this);
            _code = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Code, constructParams.Code, PermissionFeature.ReadOnly, this);
            _printingCode = BusinessItemFactory.Create("PrintingCode", constructParams.PrintingCode, PermissionFeature.ReadOnly, this);
            _ask = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Ask, constructParams.Ask, PermissionFeature.Sound, this);
            _bid = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Bid, constructParams.Bid, PermissionFeature.Sound, this);
            _deliveryRequestStatus = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Status, constructParams.DeliveryRequestStatus, PermissionFeature.Sound, this);
            _requireQuantity = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.RequireQuantity, constructParams.RequireQuantity, PermissionFeature.ReadOnly, this);
            _requireLot = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.RequireLot, constructParams.RequireLot, PermissionFeature.ReadOnly, this);
            _submitTime = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.SubmitTime, constructParams.SubmitTime, PermissionFeature.ReadOnly, this);
            _submitorId = BusinessItemFactory.Create("SubmitorId", constructParams.SubmitorId, PermissionFeature.ReadOnly, this);
            _deliveryTime = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.DeliveryTime, constructParams.DeliveryTime, PermissionFeature.Sound, this);
            _availableDeliveryTime = BusinessItemFactory.Create("AvailableDeliveryTime", (DateTime?)null, PermissionFeature.Sound, this);
            _charge = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.Charge, constructParams.Charge, PermissionFeature.Sound, this);
            _chargeCurrencyId = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.ChargeCurrencyId, constructParams.ChargeCurrencyId, PermissionFeature.Sound, this);
            _deliveryAddressId = BusinessItemFactory.Create(DeliveryRequestBusinessItemNames.DeliveryAddressId, constructParams.DeliveryAddressId, PermissionFeature.Sound, this);
            _deliveryRequestOrderRelations = new BusinessRecordDictionary<Guid, DeliveryRequestOrderRelation>(BusinessRecordCollectionNames.DeliveryRequestOrderRelations, this);
            _specifications = new BusinessRecordList<DeliveryRequestSpecification>("DeliveryRequestSpecifications", this, 2);
        }

        internal void AddDeliveryRequestOrderRelation(DeliveryRequestOrderRelation item, OperationType operationType)
        {
            _deliveryRequestOrderRelations.AddItem(item, operationType);
        }

        internal void AddDeliveryRequestSpecification(DeliveryRequestSpecification specification, OperationType operationType)
        {
            _specifications.AddItem(specification, operationType);
        }

        internal bool Cancel()
        {
            this.DeliveryRequestStatus = DeliveryRequestStatus.Cancelled;
            return true;
        }


        internal void InitPrice(string price)
        {
            _ask.SetValue(price);
            _bid.SetValue(price);
        }


        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.Id;
        }
    }


    internal sealed class DeliveryRequestSpecification : BusinessRecord
    {
        private BusinessItem<int> _quantity;
        private BusinessItem<decimal> _size;
        private BusinessItem<Guid> _unitId;

        internal DeliveryRequestSpecification(DeliveryRequest owner, Protocal.Physical.DeliveryRequestSpecificationData specification)
            : base("DeliveryRequestSpecification", 2)
        {
            owner.AddDeliveryRequestSpecification(this, OperationType.AsNewRecord);
            _quantity = BusinessItemFactory.Create("Quantity", specification.Quantity, PermissionFeature.ReadOnly, this);
            _size = BusinessItemFactory.Create("Size", specification.Size, PermissionFeature.ReadOnly, this);
            _unitId = BusinessItemFactory.Create("UnitId", specification.UnitId, PermissionFeature.ReadOnly, this);
        }

        internal int Quantity
        {
            get { return _quantity.Value; }
        }

        internal decimal Size
        {
            get { return _size.Value; }
        }
    }


    internal sealed class DeliveryRequestOrderRelation : BusinessRecord, IKeyProvider<Guid>
    {
        private const int DEFAULT_ITEMS_FACTOR = 4;
        private DeliveryRequest _owner;
        private BusinessItem<Guid> _deliveryRequestId;
        private BusinessItem<Guid> _openOrderId;
        private BusinessItem<decimal> _deliveryQuantity;
        private BusinessItem<decimal> _deliveryLot;

        internal DeliveryRequestOrderRelation(DeliveryRequest request, IDBRow  dataRow)
            : base(BusinessRecordNames.DeliveryRequestOrderRelation, DEFAULT_ITEMS_FACTOR)
        {
            this._owner = request;
            var key = (Guid)dataRow["OpenOrderId"];
            this._deliveryRequestId = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryRequestId, (Guid)dataRow["DeliveryRequestId"], PermissionFeature.Key, this);
            this._openOrderId = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.OpenOrderId, key, PermissionFeature.Key, this);
            this._deliveryQuantity = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryQuantity, (decimal)dataRow["DeliveryQuantity"], PermissionFeature.Dumb, this);
            this._deliveryLot = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryLot, (decimal)dataRow["DeliveryLot"], PermissionFeature.Dumb, this);
            request.AddDeliveryRequestOrderRelation(this, OperationType.None);
        }

        internal DeliveryRequestOrderRelation(DeliveryRequest request, Protocal.Physical.DeliveryRequestOrderRelationData orderRelationData)
            : base(BusinessRecordNames.DeliveryRequestOrderRelation, DEFAULT_ITEMS_FACTOR)
        {
            this._owner = request;
            var key = orderRelationData.OpenOrderId;
            this._deliveryRequestId = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryRequestId, request.Id, PermissionFeature.Key, this);
            this._openOrderId = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.OpenOrderId, key, PermissionFeature.Key, this);
            this._deliveryQuantity = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryQuantity, orderRelationData.DeliveryQuantity, PermissionFeature.Dumb, this);
            this._deliveryLot = BusinessItemFactory.Create(DeliveryRequestRelationBusinessItemNames.DeliveryLot, orderRelationData.DeliveryLot, PermissionFeature.Dumb, this);
            request.AddDeliveryRequestOrderRelation(this, OperationType.AsNewRecord);
        }

        internal Guid DeliveryRequestId
        {
            get { return this._deliveryRequestId.Value; }
        }

        internal Guid OpenOrderId
        {
            get { return this._openOrderId.Value; }
        }

        internal decimal DeliveryQuantity
        {
            get { return this._deliveryQuantity.Value; }
        }

        internal decimal DeliveryLot
        {
            get { return this._deliveryLot.Value; }
        }


        internal void LockDeliveryLot()
        {
            Order order = this._owner.Owner.GetOrder(this.OpenOrderId);
            if (order != null)
            {
                ((PhysicalOrder)order).LockForDelivery(this.DeliveryLot);
            }
        }

        internal void ReleaseDeliveryLot()
        {
            Order order = this._owner.Owner.GetOrder(this.OpenOrderId);
            if (order != null)
            {
                ((PhysicalOrder)order).LockForDelivery(this.DeliveryLot);
            }
        }

        Guid IKeyProvider<Guid>.GetKey()
        {
            return this.OpenOrderId;
        }
    }
}