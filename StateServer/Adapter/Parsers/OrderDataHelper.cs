using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Protocal;
using System.Xml;
using Protocal.Physical;
using iExchange.Common;

namespace iExchange.StateServer.Adapter.Parsers
{
    internal static class OrderDataHelper
    {
        internal static PhysicalOrderBookData InitalizePhysicalBookData(XmlNode node)
        {
            var parser = new PhysicalBookOrderParser();
            return (PhysicalOrderBookData)parser.Parse(node);
        }

        internal static PhysicalOrderData InitializePhysicalData(XmlNode node)
        {
            var parser = new PhysicalOrderParser();
            return (PhysicalOrderData)parser.Parse(node);
        }

        internal static BOOrderData InitializeBOData(XmlNode node)
        {
            var parser = new BOOrderParser();
            return (BOOrderData)parser.Parse(node);
        }


        internal static OrderData Initialize(XmlNode node)
        {
            OrderParser parser = new OrderParser();
            return (OrderData)parser.Parse(node);
        }

        internal static OrderBookData InitializeBookData(XmlNode node)
        {
            BookOrderParser parser = new BookOrderParser();
            return (OrderBookData)parser.Parse(node);
        }
    }

    internal abstract class OrderParserBase
    {
        internal abstract Protocal.OrderCommonData CreateOrderData();

        internal abstract Protocal.OrderRelationData CreateOrderRelationData();

        internal Protocal.OrderCommonData Parse(XmlNode orderXml)
        {
            Protocal.OrderCommonData order = this.CreateOrderData();
            this.ParseAttrs(order, orderXml);
            order.IfDoneOrderSetting = this.ParseIfDoneSetting(orderXml);
            if (!order.IsOpen)
            {
                this.ParseOrderRelation(order, orderXml);
            }
            return order;
        }
        protected virtual void ParseAttrs(Protocal.OrderCommonData order, XmlNode orderXml)
        {
            foreach (XmlAttribute attribute in orderXml.Attributes)
            {
                string nodeName = attribute.Name;
                string nodeValue = attribute.Value;
                if (nodeName.Equals("ID"))
                {
                    order.Id = new Guid(nodeValue);
                    continue;
                }
                else if (nodeName.Equals("Lot"))
                {
                    order.Lot = decimal.Parse(nodeValue);
                    continue;
                }
                else if (nodeName.Equals("IsOpen"))
                {
                    order.IsOpen = bool.Parse(nodeValue);
                    continue;
                }
                else if (nodeName.Equals("IsBuy"))
                {
                    order.IsBuy = bool.Parse(nodeValue);
                    continue;
                }
                else if (nodeName.Equals("SetPrice"))
                {
                    order.SetPrice = nodeValue;
                    continue;
                }
                else if (nodeName.Equals("SetPrice2"))
                {
                    order.SetPrice2 = nodeValue;
                    continue;
                }
                else if (nodeName.Equals("TradeOption"))
                {
                    order.TradeOption = (TradeOption)(int.Parse(nodeValue));
                    continue;
                }
                else if (nodeName.Equals("DQMaxMove"))
                {
                    order.DQMaxMove = int.Parse(nodeValue);
                    continue;
                }
                else if (nodeName.Equals("BlotterCode"))
                {
                    order.BlotterCode = nodeValue;
                    continue;
                }
            }
        }

        private IfDoneOrderSetting ParseIfDoneSetting(XmlNode xmlNode)
        {
            if (xmlNode.Attributes["Extension"] != null
                        && xmlNode.Attributes["Extension"].Value.Contains("IfDone"))
            {
                XmlNode extensionXml = xmlNode.Attributes["Extension"].Value.ToXmlNode();
                IfDoneOrderSetting ifDoneOrderSetting = new IfDoneOrderSetting();
                foreach (XmlAttribute attribute in extensionXml.Attributes)
                {
                    string nodeName = attribute.Name;
                    string nodeValue = attribute.Value;
                    if (nodeName.Equals("LimitPrice"))
                    {
                        ifDoneOrderSetting.LimitPrice = nodeValue;
                    }
                    else if (nodeName.Equals("StopPrice"))
                    {
                        ifDoneOrderSetting.StopPrice = nodeValue;
                    }
                }
                return ifDoneOrderSetting;
            }
            return null;
        }

        private void ParseOrderRelation(Protocal.OrderCommonData order, XmlNode xmlNode)
        {
            order.OrderRelations = new List<OrderRelationData>();
            foreach (XmlNode orderRelationChild in xmlNode.ChildNodes)
            {
                if (orderRelationChild.Name == "OrderRelation")
                {
                    OrderRelationData orderRelaitonData = this.CreateOrderRelationData();
                    orderRelaitonData.OpenOrderId = Guid.Parse(orderRelationChild.Attributes["OpenOrderID"].Value);
                    orderRelaitonData.CloseOrderId = order.Id;
                    orderRelaitonData.ClosedLot = Convert.ToDecimal(orderRelationChild.Attributes["ClosedLot"].Value);
                    order.OrderRelations.Add(orderRelaitonData);
                }
            }
        }


    }

    internal sealed class OrderParser : OrderParserBase
    {
        internal override OrderCommonData CreateOrderData()
        {
            return new OrderData();
        }

        internal override OrderRelationData CreateOrderRelationData()
        {
            return new OrderRelationData();
        }
    }

    internal sealed class PhysicalOrderParameters
    {
        internal Protocal.Physical.InstalmentPart InstalmentPart { get; set; }
        internal Protocal.Physical.PhysicalType PhysicalType { get; set; }
        internal PhysicalTradeSide PhysicalTradeSide { get; set; }
    }

    internal static class PhysicalOrderHelper
    {
        internal static PhysicalOrderParameters ParsePhysicalOrderParameters(this XmlNode node)
        {
            PhysicalOrderParameters result = new PhysicalOrderParameters();
            result.InstalmentPart = new InstalmentPart();
            foreach (XmlAttribute attribute in node.Attributes)
            {
                string nodeName = attribute.Name;
                string nodeValue = attribute.Value;
                if (nodeName.Equals("PhysicalTradeSide"))
                {
                    result.PhysicalTradeSide = (PhysicalTradeSide)(int.Parse(nodeValue));
                    continue;
                }
                else if (nodeName == "InstalmentPolicyId")
                {
                    result.InstalmentPart.InstalmentPolicyId = new Guid(nodeValue);
                    continue;
                }
                else if (nodeName == "PhysicalInstalmentType")
                {
                    result.InstalmentPart.InstalmentType = (InstalmentType)(int.Parse(nodeValue));
                    continue;
                }
                else if (nodeName == "RecalculateRateType")
                {
                    result.InstalmentPart.RecalculateRateType = (RecalculateRateType)(int.Parse(nodeValue));
                    continue;
                }
                else if (nodeName == "Period")
                {
                    result.InstalmentPart.Period = int.Parse(nodeValue);
                    continue;
                }
                else if (nodeName == "DownPayment")
                {
                    result.InstalmentPart.DownPayment = decimal.Parse(nodeValue);
                    continue;
                }
                else if (nodeName == "InstalmentFrequence")
                {
                    result.InstalmentPart.InstalmentFrequence = (InstalmentFrequence)(int.Parse(nodeValue));
                    continue;
                }
            }

            if (result.InstalmentPart.InstalmentPolicyId != null && result.InstalmentPart.InstalmentPolicyId != Guid.Empty)
            {
                if (result.InstalmentPart.InstalmentFrequence == InstalmentFrequence.TillPayoff)
                {
                    result.PhysicalType = PhysicalType.PrePayment;
                }
                else
                {
                    result.PhysicalType = PhysicalType.Instalment;
                }
            }
            else
            {
                result.PhysicalType = Protocal.Physical.PhysicalType.FullPayment;
                result.InstalmentPart.RecalculateRateType = RecalculateRateType.NextMonth;
            }
            return result;
        }
    }


    internal sealed class PhysicalOrderParser : OrderParserBase
    {
        internal override OrderCommonData CreateOrderData()
        {
            return new PhysicalOrderData();
        }

        internal override OrderRelationData CreateOrderRelationData()
        {
            return new OrderRelationData();
        }

        protected override void ParseAttrs(OrderCommonData order, XmlNode orderXml)
        {
            base.ParseAttrs(order, orderXml);
            PhysicalOrderParameters physicalOrderParameters = orderXml.ParsePhysicalOrderParameters();
            var physicalOrder = (PhysicalOrderData)order;
            physicalOrder.InstalmentPart = physicalOrderParameters.InstalmentPart;
            physicalOrder.PhysicalType = physicalOrderParameters.PhysicalType;
            physicalOrder.PhysicalTradeSide = physicalOrderParameters.PhysicalTradeSide;
        }

    }


    internal abstract class BookOrderParserBase : OrderParserBase
    {
        protected override void ParseAttrs(OrderCommonData order, XmlNode orderXml)
        {
            base.ParseAttrs(order, orderXml);
            if (orderXml.HasAttribute("ExecutePrice"))
            {
                ((OrderBookData)order).ExecutePrice = orderXml.GetAttrValue("ExecutePrice");
            }
        }
    }

    internal sealed class BookOrderParser : BookOrderParserBase
    {
        internal override OrderCommonData CreateOrderData()
        {
            return new OrderBookData();
        }

        internal override OrderRelationData CreateOrderRelationData()
        {
            return new OrderRelationBookData();
        }
    }

    internal sealed class PhysicalBookOrderParser : BookOrderParserBase
    {
        internal override OrderCommonData CreateOrderData()
        {
            return new PhysicalOrderBookData();
        }

        internal override OrderRelationData CreateOrderRelationData()
        {
            return new OrderRelationBookData();
        }

        protected override void ParseAttrs(OrderCommonData order, XmlNode orderXml)
        {
            base.ParseAttrs(order, orderXml);
            var physicalOrder = (PhysicalOrderBookData)order;
            PhysicalOrderParameters physicalOrderParameters = orderXml.ParsePhysicalOrderParameters();
            physicalOrder.InstalmentPart = physicalOrderParameters.InstalmentPart;
            physicalOrder.PhysicalType = physicalOrderParameters.PhysicalType;
            physicalOrder.PhysicalTradeSide = physicalOrderParameters.PhysicalTradeSide;
            if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.None)
            {
                physicalOrder.PhysicalTradeSide = order.IsBuy ? PhysicalTradeSide.Buy : PhysicalTradeSide.Sell;
            }
            physicalOrder.PhysicalRequestId = orderXml.HasAttribute("PhysicalRequestId") ? orderXml.GetGuidAttrValue("PhysicalRequestId") : (Guid?)null;
        }
    }

    internal sealed class BOOrderParser : OrderParserBase
    {
        internal override OrderCommonData CreateOrderData()
        {
            return new BOOrderData();
        }

        internal override OrderRelationData CreateOrderRelationData()
        {
            throw new NotImplementedException();
        }

        protected override void ParseAttrs(OrderCommonData order, XmlNode orderXml)
        {
            base.ParseAttrs(order, orderXml);
            var boOrder = (BOOrderData)order;
            boOrder.BOBetTypeID = (Guid)orderXml.GetAttrValue("BOBetTypeID", typeof(Guid));
            boOrder.BOFrequency = (int)orderXml.GetAttrValue("BOFrequency", typeof(Int32));
            boOrder.BOOdds = (decimal)orderXml.GetAttrValue("BOOdds", typeof(decimal));
            boOrder.BOBetOption = (long)orderXml.GetAttrValue("BOBetOption", typeof(Int64));
        }
    }

}