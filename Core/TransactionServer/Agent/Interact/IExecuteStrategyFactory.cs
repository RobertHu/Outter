using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Interact
{
    internal abstract class ExecuteStrategyFactory
    {
        protected Lazy<ExecuteStrategy> _executeStrategy;
        protected Lazy<ExecuteStrategy> _bookStrategy;

        protected ExecuteStrategyFactory(TradingEngine tradingEngine)
        {
            this.Initialize(tradingEngine);
        }

        protected abstract void Initialize(TradingEngine tradingEngine);

        internal ExecuteStrategy CreateExecuteStrategy()
        {
            return _executeStrategy.Value;
        }

        internal ExecuteStrategy CreateBookStrategy()
        {
            return _bookStrategy.Value;
        }
    }

    internal sealed class GeneralExecuteStrategyFactory : ExecuteStrategyFactory
    {
        internal GeneralExecuteStrategyFactory(TradingEngine tradingEngine)
            : base(tradingEngine) { }

        protected override void Initialize(TradingEngine tradingEngine)
        {
            _executeStrategy = new Lazy<ExecuteStrategy>(() => new GeneralExecuteStrategy(tradingEngine));
            _bookStrategy = new Lazy<ExecuteStrategy>(() => new GeneralBookStrategy(tradingEngine));
        }
    }

    internal sealed class PhysicalExecuteStrategyFactory : ExecuteStrategyFactory
    {
        internal PhysicalExecuteStrategyFactory(TradingEngine tradingEngine)
            : base(tradingEngine) { } 

        protected override void Initialize(TradingEngine tradingEngine)
        {
            _executeStrategy = new Lazy<ExecuteStrategy>(() => new PhysicalExecuteStrategy(tradingEngine));
            _bookStrategy = new Lazy<ExecuteStrategy>(() => new PhysicalBookStrategy(tradingEngine));
        }
    }

}
