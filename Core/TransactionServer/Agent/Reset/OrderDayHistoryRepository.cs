using Core.TransactionServer.Agent.DB.DBMapping;
using Core.TransactionServer.Agent.Util;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class OrderDayHistoryRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderDayHistoryRepository));

        private struct AccountInstrumentResetKey : IEquatable<AccountInstrumentResetKey>
        {
            private Guid _accountId;
            private Guid _instrumentId;
            private int? _hashValue;

            internal AccountInstrumentResetKey(Guid accountId, Guid instrumentId)
            {
                _accountId = accountId;
                _instrumentId = instrumentId;
                _hashValue = null;
            }

            internal Guid AccountId
            {
                get { return _accountId; }
            }

            internal Guid InstrumentId
            {
                get { return _instrumentId; }
            }

            public bool Equals(AccountInstrumentResetKey other)
            {
                return this.AccountId.Equals(other.AccountId) && this.InstrumentId.Equals(other.InstrumentId);
            }

            public override bool Equals(object obj)
            {
                return this.Equals((AccountInstrumentResetKey)obj);
            }

            public override int GetHashCode()
            {
                if (_hashValue == null)
                {
                    _hashValue = HashCodeGenerator.Calculate(this.AccountId.GetHashCode(), this.InstrumentId.GetHashCode());
                }
                return _hashValue.Value;
            }

        }

        private Dictionary<Guid, Dictionary<DateTime, OrderDayHistory>> _historyOrdersPerOrderDict = new Dictionary<Guid, Dictionary<DateTime, OrderDayHistory>>();

        private object _mutex = new object();

        public void AddOrderDayHistory(Order order, OrderResetResult resetResult)
        {
            lock (_mutex)
            {
                var model = this.CreateOrderDayHistory(order, resetResult);
                this.Add(model);
            }
        }

        private OrderDayHistory CreateOrderDayHistory(Order order, OrderResetResult resetResult)
        {
            OrderDayHistory result = new OrderDayHistory();
            result.TradeDay = resetResult.TradeDay;
            Debug.Assert(order != null);
            result.OrderID = order.Id;
            result.InstrumentID = order.Instrument().Id;
            result.AccountID = order.AccountId;
            result.DayInterestPLNotValued = resetResult.DayNotValuedPL.Interest;
            result.DayStoragePLNotValued = resetResult.DayNotValuedPL.Storage;
            result.LotBalance = order.LotBalance;
            result.StoragePerLot = resetResult.PerLot.Storage;
            result.InterestPerLot = resetResult.PerLot.Interest;
            result.InterestPLValued = resetResult.ValuedPL.Interest;
            result.StoragePLValued = resetResult.ValuedPL.Storage;
            result.TradePLValued = resetResult.TradePLValued;
            result.InterestPLFloat = resetResult.FloatPL.Interest;
            result.StoragePLFloat = resetResult.FloatPL.Storage;
            result.TradePLFloat = resetResult.TradePLFloat;
            result.CurrencyID = resetResult.CurrencyId;
            return result;
        }


        internal void LoadOrderDayHistorys()
        {
            lock (_mutex)
            {
                var reader = DB.DBRepository.Default.GetOrderDayHistorys(null);
                while (reader.Read())
                {
                    this.Add(new OrderDayHistory(new DBReader(reader)));
                }
            }
        }



        internal void Add(OrderDayHistory model)
        {
            lock (_mutex)
            {
                Dictionary<DateTime, OrderDayHistory> historyOrders;
                if (!_historyOrdersPerOrderDict.TryGetValue(model.OrderID, out historyOrders))
                {
                    historyOrders = new Dictionary<DateTime, OrderDayHistory>(10);
                    _historyOrdersPerOrderDict.Add(model.OrderID, historyOrders);
                }
                if (!historyOrders.ContainsKey(model.TradeDay))
                {
                    historyOrders.Add(model.TradeDay, model);
                }
            }
        }

        internal OrderDayHistory GetOrderDayHistory(Guid orderId, DateTime tradeDay)
        {
            lock (_mutex)
            {
                if (!_historyOrdersPerOrderDict.ContainsKey(orderId))
                {
                    return null;
                }
                return this.Get(orderId, tradeDay);
            }
        }


        internal List<OrderDayHistory> GetOrderDayHistorys(List<KeyValuePair<Guid, DateTime?>> orders)
        {
            lock (_mutex)
            {
                List<OrderDayHistory> result = new List<OrderDayHistory>(orders.Count);
                foreach (var eachOrder in orders)
                {
                    var item = this.Get(eachOrder.Key, eachOrder.Value.Value);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
                return result;
            }
        }

        private OrderDayHistory Get(Guid orderId, DateTime tradeDay)
        {
            Dictionary<DateTime, OrderDayHistory> historyOrders;
            if (_historyOrdersPerOrderDict.TryGetValue(orderId, out historyOrders))
            {
                OrderDayHistory result;
                if (historyOrders.TryGetValue(tradeDay, out result))
                {
                    return result;
                }
            }
            return null;
        }

        internal Dictionary<DateTime, OrderDayHistory> GetOrderDayHistorysByOrderId(Guid orderId)
        {
            lock (_mutex)
            {
                Dictionary<DateTime, OrderDayHistory> result = null;
                if (!_historyOrdersPerOrderDict.TryGetValue(orderId, out result))
                {
                    return null;
                }
                _historyOrdersPerOrderDict.TryGetValue(orderId, out result);
                return result;
            }
        }

        internal List<OrderDayHistory> GetOrderDayHistorys(List<Guid> orders, DateTime tradeDay)
        {
            lock (_mutex)
            {
                return this.GetOrderDayHistoryOrdersDirectly(orders, tradeDay);
            }
        }

        private List<OrderDayHistory> GetOrderDayHistoryOrdersDirectly(List<Guid> orders, DateTime tradeDay)
        {
            List<OrderDayHistory> result = new List<OrderDayHistory>(orders.Count);
            foreach (var eachOrderId in orders)
            {
                Dictionary<DateTime, OrderDayHistory> historyOrders;
                if (_historyOrdersPerOrderDict.TryGetValue(eachOrderId, out historyOrders))
                {
                    OrderDayHistory orderDayHistory;
                    if (historyOrders.TryGetValue(tradeDay, out orderDayHistory))
                    {
                        result.Add(orderDayHistory);
                    }
                }
            }
            return result;
        }

        //private void LoadOrderDayHistorys(List<KeyValuePair<Guid, DateTime?>> orders)
        //{
        //    if (orders == null || orders.Count == 0) return;
        //    XElement orderXml = new XElement("Orders");
        //    foreach (var eachOrder in orders)
        //    {
        //        XElement orderNode = new XElement("Order");
        //        Guid id = eachOrder.Key;
        //        DateTime? tradeDay = eachOrder.Value;
        //        orderNode.SetAttributeValue("ID", id);
        //        if (tradeDay != null)
        //        {
        //            orderNode.SetAttributeValue("TradeDay", tradeDay.Value);
        //        }
        //        orderXml.Add(orderNode);
        //    }
        //    Logger.InfoFormat("LoadOrderDayHistorys orderXml = {0}", orderXml.ToString());
        //    var orderDayHistorys = DB.DBRepository.Default.GetOrderDayHistorys(orderXml);
        //    if (orderDayHistorys != null)
        //    {
        //        foreach (var eachOrderDayHistory in orderDayHistorys)
        //        {
        //            this.Add(eachOrderDayHistory);
        //        }
        //    }
        //}


        private List<Guid> GetNotExistsOrderId(List<Guid> orders, DateTime tradeDay)
        {
            List<Guid> notExistsOrders = new List<Guid>(orders.Count);
            foreach (var eachOrderId in orders)
            {
                Dictionary<DateTime, OrderDayHistory> historyOrders;
                if (!_historyOrdersPerOrderDict.TryGetValue(eachOrderId, out historyOrders))
                {
                    notExistsOrders.Add(eachOrderId);
                }
                else
                {
                    OrderDayHistory orderDayHistory;
                    if (!historyOrders.TryGetValue(tradeDay, out orderDayHistory))
                    {
                        notExistsOrders.Add(eachOrderId);
                    }
                }
            }
            return notExistsOrders;
        }



        internal void RemoveOrderDayHistorys(List<Guid> orders, DateTime tradeDay)
        {
            lock (_mutex)
            {
                foreach (var eachOrderId in orders)
                {
                    Dictionary<DateTime, OrderDayHistory> historyOrders;
                    if (_historyOrdersPerOrderDict.TryGetValue(eachOrderId, out historyOrders))
                    {
                        if (historyOrders.ContainsKey(tradeDay))
                        {
                            historyOrders.Remove(tradeDay);
                        }
                    }
                }
            }
        }

        internal void RemoveOrderDayHistorys(Guid orderId)
        {
            lock (_mutex)
            {
                if (_historyOrdersPerOrderDict.ContainsKey(orderId))
                {
                    Logger.InfoFormat("RemoveOrderDayHistorys orderId = {0}", orderId);
                    _historyOrdersPerOrderDict.Remove(orderId);
                }
            }
        }

    }

}
