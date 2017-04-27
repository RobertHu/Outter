using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Engine.iExchange;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal sealed class CancelService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CancelService));
        private Transaction _tran;

        private TransactionSettings _settings;

        internal CancelService(Transaction tran, TransactionSettings settings)
        {
            _tran = tran;
            _settings = settings;
        }

        internal Account Account
        {
            get { return _tran.Owner; }
        }

        internal bool CanCancel
        {
            get
            {
                return _tran.Phase == TransactionPhase.Placing || _tran.Phase == TransactionPhase.Placed;
            }
        }


        internal bool ShouldAutoCancel()
        {
            var dealingPolicy = _tran.DealingPolicyPayload();
            return (_tran.Phase == TransactionPhase.Placing && _tran.SubType == TransactionSubType.IfDone)
                || _tran.OrderType == OrderType.SpotTrade || _tran.OrderType == OrderType.Market
                || _tran.OrderType == OrderType.Risk || _tran.OrderType == OrderType.MultipleClose
                || _tran.OrderType == OrderType.MarketOnClose || _tran.OrderType == OrderType.MarketOnOpen
                || (_tran.GetLotForAutoJudgment() <= dealingPolicy.AutoCancelMaxLot);
        }



        internal bool CanBeCanceledByCustomer()
        {
            if (_tran.OrderType == OrderType.Limit &&
                 (_tran.Phase == TransactionPhase.Placed ||
                 (_tran.Phase == TransactionPhase.Placing && !_tran.IsDoneTran)))
            {
                var quotation = _tran.AccountInstrument.GetQuotation();
                foreach (Order order in _tran.Orders)
                {
                    Price marketPrice = (order.IsBuy ? quotation.BuyPrice : quotation.SellPrice);
                    if (order.HitCount >= 1 || marketPrice == null || Math.Abs(order.SetPrice - marketPrice) < _tran.DealingPolicyPayload().CancelLmtVariation)
                    {
                        Logger.WarnFormat("orderId= {0}, hitCount= {1}, abs(setPrice - marketPrice) = {2}, CancelLmtVariation ={3}", order.Id, order.HitCount, Math.Abs(order.SetPrice - marketPrice), _tran.DealingPolicyPayload().CancelLmtVariation);
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// 如果AmendedSource存在，则取消.如果AmendedOrder为IFDone单， 则取消该Transaction所对应的Done单
        /// </summary>
        internal void CancelAmendedAndDoneTransactions(CancelReason cancelType = CancelReason.OtherReason)
        {
            Logger.InfoFormat("CancelAmendedAndDoneTransactions tranId = {0}, subType = {1}", _tran.Id, _tran.SubType);
            this.CancelAmendedIfTransaction(cancelType);
            this.CanceAmendedDoneTransactions(cancelType);
            this.ChangeSubType();
        }

        private void ChangeSubType()
        {
            var amendedOrder = _tran.AmendedOrder;
            if (amendedOrder != null)
            {
                var amendedTran = amendedOrder.Owner;
                if (amendedTran.SubType == TransactionSubType.IfDone)
                {
                    _settings.SubType = amendedTran.SubType;
                }
            }
        }

        private void CanceAmendedDoneTransactions(CancelReason cancelType)
        {
            var doneTrans = this.GetAmendedDoneTrans();
            Logger.InfoFormat("CanceAmendedlDoneTransactions tranId = {0} cancelType = {1}, doneTransCount = {2}, account.id = {3}", _tran.Id, cancelType, doneTrans.Count, _tran.AccountId);
            foreach (var eachDoneTran in doneTrans)
            {
                Logger.InfoFormat("amended done tran id = {0}, account.Id = {1}", eachDoneTran.Id, eachDoneTran.AccountId);
                eachDoneTran.Cancel(cancelType);
                this.Account.InvalidateInstrumentCache(eachDoneTran);
            }
        }


        private void CancelAmendedIfTransaction(CancelReason cancelType)
        {
            Order amendedOrder = _tran.AmendedOrder;
            if (amendedOrder == null) return;
            var amendedTran = amendedOrder.Owner;
            amendedTran.ChangePhaseToCancel();
            amendedOrder.Cancel(cancelType);
            Logger.InfoFormat("CancelAmendedIfTransaction tranId = {0}, orderId = {1}, tran.Type = {2}, tran.SubType = {3}, account.id = {4}", amendedTran.Id, amendedOrder.Id, amendedTran.Type, amendedTran.SubType, amendedTran.Owner.Id);
            if (amendedTran.Type == TransactionType.OneCancelOther)
            {
                foreach (Order order in amendedTran.Orders)
                {
                    Logger.InfoFormat("CancelAmendedIfTransaction, account id = {0}, cancel order id = {1}", order.Account.Id, order.Id);
                    order.Cancel(cancelType);
                }
            }
            this.Account.InvalidateInstrumentCache(amendedTran);
        }


        internal void CancelDoneTrans(CancelReason cancelType)
        {
            var doneTrans = _tran.GetDoneTransactions();
            Logger.InfoFormat("CancelDoneTrans tranId = {0} cancelType = {1}, doneTransCount = {2}, account.Id = {3}", _tran.Id, cancelType, doneTrans.Count, _tran.AccountId);
            foreach (var eachDoneTran in doneTrans)
            {
                Logger.InfoFormat("done tran id = {0}, account.id = {1}", eachDoneTran.Id, eachDoneTran.AccountId);
                eachDoneTran.Cancel(cancelType);
                this.Account.InvalidateInstrumentCache(eachDoneTran);
            }
        }


        private List<Transaction> GetAmendedDoneTrans()
        {
            List<Transaction> result = new List<Transaction>();
            if (_tran.AmendedOrder == null) return result;
            Transaction amendedTran = _tran.AmendedOrder.Owner;
            Logger.InfoFormat("GetAmendedDoneTrans amendedTran.id = {0}, account.id = {1}, amendedTran.SubType = {2}", amendedTran.Id, amendedTran.AccountId, amendedTran.SubType);
            if (!amendedTran.DoneCondition) return result;
            var doneTrans = amendedTran.GetDoneTransactions();
            foreach (var eachDoneTran in doneTrans)
            {
                result.Add(eachDoneTran);
            }
            return result;
        }

    }
}
