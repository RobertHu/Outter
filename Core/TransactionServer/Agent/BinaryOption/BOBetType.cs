using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal enum BOOption
    {
        Instance = 0,
        IntegralMinute = 1,
        Settle = 2
    }

    internal sealed class BOBetType
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BOBetType));

        internal Guid ID
        {
            get;
            private set;
        }

        internal BOOption Option
        {
            get;
            private set;
        }

        internal TimeSpan LastPlaceOrderTimeSpan
        {
            get;
            private set;
        }

        internal string Code
        {
            get;
            private set;
        }

        internal int HitCount
        {
            get;
            private set;
        }

        internal BOBetType(IDBRow row)
        {
            this.ID = (Guid)row["ID"];
            this.Code = (string)row["Code"];
            this.HitCount = (int)row["HitCount"];
            this.Option = (BOOption)(int)row["Option"];
            this.LastPlaceOrderTimeSpan = TimeSpan.FromSeconds((int)row["LastOrderTimeSpan"]);
        }

        internal BOBetType(Guid id, XElement node)
        {
            this.ID = id;
            this.Code = node.Attribute("Code").Value;
            this.HitCount = XmlConvert.ToInt32(node.Attribute("HitCount").Value);
            this.Option = (BOOption)XmlConvert.ToInt32(node.Attribute("Option").Value);
            this.LastPlaceOrderTimeSpan = TimeSpan.FromSeconds(XmlConvert.ToInt32(node.Attribute("LastOrderTimeSpan").Value));
        }

        internal void Update(XElement node)
        {
            foreach (XAttribute attribute in node.Attributes())
            {
                string name = attribute.Name.ToString();
                string value = attribute.Value;
                switch (name)
                {
                    case "Code":
                        this.Code = value;
                        break;
                    case "HitCount":
                        this.HitCount = XmlConvert.ToInt32(value);
                        break;
                    case "Option":
                        this.Option = (BOOption)XmlConvert.ToInt32(value);
                        break;
                    case "LastOrderTimeSpan":
                        this.LastPlaceOrderTimeSpan = TimeSpan.FromSeconds(XmlConvert.ToInt32(value));
                        break;
                    default:
                        Logger.WarnFormat("Unknow name={0},value={1} in update BOBetType", name, value);
                        break;
                }
            }
        }
    }
}
