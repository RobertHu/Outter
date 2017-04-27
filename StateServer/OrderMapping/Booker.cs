using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Xml;
using iExchange.Common;
using System.Threading;
using System.Data.SqlClient;
using log4net;
using System.Threading.Tasks;

namespace iExchange.StateServer.OrderMapping
{
    internal delegate TransactionError BookExecutor(MappedOrder order);

    internal class MappedOrderRelation
    {
        internal Guid CloseOrderID { get; private set; }
        private Guid OpenOrderID;
        private decimal ClosedLot;

        private MappedOrderRelation() { }

        internal static MappedOrderRelation From(DataRow row)
        {
            MappedOrderRelation relation = new MappedOrderRelation();

            relation.CloseOrderID = (Guid)row["MappedOrderID"];
            relation.OpenOrderID = (Guid)row["OpenOrderID"];
            relation.ClosedLot = (decimal)row["ClosedLot"];

            return relation;
        }

        internal XmlNode ToXmlNode(XmlDocument document, DateTime closeTime)
        {
            XmlElement node = document.CreateElement("OrderRelation");
            node.SetAttribute("OpenOrderID", XmlConvert.ToString(this.OpenOrderID));
            node.SetAttribute("ClosedLot", XmlConvert.ToString(this.ClosedLot));
            node.SetAttribute("CloseTime", XmlConvert.ToString(closeTime, "yyyy-MM-dd HH:mm:ss"));
            node.SetAttribute("Commission", XmlConvert.ToString(0));
            node.SetAttribute("Levy", XmlConvert.ToString(0));
            node.SetAttribute("InterestPL", XmlConvert.ToString(0));
            node.SetAttribute("StoragePL", XmlConvert.ToString(0));
            node.SetAttribute("TradePL", XmlConvert.ToString(0));
            return node;
        }
    }

    public class MappedOrderComparer : IComparer<MappedOrder>
    {
        internal static MappedOrderComparer Default = new MappedOrderComparer();

        int IComparer<MappedOrder>.Compare(MappedOrder x, MappedOrder y)
        {
            return x.ExecuteTime.CompareTo(y.ExecuteTime);
        }
    }

    internal class MappedOrder
    {
        internal Guid ID { get; private set; }
        private Guid TransactionID;
        private int TransactionType;
        private int OrderTypeID;
        internal Guid AccountID { get; private set; }
        private Guid InstrumentID;
        private decimal ContractSize;
        private DateTime BeginTime;
        private DateTime EndTime;
        private int ExpireType;
        private DateTime SubmitTime;
        internal DateTime ExecuteTime { get; private set; }
        private Guid SubmitorID;
        private Guid ApproverID;
        private int TradeOption;
        private bool IsOpen;
        private bool IsBuy;
        private string SetPrice;
        private string SetPrice2;
        private int SetPriceMaxMovePips;
        private string ExecutePrice;
        private decimal Lot;
        private decimal OriginalLot;
        private decimal LotBalance;
        private int DQMaxMove;
        private int TransactionSubType;
        private int InstrumentCategory;
        internal bool IsLocal { get; private set; }

        private List<MappedOrderRelation> relations = new List<MappedOrderRelation>();

        private MappedOrder() { }

        internal void AddRelation(MappedOrderRelation relation)
        {
            this.relations.Add(relation);
        }

        internal static MappedOrder From(DataRow row)
        {
            MappedOrder order = new MappedOrder();
            order.ID = (Guid)row["ID"];
            order.TransactionID = (Guid)row["TransactionID"];
            order.TransactionType = (byte)row["TransactionType"];
            order.OrderTypeID = (int)row["OrderTypeID"];
            order.AccountID = (Guid)row["AccountID"];
            order.InstrumentID = (Guid)row["InstrumentID"];
            order.ContractSize = (decimal)row["ContractSize"];
            order.BeginTime = (DateTime)row["BeginTime"];
            order.EndTime = (DateTime)row["EndTime"];
            order.ExpireType = (int)row["ExpireType"];
            order.SubmitTime = (DateTime)row["SubmitTime"];
            order.ExecuteTime = (DateTime)row["ExecuteTime"];
            order.SubmitorID = (Guid)row["SubmitorID"];
            order.ApproverID = row["ApproverID"] == DBNull.Value ? Guid.Empty : (Guid)row["ApproverID"];
            order.TradeOption = (byte)row["TradeOption"];
            order.IsOpen = (bool)row["IsOpen"];
            order.IsBuy = (bool)row["IsBuy"];
            order.SetPrice = row["SetPrice"] == DBNull.Value ? null : (string)row["SetPrice"];
            order.SetPrice2 = row["SetPrice2"] == DBNull.Value ? null : (string)row["SetPrice2"];
            order.SetPriceMaxMovePips = (int)row["SetPriceMaxMovePips"];
            order.ExecutePrice = (string)row["ExecutePrice"];
            order.Lot = (decimal)row["Lot"];
            order.OriginalLot = (decimal)row["OriginalLot"];
            order.LotBalance = (decimal)row["LotBalance"];
            order.DQMaxMove = (int)row["DQMaxMove"];
            order.TransactionSubType = (byte)row["TransactionSubType"];
            order.InstrumentCategory = (int)row["InstrumentCategory"];
            order.IsLocal = (bool)row["IsLocal"];
            return order;
        }


        internal XmlNode ToXmlNode()
        {
            XmlDocument document = new XmlDocument();
            XmlElement tranNode = document.CreateElement("Transaction");
            tranNode.SetAttribute("ID", XmlConvert.ToString(this.TransactionID));
            tranNode.SetAttribute("Code", "");
            tranNode.SetAttribute("InstrumentID", XmlConvert.ToString(this.InstrumentID));
            tranNode.SetAttribute("AccountID", XmlConvert.ToString(this.AccountID));
            tranNode.SetAttribute("InstrumentCategory", XmlConvert.ToString(this.InstrumentCategory));
            tranNode.SetAttribute("Type", XmlConvert.ToString(this.TransactionType));
            tranNode.SetAttribute("SubType", XmlConvert.ToString(this.TransactionSubType));
            tranNode.SetAttribute("OrderType", XmlConvert.ToString(this.OrderTypeID));
            tranNode.SetAttribute("Phase", XmlConvert.ToString((int)TransactionPhase.Executed));
            tranNode.SetAttribute("BeginTime", XmlConvert.ToString(this.BeginTime, "yyyy-MM-dd HH:mm:ss"));
            tranNode.SetAttribute("EndTime", XmlConvert.ToString(this.EndTime, "yyyy-MM-dd HH:mm:ss"));
            tranNode.SetAttribute("SubmitTime", XmlConvert.ToString(this.SubmitTime, "yyyy-MM-dd HH:mm:ss"));
            tranNode.SetAttribute("ExecuteTime", XmlConvert.ToString(this.ExecuteTime, "yyyy-MM-dd HH:mm:ss"));
            tranNode.SetAttribute("ExpireType", XmlConvert.ToString(this.ExpireType));
            tranNode.SetAttribute("SubmitorID", XmlConvert.ToString(this.SubmitorID));
            tranNode.SetAttribute("ApproverID", XmlConvert.ToString(this.ApproverID));
            tranNode.SetAttribute("ContractSize", XmlConvert.ToString(this.ContractSize));

            XmlElement orderNode = document.CreateElement("Order");
            tranNode.AppendChild(orderNode);
            orderNode.SetAttribute("ID", XmlConvert.ToString(this.ID));
            orderNode.SetAttribute("TradeOption", XmlConvert.ToString((int)this.TradeOption));
            orderNode.SetAttribute("IsOpen", XmlConvert.ToString(this.IsOpen));
            orderNode.SetAttribute("IsBuy", XmlConvert.ToString(this.IsBuy));
            orderNode.SetAttribute("PhysicalTradeSide", XmlConvert.ToString((int)(PhysicalTradeSide.None)));
            if (this.SetPrice != null) orderNode.SetAttribute("SetPrice", this.SetPrice);
            if (this.SetPrice2 != null) orderNode.SetAttribute("SetPrice2", this.SetPrice2);

            if (this.SetPriceMaxMovePips != 0) orderNode.SetAttribute("SetPriceMaxMovePips", XmlConvert.ToString(this.SetPriceMaxMovePips));
            if (this.DQMaxMove != 0) orderNode.SetAttribute("DQMaxMove", XmlConvert.ToString(this.DQMaxMove));
            orderNode.SetAttribute("ExecutePrice", this.ExecutePrice);
            //node.SetAttribute("ExecuteTradeDay", XmlConvert.ToString(this.ExecuteTradeDay, DateTimeFormat.Xml));
            orderNode.SetAttribute("OriginalLot", XmlConvert.ToString(this.OriginalLot));
            orderNode.SetAttribute("Lot", XmlConvert.ToString(this.Lot));
            orderNode.SetAttribute("LotBalance", XmlConvert.ToString(this.LotBalance));
            orderNode.SetAttribute("CommissionSum", XmlConvert.ToString(0));
            orderNode.SetAttribute("LevySum", XmlConvert.ToString(0));
            orderNode.SetAttribute("OtherFeeSum", XmlConvert.ToString(0));
            orderNode.SetAttribute("LivePrice", this.ExecutePrice);
            orderNode.SetAttribute("InterestPerLot", XmlConvert.ToString(0));
            orderNode.SetAttribute("StoragePerLot", XmlConvert.ToString(0));
            orderNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(0));
            orderNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(0));
            orderNode.SetAttribute("TradePLFloat", XmlConvert.ToString(0));

            foreach (MappedOrderRelation relation in this.relations)
            {
                orderNode.AppendChild(relation.ToXmlNode(document, this.ExecuteTime));
            }

            return tranNode;
        }

        internal void Adjust()
        {
            this.EndTime = DateTime.Now + TimeSpan.FromMinutes(10);
            this.OrderTypeID = (int)iExchange.Common.OrderType.Risk;
            if (this.TransactionType == (int)iExchange.Common.TransactionType.OneCancelOther)
            {
                this.TransactionType = (int)iExchange.Common.TransactionType.Single;
            }
        }
    }

    internal class Booker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Booker));

        private sealed class BookWatcher
        {
            private sealed class LastTryCount
            {
                public LastTryCount()
                {
                    this.Value = 0;
                }
                public int Value;
            }

            private static readonly ILog Logger = LogManager.GetLogger(typeof(BookWatcher));

            private object _mutex = new object();

            private const int RETRY_COUNT = 10;

            private Dictionary<Guid, LastTryCount> _mappingOrders = new Dictionary<Guid, LastTryCount>(100);

            private Booker _booker;
            private List<Guid> _toBeRemovedOrderIds = new List<Guid>();

            internal BookWatcher(Booker booker)
            {
                _booker = booker;
                _booker.Start();
                new Thread(this.DoWork) { IsBackground = true }.Start();
            }

            internal void Add(Guid orderId)
            {
                lock (_mutex)
                {
                    _mappingOrders.Add(orderId, new LastTryCount());
                }
            }

            private void DoWork()
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    lock (_mutex)
                    {
                        try
                        {
                            _toBeRemovedOrderIds.Clear();
                            foreach (var eachOrderId in _mappingOrders.Keys)
                            {
                                if (_booker.BookOrderMappedByNewVersion(eachOrderId))
                                {
                                    _toBeRemovedOrderIds.Add(eachOrderId);
                                }
                                else
                                {
                                    _mappingOrders[eachOrderId].Value++;

                                    if (_mappingOrders[eachOrderId].Value >= RETRY_COUNT)
                                    {
                                        _toBeRemovedOrderIds.Add(eachOrderId);
                                    }
                                }
                            }
                            foreach (var eachOrderId in _toBeRemovedOrderIds)
                            {
                                _mappingOrders.Remove(eachOrderId);
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }

                }
            }
        }


        private bool started = false;
        private string connectionStr;
        private Thread bookThread = null;
        private List<MappedOrder> waitingForBookOrders = new List<MappedOrder>();
        private object waitingForBookOrdersLocker = new object();

        private List<Guid> hasErrorAccounts = new List<Guid>();
        private BookExecutor bookExecutor;
        private BookWatcher _bookWatcher;

        internal Booker(string connectionStr, BookExecutor bookExecutor)
        {
            this.connectionStr = connectionStr;
            this.bookExecutor = bookExecutor;
            if (!StateServer.ShouldUserTransactionServer())
            {
                _bookWatcher = new BookWatcher(this);
            }
        }

        internal bool Start()
        {
            if (started) return true;

            this.waitingForBookOrders.Clear();
            this.hasErrorAccounts.Clear();

            if (!this.InitMappedOrders()) return false;

            this.started = true;
            bookThread = new Thread(this.BookMappedOrder);
            bookThread.IsBackground = true;
            bookThread.Start();

            return true;
        }

        internal void Add(XmlNode xmlTran)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (XmlNode xmlOrder in xmlTran.ChildNodes)
                    {
                        bool isPhysicalOrder = false;
                        PhysicalTradeSide physicalTradeSide = PhysicalTradeSide.None;
                        if (xmlOrder.Attributes["PhysicalTradeSide"] != null)
                        {
                            physicalTradeSide = (PhysicalTradeSide)(XmlConvert.ToInt32(xmlOrder.Attributes["PhysicalTradeSide"].Value));

                        }
                        isPhysicalOrder = physicalTradeSide != PhysicalTradeSide.None;
                        if (isPhysicalOrder) return;
                        Guid orderID = XmlConvert.ToGuid(xmlOrder.Attributes["ID"].Value);
                        _bookWatcher.Add(orderID);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        private bool InitMappedOrders()
        {
            try
            {
                this.GetMappedOrders();

                return true;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", string.Format("OrderMapping.Booker.InitMappedOrders failed, \r\n{0}", exception), System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        private void GetMappedOrders(Guid? sourceOrderId = null)
        {
            DataSet ds = this.GetMapperData(sourceOrderId);
            this.ProcessMappedOrders(ds);
        }


        private void ProcessMappedOrders(DataSet dataSet)
        {
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                Dictionary<Guid, MappedOrder> orders = new Dictionary<Guid, MappedOrder>();
                foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                {
                    MappedOrder order = MappedOrder.From(dataRow);
                    this.waitingForBookOrders.Add(order);
                    orders.Add(order.ID, order);
                }

                foreach (DataRow dataRow in dataSet.Tables[1].Rows)
                {
                    MappedOrderRelation relation = MappedOrderRelation.From(dataRow);
                    orders[relation.CloseOrderID].AddRelation(relation);
                }

                this.waitingForBookOrders.Sort(MappedOrderComparer.Default);
            }
        }


        private DataSet GetMapperData(Guid? sourceOrderId)
        {
            using (SqlConnection connection = new SqlConnection(this.connectionStr))
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "MappedOrder_GetNotBooked";
                command.CommandType = CommandType.StoredProcedure;
                if (sourceOrderId != null) command.Parameters.Add("@sourceOrderId", SqlDbType.UniqueIdentifier).Value = sourceOrderId.Value;
                connection.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);
                return dataSet;
            }
        }



        internal void BookOrderMappedBy(Guid sourceOrderId)
        {
            this.Start();

            try
            {
                this.GetMappedOrders(sourceOrderId);
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", string.Format("OrderMapping.Booker.BookOrderMappedBy failed, sourceOrderId = {0} \r\n{1}", sourceOrderId, exception), System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        internal bool BookOrderMappedByNewVersion(Guid sourceOrderId)
        {
            try
            {
                DataSet ds = this.GetMapperData(sourceOrderId);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    Logger.InfoFormat(" GetMappedData success sourceOrderId = {0}", sourceOrderId);
                    this.ProcessMappedOrders(ds);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("OrderMapping.Booker.BookOrderMappedBy failed, sourceOrderId = {0} \r\n{1}", sourceOrderId, ex);
                return false;
            }
        }


        internal void Stop()
        {
            if (!started) return;
            this.started = false;
        }

        private bool Book(MappedOrder order)
        {
            order.Adjust();
            TransactionError error = this.bookExecutor(order);

            if (error != TransactionError.RuntimeError)
            {
                if (error != TransactionError.OK)
                {
                    AppDebug.LogEvent("StateServer", string.Format("Book failed, error = {0} \n {1}", error, order.ToXmlNode().OuterXml), System.Diagnostics.EventLogEntryType.Warning);
                }

                this.TagAsBooked(order, string.Format("Book restult is {0}", error));

                return true;
            }
            else
            {
                AppDebug.LogEvent("StateServer", string.Format("Book failed, error = {0} \n {1}", error, order.ToXmlNode().OuterXml), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }
        }

        private void TagAsBooked(MappedOrder order, string info)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(this.connectionStr))
                {
                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = "MappedOrder_TagAsBooked";
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();
                    command.Parameters.Add("@orderId", SqlDbType.UniqueIdentifier).Value = order.ID;
                    command.Parameters.Add("@bookResult", SqlDbType.NVarChar).Value = info;

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", string.Format("TagAsBooked failed, error = {0} \n {1}", exception, order.ToXmlNode().OuterXml), System.Diagnostics.EventLogEntryType.Warning);
            }
        }
        private void BookMappedOrder(object state)
        {
            while (this.started)
            {
                if (this.waitingForBookOrders.Count > 0)
                {
                    int index = 0;
                    MappedOrder order = null;
                    while (index < this.waitingForBookOrders.Count)//get order which account doesn't have error
                    {
                        order = this.waitingForBookOrders[index];
                        if (!this.hasErrorAccounts.Contains(order.AccountID))
                        {
                            break;
                        }
                        else
                        {
                            index++;
                            order = null;
                        }
                    }

                    if (order == null)//if no order which account doesn't have error, try to book the first order
                    {
                        order = this.waitingForBookOrders[0];
                    }

                    if (this.Book(order))
                    {
                        this.waitingForBookOrders.Remove(order);
                    }
                    else
                    {
                        if (!this.hasErrorAccounts.Contains(order.AccountID)) hasErrorAccounts.Add(order.AccountID);
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}