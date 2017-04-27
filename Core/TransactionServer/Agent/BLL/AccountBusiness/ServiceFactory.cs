using Core.TransactionServer.Agent.Physical.AccountBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal abstract class StrategyFactory
    {
        protected Lazy<AccountExecuteStrategy> _executeStrategy;
        protected Lazy<AccountExecuteStrategy> _bookStrategy;

        protected StrategyFactory()
        {
            this.Initialize();
        }

        protected abstract void Initialize();

        internal AccountExecuteStrategy CreateExecuteStrategy()
        {
            return _executeStrategy.Value;
        }

        internal AccountExecuteStrategy CreateBookStrategy()
        {
            return _bookStrategy.Value;
        }
    }

    internal sealed class GeneralExecuteStrategyFactory : StrategyFactory
    {
        protected override void Initialize()
        {
            _executeStrategy = new Lazy<AccountExecuteStrategy>(() => new GeneralAccountExecuteStrategy());
            _bookStrategy = new Lazy<AccountExecuteStrategy>(() => new BookAccountExecuteStrategy());
        }
    }


    internal sealed class PhysicalExecuteStrategyFactory : StrategyFactory
    {
        protected override void Initialize()
        {
            _executeStrategy = new Lazy<AccountExecuteStrategy>(() => new PhysicalExecuteStrategy());
            _bookStrategy = new Lazy<AccountExecuteStrategy>(() => new BookAccountExecuteStrategy());
        }
    }

}
