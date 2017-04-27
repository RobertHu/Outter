using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public class OrderRelationXmlService
    {
        private OrderRelation _owner;
        public OrderRelationXmlService(OrderRelation owner)
        {
            _owner = owner;
        }

        public virtual XmlElement ToXmlNode(XmlDocument xmlTran, XmlElement orderNode)
        {
            XmlElement orderRelationNode = xmlTran.CreateElement("OrderRelation");
            orderNode.AppendChild(orderRelationNode);

            orderRelationNode.SetAttribute("OpenOrderID", XmlConvert.ToString(_owner.OpenOrderId));
            orderRelationNode.SetAttribute("ClosedLot", XmlConvert.ToString(_owner.ClosedLot));

            if (_owner.CloseTime != null)
            {
                orderRelationNode.SetAttribute("CloseTime", XmlConvert.ToString(_owner.CloseTime.Value, DateTimeFormat.Xml));
            }

            orderRelationNode.SetAttribute("Commission", XmlConvert.ToString(_owner.Commission));
            orderRelationNode.SetAttribute("Levy", XmlConvert.ToString(_owner.Levy));
            //orderRelationNode.SetAttribute("InterestPL", XmlConvert.ToString(_owner.InterestPL));
            //orderRelationNode.SetAttribute("StoragePL", XmlConvert.ToString(_owner.StoragePL));
            //orderRelationNode.SetAttribute("TradePL", XmlConvert.ToString(_owner.TradePL));

            if (_owner.ValueTime != null)
            {
                orderRelationNode.SetAttribute("ValueTime", XmlConvert.ToString(_owner.ValueTime.Value, DateTimeFormat.Xml));
                orderRelationNode.SetAttribute("RateIn", Convert.ToString(_owner.RateIn));
                orderRelationNode.SetAttribute("RateOut", Convert.ToString(_owner.RateOut));
                orderRelationNode.SetAttribute("Decimals", Convert.ToString(_owner.Decimals));
            }
            return orderRelationNode;
        }

        public void GetExecuteXmlString(StringBuilder stringBuilder)
        {
            this.GetExecuteXmlStringCommon(stringBuilder);
            this.AddEndTag(stringBuilder);
        }

        protected virtual void GetExecuteXmlStringCommon(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("<OrderRelation OpenOrderID='{0}' ClosedLot='{1}'", XmlConvert.ToString(_owner.OpenOrderId), XmlConvert.ToString(_owner.ClosedLot));
            if (_owner.CloseTime != null)
            {
                stringBuilder.AppendFormat(" CloseTime='{0}'", XmlConvert.ToString(_owner.CloseTime.Value, DateTimeFormat.Xml));
            }
            stringBuilder.AppendFormat(" Commission='{0}'", XmlConvert.ToString(_owner.Commission));
            //stringBuilder.AppendFormat(" Levy='{0}' InterestPL='{1}'", XmlConvert.ToString(_owner.Levy), XmlConvert.ToString(_owner.InterestPL));
            //stringBuilder.AppendFormat(" StoragePL='{0}' TradePL='{1}'", XmlConvert.ToString(_owner.StoragePL), XmlConvert.ToString(_owner.TradePL));
            if (_owner.ValueTime != null)
            {
                stringBuilder.AppendFormat(" ValueTime='{0}' RateIn='{1}'", XmlConvert.ToString(_owner.ValueTime.Value, DateTimeFormat.Xml), Convert.ToString(_owner.RateIn));
                stringBuilder.AppendFormat(" RateOut='{0}' Decimals='{1}'", Convert.ToString(_owner.RateOut), Convert.ToString(_owner.Decimals));
            }
        }

        private void AddEndTag(StringBuilder stringBuilder)
        {
            stringBuilder.Append(" />");
        }

    }
}
