using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal enum PlacePhase
    {
        None,
        Received = 1,
        VerifyFailed = 2,
        VerifySuccess = 3,
        PreCheckFailed = 4,
        PreCheckSuccess = 5,
        TryExecuteFailed = 6,
        TryExecuteSuccess = 7,
        PlaceToEngine = 8,
        PlaceAccepted = 9,
        PlaceRejected = 10,
        Canceled
    }

    public sealed class TransactionSettings : BusinessItemBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionSettings));

        private BusinessItem<bool> _placedByRiskMonitor;
        private BusinessItem<bool> _freePlacingPreCheck;
        private BusinessItem<Guid> _id;
        private BusinessItem<string> _code;
        private BusinessItem<TransactionType> _type;
        private BusinessItem<Guid> _accountId;
        private BusinessItem<Guid> _instrumentId;
        private BusinessItem<TransactionSubType> _subType;
        public BusinessItem<TransactionPhase> _phase;
        private BusinessItem<OrderType> _orderType;
        private BusinessItem<decimal> _contractSize;
        private BusinessItem<DateTime> _beginTime;
        public BusinessItem<DateTime> _endTime;
        private BusinessItem<ExpireType> _expireType;
        private BusinessItem<DateTime> _submitTime;
        private BusinessItem<DateTime?> _executeTime;
        private BusinessItem<Guid> _submitorId;
        private BusinessItem<Guid?> _approverId;
        private BusinessItem<Guid?> _sourceOrderId;
        private BusinessItem<DateTime?> _setPriceTimestamp;
        private BusinessItem<InstrumentCategory> _instrumentCategory;
        private BusinessItem<PlacePhase> _placePhase;
        private BusinessItem<string> _placeDetail;
        private BusinessItem<AppType> _appType;
        private BusinessItem<DateTime> _updateTime;
        private Account _account;
        private BusinessItem<bool> _disableAcceptLmtVariation;

        internal TransactionSettings(Transaction owner, TransactionConstructParams constructParams)
            : base(owner)
        {
            _account = owner.Owner;
            this.Parse(constructParams);
        }

        internal Guid Id
        {
            get { return _id.Value; }
        }

        internal string Code
        {
            get { return _code.Value; }
            set { _code.SetValue(value); }
        }

        internal TransactionType Type
        {
            get { return _type.Value; }
            set { _type.SetValue(value); }
        }

        internal Guid AccountId
        {
            get { return _accountId.Value; }
        }

        internal Guid InstrumentId
        {
            get { return _instrumentId.PlainValue; }
        }

        internal TransactionSubType SubType
        {
            get { return _subType.Value; }
            set { _subType.SetValue(value); }
        }

        internal TransactionPhase Phase
        {
            get { return _phase.Value; }
            set { _phase.SetValue(value); }
        }

        internal OrderType OrderType
        {
            get { return _orderType.Value; }
            set { _orderType.SetValue(value); }
        }

        internal decimal ContractSize
        {
            get { return _contractSize.Value; }
            set { _contractSize.SetValue(value); }
        }

        internal DateTime BeginTime
        {
            get { return _beginTime.Value; }
        }

        internal DateTime EndTime
        {
            get { return _endTime.Value; }
        }

        internal ExpireType ExpireType
        {
            get { return _expireType.Value; }
        }

        internal DateTime SubmitTime
        {
            get { return _submitTime.Value; }
        }

        internal DateTime? ExecuteTime
        {
            get { return _executeTime.Value; }
            set { _executeTime.SetValue(value); }
        }

        internal Guid SubmitorID
        {
            get { return _submitorId.Value; }
        }

        internal Guid? ApproverID
        {
            get { return _approverId.Value; }
            set { _approverId.SetValue(value); }
        }

        internal Guid? SourceOrderId
        {
            get { return _sourceOrderId.Value; }
            set { _sourceOrderId.SetValue(value); }
        }

        internal DateTime? SetPriceTimestamp
        {
            get { return _setPriceTimestamp.Value; }
        }

        internal InstrumentCategory InstrumentCategory
        {
            get { return _instrumentCategory.Value; }
        }

        internal PlacePhase PlacePhase
        {
            get { return _placePhase.Value; }
            set { _placePhase.SetValue(value); }
        }

        internal string PlaceDetail
        {
            get { return _placeDetail.Value; }
            set { _placeDetail.SetValue(value); }
        }

        internal bool PlacedByRiskMonitor
        {
            get { return _placedByRiskMonitor.Value; }
            set { _placedByRiskMonitor.SetValue(value); }
        }

        internal bool FreePlacingPreCheck
        {
            get { return _freePlacingPreCheck.Value; }
            set { _freePlacingPreCheck.SetValue(value); }
        }

        internal AppType AppType
        {
            get { return _appType.Value; }
        }

        internal DateTime UpdateTime
        {
            get { return _updateTime.Value; }
            set { _updateTime.SetValue(value); }
        }

        internal bool DisableAcceptLmtVariation
        {
            get { return _disableAcceptLmtVariation.Value; }
            set
            {
                _disableAcceptLmtVariation.SetValue(value);
            }
        }


        private void Parse(TransactionConstructParams constructParams)
        {
            _placedByRiskMonitor = this.CreateSoundItem(TransactionBusinessItemNames.PlacedByRiskMonitor, constructParams.PlaceByRiskMonitor);
            _freePlacingPreCheck = this.CreateSoundItem(TransactionBusinessItemNames.FreePlacingPreCheck, constructParams.FreePlacingPreCheck);
            _disableAcceptLmtVariation = this.CreateReadonlyItem("DisableLmtVariation", constructParams.DisableAcceptLmtVariation);
            _id = this.CreateItem(TransactionBusinessItemNames.Id, constructParams.Id, PermissionFeature.Key);
            _code = this.CreateSoundItem(TransactionBusinessItemNames.Code, constructParams.Code);
            _type = this.CreateSoundItem(TransactionBusinessItemNames.Type, constructParams.Type);
            _accountId = this.CreateReadonlyItem(TransactionBusinessItemNames.AccountId, _account.Id);
            _instrumentId = this.CreateReadonlyItem(TransactionBusinessItemNames.InstrumentId, constructParams.InstrumentId);
            _subType = this.CreateSoundItem(TransactionBusinessItemNames.SubType, constructParams.SubType);
            _phase = this.CreateSoundItem(TransactionBusinessItemNames.Phase, constructParams.Phase);
            _orderType = this.CreateReadonlyItem(TransactionBusinessItemNames.OrderType, constructParams.OrderType);
            _contractSize = this.CreateSoundItem(TransactionBusinessItemNames.ContractSize, constructParams.ConstractSize);
            _beginTime = this.CreateReadonlyItem(TransactionBusinessItemNames.BeginTime, constructParams.BeginTime);
            _endTime = this.CreateReadonlyItem(TransactionBusinessItemNames.EndTime, constructParams.EndTime);
            _expireType = this.CreateReadonlyItem(TransactionBusinessItemNames.ExpireType, constructParams.ExpireType);
            _submitTime = this.CreateReadonlyItem(TransactionBusinessItemNames.SubmitTime, constructParams.SubmitTime);
            _submitorId = this.CreateReadonlyItem(TransactionBusinessItemNames.SubmitorId, constructParams.SubmitorId);
            _executeTime = this.CreateSoundItem(TransactionBusinessItemNames.ExecuteTime, constructParams.ExecuteTime);
            _approverId = this.CreateSoundItem(TransactionBusinessItemNames.ApproverId, constructParams.ApproveId);
            _sourceOrderId = this.CreateReadonlyItem(TransactionBusinessItemNames.SourceOrderId, constructParams.SourceOrderId);
            _setPriceTimestamp = this.CreateSoundItem(TransactionBusinessItemNames.SetPriceTimestamp, constructParams.SetPriceTimestamp);
            _instrumentCategory = this.CreateReadonlyItem(TransactionBusinessItemNames.InstrumentCategory, constructParams.GetInstrumentCategory(_account.Id));
            _placePhase = this.CreateSoundItem("PlacePhase", PlacePhase.None);
            _placeDetail = this.CreateSoundItem("PlaceDetail", string.Empty);
            _appType = this.CreateReadonlyItem("AppType", constructParams.AppType);
            _updateTime = this.CreateSoundItem("UpdateTime", DateTime.Now);
        }

    }
}
