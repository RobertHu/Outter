using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class ReHitter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ReHitter));

        internal static readonly ReHitter Default = new ReHitter();

        static ReHitter() { }
        private ReHitter() { }

        internal Guid[] Hit(Guid[] orderIds, Guid[] accountIds)
        {
            try
            {
                if (accountIds == null) throw new NullReferenceException("accountIds is null");
                if (orderIds == null) throw new NullReferenceException("orderIds is null");

                if (accountIds.Length != 1 || orderIds.Length != 1)
                {
                    Logger.ErrorFormat("accountIds.length = {0}, orderIds.Length = {1}", accountIds.Length, orderIds.Length);
                    return null;
                }

                DateTime baseTime = Market.MarketManager.Now;
                List<Guid> result = new List<Guid>();
                var account = TradingSetting.Default.GetAccount(accountIds[0]);
                if (account == null) throw new NullReferenceException(string.Format("accountId = {0} can't be found", accountIds[0]));
                Order order = account.GetOrder(orderIds[0]);
                if (order == null) throw new NullReferenceException(string.Format("orderId = {0} can't be found", orderIds[0]));
                var hitStatus = this.HitOrder(account, order, baseTime);
                if (hitStatus == OrderHitStatus.Hit || hitStatus == OrderHitStatus.ToAutoFill)
                {
                    return new Guid[] { orderIds[0] };
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private OrderHitStatus HitOrder(Account account, Order order, DateTime baseTime)
        {
            Logger.InfoFormat("HitOrder accountId = {0}, orderId = {1}", account.Id, order.Id);
            var instrument = order.Owner.TradingInstrument;
            if (!instrument.CanTrade(baseTime, PlaceContext.Empty)) return OrderHitStatus.None;
            Price hitPrice = order.SetPrice;
            Price buy = hitPrice, sell = hitPrice;
            return account.TryHit(order, Quotations.Quotation.CreateByRehit(buy, sell));
        }

    }
}
