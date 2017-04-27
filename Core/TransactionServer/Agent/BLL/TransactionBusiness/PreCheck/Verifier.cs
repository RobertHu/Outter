using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.TypeExtension;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck
{
    internal class Verifier
    {
        protected Transaction _tran;
        protected AccountClass.Instrument _instrument;
        protected bool _isBuy;

        internal Verifier(Transaction tran)
        {
            _tran = tran;
            _instrument = _tran.TradingInstrument;
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
            var tranLotBalance = _tran.CalculateLotBalanceForPreCheck(asRisk);
            var oldNetPosition = oldLotBalance.NetPosition;
            var newNetPosition = oldNetPosition + tranLotBalance.NetPosition;
            return Tuple.Create(oldNetPosition, newNetPosition);
        }

        protected virtual bool ShouldCalculateTranLotBalanceAsRisk()
        {
            return false;
        }


        protected virtual BuySellLot CalculateSameDirectionLotsOfPendingOrders()
        {
            if (!_tran.IsPending) return BuySellLot.Empty;
            BuySellLot result = BuySellLot.Empty;
            foreach (Transaction eachTran in _instrument.GetTransactions())
            {
                if (this.ShouldSumPlaceMargin(eachTran))
                {
                    result += eachTran.CalculateLotBalanceForPreCheck();
                }
            }
            return result;
        }

        private bool ShouldSumPlaceMargin(Transaction tran)
        {
            bool isSameDirection = _isBuy == tran.FirstOrder.IsBuy;
            return isSameDirection && tran.ShouldSumPlaceMargin() && !object.ReferenceEquals(_tran, tran) &&
                (_tran.AmendedOrder == null || !object.ReferenceEquals(tran, _tran.AmendedTran));
        }
    }
}
