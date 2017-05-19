using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.BLL.OrderBusiness;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Services
{
    internal sealed class TransactionExecuteService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionExecuteService));

        internal static readonly TransactionExecuteService Default = new TransactionExecuteService();

        static TransactionExecuteService() { }
        private TransactionExecuteService() { }

        public void Execute(Transaction tran, ExecuteContext context)
        {
            if (tran.IsExpired(context.ExecuteTime, context.TradeDay))
            {
                var msg = string.Format("Execute: TranId {0}, ProcessBaseTime {1}, endTime {2}", tran.Id, context.ExecuteTime ?? Market.MarketManager.Now, tran.EndTime);
                Logger.Warn(msg);
                throw new TransactionServerException(TransactionError.TransactionExpired);
            }
            var oldLots = tran.AccountInstrument.GetBuySellLotBalance();
            tran.FillService.Fill(context);
            this.ExecuteOrdersAndVerify(tran, context, oldLots);
            if (context.IsFreeValidation) return;
            TransactionVerifier.VerifyForExecuting(tran, false, AppType.TradingConsole, PlaceContext.Empty);
        }

        private void ExecuteOrdersAndVerify(Transaction tran, ExecuteContext context, BuySellLot oldLots)
        {
            decimal lastEquity = tran.Owner.Equity;
            decimal deltaBalance = tran.ExecuteOrders(context, order =>
             {
                 if (order.Phase != OrderPhase.Executed)
                 {
                     Logger.WarnFormat("ExecuteOrders orderId = {0} phase = {1} not equal executed", order.Id, order.Phase);
                     return false;
                 }
                 return true;
             });

            tran.Owner.InvalidateInstrumentCache(tran);
            this.VerifyAlertLevelAndNecessary(tran, oldLots, context,lastEquity);
            if (context.IsBook)
            {
                tran.Owner.AddBalance(tran.CurrencyId, -deltaBalance, context.ExecuteTime);
            }
        }

        private void VerifyAlertLevelAndNecessary(Transaction tran, BuySellLot lots, ExecuteContext context, decimal lastEquity)
        {
            var account = tran.Owner;
            this.CalculateOrderFloatPLForcely(tran);
            account.CalculateRiskData(tran.SubmitorQuotePolicyProvider);
            context.ExecutedInfo = new ExecutedInfo(account.Balance, account.Necessary, lastEquity);
            string errorDetail;
            if (context.CheckMargin != null && context.CheckMargin.Value && !tran.ExecuteNecessaryCheckService.IsMarginEnoughToExecute(tran, lots, lastEquity, out errorDetail))
            {
                throw new TransactionServerException(TransactionError.MarginIsNotEnough, errorDetail);
            }

            if (account.IsInAlerting(tran.AccountInstrument, lots))
            {
                throw new TransactionServerException(TransactionError.AccountIsInAlerting);
            }
            if (tran.IsFreeOfNecessaryCheck) return;
            if (!account.IsNecessaryWithinThreshold)
            {
                throw new TransactionServerException(TransactionError.NecessaryIsNotWithinThreshold);
            }
            if (!tran.ExecuteNecessaryCheckService.IsMarginEnoughToExecute(tran, lots, lastEquity, out errorDetail))
            {
                throw new TransactionServerException(TransactionError.MarginIsNotEnough, errorDetail);
            }
        }

        private void CalculateOrderFloatPLForcely(Transaction tran)
        {
            foreach (var eachOrder in tran.Orders)
            {
                if (eachOrder.Phase == OrderPhase.Executed)
                {
                    eachOrder.CalculateFloatPLForcely(tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider));
                }
            }
        }


        private void FixMaxMovePercentForOrderPrice(Transaction tran, Price buyPrice, Price sellPrice)
        {
            if (tran.IsFreeOfPriceCheck(true)) return;
            var account = tran.Owner;
            var instrument = tran.AccountInstrument;
            var quotation = instrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
            Price buy = quotation.BuyPrice;
            Price sell = quotation.SellPrice;
            double maxMovePercent = 0.05;
            string bugFix_OrderPriceMaxMovePercent = ExternalSettings.Default.BugFix_OrderPriceMaxMovePercent;
            if (bugFix_OrderPriceMaxMovePercent != null)
            {
                double.TryParse(bugFix_OrderPriceMaxMovePercent, System.Globalization.NumberStyles.Float, null, out maxMovePercent);
            }

            if (buyPrice != null)
            {
                if (Math.Abs((double)buyPrice - (double)sell) / (double)sell > maxMovePercent)
                {
                    // AppDebug.LogEvent("TransactionServer", string.Format("BugFix_OrderPriceMaxMovePercent.BuyPrice: TranId {0}, marketPrice {1}, executePrice {2}", this.id, sell, buyPrice), EventLogEntryType.Warning);
                    throw new TransactionServerException(TransactionError.InvalidPrice);
                }
            }

            if (sellPrice != null)
            {
                if (Math.Abs((double)sellPrice - (double)buy) / (double)buy > maxMovePercent)
                {
                    //AppDebug.LogEvent("TransactionServer", string.Format("BugFix_OrderPriceMaxMovePercent.SellPrice: TranId {0}, marketPrice {1}, executePrice {2}", this.id, buy, sellPrice), EventLogEntryType.Warning);
                    throw new TransactionServerException(TransactionError.InvalidPrice);
                }
            }
        }
    }
}
