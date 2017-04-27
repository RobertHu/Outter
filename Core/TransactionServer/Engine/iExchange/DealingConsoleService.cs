using Core.TransactionServer.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange
{
    public class DealingConsoleService
    {
        public void AcceptPlaced(Guid accountId, Guid tranId)
        {
            var account = TradingSetting.Default.GetAccount(accountId);
            var tran = account.GetTran(tranId);
        }
    }
}
