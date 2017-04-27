using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Framework;
using Protocal;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using System.Diagnostics;

namespace Core.TransactionServer.Agent.Physical.InstalmentBusiness
{
    internal abstract class InstalmentBase
    {
        protected InstalmentBase(InstalmentPayOffParameter parameter)
        {
            this.Orders = parameter.Orders;
            this.Account = parameter.Account;
            this.Instrument = parameter.Instrument;
            this.BuyPrice = parameter.BuyPrice;
            this.SellPric = parameter.SellPrice;
            this.TradeDay = parameter.TradeDay;
            this.CurrencyRate = Settings.Setting.Default.GetCurrencyRate(this.Instrument.CurrencyId, this.Account.IsMultiCurrency ? this.Instrument.CurrencyId : this.Account.Setting().CurrencyId);
        }

        protected decimal _balance;
        protected decimal _equity;

        internal List<PhysicalOrder> Orders { get; private set; }
        internal Account Account { get; private set; }
        internal Price BuyPrice { get; private set; }
        internal Price SellPric { get; private set; }
        internal Settings.Instrument Instrument { get; private set; }
        internal Settings.CurrencyRate CurrencyRate { get; private set; }

        internal DateTime TradeDay { get; private set; }

        internal Settings.TradePolicyDetail TradePolicyDetail
        {
            get { return Account.Setting(this.TradeDay).TradePolicy(this.TradeDay)[Instrument.Id, this.TradeDay]; }
        }

        internal Guid CurrencyId
        {
            get { return Account.IsMultiCurrency ? Instrument.CurrencyId : Account.Setting(this.TradeDay).CurrencyId; }
        }

        internal int AccountCurrencyDecimals
        {
            get
            {
                return Account.Setting(this.TradeDay).Currency(this.TradeDay).Decimals;
            }
        }

        internal abstract void Payoff();

        protected void CalculateBalanceAndEquity()
        {
            decimal floatMargin = this.CalculateFloatingMargin();
            decimal pledge = this.CalculatePledge();
            _balance = this.CalculateBalance();
            _equity = _balance + floatMargin + pledge;
        }

        private decimal CalculateFloatingMargin()
        {
            decimal result = 0m;
            foreach (var eachOrder in Orders)
            {
                if (!this.ShouldCalculateFloating(eachOrder)) continue;
                var orderDayHistory = ResetManager.Default.GetOrderDayHistory(eachOrder.Id, TradeDay.AddDays(-1));
                decimal storagePerLot = orderDayHistory == null ? 0m : orderDayHistory.StoragePerLot;
                decimal interestPerLot = orderDayHistory == null ? 0m : orderDayHistory.InterestPerLot;
                decimal storageFloating = FloatingCalculator.CalculateOrderFloatRpt(storagePerLot, eachOrder.LotBalance, CurrencyRate.RateIn, CurrencyRate.RateOut, this.AccountCurrencyDecimals);
                decimal interestFloating = FloatingCalculator.CalculateOrderFloatRpt(interestPerLot, eachOrder.LotBalance, CurrencyRate.RateIn, CurrencyRate.RateOut, this.AccountCurrencyDecimals);
                Price livePrice = this.GetLivePrice(eachOrder.IsBuy);
                decimal tradeFloating = FloatingCalculator.CalculateOrderFloatTrade(eachOrder.LotBalance, eachOrder.Owner.ContractSize(this.TradeDay), eachOrder.IsBuy, (int)Instrument.TradePLFormula, eachOrder.ExecutePrice, livePrice, CurrencyRate.RateIn, CurrencyRate.RateOut, this.AccountCurrencyDecimals);
                result += storageFloating + interestFloating + tradeFloating;
            }
            return result;
        }

        private Price GetLivePrice(bool isBuy)
        {
            return isBuy == Instrument.IsNormal ? BuyPrice : SellPric;
        }


        private bool ShouldCalculateFloating(PhysicalOrder order)
        {
            return this.ShouldCalculateCommon(order);
        }


        private decimal CalculatePledge()
        {
            decimal result = 0m;
            foreach (var eachOrder in Orders)
            {
                if (!this.ShouldCalculatePledge(eachOrder)) continue;
                result += this.CalculateOrderPledge(eachOrder);
            }
            return result;
        }

        private decimal CalculateBalance()
        {
            var fund = Account.GetOrCreateFund(this.CurrencyId);
            return fund.Balance;
        }

        private decimal CalculateOrderPledge(PhysicalOrder order)
        {
            decimal orderDebit = this.CalculateOrderDebit(order);
            decimal marketValue = MarketValueCalculator.CalculateMarkValue(order.LotBalance, this.GetLivePrice(order.IsBuy), this.TradePolicyDetail.DiscountOfOdd, (int)Instrument.TradePLFormula, order.Owner.ContractSize(this.TradeDay));
            marketValue = marketValue.Exchange(CurrencyRate.RateIn, CurrencyRate.RateOut, this.AccountCurrencyDecimals, null);
            marketValue = marketValue * (orderDebit == 0 ? this.TradePolicyDetail.ValueDiscountAsMargin : this.TradePolicyDetail.InstalmentPledgeDiscount);
            return marketValue - orderDebit;
        }

        private decimal CalculateOrderDebit(PhysicalOrder order)
        {
            decimal result = 0m;
            var instalments = TradingSetting.Default.GetInstalments(order.Id);
            if (instalments == null) return result;
            foreach (var eachInstalment in instalments)
            {
                if (eachInstalment.PaidDateTime == null)
                {
                    result += eachInstalment.Principal;
                }
            }
            return result;
        }

        private bool ShouldCalculatePledge(PhysicalOrder order)
        {
            return this.ShouldCalculateCommon(order);
        }

        private bool ShouldCalculateCommon(PhysicalOrder order)
        {
            return order.LotBalance > 0 && order.IsOpen && (order.Phase == iExchange.Common.OrderPhase.Executed || order.Phase == iExchange.Common.OrderPhase.Completed);
        }
    }


    internal sealed class InstalmentManager : InstalmentBase
    {
        internal InstalmentManager(InstalmentPayOffParameter instalmentPayOffParameter)
            : base(instalmentPayOffParameter)
        {
        }

        internal override void Payoff()
        {
            this.CalculateBalanceAndEquity();
            var debitInstalments = this.GetAllDebitInstalmentOrders();
            if (debitInstalments != null && debitInstalments.Count > 0)
            {
                debitInstalments.Sort(new InstalmentResultComparer());
                this.FilterNotPayOffInstalments(debitInstalments);
                this.UpdatePayOffInstalments(debitInstalments);
                this.UpdateOrder(debitInstalments);
                this.UpdateAccountBalance(debitInstalments);
            }
        }

        private void UpdateAccountBalance(List<InstalmentResult> instalments)
        {
            decimal payoffAmount = 0m;
            foreach (var eachInstalment in instalments)
            {
                payoffAmount += eachInstalment.Principal + eachInstalment.Interest + eachInstalment.DebitInterest;
            }
            var fund = Account.GetOrCreateFund(this.CurrencyId);
            fund.AddBalance(-payoffAmount);
        }

        private void FilterNotPayOffInstalments(List<InstalmentResult> debitInstalments)
        {
            List<InstalmentResult> notPayOffInstalments = new List<InstalmentResult>();
            foreach (var eachInstalment in debitInstalments)
            {
                decimal orderAmount = eachInstalment.Principal + eachInstalment.Interest + eachInstalment.DebitInterest;
                if (_equity > orderAmount && _balance >= orderAmount)
                {
                    _balance -= orderAmount;
                }
                else
                {
                    notPayOffInstalments.Add(eachInstalment);
                }
            }

            foreach (var eachInstalment in notPayOffInstalments)
            {
                debitInstalments.Remove(eachInstalment);
            }
        }


        private void UpdatePayOffInstalments(List<InstalmentResult> instalments)
        {
            foreach (var eachInstalment in instalments)
            {
                var physicalOrder = (PhysicalOrder)Account.GetOrder(eachInstalment.OrderId);
                physicalOrder.UpdateInstalmentDetail(eachInstalment.Sequence, eachInstalment.Interest, eachInstalment.Principal, eachInstalment.DebitInterest, DateTime.Now, DateTime.Now,physicalOrder.LotBalance);
            }
        }

        private void UpdateOrder(List<InstalmentResult> instalments)
        {
            Dictionary<Guid, decimal> orderPerPrincipalDict = this.CalculateOrderPrincipal(instalments);
            foreach (var eachPair in orderPerPrincipalDict)
            {
                var order = (PhysicalOrder)Account.GetOrder(eachPair.Key);
                order.PaidPledgeBalance += -eachPair.Value;
            }
        }

        private Dictionary<Guid, decimal> CalculateOrderPrincipal(List<InstalmentResult> instalments)
        {
            Dictionary<Guid, decimal> result = new Dictionary<Guid, decimal>();
            foreach (var eachInstalment in instalments)
            {
                decimal principal;
                if (!result.TryGetValue(eachInstalment.OrderId, out principal))
                {
                    result.Add(eachInstalment.OrderId, eachInstalment.Principal);
                }
                else
                {
                    principal += eachInstalment.Principal;
                    result[eachInstalment.OrderId] = principal;
                }
            }
            return result;
        }


        private List<InstalmentResult> GetAllDebitInstalmentOrders()
        {
            List<InstalmentResult> result = new List<InstalmentResult>();
            var accountCurrency = Settings.Setting.Default.GetCurrency(this.Account.Setting(this.TradeDay).CurrencyId, this.TradeDay);
            var instrumentCurrency = Settings.Setting.Default.GetCurrency(this.Instrument.CurrencyId, this.TradeDay);
            foreach (var eachOrder in this.Orders)
            {
                if (eachOrder.IsOpen && eachOrder.Instalment != null && eachOrder.Instalment.InstalmentType != InstalmentType.FullAmount)
                {
                    foreach (var eachInstalment in eachOrder.Instalment.InstalmentDetails)
                    {

                        if (eachInstalment.PaymentDateTimeOnPlan <= this.TradeDay && eachInstalment.PaidDateTime == null)
                        {
                            InstalmentResult item = this.CreateInstalmentResult(this.TradeDay, eachInstalment, eachOrder, this.Account, this.Instrument, accountCurrency, instrumentCurrency);
                            result.Add(item);
                        }
                    }
                }
            }
            return result;
        }


        private InstalmentResult CreateInstalmentResult(DateTime tradeDay, InstalmentDetail instalment, PhysicalOrder order, Account account, Settings.Instrument instrument, Settings.Currency accountCurrency, Settings.Currency instrumentCurrency)
        {
            InstalmentResult result = new InstalmentResult();
            result.OrderId = instalment.OrderId;
            result.Code = order.Code;
            result.ExecuteTime = order.ExecuteTime.Value;
            result.Lot = order.Lot;
            result.LotBalance = order.LotBalance;
            result.CurrencyId = account.IsMultiCurrency ? instrumentCurrency.Id : accountCurrency.Id;
            result.Code = account.IsMultiCurrency ? instrumentCurrency.Code : accountCurrency.Code;
            result.CurrencyDecimals = account.IsMultiCurrency ? instrumentCurrency.Decimals : accountCurrency.Decimals;
            result.PaidPledge = order.PaidPledgeBalance;
            result.Sequence = instalment.Period;
            result.InterestRate = instalment.InterestRate;
            result.Principal = instalment.Principal;
            result.Interest = instalment.Interest;
            var instalmentPolicyDetail = order.Instalment.InstalmentPolicyDetail(null);
            result.DebitInterest = InstalmentManager.CalculateDebitInterest(instalment.Principal, instalment.Interest, tradeDay.GetDateDiff(instalment.PaymentDateTimeOnPlan.Value), instalmentPolicyDetail.InterestRate, instalmentPolicyDetail.DebitInterestType,
                                instalmentPolicyDetail.DebitInterestRatio, instalmentPolicyDetail.DebitFreeDays, instrument.InterestYearDays, result.CurrencyDecimals);
            result.PaymentDateTimeOnPlan = instalment.PaymentDateTimeOnPlan;
            return result;
        }

        private bool IsDebitInstalmentOrder(DateTime tradeDay, PhysicalOrder order, List<OrderInstalmentData> instalments, Account account, Guid instrumentId)
        {
            return Math.Abs(order.PaidAmount) < order.PhysicalOriginValue && order.Instalment != null && order.Instalment.InstalmentType != InstalmentType.FullAmount && this.ExistNotPaidInstalmentDetail(account.Id, instrumentId, instalments, tradeDay);
        }

        private bool ExistNotPaidInstalmentDetail(Guid accountId, Guid instrumentId, List<OrderInstalmentData> instalments, DateTime tradeDay)
        {
            foreach (var eachInstalment in instalments)
            {
                if (eachInstalment.AccountId == accountId && eachInstalment.InstrumentId == instrumentId && eachInstalment.PaymentDateTimeOnPlan != null && eachInstalment.PaymentDateTimeOnPlan <= tradeDay && eachInstalment.PaidDateTime == null)
                {
                    return true;
                }
            }
            return false;
        }


        internal static decimal CalculateDebitInterest(decimal principal, decimal interest, int debitDays, decimal interestRate, int debitInterestType, decimal
            debitInterestRatio, int debitFreeDays, int interestYearDays, int currencyDecimals)
        {
            decimal result = 0m;
            if (debitDays <= 0) return result;
            decimal dayRate = interestRate / interestYearDays;
            decimal freeDaysInterest = 0m;
            decimal debitDaysInterest = 0m;
            if (debitDays <= debitFreeDays)
            {
                freeDaysInterest = (principal + interest) * dayRate * debitDays;
            }
            else
            {
                freeDaysInterest = (principal + interest) * dayRate * debitFreeDays;
                debitDaysInterest = (principal + interest) * (dayRate * (1 + debitInterestRatio)) * (debitDays - debitFreeDays);
            }
            return (freeDaysInterest + debitDaysInterest).MathRound(currencyDecimals);
        }
    }



    internal sealed class InstalmentResult
    {
        internal Guid OrderId { get; set; }
        internal string Code { get; set; }
        internal DateTime ExecuteTime { get; set; }
        internal decimal Lot { get; set; }
        internal decimal LotBalance { get; set; }
        internal Guid CurrencyId { get; set; }
        internal string CurrencyCode { get; set; }
        internal int CurrencyDecimals { get; set; }
        internal decimal PaidPledge { get; set; }
        internal int Sequence { get; set; }
        internal decimal? InterestRate { get; set; }
        internal decimal Principal { get; set; }
        internal decimal Interest { get; set; }
        internal decimal DebitInterest { get; set; }
        internal DateTime? PaymentDateTimeOnPlan { get; set; }
    }

    internal sealed class InstalmentResultComparer : IComparer<InstalmentResult>
    {
        public int Compare(InstalmentResult x, InstalmentResult y)
        {
            if (x.ExecuteTime > y.ExecuteTime || (x.ExecuteTime == y.ExecuteTime && x.Sequence > y.Sequence))
            {
                return 1;
            }
            else if (x.ExecuteTime == y.ExecuteTime && x.Sequence == y.Sequence)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }

    internal sealed class OrderInstalmentItem
    {
        internal Guid OrderId { get; set; }
        internal int Sequence { get; set; }
        internal decimal InterestRate { get; set; }
        internal decimal Principal { get; set; }
        internal decimal Interest { get; set; }
        internal decimal DebitInterest { get; set; }
        internal Guid CurrencyId { get; set; }
        internal decimal Amount { get; set; }
        internal Guid SourceCurrencyId { get; set; }
        internal decimal SourceAmount { get; set; }
        internal DateTime ExecuteTime { get; set; }
        internal bool CanPayOff { get; set; }
    }

    internal class InstalmentPayOffParameter
    {
        internal InstalmentPayOffParameter(List<PhysicalOrder> orders, Account account, Settings.Instrument instrument, Price buyPrice, Price sellPrice, DateTime tradeDay)
        {
            this.Orders = orders;
            this.Account = account;
            this.Instrument = instrument;
            this.BuyPrice = buyPrice;
            this.SellPrice = sellPrice;
            this.TradeDay = tradeDay;
        }

        internal List<PhysicalOrder> Orders { get; private set; }
        internal Account Account { get; private set; }
        internal Settings.Instrument Instrument { get; private set; }
        internal Price BuyPrice { get; private set; }
        internal Price SellPrice { get; private set; }
        internal DateTime TradeDay { get; private set; }
    }

}
