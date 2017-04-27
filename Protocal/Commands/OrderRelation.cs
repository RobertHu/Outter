using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public class OrderRelation : XmlFillable<OrderRelation>
    {
        public Order Owner { get; private set; }
        public Guid OpenOrderId { get; private set; }
        public decimal ClosedLot { get; private set; }
        public DateTime CloseTime { get; private set; }

        public decimal Commission { get; private set; }
        public decimal Levy { get; private set; }
        public decimal OtherFee { get; private set; }
        public decimal InterestPL { get; private set; }
        public decimal StoragePL { get; private set; }
        public decimal TradePL { get; private set; }
        public decimal PhysicalTradePL { get; private set; }
        public decimal PayBackPledgeOfOpenOrder { get; private set; }
        public decimal ClosedPhysicalValue { get; private set; }
        public decimal PhysicalValue { get; private set; }
        public decimal OverdueCutPenalty { get; private set; }
        public decimal ClosePenalty { get; private set; }

        public DateTime? PhysicalValueMatureDay { get; private set; }

        public DateTime ValueTime { get; private set; }
        public decimal RateIn { get; private set; }
        public decimal RateOut { get; private set; }
        public int Decimals { get; private set; }

        public DateTime OpenOrderExecuteTime { get; set; }

        public string OpenOrderExecutePrice { get; set; }

        public decimal EstimateCloseCommissionOfOpenOrder { get; private set; }
        public decimal EstimateCloseLevyOfOpenOrder { get; private set; }


        public OrderRelation(Order owner)
        {
            this.Owner = owner;
        }

        public void Update(XElement node)
        {
            Debug.WriteLine(string.Format("update orderRelation, {0}", node.ToString()));
            this.InitializeProperties(node);
        }


        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.OpenOrderId, "OpenOrderID");
            this.FillProperty(m => m.ClosedLot);
            this.FillProperty(m => m.CloseTime);
            this.FillProperty(m => m.ValueTime);
            this.FillProperty(m => m.RateIn);
            this.FillProperty(m => m.RateOut);
            this.FillProperty(m => m.Decimals, "TargetDecimals");
            this.FillProperty(m => m.ClosedPhysicalValue);
            this.FillProperty(m => m.PhysicalValue);
            this.FillProperty(m => m.PhysicalValueMatureDay);
            this.FillProperty(m => m.Commission);
            this.FillProperty(m => m.Levy);
            this.FillProperty(m => m.OtherFee);
            this.FillProperty(m => m.InterestPL);
            this.FillProperty(m => m.StoragePL);
            this.FillProperty(m => m.TradePL);
            this.FillProperty(m => m.OpenOrderExecuteTime);
            this.FillProperty(m => m.OpenOrderExecutePrice);
            this.FillProperty(m => m.EstimateCloseCommissionOfOpenOrder);
            this.FillProperty(m => m.EstimateCloseLevyOfOpenOrder);
        }
    }

}
