using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;

namespace Core.TransactionServer.Agent.AccountClass.InstrumentUtil
{
    internal static class PreCheckBalanceExtension
    {
        internal static decimal CalculatePreCheckBalance(this Instrument instrument)
        {
            if (!instrument.IsPhysical) return 0;
            Price buy = null;
            Account account = instrument.Owner;
            Quotation quotation = QuotationProvider.GetLatestQuotation(instrument.Id, account);
            Debug.Assert(quotation != null);
            buy = quotation.BuyOnCustomerSide;
            decimal preCheckBalance = 0m;
            TradePolicyDetail tradePolicyDetail = instrument.TradePolicyDetail;
            foreach (Transaction tran in account.GetTransactions(instrument.Id))
            {
                foreach (PhysicalOrder order in tran.Orders)
                {
                    if (order.PhysicalTradeSide == PhysicalTradeSide.Buy && order.IsOpen)
                    {
                        if (order.Phase == OrderPhase.Placed || order.Phase == OrderPhase.Placing)
                        {
                            var price = order.SetPrice == null ? buy : order.SetPrice;
                            decimal marketValue = MarketValueCalculator.CalculateValue(instrument.Setting.TradePLFormula, order.Lot, price,
                                tradePolicyDetail.DiscountOfOdd, tradePolicyDetail.ContractSize);

                            if (order.IsInstalment)
                            {
                                decimal instalmentAdministrationFee = order.CalculateInstalmentAdministrationFee(marketValue);
                                decimal downPayment = order.CalculatePaidAmount(marketValue);
                                preCheckBalance += (instalmentAdministrationFee + downPayment);
                            }
                            else
                            {
                                preCheckBalance += marketValue;
                            }
                        }
                    }
                }
            }
            return preCheckBalance;
        }
    }
}
