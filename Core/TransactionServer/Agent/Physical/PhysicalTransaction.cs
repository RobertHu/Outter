using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Factory;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical
{
    internal sealed class PhysicalTransaction : Transaction
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PhysicalTransaction));

        internal PhysicalTransaction(Account account, TransactionConstructParams param, ITransactionServiceFactory serviceFactory)
            : base(account, param, serviceFactory) { }

        internal bool ExistsInstalmentOrder()
        {
            bool result = this.ExistsPhysicalOrderWithConditions(o => o.IsInstalment);
            if (result)
            {
                Logger.InfoFormat("ExistsInstalmentOrder tranId = {0}", this.Id);
            }
            return result;
        }

        internal bool ExistsFilledShortSellOrder()
        {
            return this.ExistsPhysicalOrderWithConditions(o => o.IsExecuted && o.PhysicalTradeSide == PhysicalTradeSide.ShortSell && o.LotBalance > 0);
        }

        internal override bool CanBeClosedBySplit(Transaction targetTran)
        {
            return base.CanBeClosedBySplit(targetTran) && !this.Orders.Any(o => ((PhysicalOrder)o).DeliveryLockLot > 0);
        }

        private bool ExistsPhysicalOrderWithConditions(Predicate<PhysicalOrder> predicate)
        {
            Debug.Assert(this.IsPhysical);
            foreach (PhysicalOrder order in this.Orders)
            {
                if (predicate(order)) return true;
            }
            return false;
        }
    }
}
