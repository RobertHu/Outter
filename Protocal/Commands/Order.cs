using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public class Order : XmlFillable<Order>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Order));

        public Order(Transaction owner)
        {
            this.Owner = owner;
            this.Phase = null;
            this.OrderRelations = new List<OrderRelation>();
        }

        public List<OrderRelation> OrderRelations { get; private set; }

        public Transaction Owner { get; private set; }

        public Account Account
        {
            get
            {
                return this.Owner.Owner;
            }
        }

        public Guid Id { get; private set; }
        public string OriginalCode { get; private set; }
        public string Code { get; private set; }
        public OrderPhase? Phase { get; private set; }
        public TradeOption TradeOption { get; private set; }

        public CancelReason? CancelReason { get; private set; }

        public bool PlacedByRiskMonitor { get; private set; }

        public bool IsBuy { get; private set; }
        public bool IsOpen { get; private set; }

        public decimal Lot { get; private set; }
        public decimal OriginalLot { get; private set; }
        public decimal LotBalance { get; private set; }
        public decimal DeliveryLockLot { get; private set; }
        public decimal LotBalanceReal
        {
            get { return this.LotBalance + this.DeliveryLockLot; }
        }

        public decimal? MinLot { get; private set; }
        public decimal? MaxShow { get; private set; }

        public decimal CommissionSum { get; private set; }
        public decimal LevySum { get; private set; }
        public decimal OtherFeeSum { get; private set; }
        public decimal InterestPerLot { get; private set; }
        public decimal StoragePerLot { get; private set; }

        public string LivePrice { get; set; }
        public decimal InterestPLFloat { get; private set; }
        public decimal StoragePLFloat { get; private set; }
        public decimal TradePLFloat { get; set; }

        public decimal InterestPLNotValued { get; private set; }
        public decimal StoragePLNotValued { get; private set; }
        public decimal TradePLNotValued { get; private set; }

        public string SetPrice { get; private set; }
        public string SetPrice2 { get; private set; }
        public int SetPriceMaxMovePips { get; private set; }
        public int DQMaxMove { get; private set; }

        public string ExecutePrice { get; private set; }
        public string AutoLimitPrice { get; private set; }
        public string AutoStopPrice { get; private set; }
        public string JudgePrice { get; private set; }
        public DateTime? JudgePriceTimestamp { get; private set; }

        public decimal Necessary { get; private set; }

        public string BlotterCode { get; private set; }
        public PhysicalTradeSide PhysicalTradeSide { get; private set; }

        public decimal PhysicalOriginValue { get; private set; }
        public decimal PhysicalOriginValueBalance { get; private set; }
        public decimal PhysicalPaymentDiscount { get; private set; }
        public int PhysicalValueMatureDay { get; private set; }
        public decimal PaidPledge { get; private set; }
        public decimal PaidPledgeBalance { get; private set; }
        public decimal PhysicalPaidAmount { get; private set; }

        public Guid? PhysicalRequestId { get; private set; }
        public DateTime InterestValueDate { get; private set; }

        public Guid? InstalmentPolicyId { get; private set; }
        public InstalmentType InstalmentType { get; private set; }
        public int Period { get; private set; }
        public InstalmentFrequence InstalmentFrequence { get; private set; }
        public Protocal.DownPaymentBasis DownPaymentBasis { get; private set; }
        public decimal DownPayment { get; private set; }
        public RecalculateRateType RecalculateRateType { get; private set; }
        public decimal InstalmentAdministrationFee { get; private set; }

        public int HitCount { get; private set; }
        public string BestPrice { get; private set; }
        public DateTime? BestTime { get; private set; }

        public Guid? BinaryOptionBetTypeId { get; private set; }
        public int BinaryOptionFrequency { get; private set; }
        public decimal BinaryOptionOdds { get; private set; }
        public long BinaryOptionBetOption { get; private set; }

        public decimal ValueAsMargin { get; private set; }

        public bool IsInstalmentOverdue { get; private set; }

        public Physical.PhysicalType? PhysicalType { get; private set; }

        public decimal AdvanceAmount { get; private set; }

        public List<decimal> DayInterestPLNotValued { get; private set; }

        public List<decimal> DayStoragePLNotValued { get; private set; }

        public bool IsAutoFill { get; private set; }

        public decimal EstimateCloseCommission { get; private set; }
        public decimal EstimateCloseLevy { get; private set; }


        public virtual OrderPhaseChange Update(XElement orderElement)
        {
            Debug.WriteLine("in order");
            OrderPhase? oldPhase = this.Phase;
            if (orderElement.Attribute("Phase") != null && !string.IsNullOrEmpty(orderElement.Attribute("Phase").Value))
            {
                OrderPhase phase = (OrderPhase)XmlConvert.ToInt32(orderElement.Attribute("Phase").Value);
                if (phase == oldPhase)
                {
                    return this.CreateOrderChange(null);
                }
            }
            Debug.WriteLine(string.Format("old phase = {0}", oldPhase));
            int oldHitCount = this.HitCount;
            this.UpdateOrderProperties(orderElement);
            var result = this.CreateOrderChange(oldPhase);
            if (result == null && oldHitCount != this.HitCount)
            {
                return this.CreateOrderChange(OrderChangeType.Hit, this);
            }
            return result;
        }


        private OrderPhaseChange CreateOrderChange(OrderPhase? oldPhase)
        {
            Logger.InfoFormat(string.Format("orderId = {0}, current Phase = {1}, oldPhase = {2}", this.Id, this.Phase, oldPhase));
            if (oldPhase == this.Phase) return null;
            if (this.Phase == OrderPhase.Executed)
            {
                return this.CreateOrderChange((oldPhase == null && !this.Owner.PlacedByRiskMonitor ? OrderChangeType.Cut : OrderChangeType.Executed), this);
            }
            else if (this.Phase == OrderPhase.Placing)
            {
                return this.CreateOrderChange(OrderChangeType.Placing, this);
            }
            else if (this.Phase == OrderPhase.Placed)
            {
                return this.CreateOrderChange(OrderChangeType.Placed, this);
            }
            else if (this.Phase == OrderPhase.Canceled)
            {
                return this.CreateOrderChange(OrderChangeType.Canceled, this);
            }
            else if (this.Phase == OrderPhase.Deleted)
            {
                return this.CreateOrderChange(OrderChangeType.Deleted, this);
            }
            else
            {
                return null;
            }
        }

        protected virtual OrderPhaseChange CreateOrderChange(OrderChangeType changeType, Order order)
        {
            return new OrderPhaseChange(changeType, order);
        }



        private void UpdateOrderProperties(XElement orderNode)
        {
            this.UpdateProperties(orderNode);
            XElement billsElement = orderNode.Element("Bills");
            if (billsElement != null && billsElement.Elements("Bill").Count() > 0)
            {
                this.UpdateFees(billsElement);
            }
            XElement orderRelationsElement = orderNode.Element("OrderRelations");
            if (orderRelationsElement != null && orderRelationsElement.Elements("OrderRelation").Count() > 0)
            {
                this.UpdateOrderRelations(orderRelationsElement);
            }
        }

        private void UpdateOrderRelations(XElement orderRelationsNode)
        {
            foreach (XElement eachOrderRelationNode in orderRelationsNode.Elements("OrderRelation"))
            {
                Guid openOrderId = Guid.Parse(eachOrderRelationNode.Attribute("OpenOrderID").Value);
                OrderRelation orderRelation;
                if (!this.TryGetOrderRelation(openOrderId, this.Id, out orderRelation))
                {
                    orderRelation = this.CreateOrderRelation(this);
                    OrderRelations.Add(orderRelation);
                }
                orderRelation.Update(eachOrderRelationNode);
            }
        }

        private bool TryGetOrderRelation(Guid openOrderId, Guid closeOrderId, out OrderRelation orderRelation)
        {
            orderRelation = null;
            foreach (var eachOrderRelation in this.OrderRelations)
            {
                if (eachOrderRelation.OpenOrderId == openOrderId && eachOrderRelation.Owner.Id == closeOrderId)
                {
                    orderRelation = eachOrderRelation;
                    return true;
                }
            }
            return false;
        }



        protected virtual OrderRelation CreateOrderRelation(Order order)
        {
            return new OrderRelation(order);
        }

        private void UpdateProperties(XElement orderNode)
        {
            this.InitializeProperties(orderNode);
        }

        private void UpdateFees(XElement billsNode)
        {
            Debug.WriteLine("parse bills");
            foreach (XElement eachBillNode in billsNode.Elements("Bill"))
            {
                BillType billType = (BillType)(int.Parse(eachBillNode.Attribute("Type").Value));

                if (billType == BillType.Commission)
                {
                    this.CommissionSum += decimal.Parse(eachBillNode.Attribute("Value").Value);
                }
                else if (billType == BillType.OtherFee)
                {
                    this.OtherFeeSum += decimal.Parse(eachBillNode.Attribute("Value").Value);
                }
                else if (billType == BillType.Levy)
                {
                    this.LevySum += decimal.Parse(eachBillNode.Attribute("Value").Value);
                }
                else if (billType == BillType.InterestPL)
                {
                    bool isValued = bool.Parse(eachBillNode.Attribute("IsValued").Value);
                    if (!isValued)
                    {
                        this.InterestPLNotValued += decimal.Parse(eachBillNode.Attribute("Value").Value);
                    }
                }
                else if (billType == BillType.StoragePL)
                {
                    bool isValued = bool.Parse(eachBillNode.Attribute("IsValued").Value);
                    if (!isValued)
                    {
                        this.StoragePLNotValued += decimal.Parse(eachBillNode.Attribute("Value").Value);
                    }
                }
                else if (billType == BillType.TradePL)
                {
                    bool isValued = bool.Parse(eachBillNode.Attribute("IsValued").Value);
                    if (!isValued)
                    {
                        this.TradePLNotValued += decimal.Parse(eachBillNode.Attribute("Value").Value);
                    }
                }
                else if (billType == BillType.PaidPledge)
                {
                    this.PaidPledge += decimal.Parse(eachBillNode.Attribute("Value").Value);
                }
                else if (billType == BillType.InstalmentAdministrationFee)
                {
                    this.InstalmentAdministrationFee += decimal.Parse(eachBillNode.Attribute("Value").Value);
                }
            }

        }

        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.Id, "ID");
            this.FillProperty(m => m.Code);
            this.FillProperty(m => m.OriginalCode, "OriginCode");
            this.FillProperty(m => m.IsOpen);
            this.FillProperty(m => m.IsBuy);
            this.FillProperty(m => m.Phase);
            this.FillProperty(m => m.PlacedByRiskMonitor);
            this.FillProperty(m => m.TradeOption);
            this.FillProperty(m => m.SetPrice);
            this.FillProperty(m => m.SetPrice2);
            this.FillProperty(m => m.Lot);
            this.FillProperty(m => m.OriginalLot);
            this.FillProperty(m => m.DQMaxMove);
            this.FillProperty(m => m.SetPriceMaxMovePips);

            this.FillProperty(m => m.HitCount);
            this.FillProperty(m => m.BestPrice);
            this.FillProperty(m => m.BestTime);

            this.FillProperty(m => m.PhysicalTradeSide);
            this.FillProperty(m => m.PhysicalValueMatureDay);
            this.FillProperty(m => m.InterestValueDate);

            this.FillProperty(m => m.LotBalance);
            this.FillProperty(m => m.DeliveryLockLot);

            this.FillProperty(m => m.MinLot);
            this.FillProperty(m => m.MaxShow);

            this.FillProperty(m => m.ExecutePrice);
            this.FillProperty(m => m.LivePrice);
            this.FillProperty(m => m.Necessary);

            this.FillProperty(m => m.JudgePrice);
            this.FillProperty(m => m.JudgePriceTimestamp);

            this.FillProperty(m => m.ValueAsMargin);
            this.FillProperty(m => m.PaidPledge);
            this.FillProperty(m => m.PaidPledgeBalance);
            this.FillProperty(m => m.PhysicalOriginValue);
            this.FillProperty(m => m.PhysicalOriginValueBalance);

            this.FillProperty(m => m.InterestPerLot);
            this.FillProperty(m => m.StoragePerLot);

            this.FillProperty(m => m.InterestPLFloat);
            this.FillProperty(m => m.StoragePLFloat);
            this.FillProperty(m => m.TradePLFloat);

            this.FillProperty(m => m.CancelReason);

            this.FillProperty(m => m.IsInstalmentOverdue);

            this.FillProperty(m => m.PhysicalRequestId);

            this.FillProperty(m => m.InstalmentPolicyId);
            this.FillProperty(m => m.InstalmentType, "PhysicalInstalmentType");
            this.FillProperty(m => m.Period);
            this.FillProperty(m => m.DownPaymentBasis);
            this.FillProperty(m => m.DownPayment);
            this.FillProperty(m => m.RecalculateRateType);
            this.FillProperty(m => m.InstalmentFrequence, "Frequence");
            this.FillProperty(m => m.PhysicalType);
            this.FillProperty(m => m.AdvanceAmount);
            this.FillProperty(m => m.BinaryOptionBetTypeId);
            this.FillProperty(m => m.BinaryOptionFrequency, "BOFrequency");
            this.FillProperty(m => m.BinaryOptionOdds, "BOOdds");
            this.FillProperty(m => m.BinaryOptionBetOption, "BOBetOption");
            this.FillProperty(m => m.IsAutoFill);
            this.FillProperty(m => m.EstimateCloseLevy);
            this.FillProperty(m => m.EstimateCloseCommission);
        }
    }

}
