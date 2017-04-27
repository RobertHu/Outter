using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Xml;
using System.Diagnostics;

namespace Protocal.CommonSetting
{
    public enum PlaceCheckType
    {
        None = 0,
        InstanceOrder = 1,
        AllOrder = 2
    }

    public enum RiskActionOnPendingConfirmLimit
    {
        Normal = 0,
        ExecuteFirst = 1,
        StopCheckRisk = 2
    }

    public enum ExecuteActionWhenPendingOrderLotExceedMaxOtherLot
    {
        Cancel = 0,
        ExecuteWithSetLot = 1,
        ReplacedWithMaxLot = 2
    }

    public enum STPAtHitPriceOption
    {
        Always,
        OnlyWhenNetLotIncreased
    }

    public enum DQDelayTimeOption
    {
        CheckAllPrice,
        OnlyLastPrice
    }


    public interface IDBRow
    {
        object this[string columnName] { get; }

        bool Contains(string columnName);
    }


    public sealed class DBRow: IDBRow
    {
        private DataRow _dr;

        public DBRow(DataRow dr)
        {
            _dr = dr;
        }

        public object this[string columnName]
        {
            get { return _dr[columnName]; }
        }

        public bool Contains(string columnName)
        {
            return _dr.Table.Columns.Contains(columnName);
        }
    }

    public sealed class DBReader : IDBRow
    {
        private IDataReader _dr;

        public DBReader(IDataReader dr)
        {
            _dr = dr;
        }

        public object this[string columnName]
        {
            get { return _dr[columnName]; }
        }

        public bool Contains(string columnName)
        {
            try
            {
                return _dr.GetOrdinal(columnName) > 0;
            }
            catch (IndexOutOfRangeException )
            {
                return false;
            }
        }
    }


    public sealed class SystemParameter
    {
        private DateTime _tradeDayBeginTime;
        private int _mooMocAcceptDuration;
        private int _mooMocCancelDuration;

        private PlaceCheckType _placeCheckType;
        private bool _needsFillCheck;
        private bool _canDealerViewAccountInfo;

        private bool _useNightNecessaryWhenBreak;
        private bool _enableExportOrder;
        private bool _enableEmailNotify;
        private bool _emailNotifyChangePassword;

        private int _currencyRateUpdateDuration;
        private Guid? _defaultQuotePolicyId;
        private ExecuteActionWhenPendingOrderLotExceedMaxOtherLot _executeActionWhenPendingOrderLotExceedMaxOtherLot = ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.Cancel;
        private STPAtHitPriceOption _optionOfSTPAtHitPrice;
        private bool _includeFeeOnRiskAction = false;
        private bool _evaluateIfDonePlacingOnStpConfirm = true;
        private DQDelayTimeOption _dqDelayTimeOption;

        #region Common internal properties definition

        public DQDelayTimeOption DQDelayTimeOption
        {
            get { return _dqDelayTimeOption; }
        }

        public STPAtHitPriceOption STPAtHitPriceOption
        {
            get { return this._optionOfSTPAtHitPrice; }
        }

        public DateTime TradeDayBeginTime
        {
            get { return this._tradeDayBeginTime; }
        }
        public int MooMocAcceptDuration
        {
            get { return this._mooMocAcceptDuration; }
        }
        public int MooMocCancelDuration
        {
            get { return this._mooMocCancelDuration; }
        }
        public PlaceCheckType PlaceCheckType
        {
            get { return this._placeCheckType; }
        }
        public ExecuteActionWhenPendingOrderLotExceedMaxOtherLot ExecuteActionWhenPendingOrderLotExceedMaxOtherLot
        {
            get { return this._executeActionWhenPendingOrderLotExceedMaxOtherLot; }
        }
        public bool CanDealerViewAccountInfo
        {
            get { return this._canDealerViewAccountInfo; }
        }

        public bool IncludeFeeOnRiskAction
        {
            get { return this._includeFeeOnRiskAction; }
        }

        public bool UseNightNecessaryWhenBreak
        {
            get { return this._useNightNecessaryWhenBreak; }
        }
        public bool EnableExportOrder
        {
            get { return this._enableExportOrder; }
        }

        public bool EnableEmailNotify
        {
            get { return this._enableEmailNotify; }
        }

        public bool EmailNotifyChangePassword
        {
            get { return this._emailNotifyChangePassword; }
        }

        public bool EvaluateIfDonePlacingOnStpConfirm
        {
            get { return this._evaluateIfDonePlacingOnStpConfirm; }
        }

        public int CurrencyRateUpdateDuration
        {
            get { return this._currencyRateUpdateDuration; }
        }

        public Guid? DefaultQuotePolicyId
        {
            get { return this._defaultQuotePolicyId; }
        }

        public RiskActionOnPendingConfirmLimit RiskActionOnPendingConfirmLimit
        {
            get;
            private set;
        }

        public bool EnableResetTelephonePin { get; private set; }

        public bool EnableAutoResetAlertLevel { get; private set; }

        #endregion Common internal properties definition

        public SystemParameter(IDBRow systemParameterRow)
        {
            this._tradeDayBeginTime = (DateTime)systemParameterRow["TradeDayBeginTime"];
            this._mooMocAcceptDuration = (int)systemParameterRow["MooMocAcceptDuration"];
            this._mooMocCancelDuration = (int)systemParameterRow["MooMocCancelDuration"];
            this._dqDelayTimeOption = (DQDelayTimeOption)((int)systemParameterRow["DQDelayTimeOption"]);
            this._placeCheckType = (PlaceCheckType)(int)systemParameterRow["PlaceCheckType"];
            this._needsFillCheck = (bool)systemParameterRow["NeedsFillCheck"];
            this._canDealerViewAccountInfo = (bool)systemParameterRow["CanDealerViewAccountInfo"];

            this._useNightNecessaryWhenBreak = (bool)systemParameterRow["UseNightNecessaryWhenBreak"];
            this.BalanceDeficitAllowPay = (bool)systemParameterRow["BalanceDeficitAllowPay"];
            if (systemParameterRow.Contains("IncludeFeeOnRiskAction"))
            {
                this._includeFeeOnRiskAction = (bool)systemParameterRow["IncludeFeeOnRiskAction"];
            }
            if (systemParameterRow.Contains("EnableExportOrder"))
            {
                this._enableExportOrder = (bool)systemParameterRow["EnableExportOrder"];
            }
            else
            {
                this._enableExportOrder = false;
            }
            if (systemParameterRow.Contains("EnableEmailNotify"))
            {
                this._enableEmailNotify = (bool)systemParameterRow["EnableEmailNotify"];
            }
            else
            {
                this._enableEmailNotify = false;
            }

            if (systemParameterRow.Contains("EmailNotifyChangePassword"))
            {
                this._emailNotifyChangePassword = (bool)systemParameterRow["EmailNotifyChangePassword"];
            }
            else
            {
                this._emailNotifyChangePassword = false;
            }

            if (systemParameterRow.Contains("CurrencyRateUpdateDuration"))
            {
                this._currencyRateUpdateDuration = (int)systemParameterRow["CurrencyRateUpdateDuration"];
            }
            else
            {
                this._currencyRateUpdateDuration = -1;
            }

            if (systemParameterRow.Contains("DefaultQuotePolicyId"))
            {
                this._defaultQuotePolicyId = (Guid)systemParameterRow["DefaultQuotePolicyId"];
            }

            this.MaxPriceDelayForSpotOrder = null;
            if (systemParameterRow.Contains("MaxPriceDelayForSpotOrder"))
            {
                object maxPriceDelayForSpotOrder = systemParameterRow["MaxPriceDelayForSpotOrder"];
                if (maxPriceDelayForSpotOrder != DBNull.Value)
                {
                    this.MaxPriceDelayForSpotOrder = TimeSpan.FromSeconds((int)maxPriceDelayForSpotOrder);
                }
            }

            if (systemParameterRow.Contains("RiskActionOnPendingConfirmLimit"))
            {
                this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)((byte)systemParameterRow["RiskActionOnPendingConfirmLimit"]);
            }

            this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)((byte)systemParameterRow["LmtQuantityOnMaxLotChange"]);
            this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)((byte)systemParameterRow["STPAtHitPriceOption"]);
            this._evaluateIfDonePlacingOnStpConfirm = (bool)systemParameterRow["EvaluateIfDonePlacingOnStpConfirm"];
            this.EnableResetTelephonePin = (bool)systemParameterRow["EnableResetTelephonePin"];

            if (systemParameterRow.Contains("EnableAutoResetAlertLevel"))
            {
                this.EnableAutoResetAlertLevel = (bool)systemParameterRow["EnableAutoResetAlertLevel"];
            }
            else
            {
                this.EnableAutoResetAlertLevel = false;
            }

        }

        public SystemParameter(XElement node)
        {
            this._tradeDayBeginTime = DateTime.Parse(node.Attribute("TradeDayBeginTime").Value);
            this._mooMocAcceptDuration = int.Parse(node.Attribute("MooMocAcceptDuration").Value);
            this._mooMocCancelDuration = int.Parse(node.Attribute("MooMocCancelDuration").Value);
            this._dqDelayTimeOption = (DQDelayTimeOption)(int.Parse(node.Attribute("DQDelayTimeOption").Value));
            this._placeCheckType = (PlaceCheckType)(int.Parse(node.Attribute("PlaceCheckType").Value));
            this._needsFillCheck = node.DBAttToBoolean("NeedsFillCheck");
            this._canDealerViewAccountInfo = node.DBAttToBoolean("CanDealerViewAccountInfo");

            this._useNightNecessaryWhenBreak = node.DBAttToBoolean("UseNightNecessaryWhenBreak");
            this.BalanceDeficitAllowPay = node.DBAttToBoolean("BalanceDeficitAllowPay");
            if (node.Attribute("IncludeFeeOnRiskAction") != null)
            {
                this._includeFeeOnRiskAction = node.DBAttToBoolean("IncludeFeeOnRiskAction");
            }
            if (node.Attribute("EnableExportOrder") != null)
            {
                this._enableExportOrder = node.DBAttToBoolean("EnableExportOrder");
            }
            else
            {
                this._enableExportOrder = false;
            }
            if (node.Attribute("EnableEmailNotify") != null)
            {
                this._enableEmailNotify = node.DBAttToBoolean("EnableEmailNotify");
            }
            else
            {
                this._enableEmailNotify = false;
            }

            if (node.Attribute("EmailNotifyChangePassword") != null)
            {
                this._emailNotifyChangePassword = node.DBAttToBoolean("EmailNotifyChangePassword");
            }
            else
            {
                this._emailNotifyChangePassword = false;
            }

            if (node.Attribute("CurrencyRateUpdateDuration") != null)
            {
                this._currencyRateUpdateDuration = int.Parse(node.Attribute("CurrencyRateUpdateDuration").Value);
            }
            else
            {
                this._currencyRateUpdateDuration = -1;
            }

            if (node.Attribute("DefaultQuotePolicyId") != null)
            {
                this._defaultQuotePolicyId = Guid.Parse(node.Attribute("DefaultQuotePolicyId").Value);
            }

            this.MaxPriceDelayForSpotOrder = null;
            if (node.Attribute("MaxPriceDelayForSpotOrder") != null)
            {
                this.MaxPriceDelayForSpotOrder = TimeSpan.FromSeconds(int.Parse(node.Attribute("MaxPriceDelayForSpotOrder").Value));
            }

            if (node.Attribute("RiskActionOnPendingConfirmLimit") != null)
            {
                this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)(byte.Parse(node.Attribute("RiskActionOnPendingConfirmLimit").Value));
            }

            if (node.Attribute("EnableResetTelephonePin") != null)
            {
                this.EnableResetTelephonePin = XmlConvert.ToBoolean(node.Attribute("EnableResetTelephonePin").Value);
            }

            if (node.Attribute("EnableAutoResetAlertLevel") != null)
            {
                this.EnableAutoResetAlertLevel = XmlConvert.ToBoolean(node.Attribute("EnableAutoResetAlertLevel").Value);
            }


            this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)(byte.Parse(node.Attribute("LmtQuantityOnMaxLotChange").Value));
            this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)(byte.Parse(node.Attribute("STPAtHitPriceOption").Value));
            this._evaluateIfDonePlacingOnStpConfirm = node.DBAttToBoolean("EvaluateIfDonePlacingOnStpConfirm");
        }


        public void Update(XmlNode systemParameterNode)
        {
            foreach (XmlAttribute attribute in systemParameterNode.Attributes)
            {
                switch (attribute.Name)
                {
                    case "TradeDayBeginTime":
                        this._tradeDayBeginTime = Convert.ToDateTime(attribute.Value);
                        break;
                    case "MooMocAcceptDuration":
                        this._mooMocAcceptDuration = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "MooMocCancelDuration":
                        this._mooMocCancelDuration = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "PlaceCheckType":
                        this._placeCheckType = (PlaceCheckType)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "NeedsFillCheck":
                        this._needsFillCheck = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "EvaluateIfDonePlacingOnStpConfirm":
                        this._evaluateIfDonePlacingOnStpConfirm = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "CanDealerViewAccountInfo":
                        this._canDealerViewAccountInfo = XmlConvert.ToBoolean(attribute.Value);
                        break;

                    case "UseNightNecessaryWhenBreak":
                        this._useNightNecessaryWhenBreak = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "IncludeFeeOnRiskAction":
                        this._includeFeeOnRiskAction = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "RiskActionOnPendingConfirmLimit":
                        this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "LmtQuantityOnMaxLotChange":
                        this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "STPAtHitPriceOption":
                        this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "DQDelayTimeOption":
                        this._dqDelayTimeOption = (DQDelayTimeOption)(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "BalanceDeficitAllowPay":
                        this.BalanceDeficitAllowPay = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "EnableResetTelephonePin":
                        this.EnableResetTelephonePin = XmlConvert.ToBoolean(attribute.Value);
                        break;
                }
            }
        }

        public bool NeedsPlaceCheck()
        {
            return (this._placeCheckType == PlaceCheckType.AllOrder);
        }

        public bool NeedsFillCheck(OrderType orderType)
        {
            if (this.NeedsPlaceCheck() || this.NeedsOldPlaceCheck(orderType))
            {
                return this._needsFillCheck;
            }
            else
            {
                return true;
            }
        }

        public bool NeedsOldPlaceCheck(OrderType orderType)
        {
            return (this.PlaceCheckType == PlaceCheckType.InstanceOrder && (orderType == OrderType.SpotTrade || orderType == OrderType.Market));
        }

        public TimeSpan? MaxPriceDelayForSpotOrder { get; private set; }

        public bool BalanceDeficitAllowPay
        {
            get;
            private set;
        }
    }

}
