using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data;
using iExchange.Common;
using System.Xml.Linq;
using Core.TransactionServer.Engine;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class SpecialTradePolicy
    {
        private Dictionary<Guid, SpecialTradePolicyDetail> _specialTradePolicyDetails;

        internal Guid Id
        {
            get;
            private set;
        }

        internal SpecialTradePolicy(XElement tradePolicyNode)
        {
            this.Update(tradePolicyNode);

            this._specialTradePolicyDetails = new Dictionary<Guid, SpecialTradePolicyDetail>();
        }

        internal SpecialTradePolicy(IDBRow tradePolicyRow)
        {
            this.Id = (Guid)tradePolicyRow["ID"];

            this._specialTradePolicyDetails = new Dictionary<Guid, SpecialTradePolicyDetail>();
        }

        internal SpecialTradePolicyDetail this[Guid instrumentId]
        {
            get
            {
                if (_specialTradePolicyDetails.ContainsKey(instrumentId))
                {
                    return _specialTradePolicyDetails[instrumentId];
                }
                else
                {
                    return null;
                }
            }
        }

        internal bool ExistsSpecialTradePolicyDetail(Guid instrumentId)
        {
            return _specialTradePolicyDetails.ContainsKey(instrumentId);
        }


        internal void Add(SpecialTradePolicyDetail detail)
        {
            this._specialTradePolicyDetails.Add(detail.InstrumentId, detail);
        }

        internal void Remove(SpecialTradePolicyDetail detail)
        {
            this._specialTradePolicyDetails.Remove(detail.InstrumentId);
        }

        internal bool Update(XElement tradePolicyNode)
        {
            foreach (XAttribute attribute in tradePolicyNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.Id = XmlConvert.ToGuid(attribute.Value);
                        break;
                }
            }

            return true;
        }
    }

    internal enum CGSELevyCurrecyType
    {
        UseInstrumentCurrencyType = 0,
        UseAccountCurrencyType = 1
    }

    internal class SpecialTradePolicyDetail
    {
        private SpecialTradePolicy specialTradePolicy;
        private Guid instrumentId;

        private OrderLevelRiskBase autoLimitBase;
        private decimal autoLimitThreshold;
        private OrderLevelRiskBase autoStopBase;
        private decimal autoStopThreshold;

        private bool isFractionCommissionOn;
        private decimal commissionOpen;
        private decimal commissionCloseD;
        private decimal commissionCloseO;

        private bool isFractionLevyOn;
        private decimal levyOpen;
        private decimal levyClose;

        private decimal cgseNewLevyMultipler;
        private decimal cgseNewLevyRemainder;
        private decimal cgseCloseLevyMultipler;
        private decimal cgseCloseLevyRemainder;
        private CGSELevyCurrecyType cgseLevyCurrecyType;

        #region Common internal properties definition

        internal SpecialTradePolicy SpecialTradePolicy
        {
            get { return this.specialTradePolicy; }
        }

        internal Guid InstrumentId
        {
            get { return this.instrumentId; }
        }

        internal OrderLevelRiskBase AutoLimitBase
        {
            get { return this.autoLimitBase; }
        }

        internal decimal AutoLimitThreshold
        {
            get { return this.autoLimitThreshold; }
        }

        internal OrderLevelRiskBase AutoStopBase
        {
            get { return this.autoStopBase; }
        }

        internal decimal AutoStopThreshold
        {
            get { return this.autoStopThreshold; }
        }

        internal bool IsFractionCommissionOn
        {
            get { return this.isFractionCommissionOn; }
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

        internal bool IsFractionLevyOn
        {
            get { return this.isFractionLevyOn; }
        }
        internal decimal LevyOpen
        {
            get { return this.levyOpen; }
        }
        internal decimal LevyClose
        {
            get { return this.levyClose; }
        }
        internal decimal CGSENewLevyMultipler
        {
            get { return this.cgseNewLevyMultipler; }
        }
        internal decimal CGSENewLevyRemainder
        {
            get { return this.cgseNewLevyRemainder; }
        }
        internal decimal CGSECloseLevyMultipler
        {
            get { return this.cgseCloseLevyMultipler; }
        }
        internal decimal CGSECloseLevyRemainder
        {
            get { return this.cgseCloseLevyRemainder; }
        }
        internal CGSELevyCurrecyType CGSELevyCurrecyType
        {
            get { return this.cgseLevyCurrecyType; }
        }

        #endregion Common internal properties definition

        internal SpecialTradePolicyDetail(IDBRow  tradePolicyDetailRow, SpecialTradePolicy specialTradePolicy)
        {
            this.specialTradePolicy = specialTradePolicy;
            this.Update(tradePolicyDetailRow, specialTradePolicy);
        }

        internal SpecialTradePolicyDetail(XElement tradePolicyDetailNode, SpecialTradePolicy specialTradePolicy)
        {
            this.specialTradePolicy = specialTradePolicy;
            this.InternalUpdate(tradePolicyDetailNode, specialTradePolicy);
        }

        private void Update(IDBRow  tradePolicyDetailRow, SpecialTradePolicy specialTradePolicy)
        {
            this.instrumentId = (Guid)tradePolicyDetailRow["InstrumentID"];
            this.autoLimitBase = (OrderLevelRiskBase)tradePolicyDetailRow["AutoLimitBase"];
            this.autoLimitThreshold = (decimal)tradePolicyDetailRow["AutoLimitThreshold"];
            this.autoStopBase = (OrderLevelRiskBase)tradePolicyDetailRow["AutoStopBase"];
            this.autoStopThreshold = (decimal)tradePolicyDetailRow["AutoStopThreshold"];

            this.isFractionCommissionOn = (bool)tradePolicyDetailRow["IsFractionCommissionOn"];
            this.commissionOpen = (decimal)tradePolicyDetailRow["CommissionOpen"];
            this.commissionCloseD = (decimal)tradePolicyDetailRow["CommissionCloseD"];
            this.commissionCloseO = (decimal)tradePolicyDetailRow["CommissionCloseO"];

            this.isFractionLevyOn = (bool)tradePolicyDetailRow["IsFractionLevyOn"];
            this.levyOpen = (decimal)tradePolicyDetailRow["LevyOpen"];
            this.levyClose = (decimal)tradePolicyDetailRow["LevyClose"];

            this.cgseNewLevyMultipler = (decimal)tradePolicyDetailRow["CGSENewLevyMultipler"];
            this.cgseCloseLevyMultipler = (decimal)tradePolicyDetailRow["CGSECloseLevyMultipler"];
            this.cgseNewLevyRemainder = (decimal)tradePolicyDetailRow["CGSENewLevyRemainder"];
            this.cgseCloseLevyRemainder = (decimal)tradePolicyDetailRow["CGSECloseLevyRemainder"];
            this.cgseLevyCurrecyType = (CGSELevyCurrecyType)Enum.ToObject(CGSELevyCurrecyType.GetType(), tradePolicyDetailRow["CGSELevyCurrecyType"]);

            specialTradePolicy.Add(this);
        }

        internal void Update(XElement tradePolicyDetailNode, SpecialTradePolicy specialTradePolicy)
        {
            this.specialTradePolicy.Remove(this);

            this.InternalUpdate(tradePolicyDetailNode, specialTradePolicy);

            specialTradePolicy.Add(this);
        }

        private void InternalUpdate(XElement tradePolicyDetailNode, SpecialTradePolicy specialTradePolicy)
        {
            foreach (XAttribute attribute in tradePolicyDetailNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "InstrumentID":
                        this.instrumentId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "AutoLimitBase":
                        this.autoLimitBase = (OrderLevelRiskBase)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AutoLimitThreshold":
                        this.autoLimitThreshold = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AutoStopBase":
                        this.autoStopBase = (OrderLevelRiskBase)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AutoStopThreshold":
                        this.autoStopThreshold = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "IsFractionCommissionOn":
                        this.isFractionCommissionOn = XmlConvert.ToBoolean(attribute.Value);
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

                    case "IsFractionLevyOn":
                        this.isFractionLevyOn = XmlConvert.ToBoolean(attribute.Value);
                        break;
                    case "LevyOpen":
                        this.levyOpen = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "LevyClose":
                        this.levyClose = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CGSENewLevyMultipler":
                        this.cgseNewLevyMultipler = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CGSECloseLevyMultipler":
                        this.cgseCloseLevyMultipler = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CGSENewLevyRemainder":
                        this.cgseNewLevyRemainder = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CGSECloseLevyRemainder":
                        this.cgseCloseLevyRemainder = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "CGSELevyCurrecyType":
                        this.cgseLevyCurrecyType = (CGSELevyCurrecyType)XmlConvert.ToInt32(attribute.Value);
                        break;
                }
            }
        }

        internal decimal GetCommissionClose(bool isDayCloseRelation)
        {
            return isDayCloseRelation ? this.commissionCloseD : this.commissionCloseO;
        }

        internal CurrencyRate GetCGSELevyCurrencyRate(Account account, Instrument instrument, CurrencyRate originCurrencyRate, ExecuteContext context)
        {
            if (this.CGSELevyCurrecyType == CGSELevyCurrecyType.UseInstrumentCurrencyType)
            {
                return originCurrencyRate;
            }
            Guid sourceCurrencyId = account.CurrencyId;
            Guid targetCurrencyId = account.IsMultiCurrency ? instrument.CurrencyId : account.CurrencyId;
            return !context.ShouldUseHistorySettings ? Setting.Default.GetCurrencyRate(sourceCurrencyId, targetCurrencyId)
                : Setting.Default.GetCurrencyRate(sourceCurrencyId, targetCurrencyId, context.TradeDay);
        }

    }
}