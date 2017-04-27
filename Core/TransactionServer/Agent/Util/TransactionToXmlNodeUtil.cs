using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using iExchange.Common;
using Core.TransactionServer.Agent.Settings;

namespace Core.TransactionServer.Agent.Util
{
    public static class TransactionToXmlNodeUtil
    {
        private static readonly string PLACE_STYLE_NAME = "Place.xslt";
        private static readonly string ASSIGN_STYLE_NAME = "Assign.xslt";

        public static XmlNode ToPlaceNode(XmlNode source)
        {
            return TransformCommon(source, PLACE_STYLE_NAME);
        }

        public static XmlNode ToAssignNode(XmlNode source)
        {
            return TransformCommon(source, ASSIGN_STYLE_NAME);
        }

        private static XmlNode TransformCommon(XmlNode source, string styleName)
        {
            var path = GetStylePath(styleName);
            return XmlTransform.Transform(source, path, null);
        }

        private static string GetStylePath(string styleName)
        {
            return Path.Combine(ExternalSettings.Default.StyleSheetDirectory, styleName);
        }
    }
}
