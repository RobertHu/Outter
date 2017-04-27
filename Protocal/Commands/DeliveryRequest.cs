using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Xml;

namespace Protocal.Commands
{
    public sealed class DeliveryRequest : XmlFillable<DeliveryRequest>
    {
        public DeliveryRequest(Account owner, XElement node)
        {
            this.Owner = owner;
            this.Relations = new List<DeliveryRequestOrderRelation>();
            this.Specifications = new List<DeliveryRequestSpecification>();
            this.Update(node);
        }

        public List<DeliveryRequestOrderRelation> Relations { get; private set; }

        public List<DeliveryRequestSpecification> Specifications { get; private set; }

        public Account Owner { get; private set; }

        public Guid Id { get; private set; }

        public Guid AccountId { get; private set; }

        public Guid InstrumentId { get; private set; }

        public string Code { get; private set; }

        public string PrintingCode { get; private set; }

        public string Ask { get; private set; }

        public string Bid { get; private set; }

        public DeliveryRequestStatus Status { get; private set; }

        public decimal RequireQuantity { get; private set; }

        public decimal RequireLot { get; private set; }

        public DateTime SubmitTime { get; private set; }

        public Guid SubmitorId { get; private set; }

        public DateTime DeliveryTime { get; private set; }

        public decimal Charge { get; private set; }

        public Guid ChargeCurrencyId { get; private set; }

        public Guid DeliveryAddressId { get; private set; }

        public void Update(XElement node)
        {
            this.InitializeProperties(node);
            XElement relationsNode = node.Element("DeliveryRequestOrderRelations");
            if (relationsNode != null && relationsNode.Elements("DeliveryRequestOrderRelation").Count() > 0)
            {
                this.ParseRelations(relationsNode);
            }

            XElement specificationsNode = node.Element("DeliveryRequestSpecifications");
            if (specificationsNode != null && specificationsNode.Elements("DeliveryRequestSpecification").Count() > 0)
            {
                this.ParseSpecifications(specificationsNode);
            }

        }

        private void ParseSpecifications(XElement specificationsNode)
        {
            foreach (XElement eachSpecificationNode in specificationsNode.Elements("DeliveryRequestSpecification"))
            {
                this.Specifications.Add(new DeliveryRequestSpecification(eachSpecificationNode));
            }
        }

        private void ParseRelations(XElement relationsNode)
        {
            foreach (XElement eachRelationNode in relationsNode.Elements("DeliveryRequestOrderRelation"))
            {
                Guid openOrderId = eachRelationNode.AttrToGuid("OpenOrderId");
                var relation = this.Relations.Count == 0 ? null : this.Relations.Where(m => m.OpenOrderId == openOrderId).SingleOrDefault();
                if (relation != null)
                {
                    relation.Update(eachRelationNode);
                }
                else
                {
                    this.Relations.Add(new DeliveryRequestOrderRelation(this, eachRelationNode));
                }
            }
        }

        public XmlNode ToXmlNode()
        {
            XmlDocument document = new XmlDocument();

            XmlElement deliveryNode = document.CreateElement("ApplyDelivery");
            document.AppendChild(deliveryNode);
            deliveryNode.SetAttribute("Id", XmlConvert.ToString(this.Id));
            deliveryNode.SetAttribute("AccountId", XmlConvert.ToString(this.AccountId));
            deliveryNode.SetAttribute("InstrumentId", XmlConvert.ToString(this.InstrumentId));
            deliveryNode.SetAttribute("RequireQuantity", XmlConvert.ToString(this.RequireQuantity));
            deliveryNode.SetAttribute("RequireLot", XmlConvert.ToString(this.RequireLot));
            deliveryNode.SetAttribute("Status", XmlConvert.ToString((int)this.Status));
            deliveryNode.SetAttribute("Charge", XmlConvert.ToString(this.Charge));
            deliveryNode.SetAttribute("ChargeCurrencyId", XmlConvert.ToString(this.ChargeCurrencyId));
            if (this.Code != null) deliveryNode.SetAttribute("Code", this.Code);
            deliveryNode.SetAttribute("SubmitTime", XmlConvert.ToString(this.SubmitTime, DateTimeFormat.Xml));
            deliveryNode.SetAttribute("DeliveryTime", XmlConvert.ToString(this.DeliveryTime, DateTimeFormat.Xml));
            deliveryNode.SetAttribute("Ask", this.Ask);
            deliveryNode.SetAttribute("Bid", this.Bid);
            if (this.DeliveryAddressId != null)
            {
                deliveryNode.SetAttribute("DeliveryAddressId", XmlConvert.ToString(this.DeliveryAddressId));
            }
            foreach (DeliveryRequestOrderRelation relation in this.Relations)
            {
                XmlElement relationNode = document.CreateElement("DeliveryRequestOrderRelation");
                deliveryNode.AppendChild(relationNode);

                relationNode.SetAttribute("OpenOrderId", XmlConvert.ToString(relation.OpenOrderId));
                relationNode.SetAttribute("DeliveryQuantity", XmlConvert.ToString(relation.DeliveryQuantity));
                relationNode.SetAttribute("DeliveryLot", XmlConvert.ToString(relation.DeliveryLot));
            }

            if (this.Specifications != null)
            {
                XmlElement deliveryRequestSpecificationsNode = document.CreateElement("DeliveryRequestSpecifications");
                deliveryNode.AppendChild(deliveryRequestSpecificationsNode);
                foreach (DeliveryRequestSpecification deliveryRequestSpecification in this.Specifications)
                {
                    XmlElement deliveryRequestSpecificationNode = document.CreateElement("DeliveryRequestSpecification");
                    deliveryRequestSpecificationsNode.AppendChild(deliveryRequestSpecificationNode);
                    deliveryRequestSpecificationNode.SetAttribute("Quantity", XmlConvert.ToString(deliveryRequestSpecification.Quantity));
                    deliveryRequestSpecificationNode.SetAttribute("Size", XmlConvert.ToString(deliveryRequestSpecification.Size));
                }
            }

            return deliveryNode;
        }


        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.Id);
            this.FillProperty(m => m.AccountId);
            this.FillProperty(m => m.InstrumentId);
            this.FillProperty(m => m.Code);
            this.FillProperty(m => m.PrintingCode);
            this.FillProperty(m => m.Ask);
            this.FillProperty(m => m.Bid);
            this.FillProperty(m => m.Status);
            this.FillProperty(m => m.RequireQuantity);
            this.FillProperty(m => m.RequireLot);
            this.FillProperty(m => m.SubmitTime);
            this.FillProperty(m => m.SubmitorId);
            this.FillProperty(m => m.DeliveryTime);
            this.FillProperty(m => m.Charge);
            this.FillProperty(m => m.ChargeCurrencyId);
            this.FillProperty(m => m.DeliveryAddressId);
        }
    }
}
