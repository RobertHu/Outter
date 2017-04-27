using Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Commands
{
    internal abstract class AddLmtQuantiryOnMaxLotChangeOrderCommandBase : AddOrderCommandBase
    {
        protected AddLmtQuantiryOnMaxLotChangeOrderCommandBase(Transaction tran, AddOrderCommandServiceBase commandService, Order originOrder, decimal lot) :
            base(tran, Visitors.AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor.Default, commandService)
        {
            this.OriginOrder = originOrder;
            this.Lot = lot;
        }

        internal Order OriginOrder { get; private set; }

        internal decimal Lot { get; private set; }

        internal decimal LotBalance
        {
            get { return this.OriginOrder.IsOpen ? this.Lot : 0m; }
        }
    }


    internal sealed class AddLmtQuantiryOnMaxLotChangeOrderCommand : AddLmtQuantiryOnMaxLotChangeOrderCommandBase
    {
        internal AddLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot)
            : base(tran, AddOrderCommandService.Default, originOrder, lot)
        {
        }
    }

    internal sealed class AddPhysicalLmtQuantiryOnMaxLotChangeOrderCommand : AddLmtQuantiryOnMaxLotChangeOrderCommandBase
    {
        internal AddPhysicalLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot)
            : base(tran, AddPhysicalOrderCommandService.Default, originOrder, lot)
        {
        }
    }


}
