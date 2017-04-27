using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Diagnostics;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderResetData
    {
        internal decimal InterestPerLot { get; set; }

        internal decimal StoragePerLot { get; set; }

        internal bool IsInterestUseAccountCurrency { get; set; }

        internal int InterestYearDays { get; set; }

        internal DateTime? InterestValueDate { get; set; }

        internal decimal InstalmentRemainsAmount { get; set; }

        internal decimal InstalmentInterestRate { get; set; }

        internal decimal InstalmentInterest { get; set; }
    }

    public sealed class OrderResetResult
    {
        public Guid OrderId { get; set; }
        public DateTime TradeDay { get; set; }
        internal Guid CurrencyId { get; set; }
        internal decimal LotBalance { get; set; }
        internal decimal Margin { get; set; }
        internal InterestStorage PerLot { get; set; }
        internal Price DayClosePrice { get; set; }
        internal InterestStorage FloatPL { get; set; }
        internal decimal TradePLFloat { get; set; }
        internal InterestStorage DayNotValuedPL { get; set; }
        internal InterestStorage NotValuedPL { get; set; }
        internal decimal TradePLNotValued { get; set; }
        internal InterestStorage ValuedPL { get; set; }
        internal decimal TradePLValued { get; set; }
        internal decimal PhysicalPaidAmount { get; set; }
        internal decimal PhysicalTradePLValued { get; set; }
        internal decimal PhysicalTradePLNotValued { get; set; }
        internal decimal PaidPledgeBalance { get; set; }
        internal decimal PhysicalOriginValueBalance { get; set; }
        internal decimal InstalmentInterest { get; set; }
        internal decimal FullPaymentCost { get; set; }
        internal decimal PledgeCost { get; set; }
    }
}
