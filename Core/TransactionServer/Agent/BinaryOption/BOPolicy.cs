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
    public sealed class BOPolicy
    {
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

        public int? MaxOrderCount
        {
            get;
            private set;
        }

        public decimal? TotalBetLimit
        {
            get;
            private set;
        }

        public BOPolicy(IDBRow row)
        {
            this.ID = (Guid)row["ID"];
            this.Code = (string)row["Code"];
            if (row["MaxOrderCount"] == DBNull.Value)
            {
                this.MaxOrderCount = null;
            }
            else
            {
                this.MaxOrderCount = (int)row["MaxOrderCount"];
            }

            if (row["TotalBetLimit"] == DBNull.Value)
            {
                this.TotalBetLimit = null;
            }
            else
            {
                this.TotalBetLimit = (decimal)row["TotalBetLimit"];
            }
        }

        public BOPolicy(XElement node)
        {
            this.ID = XmlConvert.ToGuid(node.Attribute("ID").Value);
            this.Code = node.Attribute("Code").Value;
            if (node.Attribute("MaxOrderCount") != null)
            {
                string value = node.Attribute("MaxOrderCount").Value;
                if (string.IsNullOrEmpty(value))
                {
                    this.MaxOrderCount = null;
                }
                else
                {
                    this.MaxOrderCount = XmlConvert.ToInt32(value);
                }
            }

            if (node.Attribute("TotalBetLimit") != null)
            {
                string value = node.Attribute("TotalBetLimit").Value;
                if (string.IsNullOrEmpty(value))
                {
                    this.TotalBetLimit = null;
                }
                else
                {
                    this.TotalBetLimit = XmlConvert.ToDecimal(value);
                }
            }
        }

        internal void Update(XElement node)
        {
            if (node.Attribute("MaxOrderCount") != null)
            {
                string value = node.Attribute("MaxOrderCount").Value;
                if (string.IsNullOrEmpty(value))
                {
                    this.MaxOrderCount = null;
                }
                else
                {
                    this.MaxOrderCount = XmlConvert.ToInt32(value);
                }
            }

            if (node.Attribute("TotalBetLimit") != null)
            {
                string value = node.Attribute("TotalBetLimit").Value;
                if (string.IsNullOrEmpty(value))
                {
                    this.TotalBetLimit = null;
                }
                else
                {
                    this.TotalBetLimit = XmlConvert.ToDecimal(value);
                }
            }
        }
    }
}
