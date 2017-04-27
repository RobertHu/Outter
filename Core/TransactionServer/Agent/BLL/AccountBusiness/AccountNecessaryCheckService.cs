using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class AccountNecessaryCheckService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountNecessaryCheckService));
        private Account _owner;
        private Fund _fund;
        internal AccountNecessaryCheckService(Account owner, Fund fund)
        {
            _owner = owner;
            _fund = fund;
        }

        internal bool IsNecessaryWithinThreshold
        {
            get { return _owner.Setting().MaxNecessary == 0 || _owner.SumFund.Necessary <= _owner.Setting().MaxNecessary; }
        }


        internal bool HasEnoughMoneyToFill(AccountClass.Instrument instrument, bool existsCloseOrder, decimal fee, bool isNecessaryFreeOrder, decimal lastEquity, bool isForPayoff, out string errorInfo)
        {
            decimal riskCredit = this.CalculateCredit(instrument.Id);
            decimal unclearBalance = _owner.UnclearDepositManager.Sum();
            TradePolicy tradePolicy = _owner.Setting().TradePolicy(null);
            MarginCheckOption marginCheckOption = this.GetMarginCheckOption(existsCloseOrder, tradePolicy);
            decimal fillCheckNecessary = existsCloseOrder ? _fund.RiskRawData.NecessaryFillingCloseOrder : _fund.RiskRawData.NecessaryFillingOpenOrder;
            errorInfo = string.Empty;
            bool isBalanceEnough = this.CheckBalanceIsEnough(instrument.IsPhysical, marginCheckOption, unclearBalance, fillCheckNecessary, out errorInfo);
            bool isEquityEnough = this.CheckEquityIsEnough(instrument.IsPhysical, marginCheckOption, unclearBalance, fillCheckNecessary, fee, isForPayoff, riskCredit, isNecessaryFreeOrder, lastEquity, out errorInfo);
            Logger.Warn(this.BuildLoggerInfo(existsCloseOrder, riskCredit, unclearBalance, marginCheckOption, instrument.Id, instrument.Owner));
            return isBalanceEnough && isEquityEnough;
        }

        private string BuildLoggerInfo(bool existsCloseOrder, decimal riskCredit, decimal unclearBalance, MarginCheckOption marginCheckOption, Guid instrumentId, Account account)
        {
            var fund = account.SumFund;
            StringBuilder sb = Protocal.StringBuilderCache.Acquire(200);
            sb.Append("HasEnoughMoneyToFill NecessaryFillingCloseOrder =");
            sb.Append(fund.RiskRawData.NecessaryFillingCloseOrder);
            sb.Append(", NecessaryFillingOpenOrder =");
            sb.Append(fund.RiskRawData.NecessaryFillingOpenOrder);
            sb.Append(", existsCloseOrder  =");
            sb.Append(existsCloseOrder);
            sb.Append(", riskCredit = ");
            sb.Append(riskCredit);
            sb.Append(", unclearBalance = ");
            sb.Append(unclearBalance);
            sb.Append(", marginCheckOption =");
            sb.Append(marginCheckOption);
            sb.Append(", shortMargin = ");
            sb.Append(account.Setting().ShortMargin);
            sb.Append(", equity = ");
            sb.Append(account.SumFund.Equity);
            sb.Append(", instrumentId = ");
            sb.Append(instrumentId);
            sb.Append(", accountId = ");
            sb.Append(account.Id);
            return Protocal.StringBuilderCache.GetStringAndRelease(sb);
        }



        private bool CheckBalanceIsEnough(bool isPhysical, MarginCheckOption marginCheckOption, decimal unclearBalance, decimal fillCheckNecessary, out string errorInfo)
        {
            errorInfo = string.Empty;
            if (marginCheckOption == MarginCheckOption.Balance || marginCheckOption == MarginCheckOption.All)
            {
                bool result = true;
                if (isPhysical)
                {
                    result = this.CheckBalanceIsEnoughForPhysicalOrder(unclearBalance);
                }
                else
                {
                    result = this.CheckBalanceIsEnoughForNormal(unclearBalance, fillCheckNecessary);
                }
                if (!result)
                {
                    errorInfo = this.BuildCheckBalanceFailedInfo(isPhysical, unclearBalance, fillCheckNecessary);
                }
                return result;
            }
            return true;
        }

        private string BuildCheckBalanceFailedInfo(bool isPhysical, decimal unclearBalance, decimal fillCheckNecessary)
        {
            return string.Format("CheckBalanceIsEnough failed, checkBalance = {0},  compareBalance = {1}", this.CalculateBalanceForCheck(unclearBalance), isPhysical ? unclearBalance : fillCheckNecessary);
        }


        private bool CheckEquityIsEnough(bool isPhysical, MarginCheckOption marginCheckOption, decimal unclearBalance, decimal fillCheckNecessary, decimal fee, bool isForPayoff, decimal riskCredit, bool isNecessaryFreeOrder, decimal lastEquity, out string errorInfo)
        {
            errorInfo = string.Empty;
            if (marginCheckOption == MarginCheckOption.Equity || marginCheckOption == MarginCheckOption.All)
            {
                decimal equity = isNecessaryFreeOrder ? _fund.Equity : lastEquity;
                if (isForPayoff)
                {
                    equity -= fee;
                }
                var equityForCheck = equity - unclearBalance + _owner.ShortMargin + riskCredit;
                bool result = equityForCheck >= fillCheckNecessary;
                if (!result)
                {
                    errorInfo = string.Format("CheckEquityIsEnough failed, equityForCheck = {0}, comparedEquity = {1}, equity = {2}, fee = {3}, lastEquity = {4}, isNecessaryFreeOrder = {5}", equityForCheck, fillCheckNecessary, equity, fee, lastEquity, isNecessaryFreeOrder);
                }
                return result;
            }
            return true;
        }

        private MarginCheckOption GetMarginCheckOption(bool existCloseOrder, TradePolicy tradePolicy)
        {
            if (existCloseOrder)
            {
                return tradePolicy.CloseNecessaryPolicy.MarginCheckOption;
            }
            else
            {
                return tradePolicy.OpenNecessaryPolicy.MarginCheckOption;
            }
        }


        private bool CheckBalanceIsEnoughForPhysicalOrder(decimal unclearBalance)
        {
            return this.CalculateBalanceForCheck(unclearBalance) >= 0;
        }

        private bool CheckBalanceIsEnoughForNormal(decimal unclearbalance, decimal fillCheckNecessary)
        {
            return this.CalculateBalanceForCheck(unclearbalance) >= fillCheckNecessary;
        }


        private decimal CalculateBalanceForCheck(decimal unclearBalance)
        {
            return _fund.Balance - unclearBalance + _owner.ShortMargin + _fund.RiskRawData.RiskCredit;
        }

        private decimal CalculateCredit(Guid instrumentId)
        {
            decimal credit = _owner.Setting().CreditLotD * _owner.Setting().TradePolicy()[instrumentId, null].MarginD;
            decimal creditAmount = _owner.Setting().CreditAmount;
            if (credit == 0)
            {
                return creditAmount;
            }
            else if (creditAmount == 0)
            {
                return credit;
            }
            else
            {
                return Math.Min(credit, creditAmount);
            }
        }

    }
}
