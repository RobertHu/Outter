using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.AccountClass;
using Protocal;
using Protocal.Physical;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Services;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Engine;

namespace Core.TransactionServer.Agent.Physical
{
    internal sealed class InstalmentFrequenceException : Exception
    {
        internal InstalmentFrequenceException(InstalmentFrequence frequence)
        {
            this.Frequence = frequence;
        }

        internal InstalmentFrequence Frequence { get; private set; }
    }


    internal sealed class PhysicalOrder : Order
    {
        private sealed class InstalmentDetailManager
        {
            private Instalment _instalment;
            private PhysicalOrder _order;
            private static readonly DateTime MAXDATE = new DateTime(9999, 12, 31);

            internal InstalmentDetailManager(PhysicalOrder order, Instalment instalment)
            {
                _order = order;
                _instalment = instalment;
            }

            internal void Add(InstalmentDetail detail, OperationType operationType)
            {
                Debug.Assert(_instalment != null);
                _instalment.AddDetail(detail, operationType);
                TradingSetting.Default.AddOrderInstalment(detail.ToOrderInstalmentData());
            }

            internal void DelateAll()
            {
                if (_instalment == null) return;
                List<InstalmentDetail> toBeRemovedDetails = new List<InstalmentDetail>();
                foreach (var eachInstalmentDetail in _instalment.InstalmentDetails)
                {
                    if (eachInstalmentDetail.IsDeleted || eachInstalmentDetail.PaidDateTime != null) continue;
                    toBeRemovedDetails.Add(eachInstalmentDetail);
                }

                foreach (var eachIntalmentDetail in toBeRemovedDetails)
                {
                    this.Delete(eachIntalmentDetail);
                }
            }


            internal void Delete(InstalmentDetail detail)
            {
                _instalment.DeleteDetail(detail);
                TradingSetting.Default.DeleteOrderInstalment(detail.OrderId, detail.Period);
            }

            internal void Add(Protocal.Physical.OrderInstalmentData data)
            {
                if (_instalment == null) return;
                var detail = new InstalmentDetail(_order, data);
                _instalment.AddDetail(detail, OperationType.None);
            }


            internal void Update(int sequence, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime,decimal lotBalance)
            {
                _instalment.UpdateDetail(sequence, interest, principal, debitInterest, paidDateTime, updateTime, lotBalance);
                TradingSetting.Default.UpdateOrderInstalment(_order.Id, sequence, interest, principal, debitInterest, paidDateTime, updateTime);
            }

            internal void Update(int sequence, decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
            {
                var detail = _instalment.GetDetail(sequence);
                if (detail.PaidDateTime != null) return;
                _instalment.UpdateDetail(sequence, interestRate, interest, principal, debitInterest, paidDateTime, updateTime, updatePersonId, lotBalance);
                TradingSetting.Default.UpdateOrderInstalment(_order.Id, sequence, interestRate, interest, principal, debitInterest, paidDateTime, updateTime, updatePersonId, lotBalance);
            }

            internal void Generate(DateTime tradeDay)
            {
                Debug.Assert(_instalment != null);
                InstalmentDetailCalculator.Decimals = 2;
                decimal instalmentAmount = _order.PhysicalOriginValueBalance - Math.Abs(_order.PaidPledgeBalance);
                int period = _instalment.InstalmentPolicy(null).IsDownPayAsFirstPay ? (_instalment.Period - 1) : _instalment.Period;
                if (_instalment.Frequence == InstalmentFrequence.TillPayoff)
                {
                    _order.AddInstalmentDetail(new InstalmentDetail(_order, 1, instalmentAmount, 0m, 0m, MAXDATE, null), OperationType.AsNewRecord);
                }
                else
                {
                    int totalPeriod = InstalmentCalculator.Default.CalculateInstalmentQuantity(period, _instalment.Frequence);
                    this.Generate(instalmentAmount, totalPeriod, tradeDay);
                }
            }

            internal void Generate(decimal instalmentAmount, int totalPeriod, DateTime tradeDay)
            {
                this.Generate(_instalment.InstalmentPolicyDetail(null), instalmentAmount, totalPeriod, tradeDay);
            }

            internal void Generate(Settings.InstalmentPolicyDetail instalmentPolicyDetail, decimal instalmentAmount, int totalPeriod, DateTime tradeDay, int sequenceOffset = 1)
            {
                decimal monthRate = this.CalculateMonthRate(instalmentPolicyDetail.InterestRate, _instalment.Frequence);
                decimal remainAmount = instalmentAmount;
                decimal principle;
                decimal interest;
                for (int period = 0; period < totalPeriod; period++)
                {
                    if (this.IsInstalmentDetailExist(period + 1)) continue;
                    if (_instalment.InstalmentType == InstalmentType.EqualPrincipal || monthRate == 0m)
                    {
                        principle = InstalmentDetailCalculator.Round(instalmentAmount / totalPeriod);
                        interest = InstalmentDetailCalculator.Round(remainAmount * monthRate);
                    }
                    else
                    {
                        var item = this.CalculateEqualInstalmentPrincipleAndInterest(monthRate, period, totalPeriod, instalmentAmount, remainAmount);
                        principle = item.Item1;
                        interest = item.Item2;
                    }
                    remainAmount -= principle;
                    if (period == totalPeriod - 1 && remainAmount != 0)
                    {
                        principle += remainAmount;
                        remainAmount = 0m;
                    }
                    DateTime dateTimeOnPlan = this.CalculateDateTimeOnPlan(tradeDay, _instalment.Frequence, period);
                    _order.AddInstalmentDetail(new InstalmentDetail(_order, (sequenceOffset - 1) + period + 1, principle, interest, 0m, dateTimeOnPlan, null), OperationType.AsNewRecord);

                }
            }


            internal bool IsInstalmentDetailExist(int period)
            {
                if (_instalment.InstalmentDetails == null) return false;
                foreach (var eachInstalmentDetail in _instalment.InstalmentDetails)
                {
                    if (eachInstalmentDetail.Period == period && eachInstalmentDetail.PaidDateTime != null) return true;
                }
                return false;
            }



            private DateTime CalculateDateTimeOnPlan(DateTime tradeDay, InstalmentFrequence frequence, int period)
            {
                DateTime result;
                int i = period + 1;
                if (frequence == InstalmentFrequence.Month)
                {
                    result = tradeDay.AddMonths(i);
                }
                else if (frequence == InstalmentFrequence.Season)
                {
                    result = tradeDay.AddMonths(i * 3);
                }
                else if (frequence == InstalmentFrequence.TwoWeek)
                {
                    result = tradeDay.AddDays(i * 2 * 7);
                }
                else if (frequence == InstalmentFrequence.Year)
                {
                    result = tradeDay.AddYears(i);
                }
                else
                {
                    throw new InstalmentFrequenceException(frequence);
                }
                var dayOfWeek = result.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Sunday)
                {
                    result = result.AddDays(1);
                }
                else if (dayOfWeek == DayOfWeek.Saturday)
                {
                    result = result.AddDays(2);
                }

                return result;
            }

            private Tuple<decimal, decimal> CalculateEqualInstalmentPrincipleAndInterest(decimal monthRate, int period, int totalPeriod, decimal amount, decimal remainAmount)
            {
                double x = 1 + (double)monthRate;
                decimal p1 = (decimal)Math.Pow(x, (period + 1));
                decimal p2 = (decimal)Math.Pow(x, period);
                decimal p3 = (decimal)Math.Pow(x, totalPeriod) - 1;
                decimal principle = InstalmentDetailCalculator.Round((p1 - p2) / p3 * amount);
                decimal interest = InstalmentDetailCalculator.Round(remainAmount * monthRate);
                return Tuple.Create(principle, interest);
            }

            private decimal CalculateMonthRate(decimal interestRate, InstalmentFrequence frequence)
            {
                // @rate / (CASE @frequence WHEN @FREQUENCE_MONTH THEN 12.0 WHEN @FREQUENCE_QUARTER THEN 4.0 ELSE 26.0 END)
                decimal dividend;
                if (frequence == InstalmentFrequence.Month)
                {
                    dividend = 12m;
                }
                else if (frequence == InstalmentFrequence.Season)
                {
                    dividend = 4m;
                }
                else
                {
                    dividend = 26m;
                }
                return interestRate / dividend;
            }

        }

        private Instalment _instalment;
        private PhysicalOrderSettings _physicalSettings;
        private InstalmentDetailManager _instalmentDetailManager;

        #region Constructors
        internal PhysicalOrder(Transaction owner, PhysicalOrderConstructParams constructParams, OrderServiceFactoryBase factory)
            : base(owner, constructParams, factory)
        {
            _physicalSettings = (PhysicalOrderSettings)_orderSettings;
            _instalment = _physicalSettings.Instalment;
            _instalmentDetailManager = new InstalmentDetailManager(this, _instalment);
        }

        #endregion

        #region Properties
        internal decimal DeliveryLockLot
        {
            get { return _physicalSettings.DeliveryModel.DeliveryLockLot; }
            private set { _physicalSettings.DeliveryModel.DeliveryLockLot = value; }
        }

        internal override decimal LotBalanceReal
        {
            get
            {
                return this.LotBalance + this.DeliveryLockLot;
            }
        }

        internal Guid? PhysicalRequestId
        {
            get { return _physicalSettings.PhysicalRequestId; }
        }

        internal bool IsInstalment
        {
            get { return _instalment != null; }
        }

        internal Instalment Instalment
        {
            get { return _instalment; }
        }

        internal decimal OverdueCutPenalty
        {
            get { return this.GetBillValue(BillType.OverdueCutPenalty); }
        }

        internal decimal ClosePenalty
        {
            get { return this.GetBillValue(BillType.ClosePenalty); }
        }

        internal decimal FrozenFund
        {
            get { return this.GetBillValue(BillType.FrozenFund); }
        }

        internal decimal PayBackPledge
        {
            get { return this.GetBillValue(BillType.PayBackPledge); }
        }

        internal decimal PaidAmount
        {
            get { return this.CalculatePaidAmount(); }
        }

        internal decimal InstalmentAdministrationFee
        {
            get { return this.GetBillValue(BillType.InstalmentAdministrationFee); }
        }

        internal PhysicalType PhysicalType
        {
            get { return _physicalSettings.PhysicalType; }
        }

        internal override bool IsRisky
        {
            get
            {
                bool buySide = this.PhysicalTradeSide == PhysicalTradeSide.Buy;
                bool shortSellSide = this.PhysicalTradeSide == PhysicalTradeSide.ShortSell;
                return (buySide && !this.IsPayoff) || shortSellSide;
            }
        }

        internal override bool IsFreeOfNecessaryCheck
        {
            get
            {
                return this.PhysicalTradeSide == PhysicalTradeSide.Buy && (!this.IsInstalment || this.Frequence != InstalmentFrequence.TillPayoff);
            }
        }

        internal decimal PhysicalOriginValue
        {
            get { return _physicalSettings.PhysicalOriginValue; }
            private set { _physicalSettings.PhysicalOriginValue = value; }
        }

        internal decimal PhysicalOriginValueBalance
        {
            get { return _physicalSettings.PhysicalOriginValueBalance; }
            set { _physicalSettings.PhysicalOriginValueBalance = value; }
        }

        internal decimal PaidPledge
        {
            get { return this.GetBillValue(BillType.PaidPledge); }
        }

        internal decimal PaidPledgeBalance
        {
            get { return _physicalSettings.PaidPledgeBalance; }
            set { _physicalSettings.PaidPledgeBalance = value; }
        }

        internal int Period
        {
            get { return _instalment.Period; }
        }

        internal InstalmentFrequence Frequence
        {
            get { return _instalment.Frequence; }
        }


        internal PhysicalTradeSide PhysicalTradeSide
        {
            get { return _physicalSettings.PhysicalTradeSide; }
            set { _physicalSettings.PhysicalTradeSide = value; }
        }

        internal int PhysicalValueMatureDay
        {
            get { return _physicalSettings.PhysicalValueMatureDay; }
        }

        internal bool IsPayoff
        {
            get { return Math.Abs(this.PaidPledgeBalance) == this.PhysicalOriginValueBalance; }
        }


        internal bool IsPartialPaymentPhysicalOrder
        {
            get
            {
                bool isBuySide = this.PhysicalTradeSide == PhysicalTradeSide.Buy;
                bool isShortSell = this.PhysicalTradeSide == PhysicalTradeSide.ShortSell;
                return (isBuySide && !this.IsPayoff) || (isShortSell && this.PaidPledgeBalance != 0);
            }
        }

        internal decimal RemainAmount
        {
            get { return this.PhysicalOriginValueBalance - Math.Abs(this.PaidPledgeBalance); }
        }

        internal bool IsDelivery
        {
            get { return this.PhysicalTradeSide == iExchange.Common.PhysicalTradeSide.Delivery; }
        }


        internal decimal ValueAsMargin
        {
            get
            {
                return this.PhysicalFloating.ValueAsMargin;
            }
        }

        internal decimal MarketValue
        {
            get { return this.PhysicalFloating.MarketValue; }
        }

        internal PhysicalFloating PhysicalFloating
        {
            get
            {
                return _openOrderCalculator.Value.FloatPLCalculator as PhysicalFloating;
            }
        }


        internal decimal PhysicalPaymentDiscount
        {
            get { return this.GetBillValue(BillType.PhysicalPaymentDiscount); }
        }

        internal override bool ShouldCalculateAutoPrice
        {
            get
            {
                return base.ShouldCalculateAutoPrice && (this.Instalment == null || this.IsPayoff);
            }
        }

        internal IEnumerable<InstalmentDetail> InstalmentDetails
        {
            get { return this.Instalment == null ? null : this.Instalment.InstalmentDetails; }
        }

        #endregion

        internal void LockForDelivery(decimal deliveryLot)
        {
            _physicalSettings.DeliveryModel.LockForDelivery(deliveryLot);
        }


        internal void AddInstalmentDetail(InstalmentDetail detail, OperationType operationType)
        {
            _instalmentDetailManager.Add(detail, operationType);
        }

        internal void DeleteInstalmentDetail(InstalmentDetail detail)
        {
            _instalmentDetailManager.Delete(detail);
        }

        internal void DeleteAllInstalmentDetail()
        {
            _instalmentDetailManager.DelateAll();
        }

        internal void AddInstalmentDetail(Protocal.Physical.OrderInstalmentData data)
        {
            _instalmentDetailManager.Add(data);
        }

        internal void UpdateInstalmentDetail(int sequence, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, decimal lotBalance)
        {
            _instalmentDetailManager.Update(sequence, interest, principal, debitInterest, paidDateTime, updateTime, LotBalance);
        }

        internal void UpdateInstalmentDetail(int sequence, decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
        {
            _instalmentDetailManager.Update(sequence, interestRate, interest, principal, debitInterest, paidDateTime, updateTime, updatePersonId, lotBalance);
        }

        internal void GenerateInstalmentDetails(DateTime tradeDay)
        {
            _instalmentDetailManager.Generate(tradeDay);
        }

        internal void GenerateInstalmentDetails(decimal instalmentAmount, int totalPeriod, DateTime tradeDay)
        {
            _instalmentDetailManager.Generate(instalmentAmount, totalPeriod, tradeDay);
        }

        internal void GenerateInstalmentDetails(Settings.InstalmentPolicyDetail instalmentPolicyDetail, decimal instalmentAmount, int totalPeriod, DateTime tradeDay, int sequenceOffset)
        {
            _instalmentDetailManager.Generate(instalmentPolicyDetail, instalmentAmount, totalPeriod, tradeDay, sequenceOffset);
        }

        internal override bool CanBeClosed()
        {
            if (!this.IsOpen) return false;
            if (!this.IsInstalment || this.IsPayoff)
            {
                return true;
            }
            return _instalment.CanBeClosed;
        }

        internal override decimal SumFee()
        {
            return base.SumFee() + this.InstalmentAdministrationFee;
        }

        internal decimal CalculatePaidAmountForPledge()
        {
            return _instalment.CalculatePaidAmount();
        }


        internal decimal CalculatePaidAmountForPledge(decimal marketValue)
        {
            return _instalment.CalculatePaidAmount(marketValue);
        }

        internal decimal CalculateInstalmentAdministrationFee(ExecuteContext context)
        {
            if (_instalment == null) return 0m;
            return _instalment.CalculateInstalmentAdministrationFee(context);
        }

        internal decimal CalculateInstalmentAdministrationFee(decimal marketValue)
        {
            return _instalment.CalculateInstalmentAdministrationFee(marketValue, ExecuteContext.Empty);
        }

        internal decimal CalculateClosePenalty(Order closeOrder, decimal closedLot, ExecuteContext context)
        {
            return _instalment.CalculateClosePenalty(closeOrder, closedLot, context);
        }

        internal decimal CalculateOverdueCutPenalty(Order closeOrder, decimal closedLot, ExecuteContext context)
        {
            return _instalment.CalculateOverdueCutPenalty(closeOrder, closedLot, context);
        }

        internal override void UpdateLotBalance(decimal lot, bool isForDelivery)
        {
            if (isForDelivery)
            {
                var physicalOpenOrderCalculator = _openOrderCalculator.Value as PhysicalOpenOrderCalculator;
                physicalOpenOrderCalculator.UpdateLotBalanceForDelivery(lot);
            }
            else
            {
                base.UpdateLotBalance(lot, isForDelivery);
            }
        }


        internal void CalculateOriginValue(ExecuteContext context)
        {
            var tran = this.Owner;
            var instrument = tran.SettingInstrument();
            decimal originValue = MarketValueCalculator.CalculateValue(instrument.TradePLFormula, this.Lot, this.ExecutePrice, tran.TradePolicyDetail(context.TradeDay).DiscountOfOdd, tran.ContractSize(context.TradeDay));
            this.PhysicalOriginValue = this.IsBuy ? tran.CurrencyRate(null).Exchange(originValue, ExchangeDirection.RateOut) : tran.CurrencyRate(null).Exchange(originValue, ExchangeDirection.RateIn);
            if (this.IsOpen)
            {
                this.PhysicalOriginValueBalance = this.PhysicalOriginValue;
            }
            else
            {
                this.CalculateFrozenLotAndFund(context);
            }
        }

        internal bool CanPrepayment()
        {
            if (this.Instalment == null)
            {
                return false;
            }
            InstalmentPolicy instalmentPolicy = this.Instalment.InstalmentPolicy(null);
            if (instalmentPolicy.AdvancePaymentOption == AdvancePaymentOption.AllowAll
                || (instalmentPolicy.AdvancePaymentOption == AdvancePaymentOption.AllowInstalment && this.Instalment.Frequence != InstalmentFrequence.TillPayoff)
                || (instalmentPolicy.AdvancePaymentOption == AdvancePaymentOption.AllowPrepayment && this.Instalment.Frequence == InstalmentFrequence.TillPayoff))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CalculateFrozenLotAndFund(ExecuteContext context)
        {
            var frozenLot = this.CalculateFrozenLot();
            var frozenFund = this.CalculateFrozenFund(frozenLot);
            this.AssignFrozenAndUnFrozenPhysicalValue(frozenFund, frozenLot);
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, frozenFund, BillType.FrozenFund, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));

        }

        private void AssignFrozenAndUnFrozenPhysicalValue(decimal frozenFund, decimal frozenLot)
        {
            Debug.Assert(!this.IsOpen);
            var physicalCloseOrderCalculator = _closeOrderCalculator.Value as PhysicalCloseOrderCalculator;
            physicalCloseOrderCalculator.AssignFrozenAndUnFrozenPhysicalValue(frozenFund, frozenLot);
        }

        private decimal CalculateFrozenFund(decimal frozenLot)
        {
            if (frozenLot == this.Lot) return this.PhysicalOriginValue;
            return Math.Ceiling((frozenLot / this.Lot) * this.PhysicalOriginValue);
        }

        private decimal CalculateFrozenLot()
        {
            decimal frozenLot = 0;
            foreach (PhysicalOrderRelation orderRelation in this.OrderRelations)
            {
                var openOrder = orderRelation.OpenOrder as PhysicalOrder;
                if (this.ShouldFrozen(openOrder))
                {
                    frozenLot += orderRelation.ClosedLot;
                }
            }
            return frozenLot;
        }

        private bool ShouldFrozen(PhysicalOrder openOrder)
        {
            return this.PhysicalTradeSide != PhysicalTradeSide.Delivery && openOrder.PhysicalTradeSide == PhysicalTradeSide.Deposit && openOrder.PhysicalValueMatureDay > 0;
        }

        internal void CalculatePaidPledge(ExecuteContext context)
        {
            var currencyRate = this.Owner.CurrencyRate(context.TradeDay);
            decimal paidPledge = 0m;
            decimal paidPledgeBalance = 0m;
            if (this.PhysicalTradeSide == PhysicalTradeSide.Buy)
            {
                var paidPledgeTuple = this.CalculatePaidPledgeForBuyTradeSide(currencyRate, context);
                paidPledge = paidPledgeTuple.Item1;
                paidPledgeBalance = paidPledgeTuple.Item2;
            }
            else if (this.PhysicalTradeSide == PhysicalTradeSide.Deposit)
            {
                if (this.IsOpen)
                {
                    paidPledge = paidPledgeBalance = -this.PhysicalOriginValue;
                }
                else
                {
                    paidPledge = this.PhysicalOriginValue;
                    paidPledgeBalance = -this.PhysicalOriginValue;
                }
            }
            else if (this.PhysicalTradeSide == PhysicalTradeSide.ShortSell && this.Owner.TradePolicyDetail(context.TradeDay).ShortSellDownPayment > 0)
            {
                if (this.IsOpen)
                {
                    paidPledge = paidPledgeBalance = this.CaculatePaidPledgeForShortSell(this.Owner.TradePolicyDetail(context.TradeDay).ShortSellDownPayment, context.TradeDay);
                }
            }
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, paidPledge, BillType.PaidPledge, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            _physicalSettings.PaidPledgeForDB = paidPledge;
            this.PaidPledgeBalance = paidPledgeBalance;
        }

        private Tuple<decimal, decimal> CalculatePaidPledgeForBuyTradeSide(CurrencyRate currencyRate, ExecuteContext context)
        {
            decimal paidPledge = 0m, paidPledgeBalance = 0m;
            AllowedPaymentForm form = AllowedPaymentForm.Fullpayment;
            if (this.IsInstalment)
            {
                decimal paidAmount = this.CalculatePaidAmountForPledge();
                form = this.Instalment.Frequence == InstalmentFrequence.TillPayoff ? AllowedPaymentForm.AdvancePayment : AllowedPaymentForm.Instalment;
                this.CalculatePhysicalPaymentDiscount(form, currencyRate, context);
                paidPledge = paidPledgeBalance = -(Math.Round(paidAmount, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero) + this.PhysicalPaymentDiscount);
            }
            else
            {
                this.CalculatePhysicalPaymentDiscount(form, currencyRate, context);
                if (this.IsOpen)
                {
                    paidPledge = paidPledgeBalance = -this.PhysicalOriginValue;
                }
            }
            return Tuple.Create(paidPledge, paidPledgeBalance);
        }

        private void CalculatePhysicalPaymentDiscount(AllowedPaymentForm form, CurrencyRate currencyRate, ExecuteContext context)
        {
            var tradePolicyDetail = this.Owner.TradePolicyDetail(context.TradeDay);
            var physicalPaymentDiscountPolicy = tradePolicyDetail.PhysicalPaymentDiscountPolicy(context.TradeDay);

            if (physicalPaymentDiscountPolicy != null)
            {
                var physicalPaymentDiscount = physicalPaymentDiscountPolicy.CalculateDiscount(this.Lot, this.PhysicalOriginValue, form, currencyRate);
                this.AddBill(new Bill(this.AccountId, this.CurrencyId, physicalPaymentDiscount, BillType.PhysicalPaymentDiscount, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            }

        }

        private decimal CaculatePaidPledgeForShortSell(decimal shortSellDownPayment, DateTime? tradeDay)
        {
            var tran = this.Owner;
            CurrencyRate currencyRate = tran.CurrencyRate(tradeDay);
            decimal contractSize = tran.ContractSize(tradeDay);
            decimal lotBalance = this.LotBalance;
            Price price = this.ExecutePrice;
            decimal paidPledge = 0;
            switch (tran.SettingInstrument().MarginFormula)
            {
                case MarginFormula.FixedAmount:
                    paidPledge = lotBalance * shortSellDownPayment;
                    paidPledge = -Math.Round(paidPledge, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
                case MarginFormula.CS:
                    paidPledge = lotBalance * contractSize * shortSellDownPayment;
                    paidPledge = currencyRate.Exchange(-paidPledge);
                    break;
                case MarginFormula.CSiPrice:
                case MarginFormula.CSiMarketPrice:
                    paidPledge = lotBalance * contractSize * shortSellDownPayment / (decimal)price;
                    paidPledge = currencyRate.Exchange(-paidPledge);
                    break;
                case MarginFormula.CSxPrice:
                case MarginFormula.CSxMarketPrice:
                    paidPledge = lotBalance * contractSize * (decimal)price * shortSellDownPayment;
                    paidPledge = currencyRate.Exchange(-paidPledge);
                    break;

                // TODO: Process marginFormula 4-FKLI,5-FCPO for bursa instrument when calculate on Order.
                case MarginFormula.FKLI:
                case MarginFormula.FCPO:
                    paidPledge = lotBalance * shortSellDownPayment;
                    paidPledge = -Math.Round(paidPledge, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    break;
            }
            return paidPledge;
        }

        internal void CalculatePenalty(ExecuteContext context)
        {
            decimal closePenalty = 0m;
            decimal overdueCutPenalty = 0m;
            foreach (PhysicalOrderRelation orderRelation in this.OrderRelations)
            {
                closePenalty += orderRelation.CalculateClosePenalty(context);
                overdueCutPenalty += orderRelation.CalculateOverdueCutPenalty(context);
            }
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, -closePenalty, BillType.ClosePenalty, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            this.AddBill(new Bill(this.AccountId, this.CurrencyId, -overdueCutPenalty, BillType.OverdueCutPenalty, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
        }

        internal override IFees CalculateFee(ExecuteContext context)
        {
            var fees = base.CalculateFee(context);
            if (this.Instalment != null)
            {
                var physicalFees = fees as PhysicalOrderFees;
                Debug.Assert(physicalFees != null);
                this.AddBill(new Bill(this.AccountId, this.CurrencyId, -physicalFees.InstalmentAdministrationFee, BillType.InstalmentAdministrationFee, BillOwnerType.Order, context.ExecuteTime ?? DateTime.Now));
            }
            return fees;
        }

        internal override void CalculateInit()
        {
            base.CalculateInit();
            var fund = this.Owner.Owner.GetOrCreateFund(this.Owner.CurrencyId);
            fund.AddFrozenFund(this.FrozenFund);
        }


        internal void PayOff(DateTime? tradeDay)
        {
            this.PaidPledgeBalance = -this.PhysicalOriginValueBalance;
            if (!this.IsPartialPaymentPhysicalOrder)
            {
                //this.AddBill(new TransactionBill(this.AccountId, -this.Necessary, BillType.Margin, BillOwnerType.Order));
                //this.AddBill(new TransactionBill(this.AccountId, -this.TradePLFloat, BillType.TradePLFloat, BillOwnerType.Order));
                //this.AddBill(new TransactionBill(this.AccountId, -this.PaidAmount, BillType.PhysicalPaidAmount, BillOwnerType.Order));
                this.PhysicalFloating.CalculateNecessary(this.LotBalance);
                this.PhysicalFloating.CalculateTradePL(this.Owner.AccountInstrument.GetQuotation());
                decimal marketAsValue = 0m;
                MarketValueCalculator.CalculateMarketValue(this, this.ExecutePrice, tradeDay, out  marketAsValue);
                //this.AddBill(new TransactionBill(this.AccountId, this.Necessary, BillType.Margin, BillOwnerType.Order));
                //this.AddBill(new TransactionBill(this.AccountId, this.TradePLFloat, BillType.TradePLFloat, BillOwnerType.Order));
                //this.AddBill(new TransactionBill(this.AccountId, marketAsValue, BillType.PhysicalMarketAsValue, BillOwnerType.Order));
            }
        }

        internal decimal CalculatePaidAmount()
        {
            decimal paidAmount = 0;
            if (this.PhysicalTradeSide == PhysicalTradeSide.Buy)
            {
                if (this.IsOpen && !this.IsPayoff)
                {
                    paidAmount = -this.PaidPledgeBalance;
                }
            }
            else if (this.PhysicalTradeSide == PhysicalTradeSide.ShortSell && this.PaidPledgeBalance != 0)
            {
                paidAmount = -this.PaidPledgeBalance;
            }
            return paidAmount;
        }


        protected override void InnerCancelExecute(bool isForDelivery)
        {
            base.InnerCancelExecute(this.PhysicalTradeSide == iExchange.Common.PhysicalTradeSide.Delivery);
            this.PhysicalOriginValue = 0m;
            this.PhysicalOriginValueBalance = 0m;
            this.PaidPledgeBalance = 0m;
        }

    }

    [Flags]
    public enum AllowedPaymentForm
    {
        Fullpayment = 1,
        AdvancePayment = 2,
        Instalment = 4
    }

}
