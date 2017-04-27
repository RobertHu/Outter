using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.TransactionBusiness
{
    internal sealed class PhysicalInstalmentTransactionParams
    {
        internal PhysicalTransaction OldTransaction { get; set; }
        internal Guid SourceOrderId { get; set; }
        internal PhysicalOrder OldOrder { get; set; }
        internal decimal Lot { get; set; }
        internal bool IsOpen { get; set; }
        internal bool IsBuy { get; set; }
    }

    public sealed class AddPhysicalTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddPhysicalTransactionCommandFactory Default = new AddPhysicalTransactionCommandFactory();

        private AddPhysicalTransactionCommandFactory() { }

        internal override AddTransactionCommandBase Create(Account account, System.Data.DataRow dataRowTran)
        {
            return new AddPhysicalTransactionCommand(account, dataRowTran);
        }

        internal override AddTransactionCommandBase CreateByAutoClose(Account account, Order openOrder, iExchange.Common.Price closePrice, iExchange.Common.OrderType orderType)
        {
            return new AddPhysicalTransactionCommand(account, (PhysicalOrder)openOrder, closePrice, orderType);
        }

        internal override AddTransactionCommandBase CreateByClose(Account account, Order openOrder)
        {
            return new AddPhysicalTransactionCommand(account, (PhysicalOrder)openOrder);
        }

        internal override AddTransactionCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, iExchange.Common.Price limitPrice, iExchange.Common.Price stopPrice)
        {
            return new AddPhysicalTransactionCommand(account, (PhysicalTransaction)ifTran, sourceOrderId, limitPrice, stopPrice);
        }

        internal override AddTransactionCommandBase CreateCutTransaction(Account account, CutTransactionParams cutTransactionParams)
        {
            return new AddPhysicalTransactionCommand(account, cutTransactionParams);
        }

        internal AddTransactionCommandBase CreateInstalmentTransaction(Account account, PhysicalInstalmentTransactionParams physicalInstalmentTransactionParams)
        {
            return new AddPhysicalTransactionCommand(account, physicalInstalmentTransactionParams);
        }

        public override AddTransactionCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            return new AddPhysicalTransactionCommand(account, tranData);
        }
    }
}
