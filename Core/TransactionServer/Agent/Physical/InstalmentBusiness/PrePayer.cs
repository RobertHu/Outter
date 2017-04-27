using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.Physical;
using Core.TransactionServer.Agent.BroadcastBLL;
using log4net;

namespace Core.TransactionServer.Agent.Physical.InstalmentBusiness
{
    internal static class PrePayer
    {
        internal static void Payoff(Account account, Guid currencyId, Guid userId, TerminateData terminateData)
        {
            PhysicalOrder order = (PhysicalOrder)account.GetOrder(terminateData.OrderId);
            if (order.PhysicalTradeSide != iExchange.Common.PhysicalTradeSide.Buy) return;
            order.PaidPledgeBalance = -order.PhysicalOriginValueBalance;
            foreach (var eachInstalmentItem in order.Instalment.InstalmentDetails)
            {
                eachInstalmentItem.UpdateByPrePay(0, 0, 0, DateTime.Now, DateTime.Now, userId, order.LotBalance);
            }
            Guid sourceCurrencyId = terminateData.SourceCurrencyId != Guid.Empty ? terminateData.SourceCurrencyId : currencyId;
            decimal value = -(terminateData.SourceAmount + terminateData.SourceTerminateFee);
            order.AddBill(new Bill(account.Id, sourceCurrencyId, value, Protocal.BillType.PrePay, BillOwnerType.Order, DateTime.Now));
            account.AddBalance(sourceCurrencyId, value, null);
            account.CalculateRiskData();
        }
    }

    internal static class InstalmentPayer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstalmentPayer));

        internal static void Payoff(Account account, Guid userId, List<InstalmentData> instaments)
        {
            foreach (var eachInstalmentData in instaments)
            {
                Logger.Info(eachInstalmentData.ToString());
                var order = (PhysicalOrder)account.GetOrder(eachInstalmentData.OrderID);
                if (order.Instalment == null) continue;
                order.UpdateInstalmentDetail(eachInstalmentData.Sequence, eachInstalmentData.InterestRate, eachInstalmentData.Interest, eachInstalmentData.Principal, eachInstalmentData.DebitInterest, eachInstalmentData.PaidDateTime, DateTime.Now, userId, eachInstalmentData.LotBalance);
            }

            foreach (var eachPair in instaments.CalculateSourceAmountPerCurrency())
            {
                Guid currencyId = eachPair.Key;
                decimal amount = eachPair.Value;
                account.AddBalance(currencyId, -amount, DateTime.Now);
            }

            foreach (var eachPair in instaments.CalculateSouceAmountPerOrderPerCurrency())
            {
                var order = (PhysicalOrder)account.GetOrder(eachPair.Key);
                Dictionary<Guid, decimal> amountPerCurrency = eachPair.Value;
                foreach (var eachCurrencyPair in amountPerCurrency)
                {
                    Guid currencyId = eachCurrencyPair.Key;
                    decimal amount = eachCurrencyPair.Value;
                    order.AddBill(new Bill(account.Id, currencyId, -amount, Protocal.BillType.Instalment, BillOwnerType.Order, DateTime.Now));
                    Logger.InfoFormat("InstalmentPayer.PayOff, accounId = {0}, currencyId = {1}, amount = {2}", account.Id, currencyId, amount);
                    order.PaidPledgeBalance += -Math.Abs(amount);
                }
            }

        }

        private static Dictionary<Guid, decimal> CalculateOrderPerPrincipals(this List<InstalmentData> instaments)
        {
            return instaments.CalculateCommon(m => m.OrderID, m => m.Principal);
        }

        private static Dictionary<Guid, decimal> CalculateSourceAmountPerCurrency(this List<InstalmentData> instaments)
        {
            return instaments.CalculateCommon(m => m.SourceCurrencyId, m => m.SourceAmount);
        }

        private static Dictionary<Guid, Dictionary<Guid, decimal>> CalculateSouceAmountPerOrderPerCurrency(this List<InstalmentData> instaments)
        {
            Dictionary<Guid, Dictionary<Guid, decimal>> result = new Dictionary<Guid, Dictionary<Guid, decimal>>();
            foreach (var eachInstalment in instaments)
            {
                Dictionary<Guid, decimal> amountPerCurrency;
                if (!result.TryGetValue(eachInstalment.OrderID, out amountPerCurrency))
                {
                    amountPerCurrency = new Dictionary<Guid, decimal>();
                    result.Add(eachInstalment.OrderID, amountPerCurrency);
                }
                decimal amount;
                if (!amountPerCurrency.TryGetValue(eachInstalment.CurrencyId, out amount))
                {
                    amountPerCurrency.Add(eachInstalment.CurrencyId, eachInstalment.SourceAmount);
                }
                else
                {
                    amountPerCurrency[eachInstalment.CurrencyId] = amount + eachInstalment.SourceAmount;
                }
            }
            return result;
        }

        private static Dictionary<Guid, decimal> CalculateCommon(this List<InstalmentData> instaments, Func<InstalmentData, Guid> keyProvider, Func<InstalmentData, decimal> valueProvider)
        {
            Dictionary<Guid, decimal> result = new Dictionary<Guid, decimal>();
            foreach (var eachInstalmentData in instaments)
            {
                decimal oldValue;
                if (!result.TryGetValue(keyProvider(eachInstalmentData), out oldValue))
                {
                    result.Add(keyProvider(eachInstalmentData), valueProvider(eachInstalmentData));
                }
                else
                {
                    result[keyProvider(eachInstalmentData)] = oldValue + valueProvider(eachInstalmentData);
                }
            }
            return result;
        }


    }


}
