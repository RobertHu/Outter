using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.CommandFactorys;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Engine;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands;
using log4net;

namespace Core.TransactionServer.Agent.Physical
{
    internal sealed class PhysicalShortSellOrderCloser
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PhysicalShortSellOrderCloser));
        internal static readonly PhysicalShortSellOrderCloser Default = new PhysicalShortSellOrderCloser();
        private PhysicalShortSellOrderCloser() { }
        static PhysicalShortSellOrderCloser() { }

        internal void ExecuteInstallmentOrderWithShortSellOrder(ExecuteContext context)
        {
            Logger.InfoFormat("ExecuteInstallmentOrderWithShortSellOrder tranId = {0}, accountId = {1}", context.TranId, context.AccountId);
            var tran = context.Tran;
            var account = tran.Owner;
            Transaction newTran = null, newInstalmentTran = null;
            var executedOrderId = context.ExecuteOrderId != null ? context.ExecuteOrderId.Value : tran.FirstOrder.Id;
            this.SplitInstalmentOrderByShortSell(tran, executedOrderId, context, out newTran, out newInstalmentTran);
            Debug.Assert(account.GetTran(newTran.Id) != null);
            account.SaveAndBroadcastChanges();
            this.Execute(newTran, true, context);
            account.SaveAndBroadcastChanges();
            if (newInstalmentTran != null)
            {
                this.Execute(newInstalmentTran, false, context);
            }
            account.SaveAndBroadcastChanges();
            InteractFacade.Default.TradingEngine.Cancel(tran, CancelReason.SplittedForHasShortSell);
        }

        private void Execute(Transaction tran, bool shouldCancelExecute, ExecuteContext oldContext)
        {
            try
            {
                ExecuteContext context = new ExecuteContext(tran.AccountId, tran.Id, ExecuteStatus.Filled, tran.CreateOrderPriceInfo())
                {
                    BookInfo = oldContext.BookInfo,
                    IsFreeValidation = oldContext.IsFreeValidation
                };

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
            catch (TransactionServerException tse)
            {
                tran.Owner.RejectChanges();
                tran.Cancel(tse.ErrorCode.ToCancelReason());
                throw;
            }
            catch
            {
                tran.Owner.RejectChanges();
                tran.Cancel(TransactionError.RuntimeError.ToCancelReason());
                throw;
            }
        }


        internal bool ShouldExecuteInstallmentOrderWithShortSellOrder(Transaction tran)
        {
            Debug.Assert(tran.IsPhysical);
            var physicalTran = (PhysicalTransaction)tran;
            var account = tran.Owner;
            return tran.IsPhysical && physicalTran.ExistsInstalmentOrder() && account.HasFilledShortSellOrders(tran.InstrumentId);
        }

        private void SplitInstalmentOrderByShortSell(Transaction oldInstalmentTran, Guid executedOrderId, ExecuteContext context, out Transaction newTran, out Transaction newInstalmentTran)
        {
            newTran = newInstalmentTran = null;
            var account = oldInstalmentTran.Owner;
            var instrumentId = oldInstalmentTran.InstrumentId;
            var oldInstalmentOrder = account.GetOrder(executedOrderId);
            Debug.Assert(oldInstalmentOrder.IsPhysical);
            var shouldBeClosedLot = this.CalculateShouldBeClosedLot(oldInstalmentTran.Owner, oldInstalmentTran.InstrumentId, oldInstalmentOrder.Lot);
            var newtranOrderPair = this.CreateNewTranToClose(oldInstalmentTran, (PhysicalOrder)oldInstalmentOrder, shouldBeClosedLot, context);
            this.CloseShortSellOrder(shouldBeClosedLot, account, instrumentId, newtranOrderPair.Order);
            var remainLot = oldInstalmentOrder.Lot - shouldBeClosedLot;
            if (remainLot > 0)
            {
                newInstalmentTran = this.CreateNewInstalmentTran(remainLot, oldInstalmentTran, (PhysicalOrder)oldInstalmentOrder, context);
            }
            newTran = newtranOrderPair.Tran;
        }

        private Transaction CreateNewInstalmentTran(decimal remainLot, Transaction oldTran, PhysicalOrder oldOrder, ExecuteContext context)
        {
            var command = AddPhysicalTransactionCommandFactory.Default.CreateInstalmentTransaction(oldTran.Owner, (PhysicalTransaction)oldTran, oldOrder.Id, oldOrder, true, true, remainLot);
            var instalmentCommand = (AddPhysicalInstalmentTransactionCommand)command;
            instalmentCommand.BaseTime = context.ExecuteTime;
            command.Execute();
            return command.Result;
        }

        private void CloseShortSellOrder(decimal shouldBeClosedLot, Account account, Guid instrumentId, Order closeOrder)
        {
            decimal remainLot = shouldBeClosedLot;
            foreach (var order in account.GetFilledShortSellOrders(instrumentId))
            {
                var canBeClosedLot = Math.Min(order.LotBalance, remainLot);
                this.CreateOrderRelation(closeOrder, order, canBeClosedLot);
                remainLot -= canBeClosedLot;
                if (remainLot == 0) break;
            }
        }

        private void CreateOrderRelation(Order closeOrder, Order openOrder, decimal closedLot)
        {
            var factory = OrderRelationFacade.Default.GetAddOrderRelationFactory(closeOrder);
            var command = factory.Create(openOrder, closeOrder, closedLot);
            command.Execute();
        }

        private TranOrderPair CreateNewTranToClose(Transaction oldTran, PhysicalOrder oldOrder, decimal shouldCloseLot, ExecuteContext context)
        {
            var command = AddPhysicalTransactionCommandFactory.Default.CreateInstalmentTransaction(oldTran.Owner, (PhysicalTransaction)oldTran, oldOrder.Id, oldOrder, true, false, shouldCloseLot);
            var instalmentCommand = (AddPhysicalInstalmentTransactionCommand)command;
            instalmentCommand.BaseTime = context.ExecuteTime;
            command.Execute();
            var newCloseTran = command.Result;
            var newCloseOrder = newCloseTran.FirstOrder;
            return new TranOrderPair(newCloseTran, newCloseOrder);
        }


        private decimal CalculateShouldBeClosedLot(Account account, Guid instrumentId, decimal canBeClosedLot)
        {
            var needToBeClosedLot = this.CalculateNeedToBeClosedLot(account, instrumentId);
            var shouldBeClosedLot = Math.Min(needToBeClosedLot, canBeClosedLot);
            return shouldBeClosedLot;
        }

        private decimal CalculateNeedToBeClosedLot(Account account, Guid instrumentId)
        {
            decimal result = 0;
            foreach (var order in account.GetFilledShortSellOrders(instrumentId))
            {
                result += order.LotBalance;
            }
            return result;
        }

        private struct TranOrderPair
        {
            private readonly Transaction _tran;
            private readonly Order _order;
            internal TranOrderPair(Transaction tran, Order order)
            {
                _tran = tran;
                _order = order;
            }
            internal Transaction Tran { get { return _tran; } }
            internal Order Order { get { return _order; } }
        }
    }
}
