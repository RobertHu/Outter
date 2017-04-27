using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Core.TransactionServer.Engine.iExchange.BLL.OrderBLL
{
    internal sealed class OrderHitManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderHitManager));
        internal static OrderHitManager Default = new OrderHitManager();

        private const int AUTO_CLOSE_TRANS_CAPACITY_FACTOR = 15;
        private ActionBlock<QuotationBulk> _quotationBlock;

        static OrderHitManager() { }
        private OrderHitManager()
        {
            _quotationBlock = new ActionBlock<QuotationBulk>((Action<QuotationBulk>)this.Hit);
        }

        internal void Add(QuotationBulk quotationBulk)
        {
            _quotationBlock.Post(quotationBulk);
        }

        private void Hit(QuotationBulk quotationBulk)
        {
            try
            {
                TradingSetting.Default.DoParallelForAccounts(a => a.Hit(quotationBulk));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
