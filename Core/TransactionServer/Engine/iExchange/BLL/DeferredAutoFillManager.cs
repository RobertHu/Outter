using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace Core.TransactionServer.Engine.iExchange.BLL
{
    internal sealed class DeferredAutoFillManager
    {
        internal static DeferredAutoFillManager Default = new DeferredAutoFillManager();

        private const int AUTO_FILL_ENTITY_LIST_LOAD_FACTOR = 57;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(500);
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeferredAutoFillManager));
        private Dictionary<Guid, List<DeferredAutoFillInfo>> _deferredAutoFillEntitiesDict = new Dictionary<Guid, List<DeferredAutoFillInfo>>();
        private object _mutex = new object();
        private Timer _checkTimer;

        static DeferredAutoFillManager() { }

        private DeferredAutoFillManager()
        {
            _checkTimer = new Timer(this.CheckHandler, null, Timeout.Infinite, Timeout.Infinite);
            _checkTimer.Change((long)CheckInterval.TotalMilliseconds, Timeout.Infinite);
        }

        internal DeferredAutoFillInfo Add(Transaction tran)
        {
            var order = tran.FirstOrder;
            var isBuy = order.IsBuy;
            var price = order.SetPrice;
            DeferredAutoFillInfo entity = new DeferredAutoFillInfo(isBuy, price, tran);
            this.InnerAdd(entity, tran.InstrumentId);
            return entity;
        }

        private void InnerAdd(DeferredAutoFillInfo item, Guid instrumentId)
        {
            lock (_mutex)
            {
                List<DeferredAutoFillInfo> autoFillEntities = null;
                if (!_deferredAutoFillEntitiesDict.TryGetValue(instrumentId, out autoFillEntities))
                {
                    autoFillEntities = new List<DeferredAutoFillInfo>(AUTO_FILL_ENTITY_LIST_LOAD_FACTOR);
                    _deferredAutoFillEntitiesDict.Add(instrumentId, autoFillEntities);
                }

                Logger.InfoFormat("tranID = {0}, isValid ={1}", item.Transaction.Id, item.IsValid);

                if (item.IsValid)
                {
                    autoFillEntities.Add(item);
                }
                else
                {
                    TransactionExpireChecker.Default.Add(item.Transaction);
                }

            }
        }

        private void CheckHandler(object state)
        {
            try
            {
                var shouldFillEntities = this.CheckAndDequeueShouldBeFilledEntities();
                if (shouldFillEntities != null)
                {
                    shouldFillEntities = this.CancelAndFilterInvalidEntities(shouldFillEntities);
                    AutoFillExecutorAndCanceler.Default.ExecuteOrders(shouldFillEntities);
                }
            }
            finally
            {
                _checkTimer.Change((long)CheckInterval.TotalMilliseconds, Timeout.Infinite);
            }
        }

        private List<DeferredAutoFillInfo> CancelAndFilterInvalidEntities(List<DeferredAutoFillInfo> shouldBeFilledEntities)
        {
            var invalidEntities = this.CancelAndGetInvalidDeferredEntities(shouldBeFilledEntities);
            if (invalidEntities != null)
            {
                foreach (var item in invalidEntities)
                {
                    shouldBeFilledEntities.Remove(item);
                }
            }
            return shouldBeFilledEntities;
        }

        private List<DeferredAutoFillInfo> CheckAndDequeueShouldBeFilledEntities()
        {
            var result = this.CheckAndGetShouldBeFilledEntities();
            if (result != null)
            {
                this.Remove(result);
            }
            return result;
        }

        private List<DeferredAutoFillInfo> CancelAndGetInvalidDeferredEntities(List<DeferredAutoFillInfo> willBeFilledEntities)
        {
            List<DeferredAutoFillInfo> shouldCancelEntities = null;
            if (Setting.Default.SystemParameter.DQDelayTimeOption == DQDelayTimeOption.OnlyLastPrice)
            {
                foreach (DeferredAutoFillInfo entity in willBeFilledEntities)
                {
                    if (!entity.Verify())
                    {
                        if (shouldCancelEntities == null)
                        {
                            shouldCancelEntities = new List<DeferredAutoFillInfo>();
                        }
                        shouldCancelEntities.Add(entity);
                    }
                }
            }

            if (shouldCancelEntities != null)
            {
                AutoFillExecutorAndCanceler.Default.CancelInvalidAutoFillTransactions(shouldCancelEntities);
            }
            return shouldCancelEntities;
        }

        private List<DeferredAutoFillInfo> CheckAndGetShouldBeFilledEntities()
        {
            lock (_mutex)
            {
                List<DeferredAutoFillInfo> result = null;
                foreach (var eachEntityList in _deferredAutoFillEntitiesDict.Values)
                {
                    foreach (var eachEntity in eachEntityList)
                    {
                        var shouldAutoFill = eachEntity.ReduceDeferredTime(CheckInterval) <= TimeSpan.Zero;
                        if (shouldAutoFill)
                        {
                            if (result == null)
                            {
                                result = new List<DeferredAutoFillInfo>(AUTO_FILL_ENTITY_LIST_LOAD_FACTOR);
                            }
                            result.Add(eachEntity);
                        }
                    }
                }
                return result;
            }
        }

        private void Remove(List<DeferredAutoFillInfo> toRemoveEntities)
        {
            lock (_mutex)
            {
                foreach (DeferredAutoFillInfo entity in toRemoveEntities)
                {
                    var target = _deferredAutoFillEntitiesDict[entity.Instrument.Id];
                    target.Remove(entity);
                }
            }
        }
    }

    internal sealed class DeferredAutoFillInfo
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeferredAutoFillInfo));
        private TimeSpan _deferredTime;
        private int _dqMaxMove;

        internal DeferredAutoFillInfo(bool isBuy, Price setPrice, Transaction tran)
        {
            this.Account = tran.Owner;
            this.Instrument = tran.SettingInstrument();
            this.IsBuy = isBuy;
            this.SetPrice = setPrice;
            this.DealingPolicyDetail = tran.DealingPolicyPayload();
            this.Transaction = tran;
            _dqMaxMove = tran.FirstOrder.DQMaxMove;
            _deferredTime = this.DealingPolicyDetail.AutoDQDelay;
            this.IsValid = this.Verify();
        }

        internal Agent.Account Account { get; private set; }
        internal Instrument Instrument { get; private set; }
        internal bool IsBuy { get; private set; }
        internal DealingPolicyPayload DealingPolicyDetail { get; private set; }
        internal Price SetPrice { get; private set; }
        internal Token Token { get; private set; }
        internal Transaction Transaction { get; private set; }
        internal Price ComparePrice { get; private set; }
        internal DateTime ComparePriceTimestamp { get; private set; }


        internal TimeSpan ReduceDeferredTime(TimeSpan value)
        {
            this._deferredTime -= value;
            return this._deferredTime;
        }

        internal bool IsValid { get; private set; }

        internal bool Verify()
        {
            var quotation = this.Transaction.AccountInstrument.GetQuotation(this.Transaction.SubmitorQuotePolicyProvider);
            this.ComparePrice = (this.IsBuy ? quotation.BuyPrice : quotation.SellPrice);
            this.ComparePriceTimestamp = quotation.Timestamp;
            int sign = (this.Instrument.IsNormal ^ this.IsBuy) ? 1 : -1;
            Price setPrice = this.SetPrice - this._dqMaxMove * sign;

            int priceVariation = (setPrice - this.ComparePrice) * sign;

            if (priceVariation > this.DealingPolicyDetail.AcceptDQVariation)
            {
                Logger.InfoFormat("setPrice = {0}, sign = {1}, comparePrice = {2} ,acceptDQVariation ={3}, priceVariation = {4}", setPrice, sign, this.ComparePrice, this.DealingPolicyDetail.AcceptDQVariation, priceVariation);
                return false;
            }
            return true;
        }
    }

}
