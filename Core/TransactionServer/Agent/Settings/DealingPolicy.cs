using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data;
using iExchange.Common;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Diagnostics;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class DealingPolicy
    {
        private Guid id;

        private Dictionary<Guid, DealingPolicyDetail> dealingPolicyDetails;

        #region Common internal properties definition
        internal Guid ID
        {
            get { return this.id; }
        }
        #endregion Common internal properties definition

        internal DealingPolicy(XElement dealingPolicyNode)
        {
            this.Update(dealingPolicyNode);

            this.dealingPolicyDetails = new Dictionary<Guid, DealingPolicyDetail>();
        }

        internal DealingPolicy(IDBRow  dealingPolicyRow)
        {
            this.id = (Guid)dealingPolicyRow["ID"];

            this.dealingPolicyDetails = new Dictionary<Guid, DealingPolicyDetail>();
        }

        internal bool ExistDealingPolicyDetail(Guid instrumentId)
        {
            return this.dealingPolicyDetails.ContainsKey(instrumentId);
        }


        internal DealingPolicyDetail this[Guid instrumentId]
        {
            get { return this.dealingPolicyDetails[instrumentId]; }
        }

        internal void Add(DealingPolicyDetail detail)
        {
            this.dealingPolicyDetails.Add(detail.InstrumentId, detail);
        }

        internal void Remove(DealingPolicyDetail detail)
        {
            this.dealingPolicyDetails.Remove(detail.InstrumentId);
        }

        internal bool Update(XElement dealingPolicyNode)
        {
            foreach (XAttribute attribute in dealingPolicyNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.id = XmlConvert.ToGuid(attribute.Value);
                        break;
                }
            }

            return true;
        }
    }

    public class DealingPolicyPayload
    {
        private decimal _maxDQLot;
        private decimal _maxOtherLot;
        private decimal _dqQuoteMinLot;//useless in transactionServer

        private decimal _autoDQMaxLot;
        private int _acceptDQVariation;

        private decimal _autoLmtMktMaxLot;
        private int _acceptLmtVariation;
        private int _acceptCloseLmtVariation;

        private int _cancelLmtVariation;
        private TimeSpan _autoDQDelay;

        private decimal _autoAcceptMaxLot;
        private decimal _autoCancelMaxLot;

        private int _hitPriceVariationForSTP;
        private AllowedOrderSides _allowedNewTradeSides;
        private TimeSpan _placeSptMktTimeSpan;

        internal decimal MaxDQLot
        {
            get { return this._maxDQLot; }
        }

        internal decimal MaxOtherLot
        {
            get { return this._maxOtherLot; }
        }

        internal AllowedOrderSides AllowedNewTradeSides
        {
            get { return this._allowedNewTradeSides; }
        }

        internal decimal DqQuoteMinLot
        {
            get { return this._dqQuoteMinLot; }
        }

        internal decimal AutoDQMaxLot
        {
            get { return this._autoDQMaxLot; }
        }

        internal int AcceptDQVariation
        {
            get { return this._acceptDQVariation; }
        }

        internal decimal AutoLmtMktMaxLot
        {
            get { return this._autoLmtMktMaxLot; }
        }

        internal int AcceptLmtVariation
        {
            get { return this._acceptLmtVariation; }
        }

        internal int AcceptCloseLmtVariation
        {
            get { return this._acceptCloseLmtVariation; }
        }

        internal int CancelLmtVariation
        {
            get { return this._cancelLmtVariation; }
        }

        public TimeSpan AutoDQDelay
        {
            get { return this._autoDQDelay; }
            protected set { this._autoDQDelay = value; }
        }

        public decimal AutoAcceptMaxLot
        {
            get { return this._autoAcceptMaxLot; }
            protected set { this._autoAcceptMaxLot = value; }
        }

        public decimal AutoCancelMaxLot
        {
            get { return this._autoCancelMaxLot; }
            protected set { this._autoCancelMaxLot = value; }
        }

        public int HitPriceVariationForSTP
        {
            get { return this._hitPriceVariationForSTP; }
            protected set { this._hitPriceVariationForSTP = value; }
        }

        internal TimeSpan PlaceSptMktTimeSpan
        {
            get { return _placeSptMktTimeSpan; }
        }


        internal virtual void Update(IDBRow  dataRow)
        {
            this._maxDQLot = dataRow.GetColumn<decimal>("MaxDQLot");
            this._maxOtherLot = dataRow.GetColumn<decimal>("MaxOtherLot");
            this._dqQuoteMinLot = dataRow.GetColumn<decimal>("DQQuoteMinLot");

            this._autoDQMaxLot = dataRow.GetColumn<decimal>("AutoDQMaxLot");
            this._acceptDQVariation = dataRow.GetColumn<int>("AcceptDQVariation");

            this._autoLmtMktMaxLot = dataRow.GetColumn<decimal>("AutoLmtMktMaxLot");
            this._acceptLmtVariation = dataRow.GetColumn<int>("AcceptLmtVariation");
            this._acceptCloseLmtVariation = dataRow.GetColumn<int>("AcceptCloseLmtVariation");

            this._cancelLmtVariation = dataRow.GetColumn<int>("CancelLmtVariation");
            this._autoDQDelay = TimeSpan.FromSeconds(dataRow.GetColumn<Int16>("AutoDQDelay"));

            this._autoAcceptMaxLot = dataRow.GetColumn<decimal>("AutoAcceptMaxLot");
            this._autoCancelMaxLot = dataRow.GetColumn<decimal>("AutoCancelMaxLot");

            this._hitPriceVariationForSTP = dataRow.GetColumn<int>("HitPriceVariationForSTP");
            this._allowedNewTradeSides = (AllowedOrderSides)dataRow.GetColumn<byte>("AllowedNewTradeSides");
            if (dataRow.Contains("PlaceSptMktTimeSpan"))
            {
                this._placeSptMktTimeSpan = TimeSpan.FromSeconds((int)dataRow["PlaceSptMktTimeSpan"]);
            }
        }

        internal virtual void Update(XElement xmlNode)
        {
            foreach (XAttribute attribute in xmlNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "MaxDQLot":
                        this._maxDQLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "MaxOtherLot":
                        this._maxOtherLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "DQQuoteMinLot":
                        this._dqQuoteMinLot = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "AutoDQMaxLot":
                        this._autoDQMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AcceptDQVariation":
                        this._acceptDQVariation = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "AutoLmtMktMaxLot":
                        this._autoLmtMktMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "AcceptLmtVariation":
                        this._acceptLmtVariation = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AcceptCloseLmtVariation":
                        this._acceptCloseLmtVariation = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "CancelLmtVariation":
                        this._cancelLmtVariation = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "AutoDQDelay":
                        this._autoDQDelay = TimeSpan.FromSeconds(XmlConvert.ToInt16(attribute.Value));
                        break;

                    case "AutoAcceptMaxLot":
                        this._autoAcceptMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "PlaceSptMktTimeSpan":
                        this._placeSptMktTimeSpan = TimeSpan.FromSeconds(XmlConvert.ToInt32(attribute.Value));
                        break;

                    case "AutoCancelMaxLot":
                        this._autoCancelMaxLot = XmlConvert.ToDecimal(attribute.Value);
                        break;

                    case "HitPriceVariationForSTP":
                        this._hitPriceVariationForSTP = XmlConvert.ToInt32(attribute.Value);
                        break;

                    case "AllowedNewTradeSides":
                        this._allowedNewTradeSides = (AllowedOrderSides)XmlConvert.ToInt32(attribute.Value);
                        break;
                }
            }
        }
    }

    internal sealed class DealingPolicyDetail : DealingPolicyPayload
    {
        private DealingPolicy dealingPolicy; //It's null when used by Instrument.defaultDealingPolicyDetail
        private Guid instrumentId;

        #region Common internal properties definition

        internal DealingPolicy DealingPolicy
        {
            get { return this.dealingPolicy; }
        }
        internal Guid InstrumentId
        {
            get { return this.instrumentId; }
        }

        #endregion Common internal properties definition

        internal DealingPolicyDetail(IDBRow  dealingPolicyDetailRow, DealingPolicy dealingPolicy)
        {
            this.Update(dealingPolicyDetailRow, dealingPolicy);
        }

        internal DealingPolicyDetail(XElement dealingPolicyDetailNode, DealingPolicy dealingPolicy)
        {
            this.InternalUpdate(dealingPolicyDetailNode, dealingPolicy);
        }

        internal void Update(IDBRow  dealingPolicyDetailRow, DealingPolicy dealingPolicy)
        {
            this.instrumentId = (Guid)dealingPolicyDetailRow["InstrumentID"];
            base.Update(dealingPolicyDetailRow);

            this.AddTo(dealingPolicy);
        }

        internal void Update(XElement dealingPolicyDetailNode, DealingPolicy dealingPolicy)
        {
            this.dealingPolicy.Remove(this);
            this.InternalUpdate(dealingPolicyDetailNode, dealingPolicy);
        }

        private bool InternalUpdate(XElement dealingPolicyDetailNode, DealingPolicy dealingPolicy)
        {
            this.instrumentId = dealingPolicyDetailNode.AttrToGuid("InstrumentID");
            base.Update(dealingPolicyDetailNode);
            this.AddTo(dealingPolicy);
            return true;
        }

        private void AddTo(DealingPolicy dealingPolicy)
        {
            this.dealingPolicy = dealingPolicy;

            if (this.dealingPolicy != null)
            {
                this.dealingPolicy.Add(this);
            }
        }
    }
}