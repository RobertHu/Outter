using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Protocal.TypeExtensions;
using System.Xml.Linq;

namespace Protocal.UnitTest
{
    public class XmlNodeAppendTest
    {

        public void AppendTest()
        {
            XmlDocument doc = new XmlDocument();
            var root = doc.CreateElement("Instruoment");
            root.SetAttribute("X", "5");
            root.SetAttribute("y", "10");
            doc.AppendChild(root);
            Debug.WriteLine(doc.DocumentElement.OuterXml);
            XElement quote = new XElement("QuoteAnswer");
            var oldRoot = XElement.Load(doc.DocumentElement.CreateNavigator().ReadSubtree());
            quote.Add(oldRoot);
            Debug.WriteLine(quote.ToString());
        }

    }
}
