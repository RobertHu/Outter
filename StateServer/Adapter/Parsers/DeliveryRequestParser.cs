using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace iExchange.StateServer.Adapter.Parsers
{
    internal static class DeliveryRequestParser
    {
        internal static Protocal.Physical.DeliveryRequestData Parser(XmlNode node)
        {
            var result = new Protocal.Physical.DeliveryRequestData();
            result.OrderRelations = new List<Protocal.Physical.DeliveryRequestOrderRelationData>();
            result.Specifications = new List<Protocal.Physical.DeliveryRequestSpecificationData>();
            result.ParserRequestAttrs(node);
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name.Equals("DeliveryRequestOrderRelation", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.ParseDeliveryRequestOrderRelation(childNode);
                }
                if (childNode.Name.Equals("DeliveryRequestSpecifications", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (XmlNode deliveryRequestSpecificationNode in childNode.ChildNodes)
                    {
                        result.ParseDeliveryRequestSpecification(deliveryRequestSpecificationNode);
                    }
                }
              
            }
            return result;
        }

        private static void ParseDeliveryRequestOrderRelation(this Protocal.Physical.DeliveryRequestData deliveryRequest, XmlNode node)
        {
            var deliveryRequestOrderRelation = new Protocal.Physical.DeliveryRequestOrderRelationData();
            XmlAttributeCollection attributes = node.Attributes;
            deliveryRequestOrderRelation.DeliveryRequestId = deliveryRequest.Id;
            deliveryRequestOrderRelation.OpenOrderId = XmlConvert.ToGuid(attributes["OpenOrderId"].Value);
            deliveryRequestOrderRelation.DeliveryQuantity = XmlConvert.ToDecimal(attributes["DeliveryQuantity"].Value);
            deliveryRequestOrderRelation.DeliveryLot = XmlConvert.ToDecimal(attributes["DeliveryLot"].Value);
            deliveryRequest.OrderRelations.Add(deliveryRequestOrderRelation);
        }

        private static void ParseDeliveryRequestSpecification(this Protocal.Physical.DeliveryRequestData deliveryRequest, XmlNode node)
        {
            XmlAttributeCollection attributes = node.Attributes;
            int quantity = XmlConvert.ToInt32(attributes["Quantity"].Value);
            decimal size = XmlConvert.ToDecimal(attributes["Size"].Value);
            deliveryRequest.Specifications.Add(new Protocal.Physical.DeliveryRequestSpecificationData { Size = size, Quantity = quantity });
        }



        private static void ParserRequestAttrs(this Protocal.Physical.DeliveryRequestData deliveryRequest, XmlNode node)
        {
            XmlAttributeCollection attributes = node.Attributes;
            deliveryRequest.Id = XmlConvert.ToGuid(attributes["Id"].Value);
            deliveryRequest.AccountId = XmlConvert.ToGuid(attributes["AccountId"].Value);
            deliveryRequest.InstrumentId = XmlConvert.ToGuid(attributes["InstrumentId"].Value);
            deliveryRequest.RequireQuantity = XmlConvert.ToDecimal(attributes["RequireQuantity"].Value);
            deliveryRequest.RequireLot = XmlConvert.ToDecimal(attributes["RequireLot"].Value);
            deliveryRequest.DeliveryTime = attributes["DeliveryTime"] == null ? DateTime.Now : XmlConvert.ToDateTime(attributes["DeliveryTime"].Value);
            deliveryRequest.Charge = XmlConvert.ToDecimal(attributes["Charge"].Value);
            deliveryRequest.ChargeCurrencyId = XmlConvert.ToGuid(attributes["ChargeCurrencyId"].Value);
            if (attributes["DeliveryAddressId"] != null && !string.IsNullOrEmpty(attributes["DeliveryAddressId"].Value))
            {
                deliveryRequest.DeliveryAddressId = XmlConvert.ToGuid(attributes["DeliveryAddressId"].Value);
            }
        }

    }
}