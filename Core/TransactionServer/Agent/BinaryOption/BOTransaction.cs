using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Factory;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOTransaction : Transaction
    {
        internal BOTransaction(Account account, TransactionConstructParams constructParams, ITransactionServiceFactory serviceFactory)
            : base(account, constructParams, serviceFactory)
        {
        }

        internal override bool ShouldCheckIsExceedMaxOpenLot
        {
            get
            {
                return false;
            }
        }

        internal override bool DeferredToFill
        {
            get
            {
                return this.FirstOrder.SetPrice == null;
            }
        }


        internal override bool CanBeClosedBySplit(Transaction targetTran)
        {
            return false;
        }

        public override bool IsFreeOfPriceCheck(bool isForExecuting)
        {
            return isForExecuting || this.Type == TransactionType.MultipleClose || this.SubType == TransactionSubType.Mapping;
        }


        internal override bool CanAutoAcceptPlace()
        {
            var tradePolicyDetail = this.TradePolicyDetail();
            BOPolicyDetail binaryOptionPolicyDetail = null;
            Order order = (BinaryOption.Order)this.FirstOrder;
            if (tradePolicyDetail.BinaryOptionPolicyID != null
                && BOPolicyDetailRepository.Default.TryGet(new BOPolicyDetailKey(tradePolicyDetail.BinaryOptionPolicyID.Value, order.BetTypeId, order.Frequency), out binaryOptionPolicyDetail))
            {
                return order.Lot <= binaryOptionPolicyDetail.AutoAcceptMaxBet;
            }
            else
            {
                return false;
            }
        }

        internal override void ExecuteDirectly(Engine.ExecuteContext context)
        {
            base.ExecuteDirectly(context);
            BOEngine.Default.HandleExecutedTransaction(this.Owner, this);
        }

        internal override void Execute(Engine.ExecuteContext context)
        {
            base.Execute(context);
            BOEngine.Default.HandleExecutedTransaction(this.Owner, this);
        }


    }
}
