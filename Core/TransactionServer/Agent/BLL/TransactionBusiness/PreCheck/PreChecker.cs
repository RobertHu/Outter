using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using Core.TransactionServer.Agent.AccountClass.InstrumentUtil;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck
{
    internal class PreChecker
    {
        internal static PreChecker Default = new PreChecker();
        private PreChecker()
        {
        }

        public bool HasEnoughMoneyToPlace(Transaction tran)
        {
            if (tran.IsFreeOfPlaceMarginCheck()) return true;
            Account account = tran.Owner;
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


    }
}
