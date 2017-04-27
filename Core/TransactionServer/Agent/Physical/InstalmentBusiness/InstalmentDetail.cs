using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.Physical;

namespace Core.TransactionServer.Agent.Physical.InstalmentBusiness
{
    internal sealed class InstalmentDetail : BusinessRecord
    {
        private BusinessItem<Guid> _orderId;
        private BusinessItem<int> _period;
        private BusinessItem<decimal> _principal;
        private BusinessItem<decimal> _interest;
        private BusinessItem<decimal> _debitInterest;
        private BusinessItem<DateTime?> _paymentDateTimeOnPlan;
        private BusinessItem<DateTime?> _paidDateTime;
        private BusinessItem<DateTime> _updateTIme;
        private BusinessItem<decimal> _interestRate;
        private BusinessItem<decimal?> _lotBalance;
        private BusinessItem<Guid?> _updatePersonId;

        internal InstalmentDetail(PhysicalOrder owner, int period, decimal principal, decimal interest, decimal debitInterest, DateTime? paymentDateTimeOnPlan, DateTime? paidDateTime)
            : base("InstalmentDetail", 10)
        {
            this.CreateBusinessItems(owner.Id, period, principal, interest, debitInterest, paymentDateTimeOnPlan, owner.Instalment.InstalmentPolicyDetail(null).InterestRate, paidDateTime, null, owner.LotBalance, null);
            this.Initialize(owner.AccountId, owner.Instrument().Id);
        }

        internal InstalmentDetail(PhysicalOrder owner, OrderInstalmentData data)
            : base("InstalmentDetail", 10)
        {
            this.CreateBusinessItems(owner.Id, data.Sequence, data.Principal, data.Interest, data.DebitInterest, data.PaymentDateTimeOnPlan, data.InterestRate, data.PaidDateTime, data.UpdateTime, data.LotBalance, data.UpdatePersonId);
            this.Initialize(owner.AccountId, owner.Instrument().Id);
        }

        private void CreateBusinessItems(Guid orderId, int period, decimal principal, decimal interest, decimal debitInterest, DateTime? paymentDateTimeOnPlan, decimal interestRate, DateTime? paidDateTime, DateTime? updateTime, decimal? lotBalance, Guid? updatePersonId)
        {
            _orderId = BusinessItemFactory.Create("OrderId", orderId, PermissionFeature.Key, this);
            _period = BusinessItemFactory.Create("Period", period, PermissionFeature.Key, this);
            _principal = BusinessItemFactory.Create("Principal", principal, PermissionFeature.Sound, this);
            _interest = BusinessItemFactory.Create("Interest", interest, PermissionFeature.Sound, this);
            _debitInterest = BusinessItemFactory.Create("DebitInterest", debitInterest, PermissionFeature.Sound, this);
            _paymentDateTimeOnPlan = BusinessItemFactory.Create("PaymentDateTimeOnPlan", paymentDateTimeOnPlan, PermissionFeature.Sound, this);
            _paidDateTime = BusinessItemFactory.Create("PaidDateTime", paidDateTime, PermissionFeature.Sound, this);
            _updateTIme = BusinessItemFactory.Create("UpdateTime", updateTime ?? DateTime.Now, PermissionFeature.Sound, this);
            _interestRate = BusinessItemFactory.Create("InterestRate", interestRate, PermissionFeature.Sound, this);
            _lotBalance = BusinessItemFactory.Create("LotBalance", lotBalance, PermissionFeature.Sound, this);
            _updatePersonId = BusinessItemFactory.Create("UpdatePersonId", updatePersonId, PermissionFeature.Sound, this);
        }

        private void Initialize(Guid accountId, Guid instrumentId)
        {
            this.AccountId = accountId;
            this.InstrumentId = instrumentId;
        }


        internal Guid OrderId { get { return _orderId.Value; } }

        internal int Period { get { return _period.Value; } }

        internal decimal Principal
        {
            get { return _principal.Value; }
        }

        internal decimal Interest { get { return _interest.Value; } }

        internal decimal DebitInterest
        {
            get { return _debitInterest.Value; }
            set
            {
                _debitInterest.SetValue(value);
            }
        }

        internal DateTime? PaymentDateTimeOnPlan { get { return _paymentDateTimeOnPlan.Value; } }

        internal DateTime? PaidDateTime { get { return _paidDateTime.Value; } }

        internal Guid AccountId { get; private set; }

        internal Guid InstrumentId { get; private set; }

        internal decimal InterestRate
        {
            get
            {
                return _interestRate.Value;
            }
        }

        internal void Update(decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, decimal lotBalance)
        {
            if (this.PaidDateTime != null) return;
            _interest.SetValue(interest);
            _principal.SetValue(principal);
            _debitInterest.SetValue(debitInterest);
            _paidDateTime.SetValue(paidDateTime);
            _updateTIme.SetValue(updateTime);
            _lotBalance.SetValue(lotBalance);
        }

        internal void Update(decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
        {
            _interestRate.SetValue(interestRate);
            _interest.SetValue(interest);
            _principal.SetValue(principal);
            _debitInterest.SetValue(debitInterest);
            _paidDateTime.SetValue(paidDateTime);
            _updateTIme.SetValue(updateTime);
            _updatePersonId.SetValue(updatePersonId);
            _lotBalance.SetValue(lotBalance);
        }

        internal void UpdateByPrePay(decimal interest, decimal interestRate, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
        {
            if (this.PaidDateTime != null) return;
            _interest.SetValue(interest);
            _interestRate.SetValue(interestRate);
            _debitInterest.SetValue(debitInterest);
            _paidDateTime.SetValue(paidDateTime);
            _updateTIme.SetValue(updateTime);
            _updatePersonId.SetValue(updatePersonId);
            _lotBalance.SetValue(lotBalance);
        }


        internal OrderInstalmentData ToOrderInstalmentData()
        {
            var result = new OrderInstalmentData()
            {
                OrderId = this.OrderId,
                Sequence = Period,
                AccountId = this.AccountId,
                InstrumentId = this.InstrumentId,
                InterestRate = this.InterestRate,
                Principal = this.Principal,
                Interest = this.Interest,
                DebitInterest = this.DebitInterest,
                PaymentDateTimeOnPlan = this.PaymentDateTimeOnPlan,
                PaidDateTime = this.PaidDateTime,
                UpdateTime = DateTime.Now,
                LotBalance = _lotBalance.Value
            };
            return result;
        }


    }
}
