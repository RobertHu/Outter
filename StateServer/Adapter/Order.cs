using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using iExchange.Common;
using System.Xml.Linq;
using Protocal;
using System.Reflection;
using iExchange.StateServer.Adapter.BinaryOption;
using System.Data.SqlTypes;

namespace iExchange.StateServer.Adapter
{
    [Flags]
    internal enum PropertyChangeType
    {
        None = 0,
        AutoStopPrice = 1 << 0,
        AutoLimitPrice = 1 << 1,
        PhysicalPaidAmount = 1 << 2,
        PaidPledge = 1 << 3,
        PaidPledgeBalance = 1 << 4,
        HasOverdue = 1 << 5
    }

    internal static class PropertyChangeTypeExtension
    {
        internal static bool Contains(this PropertyChangeType source, PropertyChangeType item)
        {
            return source.HasFlag(item);
        }
    }

    internal class Order : Protocal.Commands.Order
    {
        private sealed class ChangedProperty
        {
            public string AutoLimitPrice { get; set; }
            public string AutoStopPrice { get; set; }
            public decimal PaidPledge { get; set; }
            public decimal PaidPledgeBalance { get; set; }
            public bool IsInstalmentOverdue { get; set; }
        }

        internal Order(Transaction owner)
            : base(owner)
        {
        }

        protected override Protocal.Commands.OrderRelation CreateOrderRelation(Protocal.Commands.Order order)
        {
            return new OrderRelation((Order)order);
        }

        public override Protocal.Commands.OrderPhaseChange Update(XElement orderElement)
        {
            ChangedProperty changedProperty = new ChangedProperty
            {
                AutoLimitPrice = this.AutoLimitPrice,
                AutoStopPrice = this.AutoStopPrice,
                PaidPledge = this.PaidPledge,
                PaidPledgeBalance = this.PaidPledgeBalance,
                IsInstalmentOverdue = this.IsInstalmentOverdue
            };
            var orderChange = base.Update(orderElement);
            if (orderChange != null) return orderChange;
            return this.CreateOrderChangeByChangedProperties(changedProperty);
        }


        private OrderChange CreateOrderChangeByChangedProperties(ChangedProperty changedPropery)
        {
            PropertyChangeType changeProperties = PropertyChangeType.None;
            if (string.Compare(changedPropery.AutoLimitPrice, this.AutoLimitPrice) != 0)
            {
                changeProperties |= PropertyChangeType.AutoLimitPrice;
            }

            if (string.Compare(changedPropery.AutoStopPrice, this.AutoStopPrice) != 0)
            {
                changeProperties |= PropertyChangeType.AutoStopPrice;
            }

            if (changedPropery.PaidPledge != this.PaidPledge)
            {
                changeProperties |= PropertyChangeType.PaidPledge;
            }

            if (changedPropery.PaidPledgeBalance != this.PaidPledgeBalance)
            {
                changeProperties |= PropertyChangeType.PaidPledgeBalance;
            }

            if (changedPropery.IsInstalmentOverdue != this.IsInstalmentOverdue)
            {
                changeProperties |= PropertyChangeType.HasOverdue;
            }

            if (changeProperties != PropertyChangeType.None)
            {
                return new OrderChange(this, Protocal.Commands.OrderChangeType.Changed, changeProperties);
            }
            return null;
        }

        protected override Protocal.Commands.OrderPhaseChange CreateOrderChange(Protocal.Commands.OrderChangeType changeType, Protocal.Commands.Order order)
        {
            return new OrderChange((Order)order, changeType);
        }


        internal XElement ToXml(bool isForGetInitData, bool isForReport)
        {
            XElement orderNode = new XElement("Order");

            bool isPhysicalTran = this.Owner.InstrumentCategory == InstrumentCategory.Physical;

            orderNode.SetAttributeValue("ID", XmlConvert.ToString(this.Id));
            if (this.LivePrice != null) orderNode.SetAttributeValue("LivePrice", (string)this.LivePrice);
            orderNode.SetAttributeValue("InterestPLFloat", XmlConvert.ToString(this.InterestPLFloat));
            orderNode.SetAttributeValue("StoragePLFloat", XmlConvert.ToString(this.StoragePLFloat));
            orderNode.SetAttributeValue("TradePLFloat", XmlConvert.ToString(this.TradePLFloat));

            if (this.ExecutePrice != null) orderNode.SetAttributeValue("ExecutePrice", (string)this.ExecutePrice);
            orderNode.SetAttributeValue("AutoLimitPrice", this.AutoLimitPrice ?? string.Empty);
            orderNode.SetAttributeValue("AutoStopPrice", this.AutoStopPrice ?? string.Empty);

            if (isForGetInitData)
            {
                orderNode.SetAttributeValue("Necessary", XmlConvert.ToString(this.Necessary));
                orderNode.SetAttributeValue("ValueAsMargin", XmlConvert.ToString(this.ValueAsMargin));
                return orderNode;
            }

            if (this.OriginalCode != null) orderNode.SetAttributeValue("Code", this.OriginalCode);
            if (this.Code != null) orderNode.SetAttributeValue("Code", this.Code);
            orderNode.SetAttributeValue("Phase", XmlConvert.ToString((int)this.Phase));
            orderNode.SetAttributeValue("TradeOption", XmlConvert.ToString((int)this.TradeOption));
            orderNode.SetAttributeValue("IsOpen", XmlConvert.ToString(this.IsOpen));
            orderNode.SetAttributeValue("IsBuy", XmlConvert.ToString(this.IsBuy));
            if (this.BlotterCode != null) orderNode.SetAttributeValue("BlotterCode", this.BlotterCode);
            orderNode.SetAttributeValue("PhysicalTradeSide", XmlConvert.ToString((int)this.PhysicalTradeSide));
            if (isPhysicalTran) orderNode.SetAttributeValue("PhysicalOriginValue", XmlConvert.ToString(this.PhysicalOriginValue));
            if (isPhysicalTran) orderNode.SetAttributeValue("PhysicalOriginValueBalance", XmlConvert.ToString(this.PhysicalOriginValueBalance));
            if (isPhysicalTran) orderNode.SetAttributeValue("PhysicalPaymentDiscount", XmlConvert.ToString(this.PhysicalPaymentDiscount));
            if (isPhysicalTran) orderNode.SetAttributeValue("PhysicalValueMatureDay", XmlConvert.ToString(this.PhysicalValueMatureDay));
            if (isPhysicalTran) orderNode.SetAttributeValue("PaidPledge", XmlConvert.ToString(this.PaidPledge));
            if (isPhysicalTran) orderNode.SetAttributeValue("PaidPledgeBalance", XmlConvert.ToString(this.PaidPledgeBalance));

            if (this.PhysicalRequestId != null) orderNode.SetAttributeValue("PhysicalRequestId", XmlConvert.ToString(this.PhysicalRequestId.Value));
            if (this.InterestValueDate != DateTime.MinValue)
                orderNode.SetAttributeValue("InterestValueDate", XmlConvert.ToString(this.InterestValueDate, DateTimeFormat.Xml));

            orderNode.SetAttributeValue("TotalDeposit", XmlConvert.ToString(this.Owner.Owner.TotalDeposit));
            orderNode.SetAttributeValue("Equity", XmlConvert.ToString(this.Owner.Owner.Equity));

            if (this.InstalmentPolicyId != null)
            {
                orderNode.SetAttributeValue("InstalmentPolicyId", XmlConvert.ToString(this.InstalmentPolicyId.Value));
                orderNode.SetAttributeValue("PhysicalInstalmentType", XmlConvert.ToString((int)this.InstalmentType));
                orderNode.SetAttributeValue("Period", XmlConvert.ToString(this.Period));
                orderNode.SetAttributeValue("DownPaymentBasis", XmlConvert.ToString((int)this.DownPaymentBasis));
                orderNode.SetAttributeValue("DownPayment", XmlConvert.ToString(this.DownPayment));
                orderNode.SetAttributeValue("RecalculateRateType", XmlConvert.ToString((int)this.RecalculateRateType));
                orderNode.SetAttributeValue("InstalmentFrequence", XmlConvert.ToString((int)this.InstalmentFrequence));
                orderNode.SetAttributeValue("InstalmentAdministrationFee", XmlConvert.ToString(-this.InstalmentAdministrationFee));
            }

            if (this.BinaryOptionBetTypeId != null)
            {
                orderNode.SetAttributeValue("BOBetTypeID", XmlConvert.ToString(this.BinaryOptionBetTypeId.Value));
                orderNode.SetAttributeValue("BOFrequency", XmlConvert.ToString(this.BinaryOptionFrequency));
                orderNode.SetAttributeValue("BOOdds", XmlConvert.ToString(this.BinaryOptionOdds));
                orderNode.SetAttributeValue("BOBetOption", XmlConvert.ToString(this.BinaryOptionBetOption));
            }

            if (this.SetPrice != null) orderNode.SetAttributeValue("SetPrice", (string)this.SetPrice);
            if (this.SetPrice2 != null) orderNode.SetAttributeValue("SetPrice2", (string)this.SetPrice2);
            if (this.SetPriceMaxMovePips != 0) orderNode.SetAttributeValue("SetPriceMaxMovePips", XmlConvert.ToString(this.SetPriceMaxMovePips));
            if (this.DQMaxMove != 0) orderNode.SetAttributeValue("DQMaxMove", XmlConvert.ToString(this.DQMaxMove));

            if (this.JudgePrice != null)
            {
                orderNode.SetAttributeValue("JudgePrice", (string)this.JudgePrice);
                orderNode.SetAttributeValue("JudgePriceTimestamp", this.JudgePriceTimestamp.Value.ToString(DateTimeFormat.Xml));
            }

            orderNode.SetAttributeValue("Lot", XmlConvert.ToString(this.Lot));
            if (this.MinLot != null) orderNode.SetAttributeValue("MinLot", XmlConvert.ToString(this.MinLot.Value));
            if (this.MaxShow != null) orderNode.SetAttributeValue("MaxShow", XmlConvert.ToString(this.MaxShow.Value));
            orderNode.SetAttributeValue("OriginalLot", XmlConvert.ToString(this.OriginalLot));
            orderNode.SetAttributeValue("LotBalance", XmlConvert.ToString(isForReport ? this.LotBalanceReal : this.LotBalance));
            orderNode.SetAttributeValue("DeliveryLockLot", XmlConvert.ToString(this.DeliveryLockLot));

            orderNode.SetAttributeValue("CommissionSum", XmlConvert.ToString(-this.CommissionSum));
            orderNode.SetAttributeValue("LevySum", XmlConvert.ToString(-this.LevySum));
            orderNode.SetAttributeValue("OtherFeeSum", XmlConvert.ToString(this.OtherFeeSum));
            orderNode.SetAttributeValue("InterestPerLot", XmlConvert.ToString(this.InterestPerLot));
            orderNode.SetAttributeValue("StoragePerLot", XmlConvert.ToString(this.StoragePerLot));

            orderNode.SetAttributeValue("InterestPLNotValued", XmlConvert.ToString(this.InterestPLNotValued));
            orderNode.SetAttributeValue("StoragePLNotValued", XmlConvert.ToString(this.StoragePLNotValued));
            orderNode.SetAttributeValue("TradePLNotValued", XmlConvert.ToString(this.TradePLNotValued));

            orderNode.SetAttributeValue("HitCount", XmlConvert.ToString(this.HitCount));
            orderNode.SetAttributeValue("BestPrice", this.BestPrice);
            orderNode.SetAttributeValue("BestTime", this.BestTime == null ? string.Empty : this.BestTime.Value.ToString(DateTimeFormat.Xml));

            orderNode.SetAttributeValue("EstimateCloseCommission", this.EstimateCloseCommission);
            orderNode.SetAttributeValue("EstimateCloseLevy", this.EstimateCloseLevy);

            this.FillOrderRelations(orderNode);
            return orderNode;
        }

        private void FillOrderRelations(XElement orderNode)
        {
            if (!this.IsOpen)
            {
                foreach (OrderRelation eachOrderRelation in this.OrderRelations)
                {
                    orderNode.Add(eachOrderRelation.ToXml());
                }
            }
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Order otherOrder = obj as Order;
            if (otherOrder == null) return false;
            return this.Id == otherOrder.Id;
        }

        public override string ToString()
        {
            return string.Format("Id={0}, code={1}, phase = {2}", this.Id, this.Code, this.Phase);
        }

    }

}