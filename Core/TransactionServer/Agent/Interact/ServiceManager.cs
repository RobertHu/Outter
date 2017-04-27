using Core.TransactionServer.Engine;
using Core.TransactionServer.Engine.iExchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Interact
{
    internal static class ServiceManager
    {
        private static TradingEngine _tradingEngine;
        internal static void Initialize(TradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
        }

        internal static CancelService GetCancelService()
        {
            throw new NotImplementedException();
        }
    }


   

}
