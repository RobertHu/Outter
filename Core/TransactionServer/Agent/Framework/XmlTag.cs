using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Framework
{
    public sealed class XmlTag
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(XmlTag));
        private XElement _content;

        public XmlTag(string name)
        {
            _content = new XElement(name);
        }

        internal XmlTag() { }

        public void AddAttribute(string name, string value)
        {
            _content.SetAttributeValue(name, value);
        }

        public XElement Value
        {
            get { return _content; }
        }

        public void AddChild(XmlTag xmlTag)
        {
            if (xmlTag == null) return;
            if (_content == null)
            {
                _content = xmlTag.Value;
            }
            else
            {
                _content.Add(xmlTag.Value);
            }
        }

        public override string ToString()
        {
            return _content.ToString();
        }
    }


}
