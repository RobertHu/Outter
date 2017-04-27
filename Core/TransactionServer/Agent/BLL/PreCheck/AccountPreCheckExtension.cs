using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal static class AccountPreCheckExtension
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountPreCheckExtension));

        internal static bool HasEnoughMoneyToPlace(this Account account, Transaction tran)
        {
            if (tran.IsFreeOfPlaceMarginCheck()) return true;
            AccountClass.Instrument instrument = account.GetOrCreateInstrument(tran.InstrumentId);
            decimal preCheckNecessaryForBalanceCheckOption = 0;
            decimal placeCheckNecessary = account.CalculatePreCheckNecessary(tran, out preCheckNecessaryForBalanceCheckOption);
            decimal placeCheckBalance = account.CalculatePreCheckBalance();
            decimal credit = account.CalculateCredit(instrument);
            decimal unclearBalance = account.CaculateUnclearBalance();
            TradePolicy tradePolicy = account.Setting().TradePolicy(null);

            MarginCheckOption marginCheckOption = tradePolicy.OpenNecessaryPolicy.MarginCheckOption;
            bool isMarginEnough = true;

            decimal balanceRemainAmount = account.Balance - unclearBalance - placeCheckBalance + account.Setting().ShortMargin + credit;
            Logger.InfoFormat("precheck placeCheckNecessary = {0}, placeCheckBalance  = {1}, credit  = {2}, unclearBalance = {3}, balanceRemainAmount = {4}, ShortMargin = {5} , accountId = {6}, marginCheckOption = {7}, tranId = {8}, isPhysical = {9}, equity = {10}", placeCheckNecessary,
                placeCheckBalance, credit, unclearBalance, balanceRemainAmount, account.Setting().ShortMargin, account.Id, marginCheckOption, tran.Id, tran.IsPhysical, account.Equity);

            if (marginCheckOption == MarginCheckOption.Balance || marginCheckOption == MarginCheckOption.All)
            {
                if (tran.IsPhysical)
                {
                    isMarginEnough &= balanceRemainAmount >= 0;
                }
                else
                {
                    isMarginEnough &= preCheckNecessaryForBalanceCheckOption <= balanceRemainAmount;
                }
            }

            if (marginCheckOption == MarginCheckOption.Equity || marginCheckOption == MarginCheckOption.All)
            {
                decimal equity = account.Equity - unclearBalance - placeCheckBalance + account.Setting().ShortMargin + credit;
                isMarginEnough &= placeCheckNecessary <= equity;
            }
            return isMarginEnough;
        }

        private static decimal CalculatePreCheckNecessary(this Account account, Transaction tran, out decimal preCheckNecessaryForBalanceCheckOption)
        {
            decimal result = 0m;
            preCheckNecessaryForBalanceCheckOption = 0m;
            foreach (var eachInstrument in account.Instruments)
            {
                var necessary = eachInstrument.CalculatePreCheckNecessary(tran);
                var exchangedNecessary = account.IsMultiCurrency ? eachInstrument.CurrencyRate().Exchange(necessary) : necessary;
                result += exchangedNecessary;
                if (!eachInstrument.IsPhysical)
                {
                    preCheckNecessaryForBalanceCheckOption += exchangedNecessary;
                }
            }
            return result;
        }


        private static decimal CalculatePreCheckBalance(this Account account)
        {
            decimal result = 0m;
            foreach (var eachTran in account.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    var balance = eachOrder.CalculatePreCheckBalance();
                    if (account.IsMultiCurrency)
                    {
                        result += eachTran.CurrencyRate(null).Exchange(balance);
                    }
                    else
                    {
                        result += balance;
                    }
                }
            }
            return result;
        }
    }
}
