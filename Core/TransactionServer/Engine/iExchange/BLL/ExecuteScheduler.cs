using Core.TransactionServer.Agent;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Core.TransactionServer.Engine.iExchange.BLL
{
    internal sealed class ExecuteScheduler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExecuteScheduler));
        private ConcurrentDictionary<Guid, ActionBlock<ExecuteEventArgs>> _schedulerDict;
        private IInnerTradingEngine _tradingEngine;

        internal ExecuteScheduler(IInnerTradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
            _schedulerDict = new ConcurrentDictionary<Guid, ActionBlock<ExecuteEventArgs>>();
        }

        internal void Add(ExecuteEventArgs e)
        {
            ActionBlock<ExecuteEventArgs> block;
            if (!_schedulerDict.TryGetValue(e.Account.Id, out block))
            {
                block = new ActionBlock<ExecuteEventArgs>(request => Task.Factory.StartNew(() => _tradingEngine.Execute(request)));
                _schedulerDict.TryAdd(e.Account.Id, block);
            }
            block.Post(e);
        }

    }
}
