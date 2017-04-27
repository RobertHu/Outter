using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using Protocal;
using System.Xml;

namespace iExchange.StateServer.Adapter
{
    internal static class XMLStringExtension
    {
        internal static XmlNode ConvertToXmlNode(this string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }
    }


}