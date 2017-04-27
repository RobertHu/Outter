using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public class OrderRelationConstructParams
    {
        internal OrderRelationConstructParams()
        {
            this.Id = Guid.NewGuid();
        }

        internal Guid Id { get; set; }
        internal Order CloseOrder { get; set; }
        internal Order OpenOrder { get; set; }
        internal decimal ClosedLot { get; set; }
        internal DateTime? CloseTime { get; set; }
        internal decimal Commission { get; set; }
        internal decimal Levy { get; set; }
        internal decimal OtherFee { get; set; }
        internal decimal InterestPL { get; set; }
        internal decimal StoragePL { get; set; }
        internal decimal TradePL { get; set; }
        internal DateTime? ValueTime { get; set; }
        internal int Decimals { get; set; }
        internal decimal RateIn { get; set; }
        internal decimal RateOut { get; set; }
        internal OperationType OperationType { get; set; }
    }

}
