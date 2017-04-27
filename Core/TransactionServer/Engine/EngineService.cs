using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Engine.iExchange.BLL.OrderBLL;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Engine
{
    public sealed class EngineService
    {
        public static EngineService Default = new EngineService();

        static EngineService() { }
        private EngineService() { }

        public void SetQuotation(QuotationBulk quotationBulk)
        {
            OrderHitManager.Default.Add(quotationBulk);
        }

        public void UpdateInstrumentStatus(Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>> status)
        {
            iExchange.iExchangeEngine.Default.UpdateInstrumentStatus(status);
        }

    }
}
