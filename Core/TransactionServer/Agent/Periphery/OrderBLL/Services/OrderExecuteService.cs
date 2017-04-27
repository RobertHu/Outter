using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.BLL.TypeExtensions;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Services
{
    public abstract class OrderExecuteServiceBase
    {
        protected Order _order;
        protected Account _account;
        protected OrderSettings _settings;

        protected OrderExecuteServiceBase(Order order, OrderSettings settings)
        {
            _order = order;
            _account = order.Owner.Owner;
            _settings = settings;
        }

        public void Execute(ExecuteContext context)
        {
            if (this.ShouldVerifyPendingConfirmLimitOrderLot())
            {
                this.VerifyPendingConfirmLimitOrderLot();
            }
            this.Calculate(context);
            this.UpdateInterestValueDate(context);
            if (_order.OrderType == OrderType.MultipleClose) //Special handle, prevent from changing report
            {
                _settings.Lot = 0;
            }
            AutoPriceCalculator.CalculateAutoPrice(_order);
            this.FillJudgePrice();
        }

        private void FillJudgePrice()
        {
            DateTime priceTimestamp;
            Price marketPrice = _order.GetMarketPrice(out priceTimestamp);
            _order.JudgePrice = marketPrice;
            _order.JudgePriceTimestamp = priceTimestamp;

        }

        private void Calculate(ExecuteContext context)
        {
            if (this.ShouldCalculateFee(context))
            {
                _order.CalculateFeeAsCost(context);
            }
            if (_order.IsOpen)
            {
                this.CalculateForOpenOrder(context);
                this.CalculateEstimateFee(context);
            }
            else
            {
                this.CalculateForCloseOrder(context);
            }
            this.CalculateBalance(context);
        }

        private void CalculateEstimateFee(ExecuteContext context)
        {
            var estimateFee = _order.CalculateEstimateFee(context);
            _order.UpdateEstimateFee(estimateFee);
            _order.Account.AddEstimateFee(_order.EstimateCloseCommission, _order.EstimateCloseLevy, _order.EstimateCurrencyRate(context.TradeDay));
        }


        protected void CalculateFee(ExecuteContext context)
        {
            if (this.ShouldCalculateFee(context))
            {
                _order.CalculateFee(context);
            }
        }

        protected virtual bool ShouldCalculateFee(ExecuteContext context)
        {
            return !context.IsFreeFee && !context.ShouldCancelExecute;
        }


        protected abstract void CalculateForOpenOrder(ExecuteContext context);

        protected abstract void CalculateForCloseOrder(ExecuteContext context);

        private void VerifyPendingConfirmLimitOrderLot()
        {
            if (this.IsCloseOrderExceedPendingConfirLimitOrderLot())
            {
                throw new TransactionServerException(TransactionError.ExistPendingLimitCloseOrder, "closeLot plus pendingConfirmLimitOrderLot exceed related open order' s balance");
            }
        }

        protected virtual bool ShouldVerifyPendingConfirmLimitOrderLot()
        {
            return !_order.IsOpen && !_order.Owner.IsPending;
        }

        private bool IsCloseOrderExceedPendingConfirLimitOrderLot()
        {
            Debug.Assert(!_order.IsOpen);
            foreach (var eachOrderRelation in _order.OrderRelations)
            {
                var pendingConfirmClosedLot = this.GetPendingConfirmClosedLot(eachOrderRelation.OpenOrder);
                if (pendingConfirmClosedLot + eachOrderRelation.ClosedLot > eachOrderRelation.OpenOrder.LotBalance)
                {
                    return true;
                }
            }
            return false;
        }


        private decimal GetPendingConfirmClosedLot(Order openOrder)
        {
            var ocoTrans = new List<Transaction>();
            var closeOrders = openOrder.GetAllCloseOrderAndClosedLot();
            var pendingConfirmClosedLot = 0m;
            foreach (var closeOrderAndLot in closeOrders)
            {
                var closeOrder = closeOrderAndLot.Key;
                if (ocoTrans.Contains(closeOrder.Owner)) continue;
                var closedLot = closeOrderAndLot.Value;
                var pendingConfirmLimitOrders = _account.GetPendingConfirmLimitOrders();
                if (closeOrder.Owner.OrderType.IsPendingType() && pendingConfirmLimitOrders.Contains(closeOrder))
                {
                    pendingConfirmClosedLot += closedLot;
                }
                if (closeOrder.Owner.Type == TransactionType.OneCancelOther)
                {
                    ocoTrans.Add(closeOrder.Owner);
                }
            }
            return pendingConfirmClosedLot;
        }

        private void UpdateInterestValueDate(ExecuteContext context)
        {
            var tradePolicyDetail = _order.Owner.TradePolicyDetail(context.TradeDay);
            var tradeDay = Settings.Setting.Default.GetTradeDay(context.TradeDay);
            int interestValueDay = _order.IsBuy ? tradePolicyDetail.BuyInterestValueDay : tradePolicyDetail.SellInterestValueDay;
            var accountId = _order.Owner.Owner.Id;
            _settings.InterestValueDate = tradeDay.Day.AddDays(interestValueDay);
        }

        private void CalculateBalance(ExecuteContext context)
        {
            Debug.Assert(_order.IsExecuted);
            if (context.IsBook) return;
            var deltaBalance = _order.SumBillsForBalance();
            var account = _order.Owner.Owner;
            var currencyId = _order.Owner.CurrencyId;
            _order.Account.AddBalance(_order.Owner.CurrencyId, deltaBalance, context.ExecuteTime);
        }
    }

    internal sealed class OrderExecuteAccountBalanceNotEnoughException : Exception
    {
        internal OrderExecuteAccountBalanceNotEnoughException(Guid accountId, decimal balance)
        {
            this.AccountId = accountId;
            this.Balance = balance;
        }

        internal Guid AccountId { get; private set; }
        internal decimal Balance { get; private set; }

    }


    internal class OrderExecuteService : OrderExecuteServiceBase
    {
        internal OrderExecuteService(Order order, OrderSettings settings)
            : base(order, settings) { }

        protected override void CalculateForOpenOrder(ExecuteContext context)
        {
            this.CalculateFee(context);
        }

        protected override void CalculateForCloseOrder(ExecuteContext context)
        {
            _order.UpdateOpenOrder(context);
            this.CalculateFee(context);
            if (!context.ShouldCancelExecute)
            {
                _order.CalculateValuedPL(context);
            }
        }
    }




    internal class PhysicalOrderExecuteService : OrderExecuteServiceBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PhysicalOrderExecuteService));

        private PhysicalOrder _physicalOrder;
        internal PhysicalOrderExecuteService(PhysicalOrder order, OrderSettings settings)
            : base(order, settings)
        {
            _physicalOrder = order;
        }

        protected override bool ShouldVerifyPendingConfirmLimitOrderLot()
        {
            return false;
        }

        protected override void CalculateForOpenOrder(ExecuteContext context)
        {
            try
            {
                if (!context.ShouldCancelExecute)
                {
                    _physicalOrder.CalculateOriginValue(context);
                    _physicalOrder.CalculatePaidPledge(context);
                }
                this.CalculateFee(context);

                if (_physicalOrder.Instalment != null)
                {
                    _physicalOrder.GenerateInstalmentDetails(DateTime.Now);
                }
            }
            catch (PrePaymentException ex)
            {
                Logger.WarnFormat("instalmentPolicyId={0}, period={1}, {2}", ex.InstalmentPolicyId, ex.Period, ex.Message);
                throw;
            }
        }


        protected override void CalculateForCloseOrder(ExecuteContext context)
        {
            if (!context.ShouldCancelExecute)
            {
                _physicalOrder.CalculateOriginValue(context);
                _physicalOrder.CalculatePaidPledge(context);
                _physicalOrder.CalculatePenalty(context);
            }
            _physicalOrder.UpdateOpenOrder(context);
            this.CalculateFee(context);
            if (!context.ShouldCancelExecute)
            {
                _physicalOrder.CalculateValuedPL(context);
            }
            this.CloseInstalment(context.ExecuteTime ?? DateTime.Now);
        }

        private void CloseInstalment(DateTime executeTime)
        {
            var orders = this.GetPayOffOrders();
            foreach (var eachOrder in orders)
            {
                decimal debitInterest = this.CalculateDebitInterest(eachOrder);
                if (debitInterest != 0m)
                {
                    eachOrder.AddBill(new Framework.Bill(eachOrder.AccountId, eachOrder.Owner.CurrencyId, -debitInterest, BillType.DebitInterest, Framework.BillOwnerType.Order, executeTime));
                }
                this.UpdateInstalments(eachOrder);
            }
        }

        private void UpdateInstalments(PhysicalOrder order)
        {
            decimal instalmentAmount = order.PhysicalOriginValueBalance - Math.Abs(order.PaidPledgeBalance);
            if (instalmentAmount > 0m)
            {
                order.DeleteAllInstalmentDetail();
                order.GenerateInstalmentDetails(DateTime.Now.Date);
            }
            else
            {
                foreach (var eachInstalment in order.Instalment.InstalmentDetails)
                {
                    if (eachInstalment.IsDeleted) continue;
                    eachInstalment.Update(0, 0, 0, DateTime.Now, DateTime.Now, order.LotBalance);
                }
            }

        }


        private decimal CalculateDebitInterest(PhysicalOrder order)
        {
            decimal result = 0m;
            foreach (var eachInstalment in order.Instalment.InstalmentDetails)
            {
                result += eachInstalment.DebitInterest;
            }
            return result;
        }



        private List<PhysicalOrder> GetPayOffOrders()
        {
            List<PhysicalOrder> result = new List<PhysicalOrder>();
            foreach (var eachOrderRelation in _order.OrderRelations)
            {
                var openOrder = (PhysicalOrder)eachOrderRelation.OpenOrder;
                if (openOrder.Instalment != null)
                {
                    result.Add(openOrder);
                }
            }
            return result;
        }

    }



    internal sealed class BOOrderExecuteService : OrderExecuteServiceBase
    {
        private BinaryOption.Order _boOrder;
        internal BOOrderExecuteService(BinaryOption.Order order, BOOrderSettings settings)
            : base(order, settings)
        {
            _boOrder = order;
        }

        protected override void CalculateForOpenOrder(ExecuteContext context)
        {
            this.CalculateFee(context);
            _boOrder.CalculatePledge();
        }

        protected override void CalculateForCloseOrder(ExecuteContext context)
        {
            this.CalculateFee(context);
            _order.CalculateValuedPL(context);
            _order.UpdateOpenOrder(context);
            _order.AddBill(Framework.Bill.CreateForOrder(_order.AccountId, _order.CurrencyId, _boOrder.PayBackPledge, BillType.PayBackPledge));
        }

    }


}
