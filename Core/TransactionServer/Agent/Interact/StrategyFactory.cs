using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Interact
{
    //internal abstract class StrategyFactory { protected TradingEngine _tradingEngine; private ExecuteStrategy _executeStrategy; protected StrategyFactory(TradingEngine tradingEngine) { _tradingEngine = tradingEngine; } public ExecuteStrategy Create() { if (_executeStrategy == null) {
    //            _executeStrategy = this.InnerCreate();
    //        }
    //        return _executeStrategy;
    //    }

    //    protected abstract ExecuteStrategy InnerCreate();
    //}

    //internal abstract class ExecuteStrategyFactory : StrategyFactory
    //{
    //    protected ExecuteStrategyFactory(TradingEngine tradingEngine)
    //        : base(tradingEngine)
    //    {
    //    }

    //}

    //internal abstract class BookStrategyFactory : StrategyFactory
    //{
    //    protected BookStrategyFactory(TradingEngine tradingEngine)
    //        : base(tradingEngine)
    //    {
    //    }
    //}

    //internal sealed class GeneralExecuteStrategyFactory : ExecuteStrategyFactory
    //{
    //    internal GeneralExecuteStrategyFactory(ExecuteTradingEngine tradingEngine)
    //        : base(tradingEngine)
    //    {
    //    }

    //    protected override ExecuteStrategy InnerCreate()
    //    {
    //        return new GeneralExecuteStrategy(_tradingEngine);
    //    }
    //}

    //internal sealed class GeneralBookStrategyFactory : BookStrategyFactory
    //{
    //    internal GeneralBookStrategyFactory(BookTradingEngine tradingEngine)
    //        : base(tradingEngine) { }

    //    protected override ExecuteStrategy InnerCreate()
    //    {
    //        return new GeneralBookStrategy(_tradingEngine);
    //    }
    //}

    //internal sealed class PhysicalExecuteStrategyFactory : ExecuteStrategyFactory
    //{
    //    internal PhysicalExecuteStrategyFactory(ExecuteTradingEngine tradingEngine)
    //        : base(tradingEngine)
    //    {
    //    }

    //    protected override ExecuteStrategy InnerCreate()
    //    {
    //        return new PhysicalExecuteStrategy(_tradingEngine);
    //    }

    //}

    //internal sealed class PhysicalBookStrategyFactory : BookStrategyFactory
    //{
    //    internal PhysicalBookStrategyFactory(BookTradingEngine tradingEngine)
    //        : base(tradingEngine) { }

    //    protected override ExecuteStrategy InnerCreate()
    //    {
    //        return new PhysicalBookStrategy(_tradingEngine);
    //    }
    //}

    //internal sealed class ExecuteStrategyFactories
    //{
    //    internal ExecuteStrategyFactories(ExecuteStrategyFactory generalFactory, PhysicalExecuteStrategyFactory physicalFactory)
    //    {
    //        this.GeneralFactory = generalFactory;
    //        this.PhysicalFactory = physicalFactory;
    //    }
    //    internal ExecuteStrategyFactory GeneralFactory { get; private set; }
    //    internal ExecuteStrategyFactory PhysicalFactory { get; private set; }
    //}

    //internal sealed class BookStrategyFactories
    //{
    //    internal BookStrategyFactories(BookStrategyFactory generalFactory, BookStrategyFactory physicalFactory)
    //    {
    //        this.GeneralFactory = generalFactory;
    //        this.PhysicalFactory = physicalFactory;
    //    }
    //    internal BookStrategyFactory GeneralFactory { get; private set; }
    //    internal BookStrategyFactory PhysicalFactory { get; private set; }
    //}
}
