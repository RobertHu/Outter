using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL
{
    internal class MarketOrderEntity
    {
        private int remainSeconds;
        internal Order Order { get; private set; }
        internal bool ShouldAutoFill { get; private set; }

        internal MarketOrderEntity(Order order, int delayTime, bool shouldAutoFill)
        {
            this.Order = order;
            this.remainSeconds = delayTime;
            this.ShouldAutoFill = shouldAutoFill;
        }

        internal int SubstractRemainSeconds(int delta)
        {
            this.remainSeconds -= delta;
            return this.remainSeconds;
        }
    }

    internal delegate void ExecuteMarketOrder(Order order);
    internal delegate void BoardcastMarketOrderHit(Order order);

    internal sealed class MarketOrderProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MarketOrderProcessor));
        private Timer delayCheckTimer;
        private List<MarketOrderEntity> marketOrders = new List<MarketOrderEntity>();
        private object locker = new object();

        internal static readonly MarketOrderProcessor Default = new MarketOrderProcessor();

        static MarketOrderProcessor() { }
        private MarketOrderProcessor()
        {
            this.delayCheckTimer = new Timer(this.DoTimeoutCheck);
            this.delayCheckTimer.Change(1000, 1000);
        }

        public void Add(Order marketOrder)
        {
            if (marketOrder.Owner.OrderType == OrderType.Market)
            {
                var dealingPolicyDetail = marketOrder.Owner.DealingPolicyPayload();

                var quotation = marketOrder.Owner.AccountInstrument.GetQuotation(marketOrder.Owner.SubmitorQuotePolicyProvider);
                marketOrder.HitMarketOrder(Market.MarketManager.Now, quotation.BuyPrice, quotation.SellPrice, quotation.Timestamp);

                bool shouldAutoFill = marketOrder.Owner.SettingInstrument().IsAutoFill && marketOrder.Lot <= dealingPolicyDetail.AutoLmtMktMaxLot;
                if (dealingPolicyDetail.AutoDQDelay <= TimeSpan.Zero)
                {
                    this.Process(marketOrder, shouldAutoFill);
                }
                else
                {
                    lock (this.locker)
                    {
                        this.marketOrders.Add(new MarketOrderEntity(marketOrder, (int)dealingPolicyDetail.AutoDQDelay.TotalSeconds, shouldAutoFill));
                    }
                }
            }
        }

        private void DoTimeoutCheck(object state)
        {
            List<MarketOrderEntity> timeOutOrders = new List<MarketOrderEntity>();

            lock (this.locker)
            {
                foreach (MarketOrderEntity item in this.marketOrders)
                {
                    if (item.SubstractRemainSeconds(1) <= 0)
                    {
                        timeOutOrders.Add(item);
                    }
                }

                foreach (MarketOrderEntity item in timeOutOrders)
                {
                    this.marketOrders.Remove(item);
                }
            }

            foreach (MarketOrderEntity item in timeOutOrders)
            {
                this.Process(item.Order, item.ShouldAutoFill);
            }
        }

        private void Process(Order order, bool shouldAutoFill)
        {
            if (shouldAutoFill)
            {
                this.ExecuteOrder(order);
            }
            else
            {
                this.BroadcastHitOrder(order);
            }
        }

        private void ExecuteOrder(Order marketOrder)
        {
            Guid tranId = marketOrder.Owner.Id;
            string buyPrice = null, sellPrice = null;
            if (marketOrder.IsBuy) buyPrice = (string)marketOrder.BestPrice;
            else sellPrice = (string)marketOrder.BestPrice;

            if (marketOrder.Owner.Type == TransactionType.Pair)
            {
                Order otherMarketOrder = marketOrder.Owner.FirstOrder == marketOrder ? marketOrder.Owner.SecondOrder : marketOrder.Owner.FirstOrder;
                if (otherMarketOrder.IsBuy) buyPrice = (string)otherMarketOrder.BestPrice;
                else sellPrice = (string)otherMarketOrder.BestPrice;
            }
            TransactionError error = marketOrder.Account.Execute(tranId, buyPrice, sellPrice, null, marketOrder.Id);
            Logger.InfoFormat("ExecuteMarketOrder tran id = {0}; result = {1}", tranId, error);
        }

        private void BroadcastHitOrder(Order order)
        {
            if (order.Phase == OrderPhase.Placed)
            {
                Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateHitCommand(order.AccountId, order.Id));
            }
        }

    }
}
