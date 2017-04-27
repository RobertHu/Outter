using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using log4net;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.BinaryOption
{
    public sealed class BOBet
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BOBet));

        internal BOBet(DataRow row)
        {
            this.ID = (Guid)row["ID"];
            this.Code = (string)row["Code"];
            this.HitCount = (int)row["HitCount"];
        }

        internal BOBet(Guid id, XElement node)
        {
            this.ID = id;
            this.Code = node.Attribute("Code").Value;
            this.HitCount = node.AttrToInt32("HitCount");
        }

        public Guid ID
        {
            get;
            private set;
        }

        public string Code
        {
            get;
            private set;
        }

        public int HitCount
        {
            get;
            private set;
        }

        internal void Update(XElement node)
        {
            foreach (var attribute in node.Attributes())
            {
                string name = attribute.Name.LocalName;
                string value = attribute.Value;
                switch (name)
                {
                    case "Code":
                        this.Code = value;
                        break;
                    case "HitCount":
                        this.HitCount = value.XmlToInt32();
                        break;
                    default:
                        Logger.WarnFormat("Unknow name={0},value={1} in update BOBetType", name, value);
                        break;
                }
            }
        }
    }
}
