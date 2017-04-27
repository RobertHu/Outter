using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal abstract class TransactionExecuteNecessaryCheckServiceBase
    {
        internal bool IsMarginEnoughToExecute(Transaction tran, BuySellLot oldLots, decimal lastEquity ,out string errorDetail)
        {
            errorDetail = string.Empty;
            if (this.ShouldCheckMargin(tran, oldLots))
            {
                decimal fee = SumFee(tran);
                var account = tran.Owner;
                if (account.IsMultiCurrency)
                {
                    fee = tran.CurrencyRate(null).Exchange(fee, Settings.ExchangeDirection.RateOut);
                }
                if (!tran.Owner.HasEnoughMoneyToFill(tran.AccountInstrument, tran.ExistsCloseOrder(), fee, tran.FirstOrder.IsFreeOfNecessaryCheck,lastEquity, out errorDetail))
                {
                    return false;
                }
            }
            return true;
        }

        protected bool IsFreeOfMarginCheck(Transaction tran, BuySellLot oldLots)
        {
            bool existsCloseOrder = tran.ExistsCloseOrder();
            return tran.AccountInstrument.IsFreeOfFillMarginCheck(oldLots, existsCloseOrder);
        }


        private static decimal SumFee(Transaction tran)
        {
            decimal feeSum = 0;
            foreach (Order order in tran.Orders)
            {
                if (order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed)
                {
                    feeSum += order.SumFee();
                }
            }
            return feeSum;
        }

        protected abstract bool ShouldCheckMargin(Transaction tran, BuySellLot oldLots);
    }

    internal sealed class TransactionExecuteNecessaryCheckService : TransactionExecuteNecessaryCheckServiceBase
    {
        internal static readonly TransactionExecuteNecessaryCheckService Default = new TransactionExecuteNecessaryCheckService();

        static TransactionExecuteNecessaryCheckService() { }
        private TransactionExecuteNecessaryCheckService() { }

        protected override bool ShouldCheckMargin(Transaction tran, BuySellLot oldLots)
        {
            return !this.IsFreeOfMarginCheck(tran, oldLots);
        }
    }

    internal sealed class PhysicalTransactionExecuteNecessaryCheckService : TransactionExecuteNecessaryCheckServiceBase
    {
        internal static readonly PhysicalTransactionExecuteNecessaryCheckService Default = new PhysicalTransactionExecuteNecessaryCheckService();

        static PhysicalTransactionExecuteNecessaryCheckService() { }
        private PhysicalTransactionExecuteNecessaryCheckService() { }

        protected override bool ShouldCheckMargin(Transaction tran, BuySellLot oldLots)
        {
            bool needCheck = !this.IsFreeOfMarginCheck(tran, oldLots);
            foreach (PhysicalOrder eachOrder in tran.Orders)
            {
                needCheck |= eachOrder.IsOpen && eachOrder.PhysicalTradeSide == PhysicalTradeSide.Buy && eachOrder.IsPayoff;
            }
            return needCheck;
        }
    }

    internal sealed class BOTransactionExecuteNecessaryCheckService : TransactionExecuteNecessaryCheckServiceBase
    {
        internal static readonly BOTransactionExecuteNecessaryCheckService Default = new BOTransactionExecuteNecessaryCheckService();

        static BOTransactionExecuteNecessaryCheckService() { }
        private BOTransactionExecuteNecessaryCheckService() { }

        protected override bool ShouldCheckMargin(Transaction tran, BuySellLot oldLots)
        {
            bool needCheck = !this.IsFreeOfMarginCheck(tran, oldLots);
            foreach (Order order in tran.Orders)
            {
                needCheck |= order.IsOpen;
            }
            return needCheck;
        }
    }
}
