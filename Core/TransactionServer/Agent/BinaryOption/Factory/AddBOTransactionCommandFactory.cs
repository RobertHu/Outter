using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption.Factory
{
    public sealed class AddBOTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddBOTransactionCommandFactory Default = new AddBOTransactionCommandFactory();

        private AddBOTransactionCommandFactory() { }

        internal override AddTransactionCommandBase Create(Account account, System.Data.DataRow dataRowTran)
        {
            return new Command.AddBOTransactionCommand(account, dataRowTran);
        }

        internal override AddTransactionCommandBase CreateByAutoClose(Account account, Agent.Order openOrder, iExchange.Common.Price closePrice, iExchange.Common.OrderType orderType)
        {
            throw new  NotImplementedException();
        }

        internal override AddTransactionCommandBase CreateByClose(Account account, Agent.Order openOrder)
        {
            return new Command.AddBOTransactionCommand(account, (Order)openOrder);
        }

        internal override AddTransactionCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, iExchange.Common.Price limitPrice, iExchange.Common.Price stopPrice)
        {
            throw new NotImplementedException();
        }

        internal override AddTransactionCommandBase CreateCutTransaction(Account account, CutTransactionParams cutTransactionParams)
        {
            throw new NotImplementedException();
        }

        public override AddTransactionCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            throw new NotImplementedException();
        }
    }
}
