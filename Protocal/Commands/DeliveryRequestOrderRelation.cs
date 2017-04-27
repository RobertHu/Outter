using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public sealed class DeliveryRequestOrderRelation : XmlFillable<DeliveryRequestOrderRelation>
    {
        public DeliveryRequestOrderRelation(DeliveryRequest owner, XElement node)
        {
            this.Owner = owner;
            this.InitializeProperties(node);
        }

        public DeliveryRequest Owner { get; private set; }

        public Guid OpenOrderId { get; private set; }

        public decimal DeliveryQuantity { get; private set; }

        public decimal DeliveryLot { get; private set; }

        public void Update(XElement node)
        {
            this.InitializeProperties(node);
        }

        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.OpenOrderId);
            this.FillProperty(m => m.DeliveryQuantity);
            this.FillProperty(m => m.DeliveryLot);
        }
    }

    public sealed class DeliveryRequestSpecification : XmlFillable<DeliveryRequestSpecification>
    {
        public DeliveryRequestSpecification(XElement node)
        {
            this.InitializeProperties(node);
        }

        public int Quantity { get; private set; }

        public decimal Size { get; private set; }


        protected override void InnerInitializeProperties(XElement element)
        {
            this.FillProperty(m => m.Quantity);
            this.FillProperty(m => m.Size);
        }
    }


}
