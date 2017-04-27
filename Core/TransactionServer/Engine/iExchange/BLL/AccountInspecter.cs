using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Market;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange.BLL
{
    //public sealed  class AccountInspecter
    //{
    //    private static readonly int IntervalMilliSeconds = 1000;
    //    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(IntervalMilliSeconds);
    //    private Action<Account> _inspectAction;
    //    private Timer _timer;
    //    private TradingEngine _tradingEngine;
    //    public AccountInspecter(TradingEngine tradingEngine)
    //    {
    //        _tradingEngine = tradingEngine;
    //    }

    //    public void Start()
    //    {
    //        _timer = new Timer(this.Inspect, null, IntervalMilliSeconds, Timeout.Infinite);
    //        _inspectAction = InspectHandle;
    //    }

    //    private void Inspect(object state)
    //    {
    //        Parallel.ForEach(TradingSetting.Default.Accounts.Values, _inspectAction);
    //        _timer.Change(IntervalMilliSeconds, Timeout.Infinite);
    //    }

    //    private void InspectHandle(Account account)
    //    {
    //        DateTime now = MarketManager.Now;
    //        foreach (Transaction transaction in account.Transactions)
    //        {
    //            if (transaction.Phase == TransactionPhase.Placed)
    //            {
    //                if (transaction.EndTime >= now)
    //                {
    //                    _tradingEngine.Cancel(transaction);
    //                }
    //                else
    //                {
    //                    foreach (Order order in transaction.Orders)
    //                    {
    //                        if (order.ShouldSportOrderDelayFill && order.SubtractDelayFillTime(Interval) <= TimeSpan.Zero)
    //                        {
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //}

}
