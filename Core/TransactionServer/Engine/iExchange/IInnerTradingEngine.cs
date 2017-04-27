using Core.TransactionServer.Agent;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange
{
    internal interface IInnerTradingEngine
    {
        void AcceptPlace(Guid tranId);
        void Execute(ExecuteEventArgs e);
        void CancelExecute(CancelExecuteEventArgs e);
        void NotifyCanceled(CancelEventArgs e);
        void NotifyCancelRejected(CancelRejectedEventArgs e);
    }
}
