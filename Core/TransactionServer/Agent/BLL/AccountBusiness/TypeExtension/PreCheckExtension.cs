using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.AccountClass.InstrumentUtil;
using iExchange.Common;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtension
{
    internal static class PreCheckExtension
    {
        internal static bool HasEnoughMoneyToPlace(this Account account, Transaction tran)
        {
            if (tran.IsFreeOfPlaceMarginCheck()) return true;
            AccountClass.Instrument instrument = account.GetOrCreateInstrument(tran.InstrumentId);
            decimal placeCheckNecessary = account.CalculatePreCheckNecessary(tran);
            decimal placeCheckBalance = account.CalculatePreCheckBalance();
            decimal credit = account.CalculateCredit(instrument);
            decimal unclearBalance = account.CaculateUnclearBalance();
            TradePolicy tradePolicy = account.Setting.TradePolicy;
            MarginCheckOption marginCheckOption = tradePolicy.OpenNecessaryPolicy.MarginCheckOption;
            bool isMarginEnough = true;
            if (marginCheckOption == MarginCheckOption.Balance || marginCheckOption == MarginCheckOption.All)
            {
                decimal necessary = account.SumFund.Balance - unclearBalance - placeCheckBalance + account.Setting.ShortMargin + credit +
                    account.SumFund.InterestNotValued + account.SumFund.StorageNotValued + account.SumFund.TradePLNotValued;
                isMarginEnough &= placeCheckNecessary <= necessary;
            }

            if (marginCheckOption == MarginCheckOption.Equity || marginCheckOption == MarginCheckOption.All)
            {
                decimal equity = account.SumFund.Equity - unclearBalance - placeCheckBalance + account.Setting.ShortMargin + credit;
                isMarginEnough &= placeCheckNecessary <= equity;
            }
            return isMarginEnough;
        }

        private static decimal CalculatePreCheckNecessary(this Account account, Transaction tran)
        {
            decimal result = 0m;
            foreach (var eachInstrument in account.Instruments)
            {
                var necessary = eachInstrument.CalculatePreCheckNecessary(tran);
                result += account.IsMultiCurrency ? eachInstrument.CurrencyRate.Exchange(necessary) : necessary;
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
                        result += eachTran.CurrencyRate.Exchange(balance);
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
