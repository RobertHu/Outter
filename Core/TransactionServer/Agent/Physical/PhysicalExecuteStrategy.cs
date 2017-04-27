using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical
{
    public class PhysicalExecuteStrategy : ExecuteStrategy
    {
        internal PhysicalExecuteStrategy(TradingEngine tradingEngine)
            : base(tradingEngine) { }

        protected override void ExecuteTransaction(Transaction tran, OrderExecuteEventArgs e)
        {
            PhysicalExecutor.Default.Execute(tran, e, _tradingEngine);
            this.ExecuteOnAccount(tran, e);
        }
    }

    public sealed class PhysicalBookStrategy : BookExecuteStrategy
    {
        internal PhysicalBookStrategy(TradingEngine tradingEngine)
            : base(tradingEngine) { }

        protected override void ExecuteTransaction(Transaction tran, OrderExecuteEventArgs e)
        {
            PhysicalExecutor.Default.Execute(tran, e, _tradingEngine);
            base.ExecuteTransaction(tran, e);
        }
    }

}
