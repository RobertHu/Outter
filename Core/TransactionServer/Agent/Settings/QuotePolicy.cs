using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class QuotePolicyDetail
    {
        private Guid quotePolicyId;
        private Guid instrumentId;
        private int spreadPoints;
        private bool isOriginHiLo;

        #region Common internal properties definition
        internal Guid QuotePolicyId
        {
            get { return this.quotePolicyId; }
        }
        internal Guid InstrumentId
        {
            get { return this.instrumentId; }
        }
        internal int SpreadPoints
        {
            get { return this.spreadPoints; }
        }
        internal bool IsOriginHiLo
        {
            get { return this.isOriginHiLo; }
        }
        #endregion Common internal properties definition

        internal QuotePolicyDetail(IDBRow quotePolicy)
        {
            this.instrumentId = (Guid)quotePolicy["InstrumentID"];
            this.quotePolicyId = (Guid)quotePolicy["QuotePolicyID"];

            this.spreadPoints = (int)quotePolicy["SpreadPoints"];
            this.isOriginHiLo = (bool)quotePolicy["IsOriginHiLo"];
        }

        internal QuotePolicyDetail(XElement quotePolicy)
        {
            this.InternalUpdate(quotePolicy);
        }

        internal void Update(XElement quotePolicy)
        {
            this.InternalUpdate(quotePolicy);
        }

        internal void InternalUpdate(XElement quotePolicy)
        {
            foreach (XAttribute attribute in quotePolicy.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "InstrumentID":
                        this.instrumentId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "QuotePolicyID":
                        this.quotePolicyId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "SpreadPoints":
                        this.spreadPoints = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "IsOriginHiLo":
                        this.isOriginHiLo = XmlConvert.ToBoolean(attribute.Value);
                        break;
                }
            }
        }
    }
}