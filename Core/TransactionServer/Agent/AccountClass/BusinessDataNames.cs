using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal static class BusinessRecordCollectionNames
    {
        internal static readonly string Transactions = "Transactions";
        internal static readonly string Orders = "Orders";
        internal static readonly string OrderRelations = "OrderRelations";
        internal static readonly string Funds = "Funds";
        internal static readonly string DeliveryRequests = "DeliveryRequests";
        internal static readonly string DeliveryRequestOrderRelations = "DeliveryRequestOrderRelations";
    }

    internal static class BusinessRecordNames
    {
        internal static readonly string Account = "Account";
        internal static readonly string Transaction = "Transaction";
        internal static readonly string Order = "Order";
        internal static readonly string OrderRelation = "OrderRelation";
        internal static readonly string Fund = "Fund";
        internal static readonly string DeliveryRequest = "DeliveryRequest";
        internal static readonly string DeliveryRequestOrderRelation = "DeliveryRequestOrderRelation";
    }

    internal static class AccountBusinessItemNames
    {
        internal static readonly string Id = "ID";
    }

    internal static class FundBusinessItemNames
    {
        internal static readonly string CurrencyId = "CurrencyID";
        internal static readonly string Balance = "Balance";
        internal static readonly string FrozenFund = "FrozenFund";
        internal static readonly string TotalPaidAmount = "TotalPaidAmount";
    }

    internal static class TransactionBusinessItemNames
    {
        internal static readonly string PlacedByRiskMonitor = "PlacedByRiskMonitor";
        internal static readonly string FreePlacingPreCheck = "FreePlacingPreCheck";
        internal static readonly string FreeLmtVariationCheck = "FreeLmtVariationCheck";

        internal static readonly string Id = "ID";
        internal static readonly string AccountId = "AccountID";
        internal static readonly string InstrumentId = "InstrumentID";
        internal static readonly string Code = "Code";
        internal static readonly string Type = "Type";
        internal static readonly string SubType = "SubType";
        internal static readonly string Phase = "Phase";
        internal static readonly string OrderType = "OrderType";
        internal static readonly string ContractSize = "ContractSize";
        internal static readonly string BeginTime = "BeginTime";
        internal static readonly string EndTime = "EndTime";
        internal static readonly string ExpireType = "ExpireType";
        internal static readonly string SubmitTime = "SubmitTime";
        internal static readonly string ExecuteTime = "ExecuteTime";
        internal static readonly string SubmitorId = "SubmitorID";
        internal static readonly string ApproverId = "ApproverID";
        internal static readonly string SourceOrderId = "AssigningOrderID";
        internal static readonly string InstrumentCategory = "InstrumentCategory";

        internal static readonly string SetPriceTimestamp = "SetPriceTimestamp";
    }

    internal static class OrderBusinessItemNames
    {
        internal static readonly string Id = "ID";
        internal static readonly string Code = "Code";
        internal static readonly string OriginCode = "OriginCode";
        internal static readonly string BlotterCode = "BlotterCode";
        internal static readonly string Phase = "Phase";
        internal static readonly string TradeOption = "TradeOption";
        internal static readonly string IsOpen = "IsOpen";
        internal static readonly string IsBuy = "IsBuy";
        internal static readonly string SetPrice = "SetPrice";
        internal static readonly string SetPrice2 = "SetPrice2";
        internal static readonly string SetPriceMaxMovePips = "SetPriceMaxMovePips";
        internal static readonly string PriceTimestamp = "PriceTimestamp";
        internal static readonly string DQMaxMove = "DQMaxMove";
        internal static readonly string ExecutePrice = "ExecutePrice";
        internal static readonly string Lot = "Lot";
        internal static readonly string OriginalLot = "OriginalLot";
        internal static readonly string LotBalance = "LotBalance";
        internal static readonly string InterestPerLot = "InterestPerLot";
        internal static readonly string StoragePerLot = "StoragePerLot";
        internal static readonly string CommissionSum = "CommissionSum";
        internal static readonly string InterestValueDate = "InterestValueDate";
        internal static readonly string LevySum = "LevySum";
        internal static readonly string HitCount = "HitCount";
        internal static readonly string HitStatus = "HitStatus";
        internal static readonly string BestPrice = "BestPrice";
        internal static readonly string PlacedByRiskMonitor = "PlacedByRiskMonitor";

        internal static readonly string PhysicalTradeSide = "PhysicalTradeSide";
        internal static readonly string PhysicalRequestId = "PhysicalRequestId";
        internal static readonly string PhysicalOriginValue = "PhysicalOriginValue";
        internal static readonly string PhysicalOriginValueBalance = "PhysicalOriginValueBalance";
        internal static readonly string PhysicalPaidAmount = "PhysicalPaidAmount";
        internal static readonly string PaidPledge = "PaidPledge";
        internal static readonly string PaidPledgeBalance = "PaidPledgeBalance";
        internal static readonly string InstalmentPolicyId = "InstalmentPolicyId";
        internal static readonly string PhysicalValueMatureDay = "PhysicalValueMatureDay";
        internal static readonly string Period = "Period";
        internal static readonly string InstalmentFrequence = "InstalmentFrequence";
        internal static readonly string DownPayment = "DownPayment";
        internal static readonly string PhysicalInstalmentType = "PhysicalInstalmentType";
        internal static readonly string RecalculateRateType = "RecalculateRateType";
        internal static readonly string IsInstalmentOverdue = "IsInstalmentOverdue";
        internal static readonly string DownPaymentBasis = "DownPaymentBasis";
    }

    internal static class OrderRelationBusinessItemNames
    {
        internal static readonly string OpenOrderId = "OpenOrderID";
        internal static readonly string CloseOrderId = "CloseOrderID";
        internal static readonly string ClosedLot = "ClosedLot";
        internal static readonly string CloseTime = "CloseTime";
        internal static readonly string Commission = "Commission";
        internal static readonly string Levy = "Levy";
        internal static readonly string InterestPL = "InterestPL";
        internal static readonly string StoragePL = "StoragePL";
        internal static readonly string TradePL = "TradePL";

        internal static readonly string ValueTime = "ValueTime";
        internal static readonly string Decimals = "TargetDecimals";
        internal static readonly string RateIn = "RateIn";
        internal static readonly string RateOut = "RateOut";

        internal static readonly string PhysicalValue = "PhysicalValue";
        internal static readonly string PhysicalValueMatureDate = "PhysicalValueMatureDate";
    }

    internal static class DeliveryRequestBusinessItemNames
    {
        internal static readonly string Id = "Id";
        internal static readonly string AccountId = "AccountId";
        internal static readonly string InstrumentId = "InstrumentId";
        internal static readonly string Code = "Code";
        internal static readonly string Ask = "Ask";
        internal static readonly string Bid = "Bid";
        internal static readonly string Status = "Status";
        internal static readonly string RequireQuantity = "RequireQuantity";
        internal static readonly string RequireLot = "RequireLot";
        internal static readonly string SubmitTime = "SubmitTime";
        internal static readonly string DeliveryTime = "DeliveryTime";
        internal static readonly string DeliveryAddressId = "DeliveryAddressId";
        internal static readonly string Charge = "Charge";
        internal static readonly string ChargeCurrencyId = "ChargeCurrencyId";
    }

    internal static class DeliveryRequestRelationBusinessItemNames
    {
        internal static readonly string DeliveryRequestId = "DeliveryRequestId";
        internal static readonly string OpenOrderId = "OpenOrderId";
        internal static readonly string DeliveryQuantity = "DeliveryQuantity";
        internal static readonly string DeliveryLot = "DeliveryLot";
    }
}