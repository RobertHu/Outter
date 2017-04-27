using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Engine.iExchange.Common
{
    internal interface ICommandFactory
    {
        Command Create(XmlNode source, CommandSequence commandSequence);
    }




    internal class PlaceCommandFactory : ICommandFactory
    {
        public static PlaceCommandFactory Default = new PlaceCommandFactory();
        private PlaceCommandFactory() { }
        public Command Create(XmlNode source, CommandSequence commandSequence)
        {
            PlaceCommand placeCommand;
            placeCommand = new PlaceCommand((int)commandSequence.Value);
            placeCommand.InstrumentID = XmlConvert.ToGuid(source.Attributes["InstrumentID"].Value);
            placeCommand.AccountID = XmlConvert.ToGuid(source.Attributes["AccountID"].Value);
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode content = xmlDoc.CreateElement("Place");
            xmlDoc.AppendChild(content);
            placeCommand.Content = content;
            content.AppendChild(xmlDoc.ImportNode(source, true));
            return placeCommand;
        }



    }






}
