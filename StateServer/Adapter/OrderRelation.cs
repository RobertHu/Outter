using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml;
using System.Xml.Linq;
using Protocal;

namespace iExchange.StateServer.Adapter
{
    internal class OrderRelation : Protocal.Commands.OrderRelation
    {
        internal OrderRelation(Order owner)
            : base(owner)
        {
        }

        internal Order OpenOrder
        {
            get
            {
                return (Order)this.Owner.Owner.Owner.GetOrder(this.OpenOrderId);
            }
        }


        internal XElement ToXml()
        {
            XElement result = new XElement("OrderRelation");
            result.SetAttributeValue("OpenOrderID", XmlConvert.ToString(this.OpenOrderId));
            result.SetAttributeValue("ClosedLot", XmlConvert.ToString(this.ClosedLot));

            if (this.CloseTime != default(DateTime))
            {
                result.SetAttributeValue("CloseTime", XmlConvert.ToString(this.CloseTime, DateTimeFormat.Xml));
            }

            result.SetAttributeValue("Commission", XmlConvert.ToString(this.Commission));
            result.SetAttributeValue("Levy", XmlConvert.ToString(this.Levy));
            result.SetAttributeValue("OtherFee", XmlConvert.ToString(this.OtherFee));
            result.SetAttributeValue("InterestPL", XmlConvert.ToString(this.InterestPL));
            result.SetAttributeValue("StoragePL", XmlConvert.ToString(this.StoragePL));
            result.SetAttributeValue("TradePL", XmlConvert.ToString(this.TradePL));
            result.SetAttributeValue("PhysicalTradePL", XmlConvert.ToString(this.PhysicalTradePL));
            result.SetAttributeValue("PayBackPledge", XmlConvert.ToString(this.PayBackPledgeOfOpenOrder));
            result.SetAttributeValue("ClosedPhysicalValue", XmlConvert.ToString(this.ClosedPhysicalValue));
            result.SetAttributeValue("PhysicalValue", XmlConvert.ToString(this.PhysicalValue));
            result.SetAttributeValue("OverdueCutPenalty", XmlConvert.ToString(this.OverdueCutPenalty));
            result.SetAttributeValue("ClosePenalty", XmlConvert.ToString(this.ClosePenalty));
            result.SetAttributeValue("EstimateCloseCommissionOfOpenOrder", XmlConvert.ToString(this.EstimateCloseCommissionOfOpenOrder));
            result.SetAttributeValue("EstimateCloseLevyOfOpenOrder", XmlConvert.ToString(this.EstimateCloseLevyOfOpenOrder));
            if (this.PhysicalValueMatureDay != null) result.SetAttributeValue("PhysicalValueMatureDate", XmlConvert.ToString(this.PhysicalValueMatureDay.Value, DateTimeFormat.Xml));

            if (this.ValueTime != default(DateTime))
            {
                result.SetAttributeValue("ValueTime", XmlConvert.ToString(this.ValueTime, DateTimeFormat.Xml));
                result.SetAttributeValue("RateIn", Convert.ToString(this.RateIn));
                result.SetAttributeValue("RateOut", Convert.ToString(this.RateOut));
                result.SetAttributeValue("Decimals", Convert.ToString(this.Decimals));
            }
            return result;
        }
    }
}