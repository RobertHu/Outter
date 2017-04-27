using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderUpdater
    {
        internal static readonly OrderUpdater Default = new OrderUpdater();

        static OrderUpdater() { }
        private OrderUpdater() { }

        internal void UpdateInterestPerLotAndStoragePerLot(Account account, DateTime tradeDay)
        {
            foreach (var eachTran in account.Transactions)
            {
                this.UpdateOrderInterestPerLotAndStoragePerLot(eachTran, tradeDay);
            }
        }

        internal void UpdateOrderInterestPerLotAndStoragePerLot(Transaction tran, DateTime tradeDay)
        {
            foreach (var eachOrder in tran.Orders)
            {
                if (eachOrder.Phase != iExchange.Common.OrderPhase.Canceled)
                {
                    var orderDayHistory = ResetManager.Default.GetOrderDayHistory(eachOrder.Id, tradeDay);
                    if (orderDayHistory == null) continue;
                    eachOrder.InterestPerLot = orderDayHistory.InterestPerLot;
                    eachOrder.StoragePerLot = orderDayHistory.StoragePerLot;
                }
            }
        }


    }
}
