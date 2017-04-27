using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public class Transaction : XmlFillable<Transaction>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Transaction));

        protected List<Order> _orders = new List<Order>();
        public Transaction(Account owner)
        {
            this.Owner = owner;
        }

        public Account Owner { get; private set; }
        public Guid AccountId
        {
            get
            {
                return this.Owner.Id;
            }
        }

        public Guid Id { get; private set; }
        public Guid InstrumentId { get; private set; }

        public string Code { get; private set; }
        public TransactionType Type { get; private set; }
        public TransactionSubType SubType { get; private set; }
        public TransactionPhase Phase { get; private set; }
        public OrderType OrderType { get; private set; }
        public decimal ContractSize { get; private set; }
        public DateTime BeginTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public ExpireType ExpireType { get; private set; }
        public DateTime SubmitTime { get; private set; }
        public DateTime ExecuteTime { get; private set; }
        public Guid SubmitorId { get; private set; }
        public Guid? ApproverId { get; private set; }
        public Guid SourceOrderId { get; private set; }
        public InstrumentCategory InstrumentCategory { get; private set; }
        public Guid? OrderBatchInstructionId { get; private set; }
        public AppType AppType { get; private set; }
        public bool PlacedByRiskMonitor { get; private set; }


        public List<Order> Orders
        {
            get
            {
                return _orders;
            }
        }

        public void Initialize(XElement tranElement)
        {
            this.ExecuteTime = DateTime.MinValue;
            this.SourceOrderId = Guid.Empty;
            this.UpdateForInitialize(tranElement);
        }


        private void UpdateForInitialize(XElement tranElement)
        {
            this.UpdateTransactionProperties(tranElement);
            foreach (XElement orderElement in tranElement.Element("Orders").Elements("Order"))
            {
                Order order = this.CreateOrder(this);
                order.Update(orderElement);
                this._orders.Add(order);
            }
        }

        public List<OrderPhaseChange> Update(XElement tranElement)
        {
            this.UpdateTransactionProperties(tranElement);
            return this.UpdateOrders(tranElement);
        }

        protected virtual List<OrderPhaseChange> UpdateOrders(XElement tranElement)
        {
            List<OrderPhaseChange> result = new List<OrderPhaseChange>();
            if (tranElement.Element("Orders") == null || tranElement.Element("Orders").Elements("Order") == null) return result;
            foreach (XElement eachOrderElement in tranElement.Element("Orders").Elements("Order"))
            {
                OrderPhaseChange orderChange = this.UpdateOrder(eachOrderElement);
                if (orderChange != null)
                {
                    Logger.InfoFormat("OrderChange = {0}", orderChange);
                    result.Add(orderChange);
                }
            }
            return result;
        }

        private OrderPhaseChange UpdateOrder(XElement orderElement)
        {
            Order order = null;
            Guid orderId = Guid.Parse(orderElement.Attribute("ID").Value);
            if (!_orders.Exists(m => m.Id == orderId))
            {
                order = this.CreateOrder(this);
                this._orders.Add(order);
            }
            else
            {
                order = this.FindOrder(orderId);
            }
            return order.Update(orderElement);
        }

        protected virtual Order CreateOrder(Transaction tran)
        {
            return new Order(tran);
        }


        private Order FindOrder(Guid orderId)
        {
            foreach (var eachOrder in _orders)
            {
                if (eachOrder.Id == orderId) return eachOrder;
            }
            return null;
        }


        private void UpdateTransactionProperties(XElement tranElement)
        {
            this.InitializeProperties(tranElement);
        }


        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.Id, "ID");
            this.FillProperty(m => m.InstrumentId, "InstrumentID");
            this.FillProperty(m => m.SubmitorId, "SubmitorID");
            this.FillProperty(m => m.ApproverId, "ApproverID");
            this.FillProperty(m => m.Code);
            this.FillProperty(m => m.Type);
            this.FillProperty(m => m.SubType);
            this.FillProperty(m => m.OrderType);
            this.FillProperty(m => m.Phase);
            this.FillProperty(m => m.ContractSize);
            this.FillProperty(m => m.BeginTime);
            this.FillProperty(m => m.EndTime);
            this.FillProperty(m => m.SubmitTime);
            this.FillProperty(m => m.ExecuteTime);
            this.FillProperty(m => m.ExpireType);
            this.FillProperty(m => m.InstrumentCategory);
            this.FillProperty(m => m.SourceOrderId, "AssigningOrderID");
            this.FillProperty(m => m.OrderBatchInstructionId, "OrderBatchInstructionID");
            this.FillProperty(m => m.PlacedByRiskMonitor);
        }
    }


}
