using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands
{
    internal abstract class AddLmtQuantiryOnMaxLotChangeTransactionCommandBase : AddTranCommandBase
    {
        protected AddLmtQuantiryOnMaxLotChangeTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Order originOrder, decimal lot)
            : base(account, Visitors.AddLmtQuantiryOnMaxLotChangeTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.OriginOrder = originOrder;
            this.Lot = lot;
        }

        internal Order OriginOrder { get; private set; }

        internal decimal Lot { get; private set; }

        internal Transaction OriginTran
        {
            get { return this.OriginOrder.Owner; }
        }
    }


    internal sealed class AddLmtQuantiryOnMaxLotChangeTransactionCommand : AddLmtQuantiryOnMaxLotChangeTransactionCommandBase
    {
        internal AddLmtQuantiryOnMaxLotChangeTransactionCommand(Account account, Order originOrder, decimal lot)
            : base(account, AddTransactionCommandService.Default, originOrder, lot)
        {
        }
    }


    internal sealed class AddPhysicalLmtQuantiryOnMaxLotChangeTransactionCommand : AddLmtQuantiryOnMaxLotChangeTransactionCommandBase
    {
        internal AddPhysicalLmtQuantiryOnMaxLotChangeTransactionCommand(Account account, Order originOrder, decimal lot)
            : base(account, AddPhysicalTransactionCommandService.Default, originOrder, lot)
        {
        }
    }


}
