using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal enum ResetBillType
    {
        None = 0,
        Margin = 12,
        InterestPerLot = 13,
        StoragePerLot = 14,
        InterestPLFloat = 18,
        StoragePLFloat = 19,
        TradePLFloat = 20,
        DayInterestPLNotValued = 24,
        DayStoragePLNotValued = 25,
        FullPaymentCost = 41,
        PledgeCost = 42,
        InterestPLNotValued = 15,
        StoragePLNotValued = 16,
        TradePLNotValued = 17,
        InterestPLValued = 21,
        StoragePLValued = 22,
        TradePLValued = 23,
        PhysicalPaidAmount = 28,
        PhysicalTradePLNotValued = 26,
        PhysicalTradePLValued = 27,
        PhysicalOriginValueBalance = 29,
        PaidPledgeBalance = 31,
        InstalmentInterest = 32,
        LotBalance = 39,
        EstimateCloseCommission = 43,
        EstimateCloseLevy = 44
    }

    internal sealed class ResetBill : BusinessRecord
    {
        private BusinessItem<DateTime> _tradeDay;
        private BusinessItem<Guid> _id;
        private BusinessItem<decimal> _value;
        private BusinessItem<ResetBillType> _type;
        private BusinessItem<DateTime> _updateTime;
        private BusinessItem<Guid> _orderId;

        internal ResetBill(Guid orderId,decimal value, ResetBillType type, DateTime tradeDay)
            : base("Bill", 20)
        {
            _id = BusinessItemFactory.Create("ID", Guid.NewGuid(), PermissionFeature.Key, this);
            _value = BusinessItemFactory.Create("Value", value, PermissionFeature.Sound, this);
            _tradeDay = BusinessItemFactory.Create("TradeDay", tradeDay, PermissionFeature.ReadOnly, this);
            _type = BusinessItemFactory.Create("Type", type, PermissionFeature.ReadOnly, this);
            _updateTime = BusinessItemFactory.Create("UpdateTime", DateTime.Now, PermissionFeature.ReadOnly, this);
            _orderId = BusinessItemFactory.Create("OrderID", orderId, PermissionFeature.Key, this);
        }

        internal DateTime TradeDay { get { return _tradeDay.Value; } }
    }

}
