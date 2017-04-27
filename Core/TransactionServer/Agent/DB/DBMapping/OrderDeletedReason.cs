using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class OrderDeletedReason
    {
        public OrderDeletedReason(IDBRow dr)
        {
            this.ID = dr.GetColumn<Guid>("ID");
            this.Code = dr.GetColumn<string>("Code");
            this.Description = dr.GetColumn<string>("Description");
            this.ReasonDesc = dr.GetColumn<string>("ReasonDesc");
        }

        public Guid ID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string ReasonDesc { get; set; }
    }
}
