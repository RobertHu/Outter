using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderDayHistorySaver
    {
        internal static void Save(Account account, AccountClass.Instrument instrument, TradeDayCalculator calculator, TradeDayInfo tradeDayInfo, Settings.Instrument settingInstrument)
        {
            InstrumentResetItem resetItem = CreateInstrumentResetItem(tradeDayInfo.TradeDay, instrument, calculator.ResetResults, tradeDayInfo.Settings.BuyPrice, tradeDayInfo.Settings.SellPrice);
            resetItem.ResetBalance = calculator.Balance;
            instrument.AddResetItem(tradeDayInfo.TradeDay, resetItem);
            AddOrderResetToAccount(account, calculator.ResetResults);
            AddResetResultToOrderDayHistory(account, calculator.ResetResults);
        }

        private static InstrumentResetItem CreateInstrumentResetItem(DateTime tradeDay, AccountClass.Instrument instrument, Dictionary<Guid, OrderResetResult> resetResultDict, Price buyPrice, Price sellPrice)
        {
            var result = instrument.GetResetItem(tradeDay);
            if (result == null)
            {
                result = new InstrumentResetItem(tradeDay, instrument.Owner.Id, instrument.Id);
            }
            else
            {
                result.Clear();
            }
            foreach (var eachResetResult in resetResultDict.Values)
            {
                result.FloatingPL += eachResetResult.FloatPL.Interest + eachResetResult.FloatPL.Storage + eachResetResult.TradePLFloat;
                result.Necessary += eachResetResult.Margin;
                result.InterestPLNotValued += eachResetResult.NotValuedPL.Interest;
                result.StoragePLNotValued += eachResetResult.NotValuedPL.Storage;
                result.TradePLNotValued += eachResetResult.TradePLNotValued;
            }
            result.BuyPrice = buyPrice;
            result.SellPrice = sellPrice;
            return result;
        }

        private static void AddOrderResetToAccount(Account account, Dictionary<Guid, OrderResetResult> resetResultDict)
        {
            foreach (var eachOrderResetResult in resetResultDict.Values)
            {
                var order = account.GetOrder(eachOrderResetResult.OrderId);
#if TEST
                Debug.Assert(order != null);
#endif
                account.AddOrderResetItem(new OrderResetItem(order.Id, order.IsBuy));
                AddResetBills(account, eachOrderResetResult, order);
            }
        }

        private static void AddResetBills(Account account, OrderResetResult orderResetResult, Order order)
        {
            List<BillValueAndType> valueAndTypeList = new List<BillValueAndType>
            {
                new BillValueAndType(orderResetResult.Margin, ResetBillType.Margin),
                new BillValueAndType(orderResetResult.PerLot.Interest, ResetBillType.InterestPerLot),
                new BillValueAndType(orderResetResult.PerLot.Storage, ResetBillType.StoragePerLot),
                new BillValueAndType(orderResetResult.FloatPL.Interest, ResetBillType.InterestPLFloat),
                new BillValueAndType(orderResetResult.FloatPL.Storage, ResetBillType.StoragePLFloat),
                new BillValueAndType(orderResetResult.TradePLFloat, ResetBillType.TradePLFloat),
                new BillValueAndType(orderResetResult.DayNotValuedPL.Interest, ResetBillType.DayInterestPLNotValued),
                new BillValueAndType(orderResetResult.DayNotValuedPL.Storage, ResetBillType.DayStoragePLNotValued),
                new BillValueAndType(orderResetResult.NotValuedPL.Interest, ResetBillType.InterestPLNotValued),
                new BillValueAndType(orderResetResult.NotValuedPL.Storage, ResetBillType.StoragePLNotValued),
                new BillValueAndType(orderResetResult.TradePLNotValued, ResetBillType.TradePLNotValued),
                new BillValueAndType(orderResetResult.ValuedPL.Interest, ResetBillType.InterestPLValued),
                new BillValueAndType(orderResetResult.ValuedPL.Storage, ResetBillType.StoragePLValued),
                new BillValueAndType(orderResetResult.TradePLValued, ResetBillType.TradePLValued),
                new BillValueAndType(orderResetResult.PhysicalPaidAmount, ResetBillType.PhysicalPaidAmount),
                new BillValueAndType(orderResetResult.PhysicalTradePLValued, ResetBillType.PhysicalTradePLValued),
                new BillValueAndType(orderResetResult.PhysicalTradePLNotValued, ResetBillType.PhysicalTradePLNotValued),
                new BillValueAndType(orderResetResult.PhysicalOriginValueBalance, ResetBillType.PhysicalOriginValueBalance),
                new BillValueAndType(orderResetResult.PaidPledgeBalance, ResetBillType.PaidPledgeBalance),
                new BillValueAndType(orderResetResult.InstalmentInterest, ResetBillType.InstalmentInterest),
                new BillValueAndType(orderResetResult.LotBalance, ResetBillType.LotBalance),
                new BillValueAndType(orderResetResult.FullPaymentCost, ResetBillType.FullPaymentCost),
                new BillValueAndType(orderResetResult.PledgeCost, ResetBillType.PledgeCost),
                new BillValueAndType(order.EstimateCloseCommission, ResetBillType.EstimateCloseCommission),
                new BillValueAndType(order.EstimateCloseLevy, ResetBillType.EstimateCloseLevy),
            };

            foreach (var eachBillValueAndType in valueAndTypeList)
            {
                account.AddResetBill(orderResetResult.OrderId, eachBillValueAndType.Value, eachBillValueAndType.Type, orderResetResult.TradeDay);
            }
        }



        private static void AddResetResultToOrderDayHistory(Account account, Dictionary<Guid, OrderResetResult> resetResultDict)
        {
            foreach (var eachResetResult in resetResultDict.Values)
            {
                if (eachResetResult.OrderId == Guid.Empty) continue;
                Order order = account.GetOrder(eachResetResult.OrderId);
                ResetManager.Default.AddOrderDayHistory(order, eachResetResult);
            }
        }

        private struct BillValueAndType
        {
            private decimal _value;
            private ResetBillType _type;

            internal BillValueAndType(decimal value, ResetBillType type)
            {
                _value = value;
                _type = type;
            }

            internal decimal Value
            {
                get { return _value; }
            }

            internal ResetBillType Type
            {
                get { return _type; }
            }

        }

    }
}
