using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Periphery.OrderBLL;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    public class OrderSettings : BusinessItemBuilder
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<bool> _isOpen;
        private BusinessItem<bool> _isBuy;
        private BusinessItem<decimal> _lot;
        private BusinessItem<decimal> _originalLot;
        private BusinessItem<decimal> _lotBalance;
        private BusinessItem<string> _code;
        private BusinessItem<string> _blotterCode;
        private BusinessItem<string> _originCode;
        private BusinessItem<decimal?> _minLot;
        private BusinessItem<decimal?> _maxShow;

        private BusinessItem<Price> _setPrice;
        private BusinessItem<Price> _setPrice2;
        private BusinessItem<Price> _executePrice;

        private TimeSpan? _autoFillDelayTime;
        private BusinessItem<DateTime?> _interestValueDate;
        private BusinessItem<int> _setPriceMaxMovePips;
        private BusinessItem<int> _dqMaxMove;
        private BusinessItem<OrderPhase> _phase;
        private BusinessItem<TradeOption> _tradeOption;
        private BusinessItem<DateTime?> _priceTimestamp;

        private BusinessItem<Price> _autoLimitPrice;
        private BusinessItem<Price> _autoStopPrice;
        private BusinessItem<decimal> _interestPerLot;
        private BusinessItem<decimal> _storagePerLot;
        private BusinessItem<Price> _judgePrice;
        private BusinessItem<DateTime?> _judgePriceTimestamp;
        private BusinessItem<Guid?> _cancelReasonId;
        private BusinessItem<string> _cancelReasonDesc;
        private BusinessItem<CancelReason?> _cancelReason;
        private BusinessItem<Guid?> _orderBatchInstructionID;
        private BusinessItem<bool> _isAutoFill;

        private BusinessItem<decimal> _estimateCloseCommission;
        private BusinessItem<decimal> _estimateCloseLevy;
        private Order _owner;
        private HitOrderSettings _hitSettings;
        private NotValuedDayInterestAndStorage _notValuedDayInterestAndStorage;

        #region  Constructors
        internal OrderSettings(Order owner, OrderConstructParams constructParams)
            : base(owner)
        {
            _owner = owner;
            _hitSettings = new HitOrderSettings(owner, constructParams);
            _notValuedDayInterestAndStorage = new NotValuedDayInterestAndStorage(owner, constructParams);
            this.Parse(constructParams);
        }

        protected virtual void Parse(OrderConstructParams constructParams)
        {
            _id = this.CreateKey(OrderBusinessItemNames.Id, constructParams.Id);
            _isOpen = this.CreateSoundItem(OrderBusinessItemNames.IsOpen, constructParams.IsOpen);
            _isBuy = this.CreateReadonlyItem(OrderBusinessItemNames.IsBuy, constructParams.IsBuy);
            this.ParseLot(constructParams);
            this.ParseCode(constructParams);
            this.ParsePrice(constructParams);
            this.ParseSettings(constructParams);
            _cancelReasonId = this.CreateSoundItem<Guid?>("CANCELREASONID", null);
            _cancelReasonDesc = this.CreateSoundItem<string>("CANCELREASONDESC", null);
            _cancelReason = this.CreateSoundItem<CancelReason?>("CancelReason", null);
            _orderBatchInstructionID = this.CreateReadonlyItem("OrderBatchInstructionID", constructParams.OrderBatchInstructionID);
            _isAutoFill = this.CreateSoundItem("IsAutoFill", false);
            _estimateCloseCommission = this.CreateSoundItem("EstimateCloseCommission", constructParams.EstimateCloseCommission);
            _estimateCloseLevy = this.CreateSoundItem("EstimateCloseLevy", constructParams.EstimateCloseLevy);
        }

        private void ParseCode(OrderConstructParams constructParams)
        {
            _code = this.CreateSoundItem(OrderBusinessItemNames.Code, constructParams.Code);
            _originCode = this.CreateReadonlyItem(OrderBusinessItemNames.OriginCode, constructParams.OriginCode);
            _blotterCode = this.CreateReadonlyItem(OrderBusinessItemNames.BlotterCode, constructParams.BlotterCode);
        }

        private void ParseLot(OrderConstructParams constructParams)
        {
            _lot = this.CreateSoundItem(OrderBusinessItemNames.Lot, constructParams.Lot);
            _originalLot = this.CreateReadonlyItem(OrderBusinessItemNames.OriginalLot, constructParams.OriginalLot);
            _lotBalance = this.CreateSoundItem(OrderBusinessItemNames.LotBalance, constructParams.LotBalance);
            _interestPerLot = this.CreateSoundItem("InterestPerLot", constructParams.InterestPerLot);
            _storagePerLot = this.CreateSoundItem("StoragePerLot", constructParams.StoragePerLot);
            _minLot = this.CreateSoundItem("MinLot", constructParams.MinLot);
            _maxShow = this.CreateSoundItem("MaxShow", constructParams.MaxShow);
        }

        private void ParsePrice(OrderConstructParams constructParams)
        {
            _executePrice = this.CreateSoundItem(OrderBusinessItemNames.ExecutePrice, constructParams.ExecutePrice);
            _setPrice = this.CreateReadonlyItem(OrderBusinessItemNames.SetPrice, constructParams.SetPrice);
            _setPrice2 = this.CreateReadonlyItem(OrderBusinessItemNames.SetPrice2, constructParams.SetPrice2);
            _autoLimitPrice = this.CreateDumpItem<Price>("AutoLimitPrice", null);
            _autoStopPrice = this.CreateDumpItem<Price>("AutoStopPrice", null);
            _priceTimestamp = this.CreateSoundItem(OrderBusinessItemNames.PriceTimestamp, constructParams.PriceTimestamp);
            _judgePrice = this.CreateSoundItem<Price>("JudgePrice", null);
            _judgePriceTimestamp = this.CreateSoundItem<DateTime?>("JudgePriceTimestamp", null);
        }

        private void ParseSettings(OrderConstructParams constructParams)
        {
            _interestValueDate = this.CreateSoundItem(OrderBusinessItemNames.InterestValueDate, constructParams.InterestValueDate);
            _setPriceMaxMovePips = this.CreateReadonlyItem(OrderBusinessItemNames.SetPriceMaxMovePips, constructParams.SetPriceMaxMovePips);
            _dqMaxMove = this.CreateReadonlyItem(OrderBusinessItemNames.DQMaxMove, constructParams.DQMaxMove);
            _phase = this.CreateSoundItem(OrderBusinessItemNames.Phase, constructParams.Phase);
            _tradeOption = this.CreateReadonlyItem(OrderBusinessItemNames.TradeOption, constructParams.TradeOption);
        }
        #endregion

        #region Properties

        internal HitOrderSettings HitSettings
        {
            get { return _hitSettings; }
        }
        internal NotValuedDayInterestAndStorage NotValuedDayInterestAndStorage
        {
            get { return _notValuedDayInterestAndStorage; }
        }

        internal Guid Id
        {
            get { return _id.Value; }
            set { _id.SetValue(value); }
        }

        internal bool IsOpen
        {
            get { return _isOpen.Value; }
            set { _isOpen.SetValue(value); }
        }

        internal bool IsBuy
        {
            get { return _isBuy.Value; }
        }

        internal decimal Lot
        {
            get { return _lot.Value; }
            set { _lot.SetValue(value); }
        }

        internal decimal OriginalLot
        {
            get { return _originalLot.Value; }
        }

        internal decimal LotBalance
        {
            get { return _lotBalance.Value; }
            set { _lotBalance.SetValue(value); }
        }

        internal string Code
        {
            get
            {
                if (!string.IsNullOrEmpty(_originCode.Value))
                {
                    return _originCode.Value;
                }
                else if (!string.IsNullOrEmpty(_code.Value))
                {
                    return _code.Value;
                }
                else
                {
                    throw new ArgumentNullException("Order Code was not set");
                }
            }
            set { _code.SetValue(value); }
        }

        internal string OriginCode
        {
            get { return _originCode.Value; }
        }

        internal string BlotterCode
        {
            get { return _blotterCode.Value; }
            private set { _blotterCode.SetValue(value); }
        }


        internal Price SetPrice
        {
            get { return this._setPrice.Value; }
        }

        internal Price SetPrice2
        {
            get { return this._setPrice2.Value; }
        }

        internal Price ExecutePrice
        {
            get { return this._executePrice.Value; }
            set
            {
                _executePrice.SetValue(value);
            }
        }


        internal Price AutoLimitPrice
        {
            get { return _autoLimitPrice.Value; }
            set { _autoLimitPrice.SetValue(value); }
        }

        internal Price AutoStopPrice
        {
            get { return _autoStopPrice.Value; }
            set { _autoStopPrice.SetValue(value); }
        }

        internal DateTime? PriceTimestamp
        {
            get
            {
                return _priceTimestamp.Value;
            }
        }

        public TimeSpan? AutoFillDelayTime
        {
            get { return _autoFillDelayTime; }
            set { _autoFillDelayTime = value; }
        }

        public DateTime? InterestValueDate
        {
            get { return _interestValueDate.Value; }
            set { _interestValueDate.SetValue(value); }
        }

        public int SetPriceMaxMovePips
        {
            get { return _setPriceMaxMovePips.Value; }
        }

        public int DQMaxMove
        {
            get { return _dqMaxMove.Value; }
        }

        public OrderPhase Phase
        {
            get { return _phase.Value; }
            set
            {
                _phase.SetValue(value);
            }
        }

        public TradeOption TradeOption
        {
            get { return _tradeOption.Value; }
        }

        internal decimal InterestPerLot
        {
            get { return _interestPerLot.Value; }
            set { _interestPerLot.SetValue(value); }
        }

        internal decimal StoragePerLot
        {
            get { return _storagePerLot.Value; }
            set { _storagePerLot.SetValue(value); }
        }

        internal Price JudgePrice
        {
            get { return _judgePrice.Value; }
            set { _judgePrice.SetValue(value); }
        }

        internal DateTime? JudgePriceTimestamp
        {
            get { return _judgePriceTimestamp.Value; }
            set { _judgePriceTimestamp.SetValue(value); }
        }

        internal Guid? CancelReasonId
        {
            get { return _cancelReasonId.Value; }
            set { _cancelReasonId.SetValue(value); }
        }

        internal string CancelReasonDesc
        {
            get { return _cancelReasonDesc.Value; }
            set { _cancelReasonDesc.SetValue(value); }
        }

        internal CancelReason? CancelReason
        {
            get { return _cancelReason.Value; }
            set { _cancelReason.SetValue(value); }
        }

        internal bool IsAutoFill
        {
            get { return _isAutoFill.Value; }
            set { _isAutoFill.SetValue(value); }
        }

        internal decimal EstimateCloseCommission
        {
            get { return _estimateCloseCommission.Value; }
            set { _estimateCloseCommission.SetValue(value); }
        }

        internal decimal EstimateCloseLevy
        {
            get { return _estimateCloseLevy.Value; }
            set { _estimateCloseLevy.SetValue(value); }
        }

        #endregion


        internal Price GetLivePriceForCalculateNecessary()
        {
            var account = _owner.Owner.Owner;
            Quotation quotation = _owner.Owner.AccountInstrument.GetQuotation();
            Price price = _owner.IsBuy ? quotation.BuyPrice : quotation.SellPrice;
            return price;
        }

        internal void LoadForMarketOnCloseExecute(DataRow dr)
        {
            var id = (Guid)dr["ID"];
            if (this.Id != id) return;
            this.Code = (string)dr["Code"];
            this.BlotterCode = dr["BlotterCode"] == DBNull.Value ? null : (string)dr["BlotterCode"];
        }
    }

    internal sealed class NotValuedDayInterestAndStorage : BusinessItemBuilder
    {
        private List<decimal> _notValuedDayInterests = new List<decimal>();
        private List<decimal> _notValuedDayStorages = new List<decimal>();

        internal NotValuedDayInterestAndStorage(BusinessRecord parent, OrderConstructParams constructParams)
            : base(parent)
        {
        }



        public bool IsValued
        {
            get
            {
                return _notValuedDayInterests == null && _notValuedDayStorages == null;
            }
        }

        public string InterestNotValuedString
        {
            get
            {
                return Extensions.ToString(_notValuedDayInterests, '|');
            }
        }

        public string StorageNotValuedString
        {
            get
            {
                return Extensions.ToString(_notValuedDayStorages, '|');
            }
        }

        internal void Add(decimal dayInterestPLNotValued, decimal dayStoragePLNotValued)
        {
            _notValuedDayInterests.Add(dayInterestPLNotValued);
            _notValuedDayStorages.Add(dayStoragePLNotValued);
        }

        internal void CalculateNotValuedInterestAndStorage(CurrencyRate currencyRate, out decimal interestPL, out decimal storagePL)
        {
            interestPL = storagePL = 0;
            foreach (var eachInterest in _notValuedDayInterests)
            {
                interestPL += currencyRate.Exchange(eachInterest);
            }

            foreach (var eachStorage in _notValuedDayStorages)
            {
                storagePL += currencyRate.Exchange(eachStorage);
            }
        }


        internal void GetXmlString(out string interestXml, out string storageXml)
        {
            interestXml = Extensions.ToString(_notValuedDayInterests, '|');
            storageXml = Extensions.ToString(_notValuedDayStorages, '|');
        }

    }

}
