using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    internal sealed class HitOrderSettings : BusinessItemBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(HitOrderSettings));

        private BusinessItem<int> _hitCount;
        private BusinessItem<Price> _bestPrice;
        private BusinessItem<DateTime?> _bestTime;
        private BusinessItem<OrderHitStatus> _hitStatus;
        private Order _owner;

        internal HitOrderSettings(Order parent, OrderConstructParams constructParams) :
            base(parent)
        {
            _owner = parent;
            _bestTime = this.CreateSoundItem("BestTime", constructParams.BestTime);
            _hitCount = this.CreateSoundItem(OrderBusinessItemNames.HitCount, constructParams.HitCount);
            _bestPrice = this.CreateSoundItem(OrderBusinessItemNames.BestPrice, constructParams.BestPrice);
            _hitStatus = this.CreateSoundItem(OrderBusinessItemNames.HitStatus, constructParams.HitStatus);
        }

        public int HitCount
        {
            get { return _hitCount.Value; }
            set
            {
                _hitCount.SetValue(value);
            }
        }

        public Price BestPrice
        {
            get { return _bestPrice.Value; }
            set { _bestPrice.SetValue(value); }
        }

        public DateTime? BestTime
        {
            get { return _bestTime.Value; }
            set { _bestTime.SetValue(value); }
        }

        public OrderHitStatus HitStatus
        {
            get { return _hitStatus.Value; }
            set { _hitStatus.SetValue(value); }
        }


        internal bool IncreaseHitCount()
        {
            if (this.HitCount < _owner.Owner.SettingInstrument().HitTimes)
            {
                this.HitCount++;
                return true;
            }
            return false;
        }

        public OrderHitStatus HitSetPrice(Quotation quotation, DateTime baseTime, bool ignoreHitTimes)
        {
            OrderHitStatus status;
            if (_owner.OrderType == OrderType.SpotTrade)
            {
                status = Hit.SpotOrderHitter.HitSpotOrder(_owner, this, quotation, baseTime);
            }
            else
            {
                status = Hit.LimitAndMarketOrderHitter.HitMarketAndLimitOrder(_owner, this, quotation, baseTime, ignoreHitTimes);
            }
            return status;
        }


        public OrderHitStatus CalculateLimitMarketHitStatus(bool ignoreHitTimes)
        {
            var instrument = _owner.Owner.SettingInstrument();
            var tran = _owner.Owner;
            var dealingPolicyDetail = tran.DealingPolicyPayload();
            int hitTimes = ignoreHitTimes ? 0 : (int)instrument.HitTimes;
            if ((instrument.IsAutoFill && _owner.Lot <= dealingPolicyDetail.AutoLmtMktMaxLot) && (this.HitCount >= hitTimes
                || (this.HitCount > 0 && tran.OrderType == OrderType.Limit && !instrument.LmtAsMit && this.IsBreakdownPenetrationPoint)))
            {
                return OrderHitStatus.ToAutoFill;
            }
            else
            {
                StringBuilder sb = Protocal.StringBuilderCache.Acquire();
                sb.AppendFormat("CalculateLimitMarketHitStatus order id = {0} ,", _owner.Id);
                sb.AppendFormat("instrument.isAutoFill = {0}, ", instrument.IsAutoFill);
                sb.AppendFormat("order.Lot = {0}, AutoLmtMktMaxLot = {1} ,", _owner.Lot, dealingPolicyDetail.AutoLmtMktMaxLot);
                sb.AppendFormat("hitCount = {0}, instrument.hitTimes = {1}, ignoreHitTimes = {2} ,", this.HitCount, instrument.HitTimes, ignoreHitTimes);
                sb.AppendFormat("instrument.LmtAsHit = {0}, this.IsBreakdownPenetrationPoint = {1}", instrument.LmtAsMit, this.IsBreakdownPenetrationPoint);
                Logger.Info(Protocal.StringBuilderCache.GetStringAndRelease(sb));
            }
            return OrderHitStatus.Hit;
        }

        public bool IsBreakdownPenetrationPoint
        {
            get
            {
                var penetrationPoint = _owner.Owner.SettingInstrument().PenetrationPoint;
                return penetrationPoint > 0 && this.BestPrice != null && Math.Abs(this.BestPrice - _owner.SetPrice) >= penetrationPoint;
            }
        }
    }
}
