using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal static class OrderExternalSourceFieldNames
    {
        internal const string Id = "ID";
        internal const string IsOpen = "IsOpen";
        internal const string IsBuy = "IsBuy";
        internal const string Code = "Code";
        internal const string OrginCode = "OrginCode";
        internal const string BlotterCode = "BlotterCode";
        internal const string Lot = "Lot";
        internal const string OriginalLot = "OriginalLot";
        internal const string LotBalance = "LotBalance";
        internal const string ExecutePrice = "ExecutePrice";
        internal const string SetPrice = "SetPrice";
        internal const string SetPrice2 = "SetPrice2";
        internal const string CommissionSum = "CommissionSum";
        internal const string LevySum = "LevySum";
        internal const string InterestPerLot = "InterestPerLot";
        internal const string StoragePerLot = "StoragePerLot";
        internal const string HitCount = "HitCount";
        internal const string BestPrice = "BestPrice";
        internal const string SetPriceMaxMovePips = "SetPriceMaxMovePips";
        internal const string DQMaxMove = "DQMaxMove";
        internal const string PlacedByRiskMonitor = "PlacedByRiskMonitor";
        internal const string Phase = "Phase";
        internal const string TradeOption = "TradeOption";
    }

    internal static class PhysicalOrderPartExternalSourceFieldNames
    {
        internal const string PhysicalTradeSide = "PhysicalTradeSide";
        internal const string PhysicalRequestId = "PhysicalRequestId";
        internal const string InstalmentPolicyId = "InstalmentPolicyId";
        internal const string InstalmentType = "InstalmentType";
        internal const string RecalculateRateType = "RecalculateRateType";
        internal const string Period = "Period";
        internal const string DownPayment = "DownPayment";
        internal const string DownPaymentBasis = "DownPaymentBasis";

    }
}
