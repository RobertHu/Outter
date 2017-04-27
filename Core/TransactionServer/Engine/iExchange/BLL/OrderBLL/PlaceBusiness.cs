using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine.iExchange.Util;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Engine.iExchange.BLL.OrderBLL
{
    internal sealed class PlaceBusiness
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlaceBusiness));
        private IInnerTradingEngine _tradingEngine;

        public PlaceBusiness( IInnerTradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
        }


       

    }
}
