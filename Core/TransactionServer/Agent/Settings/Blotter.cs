using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class Blotter
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

        public Blotter(IDBRow  row)
        {
            this.ID = (Guid)row["ID"];
            this.Code = (string)row["Code"];
        }

        public Blotter(XElement row)
        {
            this.Update(row);
        }

        internal void Update(XElement row)
        {
            this.ID = row.AttrToGuid("ID");
            this.Code = row.Attribute("Code").Value;
        }
    }
}
