using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Engine;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Interact
{
    //internal sealed class CancelService
    //{
    //    private TradingEngine _tradingEngine;

    //    internal CancelService(TradingEngine tradingEngine)
    //    {
    //        _tradingEngine = tradingEngine;
    //    }

    //    internal void Cancel(Transaction tran)
    //    {
    //        if (tran.SubType == TransactionSubType.IfDone)
    //        {
    //            this.CancelDoneTrans(tran);
    //        }
    //        tran.Cancel();
    //    }

    //    private void CancelDoneTrans(Transaction tran)
    //    {
    //        var doneTrans = tran.GetDoneTransactions();
    //        foreach (var doneTran in doneTrans)
    //        {
    //            _tradingEngine.Cancel(tran);
    //        }
    //    }

    //}
}
