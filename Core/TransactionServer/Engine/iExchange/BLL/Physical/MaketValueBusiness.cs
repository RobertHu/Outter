using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Core.TransactionServer.Engine.iExchange.BLL.Physical
{
    internal sealed class MaketValueBusiness
    {
        internal static decimal GetAllExecuteMarketValue(Agent.Account account)
        {
            decimal result = 0m;
            foreach (var tran in account.Transactions)
            {
                if(!tran.IsPhysical) continue;
                foreach (PhysicalOrder order in tran.Orders)
                {
                    bool isExecuted = order.Phase == OrderPhase.Executed;
                    bool isDeposit = order.PhysicalTradeSide == PhysicalTradeSide.Deposit;
                    bool isBuy = order.PhysicalTradeSide == PhysicalTradeSide.Buy;
                    if (order.IsOpen && order.IsPhysical && isExecuted && (isDeposit || isBuy))
                    {
                        result += order.MarketValue;
                    }
                }
            }
            return result;
        }

        //private IEnumerable<OrderBase> GetAllPhysicalAndExecutedOrders()
        //{
        //}

    }
}
