using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Engine;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    public sealed class BOOrderRelation : OrderRelation
    {
        private decimal _payBackPledge;

        internal BOOrderRelation(OrderRelationConstructParams constructParams)
            : base(constructParams)
        {
            _payBackPledge = 0m;
        }

        internal decimal PayBackPledge
        {
            get { return _payBackPledge; }
        }

        internal void CalculatePayBackPledge()
        {
            _payBackPledge = -((Order)this.OpenOrder).PaidPledgeBalance;
            this.AddBill(new Bill(_accountId, this.CloseOrder.CurrencyId, _payBackPledge, BillType.PayBackPledge, BillOwnerType.OrderRelation));
        }

        internal void UpdateOpenOrderPledge()
        {
            ((Order)this.OpenOrder).PaidPledgeBalance = 0;
        }

        protected override decimal CalculateTradePL(ExecuteContext context)
        {
            var boOpenOrder = (Order)this.OpenOrder;
            if (boOpenOrder.BetResult == BetResult.Lose)
            {
                return boOpenOrder.PaidPledge;
            }
            else if (boOpenOrder.BetResult == BetResult.Win)
            {
                return (-boOpenOrder.PaidPledge * boOpenOrder.Odds) + boOpenOrder.PaidPledge;
            }
            else
            {
                return 0m;
            }
        }

    }
}
