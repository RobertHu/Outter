using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using iExchange.Common;
using log4net;
using Protocal;
using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class PhysicalPayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PhysicalPayer));

        internal static TransactionError PrePayForInstalment(this Account account, Guid submitorId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            Logger.InfoFormat("PrePayForInstalment account id = {0}", account.Id);
            try
            {
                account.Verify(terminateData.OrderId, terminateData.IsPayOff, sumSourcePaymentAmount, sumSourceTerminateFee, currencyId);
            }
            catch (TransactionServerException ex)
            {
                Logger.Error(ex);
                return ex.ErrorCode;
            }
            PrePayer.Payoff(account, currencyId, submitorId, terminateData);
            account.SaveAndBroadcastChanges();
            return TransactionError.OK;
        }

        internal static TransactionError InstalmentPayoff(this Account account, Guid submitorId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<InstalmentData> instalments)
        {
            Logger.InfoFormat("instalments count = {0}", instalments.Count);
            try
            {
                account.Verify(instalments[0].OrderID, instalments[0].IsPayOff, sumSourcePaymentAmount, sumSourceTerminateFee, currencyId);
            }
            catch (TransactionServerException ex)
            {
                Logger.Error(ex);
                return ex.ErrorCode;
            }
            InstalmentPayer.Payoff(account, submitorId, instalments);
            account.SaveAndBroadcastChanges();
            return TransactionError.OK;
        }

        private static void Verify(this Account account, Guid orderId, bool isPayOff, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Guid currencyId)
        {
            PhysicalOrder order = (PhysicalOrder)account.GetOrder(orderId);
            if ((isPayOff && !order.CanPrepayment()) || order.IsPayoff)
            {
                throw new TransactionServerException(TransactionError.PrepaymentIsNotAllowed);
            }

            if (isPayOff)
            {
                var lastEquity = account.Equity;
                order.PayOff(null);
                string errorDetail;
                if (!account.HasEnoughMoneyToPayOff(order, sumSourcePaymentAmount + sumSourceTerminateFee, currencyId, lastEquity, out errorDetail))
                {
                    account.RejectChanges();
                    throw new TransactionServerException(TransactionError.BalanceOrEquityIsShort, errorDetail);
                }
            }
            else if (!account.HasEnoughMoneyToPayArrears(sumSourcePaymentAmount + sumSourceTerminateFee, currencyId))
            {
                throw new TransactionServerException(TransactionError.BalanceOrEquityIsShort);
            }
        }


    }
}
