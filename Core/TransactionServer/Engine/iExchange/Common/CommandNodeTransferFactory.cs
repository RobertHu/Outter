using Core.TransactionServer.Engine.iExchange.Util;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Engine.iExchange.Common
{
    public interface  ICommandNodeTransferFactory
    {
        XmlNode Create(XmlNode source);
    }


    public class PlaceCommandNodeTransferFactory:ICommandNodeTransferFactory
    {
        public static readonly ICommandNodeTransferFactory Default = new PlaceCommandNodeTransferFactory();
        private PlaceCommandNodeTransferFactory() { }
        public XmlNode Create(XmlNode source)
        {
            string styleName = "Place.xslt";
            string filePath = FileUtil.GetXmlStyleSheetPath(styleName);
            return XmlTransform.Transform(source, filePath, null);
        }
    }


}
