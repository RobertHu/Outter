using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Market;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange
{
    internal sealed class CancelService
    {
        private IInnerTradingEngine _tradingEngine;
        internal CancelService(IInnerTradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
        }

        internal TransactionError Cancel(Transaction tran)
        {
            TransactionError error;
            if (!this.CanBeCanceled(tran, out error))
            {
                _tradingEngine.NotifyCancelRejected(new CancelRejectedEventArgs(tran, error));
            }
            else
            {
                if (tran.SubType == TransactionSubType.IfDone)
                {
                    tran.CancelService.CancelDoneTransactions(_tradingEngine);
                }
                _tradingEngine.NotifyCanceled(new CancelEventArgs(tran));
            }
            return error;
        }

        private bool CanBeCanceled(Transaction tran, out TransactionError error)
        {
            error = this.CanBeCanceled(tran);
            return error == TransactionError.OK;
        }

        private TransactionError CanBeCanceled(Transaction tran)
        {
            if (tran == null) return TransactionError.TransactionNotExists;
            if (!tran.CancelService.CanCancel) return TransactionError.TransactionCannotBeCanceled;
            var baseTime = MarketManager.Now;
            if (!tran.PlacedByRiskMonitor)
            {
                if (!tran.SettingInstrument.CanPlace(baseTime, OrderTypeHelper.IsPendingType(tran.OrderType))) return TransactionError.PriceIsDisabled;
                if (!tran.CancelService.CanBeCanceledByCustomer()) return TransactionError.TransactionCannotBeCanceled;
                if (!tran.CancelService.ShouldAutoCancel()) return TransactionError.Action_NeedDealerConfirmCanceling;
            }
            return TransactionError.OK;
        }

    }
}
