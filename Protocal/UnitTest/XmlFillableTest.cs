#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml.Linq;
using System.Diagnostics;

namespace Protocal.UnitTest
{
    [TestFixture]
    public class XmlFillableTest
    {
        [Test]
        public void ParseTest()
        {
            XElement node = new XElement("Product");
            node.SetAttributeValue("Price", "234");
            node.SetAttributeValue("DeliveryDate", "");
            node.SetAttributeValue("IsOnLine", true);
            Product product = new Product();
            product.Update(node);
            Assert.AreEqual(234, product.Price);
            Assert.AreEqual(null, product.DeliveryDate);
            Assert.AreEqual(true, product.IsOnLine);
        }
    }


    public sealed class Product : Commands.XmlFillable<Product>
    {
        public int Price { get; private set; }

        public DateTime? DeliveryDate { get; private set; }

        public bool IsOnLine { get; private set; }

        public void Update(XElement node)
        {
            this.InitializeProperties(node);
        }

        protected override void InnerInitializeProperties(XElement element)
        {
            this.FillProperty(m => m.Price);
            this.FillProperty(m => m.DeliveryDate);
            this.FillProperty(m => m.IsOnLine);
        }
    }


}
#else
#endif
