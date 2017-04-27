using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Data;
using System.Xml;
using Core.TransactionServer.Agent.Framework;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;
using log4net;

namespace Core.TransactionServer.Agent.Settings
{
    internal enum MarginInterestOption
    {
        None,
        Usable,
        Balance,
        Equity
    }

    public class Account
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Account));
        private Guid id;
        private Guid organizationId;
        private Guid customerId;
        private Guid currencyId;
        private Guid tradePolicyId;
        private Guid? specialTradePolicyId;

        private string code;
        private AccountType type;

        private bool isActive;
        private bool isTradingAllowed;
        private DateTime beginTime;
        private DateTime endTime;

        private bool isSplitLot = true;
        private decimal maxNecessary;
        private bool isAutoClose;
        private bool isMultiCurrency;
        private decimal creditLotD;
        private decimal creditLotO;
        private decimal creditAmount;
        private decimal shortMargin;
        private decimal rateMarginD;
        private decimal rateMarginO;
        private decimal rateMarginLockD;
        private decimal rateMarginLockO;
        private decimal rateCommission;
        private decimal rateLevy;
        private decimal rateRiskNecessary;
        private decimal? maxOpenLot = null;
        private decimal? maxPhyscialValue = null;

        private bool isAutoCut;
        private RiskLevelAction riskLevelAction;
        private decimal? riskActionMinimumEquity = null;
        private int forbiddenAlert;
        private RiskActionMode riskActionMode;
        private Guid? agentId;
        private bool allowSalesTrading;
        private bool allowManagerTrading;

        #region Common internal properties definition
        internal Guid Id
        {
            get { return this.id; }
        }
        internal string Code
        {
            get { return this.code; }
        }
        internal AccountType Type
        {
            get { return this.type; }
        }
        internal Guid OrganizationId
        {
            get { return this.organizationId; }
        }

        internal bool IsSplitLot
        {
            get { return this.isSplitLot; }
        }
        internal decimal MaxNecessary
        {
            get { return this.maxNecessary; }
        }
        internal bool IsAutoClose
        {
            get { return this.isAutoClose; }
        }

        public bool IsMultiCurrency
        {
            get { return this.isMultiCurrency; }
            private set { this.isMultiCurrency = value; }
        }

        internal Guid CurrencyId
        {
            get { return this.currencyId; }
        }

        internal Currency Currency(DateTime? tradeDay)
        {
            return Settings.Setting.Default.GetCurrency(this.CurrencyId, tradeDay);
        }

        internal Guid TradePolicyId
        {
            get { return this.tradePolicyId; }
        }

        internal TradePolicy TradePolicy(DateTime? tradeDay = null)
        {
            return Settings.Setting.Default.GetTradePolicy(this.TradePolicyId, tradeDay);
        }

        internal Guid? SpecialTradePolicyId
        {
            get { return this.specialTradePolicyId; }
        }

        internal SpecialTradePolicy SpecialTradePolicy(DateTime? tradeDay)
        {
            return this.SpecialTradePolicyId == null ? null : Settings.Setting.Default.GetSpecialTradePolicy(this.SpecialTradePolicyId.Value, tradeDay);
        }

        internal decimal CreditLotD
        {
            get { return this.creditLotD; }
        }
        internal decimal CreditLotO
        {
            get { return this.creditLotO; }
        }
        internal decimal CreditAmount
        {
            get { return this.creditAmount; }
        }
        internal decimal ShortMargin
        {
            get { return this.shortMargin; }
        }
        internal decimal RateMarginD
        {
            get { return this.rateMarginD; }
        }
        internal decimal RateMarginO
        {
            get { return this.rateMarginO; }
        }
        internal decimal RateMarginLockD
        {
            get { return this.rateMarginLockD; }
        }
        internal decimal RateMarginLockO
        {
            get { return this.rateMarginLockO; }
        }
        internal decimal RateCommission
        {
            get { return this.rateCommission; }
        }
        internal decimal RateLevy
        {
            get { return this.rateLevy; }
        }
        internal decimal RateRiskNecessary
        {
            get { return this.rateRiskNecessary; }
        }

        internal bool IsAutoCut
        {
            get { return this.isAutoCut; }
        }
        internal RiskLevelAction RiskLevelAction
        {
            get { return this.riskLevelAction; }
        }
        internal decimal? RiskActionMinimumEquity
        {
            get { return this.riskActionMinimumEquity; }
        }
        internal int ForbiddenAlert
        {
            get { return this.forbiddenAlert; }
        }
        internal RiskActionMode RiskActionMode
        {
            get { return this.riskActionMode; }
        }
        internal decimal? MaxOpenLot
        {
            get { return this.maxOpenLot; }
        }
        internal decimal? MaxPhyscialValue
        {
            get { return this.maxPhyscialValue; }
        }
        internal Guid CustomerId
        {
            get { return this.customerId; }
        }
        internal Guid? AgentId
        {
            get { return this.agentId; }
        }
        internal bool AllowSalesTrading
        {
            get { return this.allowSalesTrading; }
        }
        internal bool AllowManagerTrading
        {
            get { return this.allowManagerTrading; }
        }

        internal bool IsActive
        {
            get { return isActive; }
        }


        public string ThirdParty1 { get; private set; }
        public string ThirdParty2 { get; private set; }

        internal MarginInterestOption MarginInterestOption { get; private set; }


        internal decimal RateOtherFee { get; private set; }

        internal Guid? BlotterID { get; private set; }

        internal Guid? QuotePolicyID { get; private set; }
        internal DateTime UpdateTime { get; private set; }

        internal DateTime BeginTime
        {
            get { return this.beginTime; }
        }

        internal DateTime EndTime
        {
            get { return this.endTime; }
        }

        #endregion Common internal properties definition



        internal Account(IDBRow accountRow)
        {
            this.Update(accountRow);
        }

        internal Account(XElement accountNode)
        {
            this.Update(accountNode);
        }

        internal void Update(IDBRow accountRow)
        {
            this.id = accountRow.GetColumn<Guid>("ID");
            this.customerId = accountRow.GetColumn<Guid>("CustomerID");
            this.currencyId = accountRow.GetColumn<Guid>("CurrencyID");
            this.tradePolicyId = accountRow.GetColumn<Guid>("TradePolicyID");

            if (accountRow["SpecialTradePolicyID"] != DBNull.Value)
            {
                this.specialTradePolicyId = (Guid)accountRow["SpecialTradePolicyID"];
            }

            this.type = (AccountType)accountRow.GetColumn<int>("Type");
            this.code = accountRow.GetColumn<string>("Code");
            this.organizationId = accountRow.GetColumn<Guid>("OrganizationID");

            this.isActive = accountRow.GetColumn<bool>("IsActive");
            this.isTradingAllowed = accountRow.GetColumn<bool>("IsTradingAllowed");
            this.beginTime = accountRow.GetColumn<DateTime>("BeginTime");
            this.endTime = accountRow.GetColumn<DateTime>("EndTime");
            this.UpdateTime = accountRow.GetColumn<DateTime>("UpdateTime");

            this.isSplitLot = true;// (bool)accountRow["IsSplitLot"];
            this.maxNecessary = accountRow.GetColumn<decimal>("MaxNecessary");
            this.isAutoClose = accountRow.GetColumn<bool>("IsAutoClose");
            this.isMultiCurrency = accountRow.GetColumn<bool>("IsMultiCurrency");
            this.creditLotD = accountRow.GetColumn<decimal>("CreditLotD");
            this.creditLotO = accountRow.GetColumn<decimal>("CreditLotO");
            this.creditAmount = accountRow.GetColumn<decimal>("CreditAmount");
            this.shortMargin = accountRow.GetColumn<decimal>("ShortMargin");
            this.rateMarginD = accountRow.GetColumn<decimal>("RateMarginD");
            this.rateMarginO = accountRow.GetColumn<decimal>("RateMarginO");
            this.rateMarginLockD = (decimal)accountRow["RateMarginLockD"];
            this.rateMarginLockO = (decimal)accountRow["RateMarginLockO"];
            this.rateCommission = (decimal)accountRow["RateCommission"];
            this.rateLevy = (decimal)accountRow["RateLevy"];
            this.rateRiskNecessary = (decimal)accountRow["RateRiskNecessary"];
            this.BlotterID = accountRow["BlotterID"] == DBNull.Value ? null : (Guid?)accountRow["BlotterID"];
            this.RateOtherFee = (decimal)accountRow["RateOtherFee"];
            this.MarginInterestOption = (MarginInterestOption)((int)accountRow["MarginInterestOption"]);
            this.isAutoCut = (bool)accountRow["IsAutoCut"];
            this.riskLevelAction = (RiskLevelAction)(int)accountRow["RiskLevelAction"];
            this.forbiddenAlert = (int)accountRow["ForbiddenAlert"];
            this.riskActionMode = (RiskActionMode)(int)accountRow["RiskActionMode"];
            if (!accountRow.Contains("RiskActionMinimumEquity") || accountRow["RiskActionMinimumEquity"] == DBNull.Value)
            {
                this.riskActionMinimumEquity = null;
            }
            else
            {
                this.riskActionMinimumEquity = (decimal)accountRow["RiskActionMinimumEquity"];
            }
            if (!accountRow.Contains("MaxOpenLot") || accountRow["MaxOpenLot"] == DBNull.Value)
            {
                this.maxOpenLot = null;
            }
            else
            {
                this.maxOpenLot = (decimal)accountRow["MaxOpenLot"];
            }

            if (!accountRow.Contains("MaxPhyscialValue") || accountRow["MaxPhyscialValue"] == DBNull.Value)
            {
                //this.maxPhyscialValue = null; //MaxPhyscialValue has no history, use current settings
            }
            else
            {
                this.maxPhyscialValue = (decimal)accountRow["MaxPhyscialValue"];
            }

            if (accountRow["AgentID"] != DBNull.Value)
            {
                this.agentId = (Guid)accountRow["AgentID"];
            }
            else
            {
                this.agentId = Guid.Empty;
            }
            if (accountRow.Contains("AllowSalesTrading"))
            {
                this.allowSalesTrading = (bool)accountRow["AllowSalesTrading"];
            }
            if (accountRow.Contains("AllowManagerTrading"))
            {
                this.allowManagerTrading = (bool)accountRow["AllowManagerTrading"];
            }

            if (accountRow.Contains("QuotePolicyID"))
            {
                this.QuotePolicyID = accountRow.GetColumn<Guid?>("QuotePolicyID");
            }

        }

        internal bool Update(XElement accountNode)
        {
            foreach (var attribute in accountNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.id = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "TradePolicyID":
                        Guid tradePolicyId = XmlConvert.ToGuid(attribute.Value);
                        if (this.tradePolicyId != tradePolicyId)
                        {
                            this.tradePolicyId = tradePolicyId;
                        }
                        break;
                    case "CustomerID":
                        Guid ownerId = XmlConvert.ToGuid(attribute.Value);
                        if (this.customerId != ownerId)
                        {
                            this.customerId = ownerId;
                        }
                        break;
                    case "CurrencyID":
                        Guid currencyId = XmlConvert.ToGuid(attribute.Value);
                        if (this.currencyId != currencyId)
                        {
                            this.currencyId = currencyId;
                        }
                        break;
                    case "SpecialTradePolicyID":
                        Guid? specialTradePolicyId = null;
                        if (!string.IsNullOrEmpty(attribute.Value))
                        {
                            specialTradePolicyId = XmlConvert.ToGuid(attribute.Value);
                        }
                        if (this.specialTradePolicyId != specialTradePolicyId)
                        {
                            this.specialTradePolicyId = specialTradePolicyId;
                        }

                        break;
                    case "Code":
                        this.code = attribute.Value;
                        break;
                    case "Type":
                        this.type = (AccountType)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "OrganizationID":
                        this.organizationId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "IsActive":
                        this.isActive = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "IsTradingAllowed":
                        this.isTradingAllowed = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "BeginTime":
                        try
                        {
                            this.beginTime = Convert.ToDateTime(attribute.Value);
                        }
                        catch
                        {
                            this.beginTime = DateTime.Now;//Back office broadcast tiem in wrong format
                        }
                        break;
                    case "EndTime":
                        try
                        {
                            this.endTime = Convert.ToDateTime(attribute.Value);
                        }
                        catch
                        {
                            this.endTime = DateTime.MaxValue;//Back office broadcast tiem in wrong format
                        }
                        break;
                    case "IsSplitLot":
                        this.isSplitLot = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "MaxNecessary":
                        this.maxNecessary = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "IsAutoClose":
                        this.isAutoClose = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "IsMultiCurrency":
                        this.isMultiCurrency = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "CreditLotD":
                        this.creditLotD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CreditLotO":
                        this.creditLotO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CreditAmount":
                        this.creditAmount = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "ShortMargin":
                        this.shortMargin = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateMarginD":
                        this.rateMarginD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateMarginO":
                        this.rateMarginO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateMarginLockD":
                        this.rateMarginLockD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateMarginLockO":
                        this.rateMarginLockO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateCommission":
                        this.rateCommission = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateLevy":
                        this.rateLevy = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateOtherFee":
                        this.RateOtherFee = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RateRiskNecessary":
                        this.rateRiskNecessary = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "IsAutoCut":
                        this.isAutoCut = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "AllowSalesTrading":
                        this.allowSalesTrading = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "AllowManagerTrading":
                        this.allowManagerTrading = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "RiskLevelAction":
                        this.riskLevelAction = (RiskLevelAction)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "ForbiddenAlert":
                        this.forbiddenAlert = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "MarginInterestOption":
                        this.MarginInterestOption = (MarginInterestOption)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "RiskActionMode":
                        this.riskActionMode = (RiskActionMode)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "MaxOpenLot":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.maxOpenLot = null;
                        }
                        else
                        {
                            this.maxOpenLot = XmlConvert.ToDecimal(attribute.Value);
                        }
                        break;
                    case "RiskActionMinimumEquity":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.riskActionMinimumEquity = null;
                        }
                        else
                        {
                            this.riskActionMinimumEquity = XmlConvert.ToDecimal(attribute.Value);
                        }
                        break;
                    case "MaxPhyscialValue":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.maxPhyscialValue = null;
                        }
                        else
                        {
                            this.maxPhyscialValue = XmlConvert.ToDecimal(attribute.Value);
                        }
                        break;
                    case "AgentID":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.agentId = null;
                        }
                        else
                        {
                            this.agentId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "BlotterId":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.BlotterID = null;
                        }
                        else
                        {
                            this.BlotterID = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "QuotePolicyId":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.QuotePolicyID = null;
                        }
                        else
                        {
                            this.QuotePolicyID = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                }
            }
            return true;
        }

        internal bool IsTrading(DateTime baseTime)
        {
            return this.isActive && this.isTradingAllowed && (this.beginTime <= baseTime && this.endTime > baseTime);
        }

        internal void ChangeLeverage(decimal rateMarginO, decimal rateMarginD, decimal rateMarginLockO, decimal rateMarginLockD)
        {
            this.rateMarginO = rateMarginO;
            this.rateMarginD = rateMarginD;
            this.rateMarginLockO = rateMarginLockO;
            this.rateMarginLockD = rateMarginLockD;
        }

    }
}