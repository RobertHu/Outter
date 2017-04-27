using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal abstract class OrderValidatorBase
    {
        internal abstract ILog Logger { get; }

        internal virtual void Validate(Order order)
        {
            if (!order.IsOpen) return;
            this.ValidateCommon(order);
        }

        private void ValidateCommon(Order order)
        {
            var instrument = order.Owner.TradingInstrument;
            var settingInstrument = order.Owner.SettingInstrument();
            if (!instrument.IsTrading) throw new TransactionServerException(TransactionError.TimingIsNotAcceptable);
            var tradePolicyDetail = order.Owner.TradePolicyDetail();
            BOPolicy binaryOptionPolicy = null;
            BOPolicyDetail binaryOptionPolicyDetail = null;

            if (tradePolicyDetail.BinaryOptionPolicyID == null) throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
            if (!BOPolicyRepository.TryGet(tradePolicyDetail.BinaryOptionPolicyID.Value, out binaryOptionPolicy)
                || !BOPolicyDetailRepository.Default.TryGet(new BOPolicyDetailKey(tradePolicyDetail.BinaryOptionPolicyID.Value, order.BetTypeId, order.Frequency), out binaryOptionPolicyDetail))
            {
                throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
            }

            if (binaryOptionPolicyDetail.BetType.Option == BOOption.Instance)
            {
                BOBetType betType = BOBetTypeRepository.Get(order.BetTypeId);
                TimeSpan acceptSpan = TimeSpan.FromSeconds(order.Frequency * betType.HitCount);
                DateTime acceptTime = instrument.CurrentTradingSession.AcceptEndTime - acceptSpan;
                if (Market.MarketManager.Now >= acceptTime) throw new TransactionServerException(TransactionError.TimingIsNotAcceptable);
            }
            else
            {
                if (binaryOptionPolicyDetail.BetType.HitCount > 1)
                {
                    this.Logger.WarnFormat("Hit count must be 1 when BOSettleTime is filled, order id = {0}", order.Id);
                    throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
                }

                DateTime now = Market.MarketManager.Now;
                TimeSpan acceptVariation = TimeSpan.Zero;
                if (binaryOptionPolicyDetail.BetType.Option == BOOption.Settle)
                {
                    acceptVariation = binaryOptionPolicyDetail.BetType.LastPlaceOrderTimeSpan;
                }
                else if (binaryOptionPolicyDetail.BetType.Option == BOOption.IntegralMinute)
                {
                    acceptVariation = TimeSpan.FromMinutes(order.Frequency);
                }

                if (order.SettleTime.Value > instrument.CurrentTradingSession.AcceptEndTime
                    || now > (order.SettleTime.Value - acceptVariation))
                {
                    throw new TransactionServerException(TransactionError.TimingIsNotAcceptable);
                }
            }

        }

    }

    internal sealed class OrderExecuteValidator : OrderValidatorBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(OrderExecuteValidator));

        internal static readonly OrderExecuteValidator Default = new OrderExecuteValidator();

        static OrderExecuteValidator() { }
        private OrderExecuteValidator() { }

        internal override ILog Logger
        {
            get { throw new NotImplementedException(); }
        }
    }

    internal sealed class OrderPlaceValidator : OrderValidatorBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(OrderPlaceValidator));

        internal static readonly OrderPlaceValidator Default = new OrderPlaceValidator();

        static OrderPlaceValidator() { }
        private OrderPlaceValidator() { }

        internal override ILog Logger
        {
            get { return _Logger; }
        }

        internal override void Validate(Order order)
        {
            base.Validate(order);
            var settingInstrument = order.Owner.SettingInstrument();
            if (!settingInstrument.AllowBinaryOptionOrder || order.SetPrice != null) throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
            BOPolicy binaryOptionPolicy = null;
            BOPolicyDetail binaryOptionPolicyDetail = null;

            BOPolicyRepository.TryGet(order.Owner.TradePolicyDetail().BinaryOptionPolicyID.Value, out binaryOptionPolicy);
            BOPolicyDetailRepository.Default.TryGet(new BOPolicyDetailKey(order.Owner.TradePolicyDetail().BinaryOptionPolicyID.Value, order.BetTypeId, order.Frequency), out binaryOptionPolicyDetail);

            if (order.Lot < binaryOptionPolicyDetail.MinBet || order.Lot > binaryOptionPolicyDetail.MaxBet)
            {
                throw new TransactionServerException(TransactionError.OrderLotExceedMaxLot);
            }

            var tradePolicy = order.Owner.TradePolicy;

            bool needCheck = tradePolicy.BinaryOptionBetLimit > 0
                || binaryOptionPolicy.MaxOrderCount != null || binaryOptionPolicy.TotalBetLimit != null
                || binaryOptionPolicyDetail.MaxOrderCount != null || binaryOptionPolicyDetail.TotalBetLimit != null;

            if (needCheck)
            {
                decimal totalBetAmount, totalBetAmountByPolicy, totalBetAmountByPolicyDetail;
                int totalBetOrderByPolicy, totalBetOrderByPolicyDetail;

                this.SumBetAfterPlace(order, out totalBetAmount, out totalBetAmountByPolicy,
                    out totalBetOrderByPolicy, out totalBetAmountByPolicyDetail, out totalBetOrderByPolicyDetail);

                bool exceeded = false;
                if (tradePolicy.BinaryOptionBetLimit > 0) exceeded |= totalBetAmount > tradePolicy.BinaryOptionBetLimit;

                if (binaryOptionPolicy.MaxOrderCount != null) exceeded |= totalBetOrderByPolicy > binaryOptionPolicy.MaxOrderCount.Value;
                if (binaryOptionPolicy.TotalBetLimit != null) exceeded |= totalBetAmountByPolicy > binaryOptionPolicy.TotalBetLimit.Value;

                if (binaryOptionPolicyDetail.MaxOrderCount != null) exceeded |= totalBetOrderByPolicyDetail > binaryOptionPolicyDetail.MaxOrderCount.Value;
                if (binaryOptionPolicyDetail.TotalBetLimit != null) exceeded |= totalBetAmountByPolicyDetail > binaryOptionPolicyDetail.TotalBetLimit.Value;

                if (exceeded) throw new TransactionServerException(TransactionError.ExceedMaxPhysicalValue);
            }
        }

        private void SumBetAfterPlace(Order order, out decimal totalBetAmount, out decimal totalBetAmountByPolicy,
            out int totalBetOrderByPolicy, out decimal totalBetAmountByPolicyDetail, out int totalBetOrderByPolicyDetail)
        {
            Guid instrumentId = order.Instrument().Id;
            var tradePolicyDetail = order.Owner.TradePolicyDetail();
            totalBetAmount = totalBetAmountByPolicy = totalBetAmountByPolicyDetail = this.CalculateBet(order);
            totalBetOrderByPolicy = totalBetOrderByPolicyDetail = 1;

            foreach (Transaction tran in order.Account.Transactions)
            {
                if (tran.Phase == TransactionPhase.Canceled || Object.ReferenceEquals(tran, order.Owner)) continue;

                if (tran.OrderType == OrderType.BinaryOption)
                {
                    foreach (Order item in tran.Orders)
                    {
                        if (item.IsOpen && item.LotBalance > 0)
                        {
                            decimal betAmount = this.CalculateBet(item);
                            totalBetAmount += betAmount;

                            if (item.Instrument().Id == instrumentId)
                            {
                                if (tradePolicyDetail.BinaryOptionPolicyID.Value == tran.TradePolicyDetail().BinaryOptionPolicyID.Value)
                                {
                                    totalBetAmountByPolicy += betAmount;
                                    totalBetOrderByPolicy++;

                                    if (order.BetTypeId == item.BetTypeId && order.Frequency == item.Frequency)
                                    {
                                        totalBetAmountByPolicyDetail += betAmount;
                                        totalBetOrderByPolicyDetail++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private decimal CalculateBet(Order order)
        {
            decimal betAmount = order.Lot;
            if (order.Account.IsMultiCurrency)
            {
                betAmount = -order.Owner.CurrencyRate(null).Exchange(-betAmount);
            }
            return betAmount;
        }

    }
}
