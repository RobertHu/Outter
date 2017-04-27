using Core.TransactionServer.Agent;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Market = Core.TransactionServer.Agent.Market;
using BO = Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.BinaryOption.Factory;
using System.Diagnostics;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.CommandFactorys;
using Core.TransactionServer.Engine.iExchange;
using Core.TransactionServer.Engine;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOEngine
    {
        public static readonly BOEngine Default = new BOEngine();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BOEngine));
        private SortedSet<BO.Order> _waitingForCloseOrders = new SortedSet<BO.Order>(BO.Order.Comparer);
        private object _mutex = new object();
        private Timer _hitTimer;

        static BOEngine() { }

        private BOEngine()
        {
            this._hitTimer = new Timer(this.DoWork, null, Timeout.Infinite, Timeout.Infinite);
        }

        public int WaitingForCloseOrderCount
        {
            get { return _waitingForCloseOrders.Count; }
        }

        public BO.Order LatestToCloseOrder
        {
            get { return _waitingForCloseOrders.Min; }
        }

        internal void HandleExecutedTransaction(Account account, BOTransaction tran)
        {
            try
            {
                var order = (Order)tran.FirstOrder;
                if (order.CanBeClosed())
                {
                    lock (this._mutex)
                    {
                        var boOrder = (BO.Order)order;
                        boOrder.CalculateNextHitTime();
                        this._waitingForCloseOrders.Add(boOrder);
                        this.StartHitTimer();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void AddOrders(IEnumerable<BO.Order> orders)
        {
            lock (this._mutex)
            {
                this._waitingForCloseOrders.Clear();
                foreach (BO.Order order in orders)
                {
                    if (order.CanBeClosed())
                    {
                        order.CalculateNextHitTime();
                        _waitingForCloseOrders.Add(order);
                    }
                }
                this.StartHitTimer();
            }
        }


        private void DoWork(object state)
        {
            lock (this._mutex)
            {
                DateTime now = Market.MarketManager.Now;
                while (this._waitingForCloseOrders.Count > 0)
                {
                    var order = _waitingForCloseOrders.Min;
                    if (order.NextHitTime > now) break;
                    _waitingForCloseOrders.Remove(order);
                    Task.Factory.StartNew(() => this.Hit(order, now));
                }
                this.StartHitTimer();
            }
        }

        private void StartHitTimer()
        {
            Debug.WriteLine("StartHitTimer");
            if (_waitingForCloseOrders.Count > 0)
            {
                Debug.WriteLine(string.Format("StartHitTimer, waitingForCloseOrders.Count = {0}", _waitingForCloseOrders.Count));
                BO.Order order = _waitingForCloseOrders.Min;
                TimeSpan dueTime = order.NextHitTime - Market.MarketManager.Now;
                if (dueTime < TimeSpan.Zero) dueTime = TimeSpan.Zero;
                this.ChangeTimer(dueTime);
            }
        }

        private void Hit(BO.Order order, DateTime hitTime)
        {
            try
            {
                Debug.WriteLine("Hit Order");
                if (order.ShouldClose)
                {
                    this.CloseOrder(order);
                    Debug.WriteLine("should close");
                }
                else
                {
                    order.Hit(hitTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                if (!order.IsClosed)
                {
                    Debug.WriteLine("order is not closed");
                    lock (_mutex)
                    {
                        order.CalculateNextHitTime();
                        _waitingForCloseOrders.Add(order);
                        this.StartHitTimer();
                    }
                }
            }
        }

        internal void CloseOrder(BO.Order order)
        {
            var account = order.Owner.Owner;
            var addCommand = AddBOTransactionCommandFactory.Default.CreateByClose(account, order);
            addCommand.Execute();
            var closeTran = addCommand.Result;
            iExchangeEngine.Default.Execute(new OrderExecuteEventArgs(ExecuteContext.CreateExecuteDirectly(account.Id, closeTran.Id, ExecuteStatus.Filled)));
        }

        private void ChangeTimer(TimeSpan dueTime)
        {
            _hitTimer.Change((int)dueTime.TotalMilliseconds, Timeout.Infinite);
        }
    }

}
