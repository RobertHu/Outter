using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Services
{
    internal abstract class PreCheckVerifierBase
    {
        protected Transaction _tran;
        protected AccountClass.Instrument _instrument;
        protected bool _isBuy;

        protected PreCheckVerifierBase(Transaction tran)
        {
            _tran = tran;
            _instrument = _tran.AccountInstrument;
            _isBuy = _tran.FirstOrder.IsBuy;
        }

        internal virtual bool IsFreeOfMarginCheck()
        {
            var netPos = this.CalculateNetPosition();
            decimal oldNetPosition = netPos.Item1;
            decimal newNetPosition = netPos.Item2;
            if (Math.Abs(newNetPosition) > Math.Abs(oldNetPosition)) return false;
            if (_tran.TradePolicy.IsFreeOverHedge) return true;
            bool isSameSign = newNetPosition * oldNetPosition >= 0;
            return isSameSign && (_tran.ExistsCloseOrder() || _tran.TradePolicy.IsFreeHedge);
        }

        private Tuple<decimal, decimal> CalculateNetPosition()
        {
            var oldLotBalance = new BuySellLot(_instrument.TotalBuyLotBalance, _instrument.TotalSellLotBalance);
            oldLotBalance += this.CalculateSameDirectionLotsOfPendingOrders();
            bool asRisk = this.ShouldCalculateTranLotBalanceAsRisk();
            var tranLotBalance = this.CalculateLotBalanceForPreCheck(asRisk);
            var oldNetPosition = oldLotBalance.NetPosition;
            var newNetPosition = oldNetPosition + tranLotBalance.NetPosition;
            return Tuple.Create(oldNetPosition, newNetPosition);
        }

        protected abstract bool ShouldCalculateTranLotBalanceAsRisk();

        protected virtual BuySellLot CalculateSameDirectionLotsOfPendingOrders()
        {
            if (!_tran.IsPending) return BuySellLot.Empty;
            BuySellLot result = BuySellLot.Empty;
            foreach (Transaction eachTran in _instrument.GetTransactions())
            {
                if (this.ShouldSumPlaceMargin(eachTran))
                {
                    result += this.CalculateLotBalanceForPreCheck();
                }
            }
            return result;
        }

        protected BuySellLot CalculateLotBalanceForPreCheck(bool asRisk = false)
        {
            BuySellLot result = BuySellLot.Empty;
            foreach (var eachOrder in _tran.Orders)
            {
                if (this.ShouldCalculateOrderLotBalance(eachOrder, asRisk))
                {
                    result += this.CalculateOrderLotBalance(eachOrder);
                }
            }
            return result;
        }

        private bool ShouldCalculateOrderLotBalance(Order order, bool asRisk)
        {
            bool isBetterOption = _tran.Type == TransactionType.OneCancelOther && _tran.OrderType == OrderType.Limit && order.TradeOption == TradeOption.Better;
            return !isBetterOption && (asRisk || order.IsRisky);
        }

        private BuySellLot CalculateOrderLotBalance(Order order)
        {
            decimal buyLotBalance = 0;
            decimal sellLotBalance = 0;
            if (order.Phase == OrderPhase.Executed)
            {
                if (order.IsBuy)
                {
                    buyLotBalance = order.LotBalance;
                }
                else
                {
                    sellLotBalance = order.LotBalance;
                }
            }
            else if (order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing)
            {
                if (order.IsOpen)
                {
                    if (order.IsBuy)
                    {
                        buyLotBalance = order.LotBalance;
                    }
                    else
                    {
                        sellLotBalance = order.LotBalance;
                    }
                }
                else //will close executed orders
                {
                    if (order.IsBuy)
                    {
                        sellLotBalance = -order.Lot;
                    }
                    else
                    {
                        buyLotBalance = -order.Lot;
                    }
                }
            }
            return new BuySellLot(buyLotBalance, sellLotBalance);
        }

        protected bool ShouldSumPlaceMargin(Transaction tran)
        {
            if (tran.OrderCount == 0) return false;
            bool isSameDirection = _isBuy == tran.FirstOrder.IsBuy;
            return isSameDirection && tran.ShouldSumPlaceMargin() && !object.ReferenceEquals(_tran, tran) &&
                (_tran.AmendedOrder == null || !object.ReferenceEquals(tran, _tran.AmendedTran));
        }

    }



    internal class PreCheckVerifier : PreCheckVerifierBase
    {
        internal PreCheckVerifier(Transaction tran)
            : base(tran)
        {
        }

        protected override bool ShouldCalculateTranLotBalanceAsRisk()
        {
            return false;
        }
    }

    internal sealed class PhysicalPreCheckVerifier : PreCheckVerifierBase
    {
        internal PhysicalPreCheckVerifier(PhysicalTransaction tran)
            : base(tran) { }

        protected override BuySellLot CalculateSameDirectionLotsOfPendingOrders()
        {
            var result = base.CalculateSameDirectionLotsOfPendingOrders();
            var buyDirectionLotBalance = this.CalculateBuyDirectionLotBalance();
            result += new BuySellLot(buyDirectionLotBalance, 0m);
            return result;
        }

        private decimal CalculateBuyDirectionLotBalance()
        {
            decimal result = 0m;
            foreach (var tran in _instrument.GetTransactions())
            {
                if (object.ReferenceEquals(tran, _tran)) continue;
                foreach (var eachOrder in tran.Orders)
                {
                    if (eachOrder.IsBuy && eachOrder.IsExecuted && eachOrder.LotBalance > 0)
                    {
                        result += eachOrder.LotBalance;
                    }
                }
            }
            return result;
        }

        protected override bool ShouldCalculateTranLotBalanceAsRisk()
        {
            return true;
        }


    }


    internal sealed class BOPreCheckVerifier : PreCheckVerifierBase
    {
        internal BOPreCheckVerifier(BOTransaction tran)
            : base(tran) { }
        internal override bool IsFreeOfMarginCheck()
        {
            if (_tran.FirstOrder.IsOpen) return false;
            return base.IsFreeOfMarginCheck();
        }

        protected override bool ShouldCalculateTranLotBalanceAsRisk()
        {
            return false;
        }
    }

}
