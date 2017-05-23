using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions;
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
using System.Xml;

namespace Core.TransactionServer.Agent.Interact
{
    public sealed class TransactionExecutor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionExecutor));

        internal static readonly TransactionExecutor Default = new TransactionExecutor();

        static TransactionExecutor() { }
        private TransactionExecutor() { }

        internal bool Execute(ExecuteContext context)
        {
            if (!this.CanExecute(context)) return false;
            this.VerifyIsHitReseted(context.Tran, context.ExecuteOrderId);
            this.DoExecute(context);
            return true;
        }

        private void VerifyIsHitReseted(Transaction tran, Guid? executeOrderId)
        {
            if (tran.OrderType == OrderType.Limit || tran.OrderType == OrderType.Market)
            {
                foreach (var eachOrder in tran.Orders)
                {
                    if ((eachOrder.Id == (executeOrderId ?? Guid.Empty)) && eachOrder.IsHitReseted)
                    {
                        throw new TransactionServerException(TransactionError.HitIsReseted, string.Format("orderId = {0}, accountId = {1}", eachOrder.Id, tran.AccountId));
                    }
                }
            }
        }


        private bool CanExecute(ExecuteContext context)
        {
            if (context.Tran == null)
            {
                Logger.WarnFormat("Execute faild , reason = {0}, accountId = {1}, tranId = {2} ", TransactionError.TransactionNotExists, context.AccountId, context.TranId);
                return false;
            }
            if (!context.Tran.CanExecute)
            {
                if (context.Tran.Phase == TransactionPhase.Executed)
                {
                    Logger.ErrorFormat("Execute passed, accountId = {0}, tranId = {1}, transaction has already executed", context.AccountId, context.TranId);
                    return false;
                }
                else
                {
                    Logger.ErrorFormat("Execute faild , accountId = {0}, tranId = {1}, reason = {2}", context.AccountId, context.TranId,TransactionError.TransactionCannotBeExecuted);
                    return false;
                }
            }
            return true;
        }

        public void DoExecute(ExecuteContext context)
        {
            try
            {
                this.CancelOCOOrder(context.ExecuteOrderId, context.Tran);
                if (this.ShouldCalculateCurrencyRate(context))
                {
                    BLL.CurrencyRateCaculator.Default.Caculate(context.Tran.CurrencyRate(context.TradeDay), context.AccountId);
                    Logger.InfoFormat("after calculate currencyrate accountId = {0}, tranId = {1}", context.AccountId, context.TranId);
                }
                this.ExecuteTransaction(context);
                if (IfDoneTransactionManager.Default.ShouldCancelDoneTransactions(context.Tran))//maybe have done orders needed to cancel
                {
                    Debug.Assert(context.ExecuteOrderId != null);
                    IfDoneTransactionManager.Default.CancelDoneTransactions(context.Tran, context.ExecuteOrderId.Value);
                }
                this.ProcessAfterExecuteSuccess(context.Tran, context.ExecuteOrderId);
            }
            catch (ShouldBeExecuteWithMaxOtherLotException ex)
            {
                context.Account.RejectChanges();
                context.Tran.Cancel(CancelReason.OtherReason);
                this.ExecuteExecuteWithMaxOtherLotTransaction(ex.ExceedMaxOtherLotOrder, ex.MaxOtherLot, context);
            }
        }

        private bool ShouldCalculateCurrencyRate(ExecuteContext context)
        {
            return !context.IsBook && context.Tran.OrderType != OrderType.Risk;
        }


        private void CancelOCOOrder(Guid? executeOrderId, Transaction tran)
        {
            foreach (var eachOrder in tran.Orders)
            {
                eachOrder.CancelOCOOrderIfExists(executeOrderId, tran.Type);
            }
        }


        private void ExecuteExecuteWithMaxOtherLotTransaction(Order originOrder, decimal lot, ExecuteContext originContext)
        {
            var tran = this.CreateExecuteWithMaxOtherLotTransaction(originOrder, lot);
            var originOrderInfo = originContext.OrderInfos[0];
            var contenxt = new ExecuteContext(originContext.AccountId, tran.Id, ExecuteStatus.Filled, new List<OrderPriceInfo> { new OrderPriceInfo(tran.FirstOrder.Id, originOrderInfo.BuyPrice, originOrderInfo.SellPrice) });
            this.ExecuteTransaction(contenxt);
        }


        private Transaction CreateExecuteWithMaxOtherLotTransaction(Order originOrder, decimal lot)
        {
            var tran = originOrder.Owner.Owner.CreateTransaction(originOrder, lot);
            var originTran = originOrder.Owner;
            if (originTran.SubType == TransactionSubType.IfDone && originOrder.IsOpen)
            {
                var originDoneTran = originTran.GetDoneTransaction(originOrder.Id);
                IfDoneTransactionParser.Default.FillDoneTran(originDoneTran, tran.FirstOrder.Id, originDoneTran.Orders.ToList());
            }
            return tran;
        }



        private void ExecuteTransaction(ExecuteContext context)
        {
            var tran = context.Tran;
            if (!context.IsBook && tran.IsPhysical && PhysicalShortSellOrderCloser.Default.ShouldExecuteInstallmentOrderWithShortSellOrder(tran))
            {
                PhysicalShortSellOrderCloser.Default.ExecuteInstallmentOrderWithShortSellOrder(context);
            }
            else
            {
                if (context.Tran.IsPhysical)
                {
                    string errorDetail = string.Empty;
                    if (context.CheckMaxPhysicalValue && PhysicalValueChecker.IsExceedMaxPhysicalValue((PhysicalTransaction)tran, context.TradeDay, out errorDetail))
                    {
                        throw new TransactionServerException(TransactionError.ExceedMaxPhysicalValue, errorDetail);
                    }
                }
                context.Tran.Execute(context);
            }
        }

        private void ProcessAfterExecuteSuccess(Transaction tran, Guid? executeOrderId)
        {
            if (tran.DoneCondition)
            {
                IfDoneTransactionManager.Default.ProcessAfterIfDoneTranExecuted(tran, executeOrderId);
            }
            this.CancelLimitCloseOrders(tran, executeOrderId);
        }

        private void CancelLimitCloseOrders(Transaction tran, Guid? executeOrderId)
        {
            Order executedOrder = executeOrderId == null ? tran.FirstOrder : tran.GetExecuteOrder(executeOrderId.Value);
            if (executedOrder.IsOpen) return;
            foreach (var eachOrderRelation in executedOrder.OrderRelations)
            {
                this.CancelOtherCloseOrders(eachOrderRelation.OpenOrder, eachOrderRelation.CloseOrder, eachOrderRelation.ClosedLot);
            }
        }

        private void CancelOtherCloseOrders(Order openOrder, Order originCloseOrder, decimal closedLot)
        {
            if (openOrder.LotBalance != 0) return;
            foreach (var eachOrderRelation in openOrder.OrderRelations)
            {
                var closeOrder = eachOrderRelation.CloseOrder;
                if (closeOrder != originCloseOrder && (closeOrder.Phase == OrderPhase.Placed || closeOrder.Phase == OrderPhase.Placing))
                {
                    closeOrder.Cancel(CancelReason.OtherReason);
                }
            }
        }

    }

    internal sealed class IfDoneTransactionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(IfDoneTransactionManager));

        internal static readonly IfDoneTransactionManager Default = new IfDoneTransactionManager();

        static IfDoneTransactionManager() { }
        private IfDoneTransactionManager() { }

        internal bool ShouldCancelDoneTransactions(Transaction tran)
        {
            return tran.Type == TransactionType.OneCancelOther && tran.FirstOrder.IsOpen;
        }

        internal void CancelDoneTransactions(Transaction tran, Guid executeOrderId)
        {
            foreach (var doneTran in tran.GetDoneTransactionsForOCO(executeOrderId))
            {
                InteractFacade.Default.TradingEngine.Cancel(doneTran, CancelReason.OtherReason);
            }
        }

        internal void CancelDoneTransIfExist(Transaction tran)
        {
            var doneTrans = tran.GetDoneTransactions();
            Debug.Assert(doneTrans != null);
            foreach (var eachDoneTran in doneTrans)
            {
                InteractFacade.Default.TradingEngine.Cancel(eachDoneTran, CancelReason.OtherReason);
            }
        }

        internal void ProcessAfterIfDoneTranExecuted(Transaction tran, Guid? executeOrderId)
        {
            List<Transaction> doneTrans = new List<Transaction>();
            if (executeOrderId == null)
            {
                var result = tran.GetDoneTransactions();
                if (result != null)
                {
                    doneTrans.AddRange(result);
                }
            }
            else
            {
                var result = tran.GetDoneTransaction(executeOrderId.Value);
                if (result != null)
                {
                    doneTrans.Add(result);
                }
            }
            foreach (var eachDoneTran in doneTrans)
            {
                this.ProcessDoneTran(tran, eachDoneTran, executeOrderId);
            }
        }

        private void ProcessDoneTran(Transaction tran, Transaction doneTran, Guid? executeOrderId)
        {
            if (this.IsDoneTransactionExist(doneTran))
            {
                this.ProcessWhenExistsDoneTran(tran, doneTran, executeOrderId);
            }
            else
            {
                var msg = string.Format("DoneTran has been canceled or amended \r\nIf={0}\r\nDone={1} ", tran, doneTran);
                Logger.Info(msg);
            }
        }


        private bool IsDoneTransactionExist(Transaction doneTran)
        {
            return doneTran != null && doneTran.Phase == TransactionPhase.Placing;
        }


        private void ProcessWhenExistsDoneTran(Transaction tran, Transaction doneTran, Guid? executeOrderId)
        {
            if (this.ShouldCancelDoneTransaction(tran))
            {
                InteractFacade.Default.TradingEngine.Cancel(doneTran, CancelReason.OtherReason);
            }
            else
            {
                bool isCanceledForInvalidPrice = false;
                var systemParameter = Settings.Setting.Default.SystemParameter;
                if (systemParameter.EvaluateIfDonePlacingOnStpConfirm)
                {
                    this.ProcessForDonePriceOnStpConfirm(tran, executeOrderId, doneTran, out isCanceledForInvalidPrice);
                }
                if (!isCanceledForInvalidPrice)
                {
                    doneTran.ChangePhaseToPlaced();
                    if (doneTran.OrderCount == 2)
                    {
                        doneTran.Type = TransactionType.OneCancelOther;
                        doneTran.SubType = TransactionSubType.None;
                    }
                }
            }
        }

        private bool ShouldCancelDoneTransaction(Transaction tran)
        {
            return tran.ExistsCloseOrder();
        }


        private void ProcessForDonePriceOnStpConfirm(Transaction tran, Guid? executeOrderId, Transaction doneTran, out bool isCanceledForInvalidPrice)
        {
            isCanceledForInvalidPrice = false;
            Order executedOrder = executeOrderId == null ? null : tran.GetExecuteOrder(executeOrderId.Value);
            if (this.ShouldCanceledForInvalidPrice(doneTran, executedOrder))
            {
                InteractFacade.Default.TradingEngine.Cancel(doneTran, CancelReason.InvalidPrice);
                isCanceledForInvalidPrice = true;
            }
        }

        private bool ShouldCanceledForInvalidPrice(Transaction doneTran, Order executedOrder)
        {
            Debug.Assert(executedOrder != null);
            return executedOrder.TradeOption == TradeOption.Stop
                 && executedOrder.ExecutePrice != executedOrder.SetPrice
                 && !doneTran.IsValidDonePrice(executedOrder.ExecutePrice);
        }
    }
}
