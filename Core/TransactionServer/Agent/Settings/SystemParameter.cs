using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using iExchange.Common;
using System.Diagnostics;
using System.Xml.Linq;
using Protocal;
using Core.TransactionServer.Agent.Util.TypeExtension;

namespace Core.TransactionServer.Agent.Settings
{
    //internal enum PlaceCheckType
    //{
    //    None = 0,
    //    InstanceOrder = 1,
    //    AllOrder = 2
    //}

    //internal enum RiskActionOnPendingConfirmLimit
    //{
    //    Normal = 0,
    //    ExecuteFirst = 1,
    //    StopCheckRisk = 2
    //}

    //internal enum ExecuteActionWhenPendingOrderLotExceedMaxOtherLot
    //{
    //    Cancel = 0,
    //    ExecuteWithSetLot = 1,
    //    ReplacedWithMaxLot = 2
    //}

    //internal enum STPAtHitPriceOption
    //{
    //    Always,
    //    OnlyWhenNetLotIncreased
    //}

    internal class ShouldBeExecuteWithMaxOtherLot : TransactionServerException//throw as exception
    {
        public ShouldBeExecuteWithMaxOtherLot(Order exceedMaxOtherLotOrder, decimal maxOtherLot)
            : base(TransactionError.ReplacedWithMaxLot)
        {
            this.ExceedMaxOtherLotOrder = exceedMaxOtherLotOrder;
            this.MaxOtherLot = maxOtherLot;
        }

        public Order ExceedMaxOtherLotOrder
        {
            get;
            private set;
        }

        public decimal MaxOtherLot
        {
            get;
            private set;
        }
    }

    //public sealed class SystemParameter
    //{
    //    private DateTime _tradeDayBeginTime;
    //    private int _mooMocAcceptDuration;
    //    private int _mooMocCancelDuration;

    //    private PlaceCheckType _placeCheckType;
    //    private bool _needsFillCheck;
    //    private bool _canDealerViewAccountInfo;

    //    private bool _useNightNecessaryWhenBreak;
    //    private bool _enableExportOrder;
    //    private bool _enableEmailNotify;
    //    private bool _emailNotifyChangePassword;

    //    private int _currencyRateUpdateDuration;
    //    private Guid? _defaultQuotePolicyId;
    //    private ExecuteActionWhenPendingOrderLotExceedMaxOtherLot _executeActionWhenPendingOrderLotExceedMaxOtherLot = ExecuteActionWhenPendingOrderLotExceedMaxOtherLot.Cancel;
    //    private STPAtHitPriceOption _optionOfSTPAtHitPrice;
    //    private bool _includeFeeOnRiskAction = false;
    //    private bool _evaluateIfDonePlacingOnStpConfirm = true;
    //    private DQDelayTimeOption _dqDelayTimeOption;

    //    #region Common internal properties definition

    //    internal DQDelayTimeOption DQDelayTimeOption
    //    {
    //        get { return _dqDelayTimeOption; }
    //    }

    //    internal STPAtHitPriceOption STPAtHitPriceOption
    //    {
    //        get { return this._optionOfSTPAtHitPrice; }
    //    }

    //    internal DateTime TradeDayBeginTime
    //    {
    //        get { return this._tradeDayBeginTime; }
    //    }
    //    internal int MooMocAcceptDuration
    //    {
    //        get { return this._mooMocAcceptDuration; }
    //    }
    //    internal int MooMocCancelDuration
    //    {
    //        get { return this._mooMocCancelDuration; }
    //    }
    //    internal PlaceCheckType PlaceCheckType
    //    {
    //        get { return this._placeCheckType; }
    //    }
    //    internal ExecuteActionWhenPendingOrderLotExceedMaxOtherLot ExecuteActionWhenPendingOrderLotExceedMaxOtherLot
    //    {
    //        get { return this._executeActionWhenPendingOrderLotExceedMaxOtherLot; }
    //    }
    //    internal bool CanDealerViewAccountInfo
    //    {
    //        get { return this._canDealerViewAccountInfo; }
    //    }

    //    internal bool IncludeFeeOnRiskAction
    //    {
    //        get { return this._includeFeeOnRiskAction; }
    //    }

    //    internal bool UseNightNecessaryWhenBreak
    //    {
    //        get { return this._useNightNecessaryWhenBreak; }
    //    }
    //    internal bool EnableExportOrder
    //    {
    //        get { return this._enableExportOrder; }
    //    }

    //    internal bool EnableEmailNotify
    //    {
    //        get { return this._enableEmailNotify; }
    //    }

    //    internal bool EmailNotifyChangePassword
    //    {
    //        get { return this._emailNotifyChangePassword; }
    //    }

    //    internal bool EvaluateIfDonePlacingOnStpConfirm
    //    {
    //        get { return this._evaluateIfDonePlacingOnStpConfirm; }
    //    }

    //    internal int CurrencyRateUpdateDuration
    //    {
    //        get { return this._currencyRateUpdateDuration; }
    //    }

    //    internal Guid? DefaultQuotePolicyId
    //    {
    //        get { return this._defaultQuotePolicyId; }
    //    }

    //    internal RiskActionOnPendingConfirmLimit RiskActionOnPendingConfirmLimit
    //    {
    //        get;
    //        private set;
    //    }

    //    #endregion Common internal properties definition

    //    internal SystemParameter(DataRow systemParameterRow)
    //    {
    //        this._tradeDayBeginTime = (DateTime)systemParameterRow["TradeDayBeginTime"];
    //        this._mooMocAcceptDuration = (int)systemParameterRow["MooMocAcceptDuration"];
    //        this._mooMocCancelDuration = (int)systemParameterRow["MooMocCancelDuration"];
    //        this._dqDelayTimeOption = (DQDelayTimeOption)((int)systemParameterRow["DQDelayTimeOption"]);
    //        this._placeCheckType = (PlaceCheckType)(int)systemParameterRow["PlaceCheckType"];
    //        this._needsFillCheck = (bool)systemParameterRow["NeedsFillCheck"];
    //        this._canDealerViewAccountInfo = (bool)systemParameterRow["CanDealerViewAccountInfo"];

    //        this._useNightNecessaryWhenBreak = (bool)systemParameterRow["UseNightNecessaryWhenBreak"];
    //        this.BalanceDeficitAllowPay = (bool)systemParameterRow["BalanceDeficitAllowPay"];
    //        if (systemParameterRow.Table.Columns.Contains("IncludeFeeOnRiskAction"))
    //        {
    //            this._includeFeeOnRiskAction = (bool)systemParameterRow["IncludeFeeOnRiskAction"];
    //        }
    //        if (systemParameterRow.Table.Columns.Contains("EnableExportOrder"))
    //        {
    //            this._enableExportOrder = (bool)systemParameterRow["EnableExportOrder"];
    //        }
    //        else
    //        {
    //            this._enableExportOrder = false;
    //        }
    //        if (systemParameterRow.Table.Columns.Contains("EnableEmailNotify"))
    //        {
    //            this._enableEmailNotify = (bool)systemParameterRow["EnableEmailNotify"];
    //        }
    //        else
    //        {
    //            this._enableEmailNotify = false;
    //        }

    //        if (systemParameterRow.Table.Columns.Contains("EmailNotifyChangePassword"))
    //        {
    //            this._emailNotifyChangePassword = (bool)systemParameterRow["EmailNotifyChangePassword"];
    //        }
    //        else
    //        {
    //            this._emailNotifyChangePassword = false;
    //        }

    //        if (systemParameterRow.Table.Columns.Contains("CurrencyRateUpdateDuration"))
    //        {
    //            this._currencyRateUpdateDuration = (int)systemParameterRow["CurrencyRateUpdateDuration"];
    //        }
    //        else
    //        {
    //            this._currencyRateUpdateDuration = -1;
    //        }

    //        if (systemParameterRow.Table.Columns.Contains("DefaultQuotePolicyId"))
    //        {
    //            this._defaultQuotePolicyId = (Guid)systemParameterRow["DefaultQuotePolicyId"];
    //        }

    //        this.MaxPriceDelayForSpotOrder = null;
    //        if (systemParameterRow.Table.Columns.Contains("MaxPriceDelayForSpotOrder"))
    //        {
    //            object maxPriceDelayForSpotOrder = systemParameterRow["MaxPriceDelayForSpotOrder"];
    //            if (maxPriceDelayForSpotOrder != DBNull.Value)
    //            {
    //                this.MaxPriceDelayForSpotOrder = TimeSpan.FromSeconds((int)maxPriceDelayForSpotOrder);
    //            }
    //        }

    //        if (systemParameterRow.Table.Columns.Contains("RiskActionOnPendingConfirmLimit"))
    //        {
    //            this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)((byte)systemParameterRow["RiskActionOnPendingConfirmLimit"]);
    //        }

    //        this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)((byte)systemParameterRow["LmtQuantityOnMaxLotChange"]);
    //        this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)((byte)systemParameterRow["STPAtHitPriceOption"]);
    //        this._evaluateIfDonePlacingOnStpConfirm = (bool)systemParameterRow["EvaluateIfDonePlacingOnStpConfirm"];
    //    }

    //    public SystemParameter(XElement node)
    //    {
    //        this._tradeDayBeginTime = DateTime.Parse(node.Attribute("TradeDayBeginTime").Value);
    //        this._mooMocAcceptDuration = int.Parse(node.Attribute("MooMocAcceptDuration").Value);
    //        this._mooMocCancelDuration = int.Parse(node.Attribute("MooMocCancelDuration").Value);
    //        this._dqDelayTimeOption = (DQDelayTimeOption)(int.Parse(node.Attribute("DQDelayTimeOption").Value));
    //        this._placeCheckType = (PlaceCheckType)(int.Parse(node.Attribute("PlaceCheckType").Value));
    //        this._needsFillCheck = node.DBAttToBoolean("NeedsFillCheck");
    //        this._canDealerViewAccountInfo = node.DBAttToBoolean("CanDealerViewAccountInfo");

    //        this._useNightNecessaryWhenBreak = node.DBAttToBoolean("UseNightNecessaryWhenBreak");
    //        this.BalanceDeficitAllowPay = node.DBAttToBoolean("BalanceDeficitAllowPay");
    //        if (node.Attribute("IncludeFeeOnRiskAction") != null)
    //        {
    //            this._includeFeeOnRiskAction = node.DBAttToBoolean("IncludeFeeOnRiskAction");
    //        }
    //        if (node.Attribute("EnableExportOrder") != null)
    //        {
    //            this._enableExportOrder = node.DBAttToBoolean("EnableExportOrder");
    //        }
    //        else
    //        {
    //            this._enableExportOrder = false;
    //        }
    //        if (node.Attribute("EnableEmailNotify") != null)
    //        {
    //            this._enableEmailNotify = node.DBAttToBoolean("EnableEmailNotify");
    //        }
    //        else
    //        {
    //            this._enableEmailNotify = false;
    //        }

    //        if (node.Attribute("EmailNotifyChangePassword") != null)
    //        {
    //            this._emailNotifyChangePassword = node.DBAttToBoolean("EmailNotifyChangePassword");
    //        }
    //        else
    //        {
    //            this._emailNotifyChangePassword = false;
    //        }

    //        if (node.Attribute("CurrencyRateUpdateDuration") != null)
    //        {
    //            this._currencyRateUpdateDuration = int.Parse(node.Attribute("CurrencyRateUpdateDuration").Value);
    //        }
    //        else
    //        {
    //            this._currencyRateUpdateDuration = -1;
    //        }

    //        if (node.Attribute("DefaultQuotePolicyId") != null)
    //        {
    //            this._defaultQuotePolicyId = Guid.Parse(node.Attribute("DefaultQuotePolicyId").Value);
    //        }

    //        this.MaxPriceDelayForSpotOrder = null;
    //        if (node.Attribute("MaxPriceDelayForSpotOrder") != null)
    //        {
    //            this.MaxPriceDelayForSpotOrder = TimeSpan.FromSeconds(int.Parse(node.Attribute("MaxPriceDelayForSpotOrder").Value));
    //        }

    //        if (node.Attribute("RiskActionOnPendingConfirmLimit") != null)
    //        {
    //            this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)(byte.Parse(node.Attribute("RiskActionOnPendingConfirmLimit").Value));
    //        }

    //        this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)(byte.Parse(node.Attribute("LmtQuantityOnMaxLotChange").Value));
    //        this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)(byte.Parse(node.Attribute("STPAtHitPriceOption").Value));
    //        this._evaluateIfDonePlacingOnStpConfirm = node.DBAttToBoolean("EvaluateIfDonePlacingOnStpConfirm");
    //    }


    //    internal void Update(XElement systemParameterNode)
    //    {
    //        foreach (var attribute in systemParameterNode.Attributes())
    //        {
    //            switch (attribute.Name.ToString())
    //            {
    //                case "TradeDayBeginTime":
    //                    this._tradeDayBeginTime = Convert.ToDateTime(attribute.Value);
    //                    break;
    //                case "MooMocAcceptDuration":
    //                    this._mooMocAcceptDuration = XmlConvert.ToInt32(attribute.Value);
    //                    break;
    //                case "MooMocCancelDuration":
    //                    this._mooMocCancelDuration = XmlConvert.ToInt32(attribute.Value);
    //                    break;

    //                case "PlaceCheckType":
    //                    this._placeCheckType = (PlaceCheckType)XmlConvert.ToInt32(attribute.Value);
    //                    break;
    //                case "NeedsFillCheck":
    //                    this._needsFillCheck = XmlConvert.ToBoolean(attribute.Value);
    //                    break;
    //                case "EvaluateIfDonePlacingOnStpConfirm":
    //                    this._evaluateIfDonePlacingOnStpConfirm = XmlConvert.ToBoolean(attribute.Value);
    //                    break;
    //                case "CanDealerViewAccountInfo":
    //                    this._canDealerViewAccountInfo = XmlConvert.ToBoolean(attribute.Value);
    //                    break;

    //                case "UseNightNecessaryWhenBreak":
    //                    this._useNightNecessaryWhenBreak = XmlConvert.ToBoolean(attribute.Value);
    //                    break;
    //                case "IncludeFeeOnRiskAction":
    //                    this._includeFeeOnRiskAction = XmlConvert.ToBoolean(attribute.Value);
    //                    break;
    //                case "RiskActionOnPendingConfirmLimit":
    //                    this.RiskActionOnPendingConfirmLimit = (RiskActionOnPendingConfirmLimit)(XmlConvert.ToInt32(attribute.Value));
    //                    break;
    //                case "LmtQuantityOnMaxLotChange":
    //                    this._executeActionWhenPendingOrderLotExceedMaxOtherLot = (ExecuteActionWhenPendingOrderLotExceedMaxOtherLot)(XmlConvert.ToInt32(attribute.Value));
    //                    break;
    //                case "STPAtHitPriceOption":
    //                    this._optionOfSTPAtHitPrice = (STPAtHitPriceOption)(XmlConvert.ToInt32(attribute.Value));
    //                    break;
    //                case "DQDelayTimeOption":
    //                    this._dqDelayTimeOption = (DQDelayTimeOption)(XmlConvert.ToInt32(attribute.Value));
    //                    break;
    //                case "BalanceDeficitAllowPay":
    //                    this.BalanceDeficitAllowPay = XmlConvert.ToBoolean(attribute.Value);
    //                    break;
    //            }
    //        }
    //    }

    //    internal bool NeedsPlaceCheck(OrderType orderType)
    //    {
    //        return (this._placeCheckType == PlaceCheckType.AllOrder);
    //    }

    //    internal bool NeedsFillCheck(OrderType orderType)
    //    {
    //        Trace.WriteLine("NeedsFillCheck");
    //        if (this.NeedsPlaceCheck(orderType) || this.NeedsOldPlaceCheck(orderType))
    //        {
    //            return this._needsFillCheck;
    //        }
    //        else
    //        {
    //            return true;
    //        }
    //    }

    //    internal bool NeedsOldPlaceCheck(OrderType orderType)
    //    {
    //        return (this.PlaceCheckType == PlaceCheckType.InstanceOrder && (orderType == OrderType.SpotTrade || orderType == OrderType.Market));
    //    }

    //    internal TimeSpan? MaxPriceDelayForSpotOrder { get; private set; }

    //    internal bool BalanceDeficitAllowPay
    //    {
    //        get;
    //        private set;
    //    }
    //}

}