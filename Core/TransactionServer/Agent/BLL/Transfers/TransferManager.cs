using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.Transfers
{
    internal static class TransferManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransferManager));

        internal static TransactionError AcceptTransfer(Guid userId, Guid transferID, TransferAction action, out Guid accountId, out Guid currencyId, out decimal amount)
        {
            amount = 0m;
            accountId = Guid.Empty;
            currencyId = Guid.Empty;
            try
            {
                if (!TransferHelper.AcceptOrDeclineTransfer(ExternalSettings.Default.DBConnectionString, userId, transferID, action, out amount, out currencyId, out accountId))
                {
                    return TransactionError.DbOperationFailed;
                }
                return TransactionError.OK;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return TransactionError.RuntimeError;
            }
        }



        internal static TransactionError ApplyTransfer(Guid userID, Account sourceAccount, Guid sourceCurrencyID,
           decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
           decimal rate, DateTime expireDate)
        {
            TransactionError result = TransactionError.OK;
            try
            {
                result = sourceAccount.HasEnoughMoneyToTransfer(ExternalSettings.Default.DBConnectionString, sourceCurrencyID, sourceAmount);
                if (result != TransactionError.OK)
                {
                    return result;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                TransactionServerException transactionException = exception as TransactionServerException;
                return (transactionException == null ? TransactionError.RuntimeError : transactionException.ErrorCode);
            }
            finally
            {
            }

            Guid transferId;
            if (!TransferHelper.ApplyTransferToDB(ExternalSettings.Default.DBConnectionString, userID, sourceAccount.Id, sourceCurrencyID, sourceAmount,
                targetAccountID, targetCurrencyID, targetAmount, rate, expireDate, out transferId))
            {
                Logger.Error("Save apply to DB failed");
                result = TransactionError.DbOperationFailed;
            }

            if (result == TransactionError.OK)
            {
                sourceAccount.AddDeposit(sourceCurrencyID, DateTime.Now, sourceAmount, true);
                Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateUpdateBalanceCommand(sourceAccount.Id, sourceCurrencyID, sourceAmount, Protocal.ModifyType.Add));
                Guid remitterId = sourceAccount.Customer.Id;
                Guid payeeId = TradingSetting.Default.GetAccount(targetAccountID).Customer.Id;
                Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateTradingTransferCommand(transferId, remitterId, payeeId, TransferAction.Apply));
            }

            return result;
        }


        internal static TransactionError HasEnoughMoneyToTransfer(this Account account, string connectionString, Guid currencyId, decimal amount)
        {
            decimal notClearAmount = 0;
            var fund = account.GetFund(currencyId);
            if (!TransferHelper.GetVisaAmount2(connectionString, account.Id, currencyId, out notClearAmount))
            {
                return TransactionError.DbOperationFailed;
            }

            var systemParameter = Settings.Setting.Default.SystemParameter;

            if ((fund.Equity - fund.Necessary - notClearAmount) + amount < 0
                || (!systemParameter.BalanceDeficitAllowPay && (fund.Balance - (fund.Necessary - fund.PartialPaymentPhysicalNecessary) - notClearAmount) + amount < 0))
            {
                return TransactionError.MarginIsNotEnough;
            }

            if (account.IsMultiCurrency)
            {
                Guid accountCurrencyId = account.Setting().CurrencyId;
                if (!TransferHelper.GetVisaAmount2(connectionString, account.Id, accountCurrencyId, out notClearAmount))
                {
                    return TransactionError.DbOperationFailed;
                }

                CurrencyRate currencyRate = Settings.Setting.Default.GetCurrencyRate(currencyId, accountCurrencyId);
                amount = currencyRate.Exchange(amount);

                if ((account.Equity - account.Necessary - notClearAmount) + amount < 0
                    || (!systemParameter.BalanceDeficitAllowPay && (account.Balance - (account.Necessary - account.SumFund.PartialPaymentPhysicalNecessary) - notClearAmount) + amount < 0))
                {
                    return TransactionError.MarginIsNotEnough;
                }
            }

            return TransactionError.OK;
        }

    }
}
