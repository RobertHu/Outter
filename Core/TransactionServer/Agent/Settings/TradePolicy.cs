using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using iExchange.Common;
using System.Diagnostics;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;
using Core.TransactionServer.Agent.BLL;
using log4net;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public enum LimitOption
    {
        AcceptHedgeOnly = 0, //Accept Hedge, Disallow unlock and New
        AcceptAll = 1, //Accept Hedge, New, and Unlock, means no any check needed
        DisallowAll = 2, //Disallow Hedge, New, Unlock,
        AllowUnlockOnly = 3 //Disallow Hedge, New and Accept Unlock
    }

    public static class LimitOptionHelper
    {
        public static bool AllowNew(this LimitOption option)
        {
            return option == LimitOption.AcceptAll;
        }

        public static bool AllowHedge(this LimitOption option)
        {
            return option == LimitOption.AcceptAll || option == LimitOption.AcceptHedgeOnly;
        }

        public static bool AllowUnlock(this LimitOption option)
        {
            return option == LimitOption.AcceptAll || option == LimitOption.AllowUnlockOnly;
        }
    }

    public sealed class TradePolicy
    {
        private Guid id;
        private bool isFreeHedge;
        private bool isFreeOverHedge;

        private NecessaryPolicy openNecessaryPolicy;
        private NecessaryPolicy closeNecessaryPolicy;

        private decimal alertLevel1;
        private decimal alertLevel2;
        private decimal alertLevel3;
        private decimal alertLevel4;
        private decimal alertLevel1Lock;
        private decimal alertLevel2Lock;
        private decimal alertLevel3Lock;
        private decimal alertLevel4Lock;

        internal TradePolicy(IDBRow tradePolicyRow)
        {
            this.id = (Guid)tradePolicyRow["ID"];
            this.isFreeHedge = (bool)tradePolicyRow["IsFreeHedge"];
            this.isFreeOverHedge = (bool)tradePolicyRow["IsFreeOverHedge"];

            this.openNecessaryPolicy = new NecessaryPolicy(tradePolicyRow, "Open");
            this.closeNecessaryPolicy = new NecessaryPolicy(tradePolicyRow, "Close");

            this.alertLevel1 = (decimal)tradePolicyRow["AlertLevel1"];
            this.alertLevel2 = (decimal)tradePolicyRow["AlertLevel2"];
            this.alertLevel3 = (decimal)tradePolicyRow["AlertLevel3"];
            this.alertLevel4 = (decimal)tradePolicyRow["AlertLevel4"];

            this.alertLevel1Lock = (decimal)tradePolicyRow["AlertLevel1Lock"];
            this.alertLevel2Lock = (decimal)tradePolicyRow["AlertLevel2Lock"];
            this.alertLevel3Lock = (decimal)tradePolicyRow["AlertLevel3Lock"];
            this.alertLevel4Lock = (decimal)tradePolicyRow["AlertLevel4Lock"];

            if (tradePolicyRow.Contains("BinaryOptionBetLimit"))
            {
                this.BinaryOptionBetLimit = (decimal)tradePolicyRow["BinaryOptionBetLimit"];
            }
            else
            {
                this.BinaryOptionBetLimit = 0;
            }

        }

        internal TradePolicy(XElement tradePolicyNode)
        {
            this.openNecessaryPolicy = new NecessaryPolicy(tradePolicyNode, "Open");
            this.closeNecessaryPolicy = new NecessaryPolicy(tradePolicyNode, "Close");

            this.Update(tradePolicyNode);
        }

        #region Common internal properties definition
        public Guid ID
        {
            get { return this.id; }
        }

        internal bool IsFreeHedge
        {
            get { return this.isFreeHedge; }
        }
        internal bool IsFreeOverHedge
        {
            get { return this.isFreeOverHedge; }
        }
        internal NecessaryPolicy OpenNecessaryPolicy
        {
            get { return this.openNecessaryPolicy; }
        }
        internal NecessaryPolicy CloseNecessaryPolicy
        {
            get { return this.closeNecessaryPolicy; }
        }
        internal decimal AlertLevel1
        {
            get { return this.alertLevel1; }
        }
        internal decimal AlertLevel2
        {
            get { return this.alertLevel2; }
        }
        internal decimal AlertLevel3
        {
            get { return this.alertLevel3; }
        }
        internal decimal AlertLevel4
        {
            get { return this.alertLevel4; }
        }
        internal decimal AlertLevel1Lock
        {
            get { return this.alertLevel1Lock; }
        }
        internal decimal AlertLevel2Lock
        {
            get { return this.alertLevel2Lock; }
        }
        internal decimal AlertLevel3Lock
        {
            get { return this.alertLevel3Lock; }
        }
        internal decimal AlertLevel4Lock
        {
            get { return this.alertLevel4Lock; }
        }

        internal decimal BinaryOptionBetLimit { get; private set; }

        #endregion Common internal properties definition


        public TradePolicyDetail this[Guid instrumentId, DateTime? tradeDay]
        {
            get
            {
                return Settings.Setting.Default.GetTradePolicyDetail(instrumentId, this.id, tradeDay);
            }
        }

        internal bool Update(XElement tradePolicyNode)
        {
            foreach (XAttribute attribute in tradePolicyNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.id = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "IsFreeHedge":
                        this.isFreeHedge = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "IsFreeOverHedge":
                        this.isFreeOverHedge = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "AlertLevel1":
                        this.alertLevel1 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel2":
                        this.alertLevel2 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel3":
                        this.alertLevel3 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel4":
                        this.alertLevel4 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel1Lock":
                        this.alertLevel1Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel2Lock":
                        this.alertLevel2Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel3Lock":
                        this.alertLevel3Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel4Lock":
                        this.alertLevel4Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "BinaryOptionBetLimit":
                        this.BinaryOptionBetLimit = XmlConvert.ToDecimal(attribute.Value);
                        break;
                }
            }

            this.openNecessaryPolicy.Update(tradePolicyNode, "Open");
            this.closeNecessaryPolicy.Update(tradePolicyNode, "Close");

            return true;
        }

    }

    internal sealed class NecessaryPolicy
    {
        private MarginCheckOption marginCheckOption;
        private decimal netFactor;
        private decimal hedgeFactor;

        internal NecessaryPolicy(XElement necessaryPolicyNode, string prefix)
        {
            this.Update(necessaryPolicyNode, prefix);
        }

        internal NecessaryPolicy(IDBRow necessaryPolicyRow, string prefix)
        {
            this.marginCheckOption = (MarginCheckOption)(int)necessaryPolicyRow[prefix + "MarginCheckOption"];
            this.netFactor = (decimal)necessaryPolicyRow[prefix + "NetFactor"];
            this.hedgeFactor = (decimal)necessaryPolicyRow[prefix + "HedgeFactor"];
        }

        #region Properties
        internal MarginCheckOption MarginCheckOption
        {
            get { return this.marginCheckOption; }
        }
        internal decimal NetFactor
        {
            get { return this.netFactor; }
        }
        internal decimal HedgeFactor
        {
            get { return this.hedgeFactor; }
        }
        #endregion

        #region Methods
        internal decimal Calculate(decimal netNecessary, decimal hedgeNecessary)
        {
            return netNecessary * this.netFactor + hedgeNecessary * this.hedgeFactor;
        }

        internal void Update(XElement necessaryPolicyNode, string prefix)
        {
            int prefixLength = prefix.Length;
            foreach (var attribute in necessaryPolicyNode.Attributes())
            {
                if (!attribute.Name.ToString().StartsWith(prefix)) continue;

                switch (attribute.Name.ToString().Substring(prefixLength))
                {
                    case "MarginCheckOption":
                        this.marginCheckOption = (MarginCheckOption)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "NetFactor":
                        this.netFactor = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "HedgeFactor":
                        this.hedgeFactor = XmlConvert.ToDecimal(attribute.Value);
                        break;
                }
            }
        }
        #endregion
    }

    public sealed class TradePolicyDetail
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradePolicyDetail));

        private Guid instrumentId;

        private bool isTradeActive;
        private decimal contractSize;

        private decimal minCommissionOpen;
        private decimal minCommissionClose;

        private decimal commissionOpen;
        private decimal commissionCloseD;
        private decimal commissionCloseO;
        private decimal pairRelationFactor;

        private decimal levyOpen;
        private decimal levyClose;

        private decimal marginD;
        private decimal marginO;
        private decimal marginLockedD;
        private decimal marginLockedO;
        private decimal marginSpot;
        private decimal marginSpotSpread;
        private decimal riskCredit;

        private int necessaryRound;
        private decimal alertLevel1;
        private decimal alertLevel2;
        private decimal alertLevel3;
        private decimal alertLevel4;
        private decimal alertLevel1Lock;
        private decimal alertLevel2Lock;
        private decimal alertLevel3Lock;
        private decimal alertLevel4Lock;
        private decimal? accountMaxOpenLot = null;

        private LimitOption isAcceptNewStop;
        private bool isAcceptNewMOOMOC;
        private LimitOption isAcceptNewLimit;

        private bool multipleCloseAllowed;
        private bool allowNewOCO;

        private decimal oiPercent;

        private PhysicalTradeSide allowedPhysicalTradeSides;
        private int buyInterestValueDay;
        private int sellInterestValueDay;
        private int frozenFundMatureDay;
        private decimal discountOfOdd;
        private decimal valueDiscountAsMargin;
        private decimal instalmentPledgeDiscount;

        private Guid? instalmentPolicyId;
        private Guid? physicalPaymentDiscountId;

        private decimal shortSellDownPayment;
        private decimal partPaidPhysicalNecessary;

        private Guid? binaryOptionPolicyId;

        #region Common internal properties definition
        internal TradePolicy TradePolicy { get; private set; }

        internal Guid InstrumentId
        {
            get { return this.instrumentId; }
        }
        internal bool IsTradeActive
        {
            get { return this.isTradeActive; }
        }
        internal decimal ContractSize
        {
            get { return this.contractSize; }
        }

        internal decimal MinCommissionOpen
        {
            get { return this.minCommissionOpen; }
        }
        internal decimal MinCommissionClose
        {
            get { return this.minCommissionClose; }
        }

        internal decimal CommissionOpen
        {
            get { return this.commissionOpen; }
        }
        internal decimal CommissionCloseD
        {
            get { return this.commissionCloseD; }
        }
        internal decimal CommissionCloseO
        {
            get { return this.commissionCloseO; }
        }
        internal decimal PairRelationFactor
        {
            get { return this.pairRelationFactor; }
        }

        internal decimal LevyOpen
        {
            get { return this.levyOpen; }
        }
        internal decimal LevyClose
        {
            get { return this.levyClose; }
        }
        internal decimal MarginD
        {
            get { return this.marginD; }
        }
        internal decimal MarginO
        {
            get { return this.marginO; }
        }
        internal decimal MarginLockedD
        {
            get { return this.marginLockedD; }
        }
        internal decimal MarginLockedO
        {
            get { return this.marginLockedO; }
        }

        internal decimal RiskCredit
        {
            get { return this.riskCredit; }
        }

        internal decimal MarginSpot
        {
            get { return this.marginSpot; }
        }
        internal decimal MarginSpotSpread
        {
            get { return this.marginSpotSpread; }
        }
    
        internal int NecessaryRound
        {
            get { return this.necessaryRound; }
        }
        internal decimal OIPercent
        {
            get { return this.oiPercent; }
        }
        internal decimal? AccountMaxOpenLot
        {
            get { return this.accountMaxOpenLot; }
        }
        internal decimal AlertLevel1
        {
            get
            {
                return (this.alertLevel1 >= 0 ? this.alertLevel1 : this.TradePolicy.AlertLevel1);
            }
        }
        internal decimal AlertLevel2
        {
            get
            {
                return (this.alertLevel2 >= 0 ? this.alertLevel2 : this.TradePolicy.AlertLevel2);
            }
        }
        internal decimal AlertLevel3
        {
            get
            {
                return (this.alertLevel3 >= 0 ? this.alertLevel3 : this.TradePolicy.AlertLevel3);
            }
        }
        internal decimal AlertLevel4
        {
            get
            {
                return (this.alertLevel4 >= 0 ? this.alertLevel4 : this.TradePolicy.AlertLevel4);
            }
        }
        internal decimal AlertLevel1Lock
        {
            get
            {
                return (this.alertLevel1Lock >= 0 ? this.alertLevel1Lock : this.TradePolicy.AlertLevel1Lock);
            }
        }
        internal decimal AlertLevel2Lock
        {
            get
            {
                return (this.alertLevel2Lock >= 0 ? this.alertLevel2Lock : this.TradePolicy.AlertLevel2Lock);
            }
        }
        internal decimal AlertLevel3Lock
        {
            get
            {
                return (this.alertLevel3Lock >= 0 ? this.alertLevel3Lock : this.TradePolicy.AlertLevel3Lock);
            }
        }
        internal decimal AlertLevel4Lock
        {
            get
            {
                return (this.alertLevel4Lock >= 0 ? this.alertLevel4Lock : this.TradePolicy.AlertLevel4Lock);
            }
        }

        internal LimitOption IsAcceptNewStop
        {
            get { return this.isAcceptNewStop; }
        }
        internal bool IsAcceptNewMOOMOC
        {
            get { return this.isAcceptNewMOOMOC; }
        }
        internal LimitOption IsAcceptNewLimit
        {
            get { return this.isAcceptNewLimit; }
        }

        internal bool MultipleCloseAllowed
        {
            get { return this.multipleCloseAllowed; }
        }

        //public bool ChangePlacedOrderAllowed
        //{
        //    get { return this.changePlacedOrderAllowed; }
        //}

        internal bool AllowNewOCO
        {
            get { return this.allowNewOCO; }
        }

        internal Guid? VolumeNecessaryId
        {
            get;
            private set;
        }

        internal VolumeNecessary VolumeNecessary
        {
            get;
            set;
        }

        internal PhysicalTradeSide AllowedPhysicalTradeSides
        {
            get { return this.allowedPhysicalTradeSides; }
        }

        internal int BuyInterestValueDay
        {
            get { return this.buyInterestValueDay; }
        }

        internal int SellInterestValueDay
        {
            get { return this.sellInterestValueDay; }
        }

        internal int FrozenFundMatureDay
        {
            get { return this.frozenFundMatureDay; }
        }

        internal decimal DiscountOfOdd
        {
            get { return this.discountOfOdd; }
        }

        internal decimal ValueDiscountAsMargin
        {
            get { return this.valueDiscountAsMargin; }
        }

        internal decimal InstalmentPledgeDiscount
        {
            get { return this.instalmentPledgeDiscount; }
        }

        internal Guid? InstalmentPolicyId
        {
            get { return this.instalmentPolicyId; }
        }

        internal decimal ShortSellDownPayment
        {
            get { return this.shortSellDownPayment; }
        }

        internal decimal PartPaidPhysicalNecessary
        {
            get { return this.partPaidPhysicalNecessary; }
        }

        public Guid? BinaryOptionPolicyID
        {
            get { return this.binaryOptionPolicyId; }
        }

        internal Guid? PhysicalPaymentDiscountId
        {
            get { return this.physicalPaymentDiscountId; }
        }

        internal PhysicalPaymentDiscountPolicy PhysicalPaymentDiscountPolicy(DateTime? tradeDay)
        {
            return this.PhysicalPaymentDiscountId == null ? null : Settings.Setting.Default.GetPhysicalPaymentDiscountPolicy(this.PhysicalPaymentDiscountId.Value, tradeDay);
        }

        public decimal OtherFeeClose { get; private set; }

        public decimal OtherFeeOpen { get; private set; }

        #endregion Common internal properties definition


        internal int InterestCut { get; private set; }

        internal TradePolicyDetail(IDBRow tradePolicyDetailRow, TradePolicy tradePolicy)
        {
            this.TradePolicy = tradePolicy;
            this.Update(tradePolicyDetailRow);
        }

        internal TradePolicyDetail(XElement tradePolicyDetailNode, TradePolicy tradePolicy)
        {
            this.Update(tradePolicyDetailNode, tradePolicy);
        }

        //Replace has no key change, used for get settings in the past 
        internal void Update(IDBRow tradePolicyDetailRow)
        {
            this.instrumentId = tradePolicyDetailRow.GetColumn<Guid>("InstrumentID");

            this.multipleCloseAllowed = tradePolicyDetailRow.GetColumn<bool>("MultipleCloseAllowed");
            //this.changePlacedOrderAllowed = (bool)tradePolicyDetailRow["ChangePlacedOrderAllowed"];
            this.isTradeActive = tradePolicyDetailRow.GetColumn<bool>("IsTradeActive");
            this.contractSize = tradePolicyDetailRow.GetColumn<decimal>("ContractSize");

            this.minCommissionOpen = tradePolicyDetailRow.GetColumn<decimal>("MinCommissionOpen");
            this.minCommissionClose = tradePolicyDetailRow.GetColumn<decimal>("MinCommissionClose");

            this.commissionOpen = tradePolicyDetailRow.GetColumn<decimal>("CommissionOpen");
            this.commissionCloseD = tradePolicyDetailRow.GetColumn<decimal>("CommissionCloseD");
            this.commissionCloseO = tradePolicyDetailRow.GetColumn<decimal>("CommissionCloseO");
            this.pairRelationFactor = tradePolicyDetailRow.GetColumn<decimal>("PairRelationFactor");

            this.levyOpen = tradePolicyDetailRow.GetColumn<decimal>("LevyOpen");
            this.levyClose = tradePolicyDetailRow.GetColumn<decimal>("LevyClose");

            this.marginD = tradePolicyDetailRow.GetColumn<decimal>("MarginD");
            this.marginO = tradePolicyDetailRow.GetColumn<decimal>("MarginO");
            this.marginLockedD = tradePolicyDetailRow.GetColumn<decimal>("MarginLockedD");
            this.marginLockedO = tradePolicyDetailRow.GetColumn<decimal>("MarginLockedO");
            this.marginSpot = tradePolicyDetailRow.GetColumn<decimal>("MarginSpot");
            this.marginSpotSpread = tradePolicyDetailRow.GetColumn<decimal>("MarginSpotSpread");
            this.riskCredit = tradePolicyDetailRow.GetColumn<decimal>("RiskCredit");

            this.necessaryRound = tradePolicyDetailRow.GetColumn<int>("NecessaryRound");
            this.alertLevel1 = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel1");
            this.alertLevel2 = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel2");
            this.alertLevel3 = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel3");
            this.alertLevel4 = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel4");
            this.alertLevel1Lock = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel1Lock");
            this.alertLevel2Lock = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel2Lock");
            this.alertLevel3Lock = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel3Lock");
            this.alertLevel4Lock = tradePolicyDetailRow.GetColumn<decimal>("AlertLevel4Lock");

            this.isAcceptNewStop = (LimitOption)tradePolicyDetailRow.GetColumn<int>("IsAcceptNewStop");
            this.isAcceptNewMOOMOC = tradePolicyDetailRow.GetColumn<bool>("IsAcceptNewMOOMOC");
            this.isAcceptNewLimit = (LimitOption)tradePolicyDetailRow.GetColumn<int>("IsAcceptNewLimit");
            this.oiPercent = tradePolicyDetailRow.GetColumn<decimal>("OIPercent");
            if (!tradePolicyDetailRow.ExistsColumn("AccountMaxOpenLot") || tradePolicyDetailRow["AccountMaxOpenLot"] == DBNull.Value)
            {
                this.accountMaxOpenLot = null;
            }
            else
            {
                this.accountMaxOpenLot = (decimal)tradePolicyDetailRow["AccountMaxOpenLot"];
            }

            if (tradePolicyDetailRow.ExistsColumn("AllowNewOCO"))
            {
                this.allowNewOCO = tradePolicyDetailRow.GetColumn<bool>("AllowNewOCO");
            }
            else
            {
                this.allowNewOCO = false;
            }

            this.VolumeNecessaryId = tradePolicyDetailRow.GetColumn<Guid?>("VolumeNecessaryId");
            this.instalmentPolicyId = tradePolicyDetailRow.GetColumn<Guid?>("InstalmentPolicyId");
            this.physicalPaymentDiscountId = tradePolicyDetailRow["PhysicalPaymentDiscountId"] == DBNull.Value ? null : (Guid?)tradePolicyDetailRow["PhysicalPaymentDiscountId"];

            if (tradePolicyDetailRow.ExistsColumn("BOPolicyID") && tradePolicyDetailRow["BOPolicyID"] != DBNull.Value)
            {
                this.binaryOptionPolicyId = (Guid?)tradePolicyDetailRow["BOPolicyID"];
            }
            else
            {
                this.binaryOptionPolicyId = null;
            }

            this.allowedPhysicalTradeSides = (PhysicalTradeSide)tradePolicyDetailRow.GetColumn<int>("AllowedPhysicalTradeSides");
            this.buyInterestValueDay = tradePolicyDetailRow.GetColumn<int>("BuyInterestValueDay");
            this.sellInterestValueDay = tradePolicyDetailRow.GetColumn<int>("SellInterestValueDay");
            this.frozenFundMatureDay = tradePolicyDetailRow.GetColumn<int>("PhysicalValueMatureDay");
            this.discountOfOdd = tradePolicyDetailRow.GetColumn<decimal>("DiscountOfOdd");
            this.valueDiscountAsMargin = tradePolicyDetailRow.GetColumn<decimal>("ValueDiscountAsMargin");
            this.instalmentPledgeDiscount = tradePolicyDetailRow.GetColumn<decimal>("InstalmentPledgeDiscount");
            this.shortSellDownPayment = tradePolicyDetailRow.GetColumn<decimal>("ShortSellDownPayment");
            this.partPaidPhysicalNecessary = tradePolicyDetailRow.GetColumn<decimal>("PartPaidPhysicalNecessary");
            this.OtherFeeClose = (decimal)tradePolicyDetailRow["OtherFeeClose"];
            this.OtherFeeOpen = (decimal)tradePolicyDetailRow["OtherFeeOpen"];
            this.InterestCut = (byte)tradePolicyDetailRow["InterestCut"];
        }

        //Update has key change, triggered by user modify
        internal void Update(XElement tradePolicyDetailNode, TradePolicy tradePolicy)
        {
            this.TradePolicy = tradePolicy;
            this.InternalUpdate(tradePolicyDetailNode);
        }

        private bool InternalUpdate(XElement tradePolicyDetailNode)
        {
            foreach (var attribute in tradePolicyDetailNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "InstrumentID":
                        this.instrumentId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "IsTradeActive":
                        this.isTradeActive = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "ContractSize":
                        this.contractSize = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "MinCommissionOpen":
                        this.minCommissionOpen = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MinCommissionClose":
                        this.minCommissionClose = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "CommissionOpen":
                        this.commissionOpen = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CommissionCloseD":
                        this.commissionCloseD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CommissionCloseO":
                        this.commissionCloseO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "PairRelationFactor":
                        this.pairRelationFactor = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "LevyOpen":
                        this.levyOpen = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "LevyClose":
                        this.levyClose = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "OtherFeeOpen":
                        this.OtherFeeOpen = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "OtherFeeClose":
                        this.OtherFeeClose = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginD":
                        this.marginD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginO":
                        this.marginO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginLockedD":
                        this.marginLockedD = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginLockedO":
                        this.marginLockedO = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "RiskCredit":
                        this.riskCredit = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginSpot":
                        this.marginSpot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MarginSpotSpread":
                        this.marginSpotSpread = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "NecessaryRound":
                        this.necessaryRound = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AlertLevel1":
                        this.alertLevel1 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel2":
                        this.alertLevel2 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel3":
                        this.alertLevel3 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel4":
                        this.alertLevel4 = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel1Lock":
                        this.alertLevel1Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel2Lock":
                        this.alertLevel2Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel3Lock":
                        this.alertLevel3Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AlertLevel4Lock":
                        this.alertLevel4Lock = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "IsAcceptNewStop":
                        this.isAcceptNewStop = (LimitOption)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "IsAcceptNewMOOMOC":
                        this.isAcceptNewMOOMOC = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "IsAcceptNewLimit":
                        this.isAcceptNewLimit = (LimitOption)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "MultipleCloseAllowed":
                        this.multipleCloseAllowed = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "OIPercent":
                        this.oiPercent = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AccountMaxOpenLot":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.accountMaxOpenLot = null;
                        }
                        else
                        {
                            this.accountMaxOpenLot = XmlConvert.ToDecimal(attribute.Value);
                        }
                        Logger.InfoFormat("accountMaxOpenLot = {0}, xmlValue = {1}", accountMaxOpenLot, attribute.Value);
                        break;
                    case "AllowNewOCO":
                        this.allowNewOCO = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "VolumeNecessaryId":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.VolumeNecessaryId = null;
                        }
                        else
                        {
                            this.VolumeNecessaryId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "InstalmentPolicyId":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.instalmentPolicyId = null;
                        }
                        else
                        {
                            this.instalmentPolicyId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "BOPolicyID":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.binaryOptionPolicyId = null;
                        }
                        else
                        {
                            this.binaryOptionPolicyId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "PhysicalPaymentDiscountID":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.physicalPaymentDiscountId = null;
                        }
                        else
                        {
                            this.physicalPaymentDiscountId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                    case "AllowedPhysicalTradeSides":
                        this.allowedPhysicalTradeSides = (PhysicalTradeSide)(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "BuyInterestValueDay":
                        this.buyInterestValueDay = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "SellInterestValueDay":
                        this.sellInterestValueDay = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "PhysicalValueMatureDay":
                        this.frozenFundMatureDay = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "DiscountOfOdd":
                        this.discountOfOdd = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "ValueDiscountAsMargin":
                        this.valueDiscountAsMargin = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "InstalmentPledgeDiscount":
                        this.instalmentPledgeDiscount = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "ShortSellDownPayment":
                        this.shortSellDownPayment = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "PartPaidPhysicalNecessary":
                        this.partPaidPhysicalNecessary = XmlConvert.ToDecimal(attribute.Value);
                        break;
                }
            }

            return true;
        }

        internal decimal GetCommissionClose(bool isDayCloseRelation)
        {
            return isDayCloseRelation ? this.commissionCloseD : this.commissionCloseO;
        }
    }
}
