using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class Instalment : BusinessItemBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Instalment));
        private BusinessItem<Guid> _instalmentPolicyId;
        private BusinessItem<decimal> _downPayment;
        private BusinessItem<InstalmentType> _instalmentType;
        private BusinessItem<RecalculateRateType> _recalculateRateType;
        private BusinessItem<Protocal.DownPaymentBasis> _downPaymentBasis;
        private BusinessItem<bool> _isInstalmentOverdue;
        private BusinessItem<int> _instalmentOverdueDay;
        private PhysicalOrder _owner;
        private Guid _accountId;
        private BusinessItem<int> _period;
        private BusinessItem<InstalmentFrequence> _frequence;
        private BusinessRecordList<InstalmentDetail> _details;

        #region Constructors

        private Instalment(PhysicalOrder owner, InstalmentConstructParams constructParams)
            : base(owner)
        {
            _owner = owner;
            _accountId = _owner.Owner.Owner.Id;
            _details = new BusinessRecordList<InstalmentDetail>("InstalmentDetails", owner, 5);
            this.Parse(constructParams);
        }

        private void Parse(InstalmentConstructParams constructParams)
        {
            _period = this.CreateReadonlyItem("Period", constructParams.Period);
            _frequence = this.CreateReadonlyItem("Frequence", constructParams.Frequence);
            _instalmentPolicyId = this.CreateReadonlyItem(OrderBusinessItemNames.InstalmentPolicyId, constructParams.InstalmentPolicyId.Value);
            _downPayment = this.CreateReadonlyItem(OrderBusinessItemNames.DownPayment, constructParams.DownPayment);
            _instalmentType = this.CreateReadonlyItem(OrderBusinessItemNames.PhysicalInstalmentType, constructParams.InstalmentType);
            _recalculateRateType = this.CreateReadonlyItem(OrderBusinessItemNames.RecalculateRateType, constructParams.RecalculateRateType);
            _downPaymentBasis = this.CreateReadonlyItem(OrderBusinessItemNames.DownPaymentBasis, this.InstalmentPolicyDetail(null).DownPaymentBasis);
            _isInstalmentOverdue = this.CreateReadonlyItem(OrderBusinessItemNames.IsInstalmentOverdue, constructParams.IsInstalmentOverdue);
            _instalmentOverdueDay = this.CreateReadonlyItem("InstalmentOverdueDay", constructParams.InstalmentOverdueDay);
        }

        internal static Instalment Create(PhysicalOrder owner, InstalmentConstructParams constructParams)
        {
            if (constructParams == null || (constructParams.InstalmentPolicyId ?? Guid.Empty) == Guid.Empty) return null;
            return new Instalment(owner, constructParams);
        }


        #endregion

        #region Properties
        internal Guid InstalmentPolicyId
        {
            get { return _instalmentPolicyId.Value; }
        }

        internal InstalmentPolicyDetail InstalmentPolicyDetail(DateTime? tradeDay)
        {
            return this.InstalmentPolicy(tradeDay).Get(new InstalmentPeriod(this.Period, this.Frequence));
        }

        internal InstalmentPolicy InstalmentPolicy(DateTime? tradeDay)
        {
            try
            {
                return Settings.Setting.Default.GetInstalmentPolicy(this.InstalmentPolicyId, tradeDay);
            }
            catch
            {
                Logger.ErrorFormat("instalmentPolicyId = {0} can not be found in Setting", this.InstalmentPolicyId);
                throw;
            }
        }

        internal InstalmentFrequence Frequence
        {
            get { return _frequence.Value; }
        }

        internal int Period
        {
            get { return _period.Value; }
        }

        internal decimal DownPayment
        {
            get { return _downPayment.Value; }
        }

        internal InstalmentType InstalmentType
        {
            get { return _instalmentType.Value; }
        }

        internal RecalculateRateType RecalculateRateType
        {
            get { return _recalculateRateType.Value; }
        }

        internal bool IsFullPayment
        {
            get { return this.Period < 1; }
        }


        internal Protocal.DownPaymentBasis DownPaymentBasis
        {
            get { return _downPaymentBasis.Value; }
        }

        internal bool IsInstalmentOverdue
        {
            get { return _isInstalmentOverdue.Value; }
        }

        internal int InstalmentOverdueDay
        {
            get { return _instalmentOverdueDay.Value; }
        }

        internal bool CanBeClosed
        {
            get
            {
                if (this.InstalmentPolicy(null).CloseOption == InstalmentCloseOption.AllowAll
                        || (this.InstalmentPolicy(null).CloseOption == InstalmentCloseOption.AllowWhenNotOverdue && !this.IsInstalmentOverdue)
                        || (this.InstalmentPolicy(null).CloseOption == InstalmentCloseOption.AllowPrepayment && this.Frequence == InstalmentFrequence.TillPayoff))
                {
                    return true;
                }
                return false;
            }
        }


        internal bool IsOverdueToCut
        {
            get
            {
                if (_owner.LotBalance > 0 && this.InstalmentOverdueDay > 0)
                {
                    return this.InstalmentPolicyDetail(null).LatePaymentAutoCutDay > 0 && this.InstalmentOverdueDay >= this.InstalmentPolicyDetail(null).LatePaymentAutoCutDay;
                }
                return false;
            }
        }

        internal IEnumerable<InstalmentDetail> InstalmentDetails
        {
            get
            {
                return _details.GetValues();
            }
        }

        #endregion

        internal void AddDetail(InstalmentDetail detail, OperationType operationType)
        {
            _details.AddItem(detail, operationType);
        }

        internal void DeleteDetail(InstalmentDetail detail)
        {
            if (detail.IsDeleted) return;
            _details.RemoveItem(detail);
        }

        internal InstalmentDetail GetDetail(int period)
        {
            foreach (var eachDetail in _details.GetValues())
            {
                if (eachDetail.Period == period) return eachDetail;
            }
            return null;
        }


        internal void UpdateDetail(int sequence, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, decimal lotBalance)
        {
            var item = this.GetDetail(sequence);
            item.Update(interest, principal, debitInterest, paidDateTime, updateTime, lotBalance);
        }

        internal void UpdateDetail(int sequence, decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
        {
            var item = this.GetDetail(sequence);
            item.Update(interestRate, interest, principal, debitInterest, paidDateTime, updateTime, updatePersonId, lotBalance);
        }

        internal decimal CalculatePaidAmount()
        {
            return this.CalculatePaidAmount(_owner.PhysicalOriginValue);
        }

        internal decimal CalculatePaidAmount(decimal marketValue)
        {
            if (this.DownPaymentBasis == Protocal.DownPaymentBasis.PercentageOfAmount)
            {
                return marketValue * this.DownPayment;
            }
            else
            {
                return _owner.Lot * this.DownPayment;
            }
        }

        internal decimal CalculateInstalmentAdministrationFee(ExecuteContext context)
        {
            return this.CalculateInstalmentAdministrationFee(_owner.PhysicalOriginValue, context);
        }

        internal decimal CalculateInstalmentAdministrationFee(decimal marketValue, ExecuteContext context)
        {
            var setting = this.GetHistorySetting(_owner, context);
            var currencyRate = setting.Item1;
            var instalmentPolicyDetail = setting.Item2;
            return FeeCalculator.CaculateInstalmentAdministrationFee(marketValue, _owner.Lot, instalmentPolicyDetail, currencyRate);
        }

        private Tuple<Settings.CurrencyRate, Settings.InstalmentPolicyDetail> GetHistorySetting(Order order, ExecuteContext context)
        {
            var currencyRate = order.Owner.CurrencyRate(context.TradeDay);
            var instalmentPolicyDetail = this.InstalmentPolicyDetail(context.TradeDay);
            return Tuple.Create(currencyRate, instalmentPolicyDetail);
        }


        internal decimal CalculateClosePenalty(Order closeOrder, decimal closedLot, ExecuteContext context)
        {
            Debug.Assert(_owner.IsOpen && !closeOrder.IsOpen);
            var setting = this.GetHistorySetting(closeOrder, context);
            var currencyRate = setting.Item1;
            var instalmentPolicyDetail = setting.Item2;
            decimal result = 0m;
            if (this.Frequence != InstalmentFrequence.TillPayoff && !_owner.IsPayoff && !this.IsOverdueToCut)
            {
                if (instalmentPolicyDetail.ClosePenaltyBase == ClosePenaltyBase.FixedAmount)
                {
                    result = currencyRate.Exchange(instalmentPolicyDetail.ClosePenaltyValue, ExchangeDirection.RateOut);
                }
                else if (instalmentPolicyDetail.ClosePenaltyBase == ClosePenaltyBase.FixedAmountPerLot)
                {
                    result = currencyRate.Exchange(instalmentPolicyDetail.ClosePenaltyValue * closedLot, ExchangeDirection.RateOut);
                }
                else if (instalmentPolicyDetail.ClosePenaltyBase == ClosePenaltyBase.ValueProportion)
                {
                    result = instalmentPolicyDetail.ClosePenaltyValue * _owner.RemainAmount;
                }
                else
                {
                    throw new NotSupportedException(string.Format("{0} is not a support ClosePenaltyBase", instalmentPolicyDetail.ClosePenaltyBase));
                }
            }
            return result;
        }

        internal decimal CalculateOverdueCutPenalty(Order closeOrder, decimal closedLot, ExecuteContext context)
        {
            Debug.Assert(_owner.IsOpen && !closeOrder.IsOpen);
            var setting = this.GetHistorySetting(closeOrder, context);
            var currencyRate = setting.Item1;
            var instalmentPolicyDetail = setting.Item2;
            decimal result = 0;
            if (this.IsOverdueToCut)
            {
                if (instalmentPolicyDetail.AutoCutPenaltyBase == AutoCutPenaltyBase.FixedAmount)
                {
                    result = currencyRate.Exchange(instalmentPolicyDetail.AutoCutPenaltyValue, ExchangeDirection.RateOut);
                }
                else if (instalmentPolicyDetail.AutoCutPenaltyBase == AutoCutPenaltyBase.FixedAmountPerLot)
                {
                    result = currencyRate.Exchange(instalmentPolicyDetail.AutoCutPenaltyValue * closedLot, ExchangeDirection.RateOut);
                }
                else if (instalmentPolicyDetail.AutoCutPenaltyBase == AutoCutPenaltyBase.ValueProportion)
                {
                    result = instalmentPolicyDetail.AutoCutPenaltyValue * _owner.RemainAmount;
                }
                else
                {
                    throw new NotSupportedException(string.Format("{0} is not a support AutoCutPenaltyBase", instalmentPolicyDetail.AutoCutPenaltyBase));
                }
            }
            return result;
        }

    }
}
