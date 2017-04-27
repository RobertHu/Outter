using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Data;
using System.Xml;
using Core.TransactionServer.Agent.Market;
using System.Xml.Linq;
using Protocal.TradingInstrument;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Engine;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class Instrument : DealingPolicyPayload, IEquatable<Instrument>, PriceAlert.IQuotationSetter
    {
        private static int BinaryOptionOrderTypeMask = (int)Math.Pow(2, 7);

        private Guid _currencyId;
        private Guid _id;
        private string _code;
        private string _originCode;
        private InstrumentCategory _category; //useless in transactionServer
        private int _numeratorUnit;
        private int _denominator;
        private bool _isNormal;

        private DayQuotation _dayQuotation;

        private int _lastAcceptTimeSpan;
        private bool _isActive;

        private int _orderTypeMask;
        private bool _mit; //Should be LmtAsMit

        private bool _isAutoFill;

        private int _maxMinAdjust;

        private FeeFormula _commissionFormula;
        private FeeFormula _levyFormula;
        private FeeFormula _otherFeeFormula;
        private MarginFormula _marginFormula;
        private TradePLFormula _tradePLFormula;
        private InterestFormula _interestFormula;
        private int _interestYearDays;
        private DateTime _endTime;

        private bool _isBetterPrice;
        private int _hitTimes;
        private int _penetrationPoint;

        private bool _useSettlementPriceForInterest;
        private bool _isPriceEnabled;

        private ExchangeSystem _exchangeSystem = ExchangeSystem.Local;
        private bool _mustUseNightNecessaryWhenTrading;

        private bool _canPlacePendingOrderAtAnyTime;
        private AllowedOrderSides _allowedSpotTradeOrderSides;
        private decimal _summaryQuantity;
        private int _acceptIfDoneVariation;
        private int _firstOrderTime;
        private TimeSpan _placeSptMktTimeSpan = TimeSpan.Zero;

        private bool _isExpired;
        private string _depositPrice = null;
        private string _deliveryPrice = null;
        private DateTime _priceEnableTime = DateTime.MinValue;

        internal Instrument(IDBRow instrumentRow)
        {
            this._id = (Guid)instrumentRow["ID"];
            this._currencyId = (Guid)instrumentRow["CurrencyID"];
            this._code = (string)instrumentRow["Code"];
            _originCode = instrumentRow.GetColumn<string>("OriginCode");

            this._isActive = instrumentRow.GetColumn<bool>("IsActive");
            this._lastAcceptTimeSpan = instrumentRow.GetColumn<int>("LastAcceptTimeSpan");
            this._orderTypeMask = instrumentRow.GetColumn<int>("OrderTypeMask");
            this._mit = instrumentRow.GetColumn<bool>("MIT");

            this.AutoAcceptMaxLot = instrumentRow.GetColumn<decimal>("AutoAcceptMaxLot");
            this.AutoCancelMaxLot = instrumentRow.GetColumn<decimal>("AutoCancelMaxLot");
            this._isAutoFill = instrumentRow.GetColumn<bool>("IsAutoFill");

            this._maxMinAdjust = instrumentRow.GetColumn<int>("MaxMinAdjust");

            this._interestFormula = (InterestFormula)instrumentRow.GetColumn<byte>("InterestFormula");
            this._interestYearDays = instrumentRow.GetColumn<int>("InterestYearDays");
            this._useSettlementPriceForInterest = instrumentRow.GetColumn<bool>("UseSettlementPriceForInterest");
            this.InactiveTime = instrumentRow.GetColumn<int>("OriginInactiveTime");

            this._isBetterPrice = instrumentRow.GetColumn<bool>("IsBetterPrice");
            this._hitTimes = instrumentRow.GetColumn<short>("HitTimes");
            this._penetrationPoint = instrumentRow.GetColumn<int>("PenetrationPoint");

            this._isPriceEnabled = instrumentRow.GetColumn<bool>("IsPriceEnabled");

            this.SpotPaymentTime = instrumentRow.GetColumn<DateTime?>("SpotPaymentTime");

            this._canPlacePendingOrderAtAnyTime = instrumentRow.GetColumn<bool>("CanPlacePendingOrderAtAnyTime");
            this._allowedSpotTradeOrderSides = (AllowedOrderSides)instrumentRow.GetColumn<byte>("AllowedSpotTradeOrderSides");

            this.HitPriceVariationForSTP = instrumentRow.GetColumn<int>("HitPriceVariationForSTP");

            if (instrumentRow.ExistsColumn("ExternalExchangeCode"))
            {
                if (instrumentRow["ExternalExchangeCode"] != DBNull.Value)
                {
                    this._exchangeSystem = (ExchangeSystem)Enum.Parse(typeof(ExchangeSystem), (string)(instrumentRow["ExternalExchangeCode"]), true);
                }
            }

            this.AutoDQDelay = TimeSpan.FromSeconds(instrumentRow.GetColumn<Int16>("AutoDQDelay"));
            this._summaryQuantity = instrumentRow.GetColumn<decimal>("SummaryQuantity");
            this.PLValueDay = instrumentRow.GetColumn<Int16>("PLValueDay");

            if (instrumentRow.ExistsColumn("UseAlertLevel4WhenClosed"))
            {
                this.UseAlertLevel4WhenClosed = instrumentRow["UseAlertLevel4WhenClosed"] == DBNull.Value ? false : (bool)instrumentRow["UseAlertLevel4WhenClosed"];
            }
            if (instrumentRow.ExistsColumn("HolidayAlertDayPolicyID"))
            {
                Guid policyId = Guid.Empty;
                if (Guid.TryParse(instrumentRow["HolidayAlertDayPolicyID"].ToString(), out policyId))
                {
                    this.HolidayAlertDayPolicyId = policyId;
                }
            }
            if (instrumentRow.ExistsColumn("AcceptIfDoneVariation"))
            {
                this._acceptIfDoneVariation = instrumentRow.GetColumn<int>("AcceptIfDoneVariation");
            }
            if (instrumentRow.Contains("PlaceSptMktTimeSpan"))
            {
                _placeSptMktTimeSpan = TimeSpan.FromSeconds((int)instrumentRow["PlaceSptMktTimeSpan"]);
            }

            if (instrumentRow.Contains("FirstOrderTime"))
            {
                _firstOrderTime = (int)instrumentRow["FirstOrderTime"];
            }

            this.Update(instrumentRow);
        }

        internal Instrument(XElement instrumentNode)
        {
            this.Update(instrumentNode);
        }

        #region Common internal properties definition



        internal Guid CurrencyId
        {
            get { return this._currencyId; }
        }

        public Guid Id
        {
            get { return this._id; }
            private set { _id = value; }
        }
        internal string Code
        {
            get { return this._code; }
        }

        internal string OriginCode
        {
            get { return _originCode; }
        }

        public InstrumentCategory Category
        {
            get { return this._category; }
            private set { _category = value; }
        }

        internal bool IsPhysical
        {
            get
            {
                return this.Category == InstrumentCategory.Physical;
            }
        }


        internal DayQuotation DayQuotation
        {
            get { return this._dayQuotation; }
            set { this._dayQuotation = value; }
        }

        public int NumeratorUnit
        {
            get { return this._numeratorUnit; }
            private set { _numeratorUnit = value; }
        }

        public int Denominator
        {
            get { return this._denominator; }
            private set { _denominator = value; }
        }

        internal bool IsNormal
        {
            get { return this._isNormal; }
        }

        internal bool IsActive
        {
            get { return this._isActive; }
        }

        internal int MaxMinAdjust
        {
            get { return _maxMinAdjust; }
        }

        internal int LastAcceptTimeSpan
        {
            get { return this._lastAcceptTimeSpan; }
        }

        internal bool IsAutoFill
        {
            get { return this._isAutoFill; }
        }

        internal bool IsExpired
        {
            get { return this._isExpired; }
        }

        internal DateTime DayOpenTime { get; private set; }

        internal DateTime DayCloseTime { get; private set; }

        internal DateTime ValueDate { get; private set; }

        internal DateTime NextDayOpenTime { get; private set; }

        internal FeeFormula CommissionFormula
        {
            get { return this._commissionFormula; }
        }

        internal FeeFormula LevyFormula
        {
            get { return this._levyFormula; }
        }

        public FeeFormula OtherFeeFormula
        {
            get { return _otherFeeFormula; }
        }

        internal MarginFormula MarginFormula
        {
            get { return this._marginFormula; }
        }
        internal TradePLFormula TradePLFormula
        {
            get { return this._tradePLFormula; }
        }
        internal InterestFormula InterestFormula
        {
            get { return this._interestFormula; }
        }
        internal int InterestYearDays
        {
            get { return this._interestYearDays; }
        }

        /// <summary>
        /// Limit单撞线检查时使用，是否使用对公司有利的撞线价格来作为Limit单的BestPrice
        /// </summary>
        internal bool UseBetterPriceForCompanyWhenHit
        {
            get { return this._isBetterPrice; }
        }

        /// <summary>
        /// Mit=Market if touch, Limit撞线检查时，如果本参数为true，则一旦撞线，就将该单当作一个Market单来处理，也就是说用撞线价格来成交该单
        /// </summary>
        internal bool LmtAsMit
        {
            get { return this._mit; }
        }

        /// <summary>
        ///自动成交的Limit单撞线检查时使用，撞线次数大于或等于本参数值时，该单可以成交
        /// </summary>
        internal decimal HitTimes
        {
            get { return this._hitTimes; }
        }

        /// <summary>
        ///Limit撞线检查时，如果撞线价格和SetPrcie的差超过本设置，即便满足撞线次数小于HitTimes，该单也可以成交
        /// </summary>
        internal decimal PenetrationPoint
        {
            get { return this._penetrationPoint; }
        }

        internal bool UseSettlementPriceForInterest
        {
            get { return this._useSettlementPriceForInterest; }
        }

        /// <summary>
        /// IsPriceEnabled 表示当前的价格是否有效，当该值为False时，该Instrument的所有下单都会被拒绝
        /// 主要作用是 防止价格非正常停止，客户用有利的过期价格做单
        /// </summary>
        internal bool IsPriceEnabled
        {
            get { return this._isPriceEnabled; }
        }

        internal bool CanPlacePendingOrderAtAnyTime
        {
            get { return this._canPlacePendingOrderAtAnyTime; }
        }

        internal bool MustUseNightNecessaryWhenTrading
        {
            get { return this._mustUseNightNecessaryWhenTrading; }
            set { this._mustUseNightNecessaryWhenTrading = value; }
        }

        internal AllowedOrderSides AllowedSpotTradeOrderSides
        {
            get { return _allowedSpotTradeOrderSides; }
        }

        internal int AcceptIfDoneVariation
        {
            get { return this._acceptIfDoneVariation; }
        }

        internal decimal SummaryQuantity
        {
            get { return this._summaryQuantity; }
        }

        internal bool UseAlertLevel4WhenClosed { get; private set; }
        internal Guid HolidayAlertDayPolicyId { get; private set; }

        internal String DepositPrice
        {
            get { return this._depositPrice; }
        }
        internal String DeliveryPrice
        {
            get { return this._deliveryPrice; }
        }

        internal Int16 PLValueDay { get; private set; }

        public int InactiveTime { get; set; }



        internal ExchangeSystem ExchangeSystem
        {
            get { return this._exchangeSystem; }
        }

        internal bool AllowBinaryOptionOrder
        {
            get { return (_orderTypeMask & BinaryOptionOrderTypeMask) == BinaryOptionOrderTypeMask; }
        }

        internal int FirstOrderTime
        {
            get { return _firstOrderTime; }
        }

        internal DateTime PriceEnableTime
        {
            get
            {
                return _priceEnableTime;
            }
        }

        internal DateTime? SpotPaymentTime { get; private set; }

        #endregion Common internal properties definition

        //Only Update the fields that should take effect in the past
        internal override void Update(IDBRow instrumentRow)
        {
            base.Update(instrumentRow);

            this._category = (InstrumentCategory)instrumentRow.GetColumn<int>("Category");
            this._numeratorUnit = instrumentRow.GetColumn<int>("NumeratorUnit");
            this._denominator = instrumentRow.GetColumn<int>("Denominator");
            this._isNormal = instrumentRow.GetColumn<bool>("IsNormal");

            this.DayOpenTime = (instrumentRow["DayOpenTime"] == DBNull.Value) ? default(DateTime) : (DateTime)instrumentRow["DayOpenTime"];
            this.DayCloseTime = (instrumentRow["DayCloseTime"] == DBNull.Value) ? default(DateTime) : (DateTime)instrumentRow["DayCloseTime"];
            if (instrumentRow.Contains("EndTime"))
            {
                _endTime = (instrumentRow["EndTime"] == DBNull.Value) ? default(DateTime) : (DateTime)instrumentRow["EndTime"];
            }
            this.ValueDate = (instrumentRow["ValueDate"] == DBNull.Value) ? default(DateTime) : (DateTime)instrumentRow["ValueDate"];
            this.NextDayOpenTime = (instrumentRow["NextDayOpenTime"] == DBNull.Value) ? default(DateTime) : (DateTime)instrumentRow["NextDayOpenTime"];

            this._commissionFormula = (FeeFormula)instrumentRow.GetColumn<byte>("CommissionFormula");
            this._levyFormula = (FeeFormula)instrumentRow.GetColumn<byte>("LevyFormula");
            _otherFeeFormula = (FeeFormula)instrumentRow.GetColumn<byte>("OtherFeeFormula");
            this._marginFormula = (MarginFormula)instrumentRow.GetColumn<byte>("MarginFormula");
            this._tradePLFormula = (TradePLFormula)instrumentRow.GetColumn<byte>("TradePLFormula");

            this._isExpired = this.DayOpenTime == default(DateTime) && this.DayCloseTime == default(DateTime) && this.ValueDate == default(DateTime) && this.NextDayOpenTime == default(DateTime);
        }

        internal override void Update(XElement instrumentNode)
        {
            base.Update(instrumentNode);

            foreach (XAttribute attribute in instrumentNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this._id = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "CurrencyID":
                        this._currencyId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "Code":
                        this._code = attribute.Value;
                        break;
                    case "OriginCode":
                        _originCode = attribute.Value;
                        break;
                    case "OriginInactiveTime":
                        this.InactiveTime = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "Category":
                        this._category = (InstrumentCategory)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "NumeratorUnit":
                        this._numeratorUnit = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "Denominator":
                        this._denominator = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "IsActive":
                        this._isActive = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "LastAcceptTimeSpan":
                        this._lastAcceptTimeSpan = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "OrderTypeMask":
                        this._orderTypeMask = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "MIT":
                        this._mit = XmlConvert.ToBoolean(attribute.Value);
                        break;

                    case "AutoAcceptMaxLot":
                        this.AutoAcceptMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AutoCancelMaxLot":
                        this.AutoCancelMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "IsAutoFill":
                        this._isAutoFill = XmlConvert.ToBoolean(attribute.Value);
                        break;

                    case "MaxMinAdjust":
                        this._maxMinAdjust = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "DayOpenTime":
                        this.DayOpenTime = Convert.ToDateTime(attribute.Value);
                        break;
                    case "DayCloseTime":
                        this.DayCloseTime = Convert.ToDateTime(attribute.Value);
                        break;
                    case "EndTime":
                        _endTime = Convert.ToDateTime(attribute.Value);
                        break;
                    case "ValueDate":
                        this.ValueDate = Convert.ToDateTime(attribute.Value);
                        break;
                    case "NextDayOpenTime":
                        this.NextDayOpenTime = Convert.ToDateTime(attribute.Value);
                        break;
                    case "CommissionFormula":
                        this._commissionFormula = (FeeFormula)XmlConvert.ToByte(attribute.Value);
                        break;
                    case "LevyFormula":
                        this._levyFormula = (FeeFormula)XmlConvert.ToByte(attribute.Value);
                        break;
                    case "OtherFeeFormula":
                        _otherFeeFormula = (FeeFormula)XmlConvert.ToByte(attribute.Value);
                        break;
                    case "MarginFormula":
                        this._marginFormula = (MarginFormula)XmlConvert.ToByte(attribute.Value);
                        break;

                    case "TradePLFormula":
                        this._tradePLFormula = (TradePLFormula)XmlConvert.ToByte(attribute.Value);
                        break;
                    case "InterestFormula":
                        this._interestFormula = (InterestFormula)XmlConvert.ToByte(attribute.Value);
                        break;
                    case "InterestYearDays":
                        this._interestYearDays = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "IsNormal":
                        this._isNormal = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "HitTimes":
                        this._hitTimes = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "PenetrationPoint":
                        this._penetrationPoint = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "UseSettlementPriceForInterest":
                        this._useSettlementPriceForInterest = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "CanPlacePendingOrderAtAnyTime":
                        this._canPlacePendingOrderAtAnyTime = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "AllowedSpotTradeOrderSides":
                        this._allowedSpotTradeOrderSides = (AllowedOrderSides)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "PlaceSptMktTimeSpan":
                        _placeSptMktTimeSpan = TimeSpan.FromSeconds(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "HitPriceVariationForSTP":
                        this.HitPriceVariationForSTP = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AcceptIfDoneVariation":
                        this._acceptIfDoneVariation = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "FirstOrderTime":
                        _firstOrderTime = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "IsPriceEnabled":
                        this._isPriceEnabled = XmlConvert.ToBoolean(attribute.Value);
                        _priceEnableTime = DateTime.Now;
                        break;

                    case "AutoDQDelay":
                        this.AutoDQDelay = TimeSpan.FromSeconds(XmlConvert.ToInt16(attribute.Value));
                        break;
                    case "HolidayAlertDayPolicyID":
                        this.HolidayAlertDayPolicyId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "SummaryQuantity":
                        this._summaryQuantity = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "SpotPaymentTime":
                        this.SpotPaymentTime = attribute.Value.Get<DateTime?>();
                        break;
                }
            }
        }

        internal bool IsInValueDate(Guid accountId, ExecuteContext context)
        {
            var tradeDay = Setting.Default.GetTradeDay(context.TradeDay);
            return this.ValueDate < tradeDay.Day;
        }

        internal bool IsTypeAcceptable(TransactionType tranType, OrderType orderType)
        {
            if (tranType != TransactionType.OneCancelOther)
            {
                return this.IsOrderTypeAcceptable(orderType);
            }
            else
            {
                return this.IsOrderTypeAcceptable(OrderType.OneCancelOther);
            }
        }

        private bool IsOrderTypeAcceptable(OrderType orderType)
        {
            if (this.ExchangeSystem != ExchangeSystem.Local
                && (orderType == OrderType.MarketToLimit || orderType == OrderType.StopLimit))
            {
                return true;
            }
            return ((this._orderTypeMask & (int)Math.Pow(2, (int)orderType)) == Math.Pow(2, (int)orderType));
        }

        internal void UpdateSettlementPrice(string depositPrice, string deliveryPrice)
        {
            if (!string.IsNullOrEmpty(depositPrice))
            {
                this._depositPrice = depositPrice;
            }

            if (!string.IsNullOrEmpty(deliveryPrice))
            {
                this._deliveryPrice = deliveryPrice;
            }
        }

        internal void UpdateTradingTime(DateTime? dayOpenTime, DateTime? dayCloseTime, DateTime? valueDate, DateTime? nextDayOpenTime)
        {
            this.DayOpenTime = dayOpenTime ?? this.DayOpenTime;
            this.DayCloseTime = dayCloseTime ?? this.DayCloseTime;
            this.ValueDate = valueDate ?? this.ValueDate;
            this.NextDayOpenTime = nextDayOpenTime ?? this.NextDayOpenTime;
        }

        public bool Equals(Instrument other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Instrument;
            return this.Equals(other);
        }

        public static bool operator ==(Instrument left, Instrument right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Instrument left, Instrument right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return string.Format("id = {0}, code = {1}", this.Id, this.Code);
        }


    }
}
