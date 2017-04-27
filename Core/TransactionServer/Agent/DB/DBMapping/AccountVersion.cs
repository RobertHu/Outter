using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class AccountVersion
    {
        public Guid AccountID { get; set; }
        public Int64 Version { get; set; }
    }

    public sealed class Organization
    {
        public Guid ID { get; set; }
        public string Code { get; set; }
    }

}
