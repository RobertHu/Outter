using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical.OrderRelationBusiness
{
    internal sealed class PhysicalOrderRelationXmlService : OrderRelationXmlService
    {
        private PhysicalOrderRelation _owner;
        public PhysicalOrderRelationXmlService(PhysicalOrderRelation owner)
            : base(owner)
        {
            _owner = owner;
        }

        public override XmlElement ToXmlNode(XmlDocument xmlTran, XmlElement orderNode)
        {
            var orderRelationNode = base.ToXmlNode(xmlTran, orderNode);
            orderRelationNode.SetAttribute("PayBackPledge", XmlConvert.ToString(_owner.PayBackPledgeOfOpenOrder));
            orderRelationNode.SetAttribute("ClosedPhysicalValue", XmlConvert.ToString(_owner.ClosedPhysicalValue));
            orderRelationNode.SetAttribute("PhysicalValue", XmlConvert.ToString(_owner.PhysicalValue));
            orderRelationNode.SetAttribute("OverdueCutPenalty", XmlConvert.ToString(_owner.OverdueCutPenalty));
            orderRelationNode.SetAttribute("ClosePenalty", XmlConvert.ToString(_owner.ClosePenalty));
            if (_owner.PhysicalValueMatureDay != null) orderRelationNode.SetAttribute("PhysicalValueMatureDate", XmlConvert.ToString(_owner.PhysicalValueMatureDay.Value, DateTimeFormat.Xml));
            return orderRelationNode;
        }

        protected override void GetExecuteXmlStringCommon(StringBuilder stringBuilder)
        {
            base.GetExecuteXmlStringCommon(stringBuilder);
            stringBuilder.AppendFormat(" PhysicalValue='{0}'", XmlConvert.ToString(_owner.PhysicalValue));
            stringBuilder.AppendFormat(" ClosedPhysicalValue='{0}'", XmlConvert.ToString(_owner.ClosedPhysicalValue));
            stringBuilder.AppendFormat(" PayBackPledge='{0}'", XmlConvert.ToString(_owner.PayBackPledgeOfOpenOrder));
            stringBuilder.AppendFormat(" OverdueCutPenalty='{0}'", XmlConvert.ToString(_owner.OverdueCutPenalty));
            stringBuilder.AppendFormat(" ClosePenalty='{0}'", XmlConvert.ToString(_owner.ClosePenalty));
            if (_owner.PhysicalValueMatureDay != null) stringBuilder.AppendFormat(" PhysicalValueMatureDate='{0}'", XmlConvert.ToString(_owner.PhysicalValueMatureDay.Value, DateTimeFormat.Xml));
        }

    }
}
