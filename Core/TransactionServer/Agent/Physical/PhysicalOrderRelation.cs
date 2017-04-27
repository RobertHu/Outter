using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Physical.OrderRelationBusiness;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical
{
    internal class PhysicalOrderRelation : OrderRelation
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PhysicalOrderRelation));

        private PhysicalOrderRelationSettings _physicalSettings;

        #region Constructors
        internal PhysicalOrderRelation(PhysicalOrderRelationConstructParams constructParams)
            : base(constructParams)
        {
            _physicalSettings = new PhysicalOrderRelationSettings(this, constructParams);
            this.PayBackPledgeOfCloseOrder = 0m;
            this.PayBackPledgeOfOpenOrder = 0m;

        }
        #endregion

        #region Properties

        internal decimal PhysicalValue
        {
            get { return _physicalSettings.PhysicalValue; }
            set { _physicalSettings.PhysicalValue = value; }
        }

        internal DateTime? PhysicalValueMatureDay
        {
            get { return _physicalSettings.PhysicalValueMatureDay; }
            set { _physicalSettings.PhysicalValueMatureDay = value; }
        }

        internal DateTime? RealPhysicalValueMatureDate { get; set; }

        internal decimal OverdueCutPenalty
        {
            get { return _physicalSettings.OverdueCutPenalty; }
            private set { _physicalSettings.OverdueCutPenalty = value; }
        }

        internal decimal ClosePenalty
        {
            get { return _physicalSettings.ClosePenalty; }
            private set { _physicalSettings.ClosePenalty = value; }
        }

        internal decimal PayBackPledge
        {
            get { return _physicalSettings.PayBackPledge; }
            private set { _physicalSettings.PayBackPledge = value; }
        }

        internal decimal PayBackPledgeOfCloseOrder { get; private set; }

        internal decimal PayBackPledgeOfOpenOrder { get; private set; }

        internal decimal ClosedPhysicalValue
        {
            get { return _physicalSettings.ClosedPhysicalValue; }
            private set { _physicalSettings.ClosedPhysicalValue = value; }
        }

        internal bool IsFullPayment { get; set; }

        #endregion

        protected override decimal CalculateTradePL(ExecuteContext context)
        {
            var result = 0m;
            var physicalOrder = this.CloseOrder as PhysicalOrder;
            if (physicalOrder.PhysicalTradeSide != PhysicalTradeSide.Delivery)
            {
                if (this.OpenOrder.IsBuy)
                {
                    result = this.PhysicalValue - this.ClosedPhysicalValue;
                }
                else
                {
                    result = this.ClosedPhysicalValue - this.PhysicalValue;
                }
            }
            return result;
        }

        protected override decimal GetValuedTradePL(decimal tradePL)
        {
            return tradePL;
        }


        internal override void CancelExecute(bool isForDelivery)
        {
            base.CancelExecute(isForDelivery);
            var openOrder = (PhysicalOrder)this.OpenOrder;
            openOrder.PhysicalOriginValueBalance += this.ClosedPhysicalValue;
            openOrder.PaidPledgeBalance += -this.PayBackPledgeOfOpenOrder;
        }

        internal decimal CalculateClosePenalty(ExecuteContext context)
        {
            var physicalOrder = this.OpenOrder as PhysicalOrder;
            decimal result = 0m;
            if (physicalOrder.IsInstalment)
            {
                result = physicalOrder.CalculateClosePenalty(this.CloseOrder, this.ClosedLot, context);
            }
            this.ClosePenalty += -result;
            return result;
        }

        internal decimal CalculateOverdueCutPenalty(ExecuteContext context)
        {
            decimal result = 0m;
            var physicalOrder = this.OpenOrder as PhysicalOrder;
            if (physicalOrder.IsInstalment)
            {
                result = physicalOrder.CalculateOverdueCutPenalty(this.CloseOrder, this.ClosedLot, context);
            }
            this.OverdueCutPenalty += -result;
            return result;
        }

        internal void SetPhysicalValueAndMatureDay(decimal physicalValue, DateTime? physicalValueMatureDay)
        {
            this.PhysicalValue += physicalValue;
            this.PhysicalValueMatureDay = physicalValueMatureDay;
        }

        private decimal CalculatePaybackPledgeForCloseOrder(PhysicalOrder closeOrder, bool isLast, DateTime? tradeDay)
        {
            if (closeOrder.PaidPledgeBalance != 0)
            {
                if (isLast)
                {
                    return -closeOrder.PaidPledgeBalance;
                }
                else
                {
                    decimal payBackPledge = (this.ClosedLot / closeOrder.Lot) * closeOrder.PaidPledge;
                    return Math.Round(payBackPledge, this.CurrencyDecimals(tradeDay), MidpointRounding.AwayFromZero);
                }
            }
            return 0;
        }

        private decimal CalculatePaybackPledgeForOpenOrder(PhysicalOrder openOrder, DateTime? tradeDay)
        {
            if (openOrder.PaidPledgeBalance != 0)
            {
                decimal remainOpenLot = this.OpenOrder.LotBalance + openOrder.DeliveryLockLot;
                if (remainOpenLot == 0m)
                {
                    Logger.WarnFormat("CalculatePaybackPledgeForOpenOrder openOrderId = {0}, tradeDay = {1}, remainOpenLot = 0", openOrder.Id, tradeDay);
                    return 0m;
                }
                if (remainOpenLot == this.ClosedLot)
                {
                    return -openOrder.PaidPledgeBalance;
                }
                decimal payBackPledge = (this.ClosedLot / remainOpenLot) * openOrder.PaidPledgeBalance;
                return Math.Round(-payBackPledge, this.CurrencyDecimals(tradeDay), MidpointRounding.AwayFromZero);
            }
            return 0;
        }


        internal void CalculateClosedPhysicalValue(DateTime? tradeDay)
        {
            var openOrder = this.OpenOrder as PhysicalOrder;
            decimal remainOpenLot = openOrder.LotBalance + openOrder.DeliveryLockLot;
            decimal closedValue = 0;
            if (remainOpenLot == this.ClosedLot)
            {
                closedValue = openOrder.PhysicalOriginValueBalance;
            }
            else
            {
                if (remainOpenLot == 0)
                {
                    Logger.WarnFormat("CalculateClosedPhysicalValue remainOpenLot = 0 tradeDay = {0}, openOrder id = {1}", tradeDay, openOrder.Id);
                    return;
                }
                closedValue = (this.ClosedLot / remainOpenLot) * openOrder.PhysicalOriginValueBalance;
                closedValue = Math.Round(closedValue, this.CurrencyDecimals(tradeDay), MidpointRounding.AwayFromZero);
            }
            this.ClosedPhysicalValue += closedValue;
        }


        internal void CalculatePayBackPledge(bool isLast, ExecuteContext context)
        {
            var closeOrder = this.CloseOrder as PhysicalOrder;
            var openOrder = this.OpenOrder as PhysicalOrder;
            this.PayBackPledgeOfCloseOrder = this.CalculatePaybackPledgeForCloseOrder(closeOrder, isLast, context.TradeDay);
            this.PayBackPledgeOfOpenOrder = this.CalculatePaybackPledgeForOpenOrder(openOrder, context.TradeDay);
            if (closeOrder.PhysicalTradeSide != PhysicalTradeSide.Delivery)
            {
                decimal result = this.PayBackPledgeOfOpenOrder + this.PayBackPledgeOfCloseOrder;
                this.PayBackPledge += result;
            }
            if (context.IsBook)
            {
                this.PayBackPledge = this.PayBackPledgeOfOpenOrder;
            }
        }

    }
}
