using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using iExchange.Common.Manager;
using iExchange.Common;
using System.Diagnostics;

namespace iExchange.StateServer.Manager
{
    public class ManagerHelper
    {
        private static Dictionary<string, string> _OrderRelationOpenPrics = new Dictionary<string, string>();

        internal static XmlNode ConvertQuotationXml(List<Answer> answerQutos)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Instrument");
            root.SetAttribute("ID", answerQutos[0].InstrumentId.ToString());
            root.SetAttribute("Origin", answerQutos[0].Origin);
            foreach (Answer answer in answerQutos)
            {
                XmlElement answerNode = doc.CreateElement("Customer");
                answerNode.SetAttribute("ID",answer.CustomerId.ToString());
                answerNode.SetAttribute("Ask",answer.Ask);
                answerNode.SetAttribute("Bid",answer.Bid);
                answerNode.SetAttribute("QuoteLot",answer.QuoteLot.ToString());
                answerNode.SetAttribute("AnswerLot", answer.AnswerLot.ToString());
                root.AppendChild(answerNode);
            }
            doc.AppendChild(root);
            return doc.DocumentElement;
        }

        internal static XmlNode GetInstrumentParametersXml(ParameterUpdateTask settingTask)
        {
            XmlDocument exchangeDoc = new XmlDocument();
            XmlElement xmlInstrumentRoot = exchangeDoc.CreateElement("Instruments");

            foreach (Guid instrumentId in settingTask.Instruments)
            {
                XmlElement instrumentElement = exchangeDoc.CreateElement("Instrument");
                instrumentElement.SetAttribute("ID", instrumentId.ToString());
                foreach (ExchangeSetting setting in settingTask.ExchangeSettings)
                {
                    instrumentElement.SetAttribute(setting.ParameterKey, setting.ParameterValue);
                }
                xmlInstrumentRoot.AppendChild(instrumentElement);
            }
            exchangeDoc.AppendChild(xmlInstrumentRoot);
            return exchangeDoc.DocumentElement;
        }

        internal static XmlNode GetUpdateNodeFromDic(List<Dictionary<string, string>> fileValueList,string NodeKey)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement rootNode = doc.CreateElement(NodeKey + "s");

            foreach (Dictionary<string, string> fileValues in fileValueList)
            {
                XmlElement childNode = doc.CreateElement(NodeKey);

                foreach (KeyValuePair<string, string> fileValue in fileValues)
                {
                    childNode.SetAttribute(fileValue.Key, fileValue.Value);
                }

                rootNode.AppendChild(childNode);
            }
            doc.AppendChild(rootNode);
            return doc.DocumentElement;
        }

        internal static AccountInformation GetAcountInfo(XmlNode accountInforNode)
        {
            AccountInformation accountInfo = new AccountInformation() { IsTransactionCanceledOrCannotExecute = false, InstrumentId = Guid.Empty };

            if (accountInforNode.Name == "TransactionCanceledOrCannotExecute")
            {
                accountInfo.IsTransactionCanceledOrCannotExecute = true;
            }
            else
            {
                foreach (XmlAttribute attribute in accountInforNode.Attributes)
                {
                    string nodeName = attribute.Name;
                    string nodeValue = attribute.Value;
                    if (nodeName == "ID")
                    {
                        accountInfo.AccountId = new Guid(nodeValue);
                        continue;
                    }
                    else if (nodeName == "Balance")
                    {
                        accountInfo.Balance = decimal.Parse(nodeValue);
                        continue;
                    }
                    else if (nodeName == "Equity")
                    {
                        accountInfo.Equity = decimal.Parse(nodeValue);
                        continue;
                    }
                    else if (nodeName == "Necessary")
                    {
                        accountInfo.Necessary = decimal.Parse(nodeValue);
                        continue;
                    }
                }

                if (accountInforNode.ChildNodes.Count > 0)
                {
                    XmlNode instrumentNode = accountInforNode.ChildNodes[0];
                    accountInfo.InstrumentId = new Guid(instrumentNode.Attributes["ID"].Value);
                    accountInfo.BuyLotBalanceSum = decimal.Parse(instrumentNode.Attributes["BuyLotBalanceSum"].Value);
                    accountInfo.SellLotBalanceSum = decimal.Parse(instrumentNode.Attributes["SellLotBalanceSum"].Value);
                }
            }
            return accountInfo;
        }

        #region Report
        internal static List<AccountGroupGNP> GetGroupNetPosition(XmlNode netXmlNode)
        {
            if (netXmlNode == null) return null;
            List<AccountGroupGNP> accountGroups = new List<AccountGroupGNP>();

            foreach (XmlNode groupNode in netXmlNode.ChildNodes)
            {
                Guid groupId = new Guid(groupNode.Attributes["ID"].Value);
                string groupCode = groupNode.Attributes["Code"].Value;
                AccountGroupGNP accountGroupGNP = new AccountGroupGNP(groupId, groupCode);

                foreach (XmlNode accountNode in groupNode.ChildNodes[0].ChildNodes)
                {
                    Guid accountId = new Guid(accountNode.Attributes["ID"].Value);
                    string accountCode = accountNode.Attributes["Code"].Value;
                    AccountType type = (AccountType)Enum.ToObject(typeof(AccountType), int.Parse(accountNode.Attributes["Type"].Value));
                    AccountGNP accountGNP = new AccountGNP(accountId, accountCode, type);

                    foreach (XmlNode instrumentNode in accountNode.ChildNodes[0].ChildNodes)
                    {
                        Guid instrumentId = new Guid(instrumentNode.Attributes["ID"].Value);
                        decimal lotBalance = decimal.Parse(instrumentNode.Attributes["LotBalance"].Value);
                        decimal quantity = decimal.Parse(instrumentNode.Attributes["Quantity"].Value);
                        decimal buyQuantity = decimal.Parse(instrumentNode.Attributes["BuyQuantity"].Value);
                        string buyAveragePrice = instrumentNode.Attributes["BuyAveragePrice"].Value;
                        decimal buyMultiplyValue = decimal.Parse(instrumentNode.Attributes["BuyMultiplyValue"].Value);
                        decimal sellQuantity = decimal.Parse(instrumentNode.Attributes["SellQuantity"].Value);
                        string sellAveragePrice = instrumentNode.Attributes["SellAveragePrice"].Value;
                        decimal sellMultiplyValue = decimal.Parse(instrumentNode.Attributes["SellMultiplyValue"].Value);
                        decimal sellLot = decimal.Parse(instrumentNode.Attributes["SellLot"].Value);
                        decimal buyLot = decimal.Parse(instrumentNode.Attributes["BuyLot"].Value);
                        decimal sellSumEl = decimal.Parse(instrumentNode.Attributes["SellSumEl"].Value);
                        decimal buySumEl = decimal.Parse(instrumentNode.Attributes["BuySumEl"].Value);
                        InstrumentGNP instrumentGNP = new InstrumentGNP(instrumentId);
                        instrumentGNP.LotBalance = lotBalance;
                        instrumentGNP.Quantity = quantity;
                        instrumentGNP.BuyQuantity = buyQuantity;
                        instrumentGNP.BuyAveragePrice = buyAveragePrice;
                        instrumentGNP.BuyMultiplyValue = buyMultiplyValue;
                        instrumentGNP.SellQuantity = sellQuantity;
                        instrumentGNP.SellAveragePrice = sellAveragePrice;
                        instrumentGNP.SellMultiplyValue = sellMultiplyValue;
                        instrumentGNP.SellLot = sellLot;
                        instrumentGNP.BuyLot = buyLot;
                        instrumentGNP.SellSumEl = sellSumEl;
                        instrumentGNP.BuySumEl = buySumEl;

                        accountGNP.InstrumentGNPs.Add(instrumentGNP);
                    }
                    accountGroupGNP.AccountGNPs.Add(accountGNP);
                }
                accountGroups.Add(accountGroupGNP);
            }

            return accountGroups;
        }

        internal static List<OpenInterestSummary> GetOpenInterestInstrumentSummary(XmlNode summaryXmlNode)
        {
            if (summaryXmlNode == null) return null;
            List<OpenInterestSummary> openInterestSummarys = new List<OpenInterestSummary>();

            foreach (XmlNode instrumentNode in summaryXmlNode.ChildNodes)
            {
                OpenInterestSummary instrumentSummary = new OpenInterestSummary();
                instrumentSummary.Initialize(instrumentNode);
                openInterestSummarys.Add(instrumentSummary);
            }
            return openInterestSummarys;
        }

        internal static List<OpenInterestSummary> GetOpenInterestAccountSummary(XmlNode accountSummaryNode)
        {
            List<OpenInterestSummary> openInterestSummarys = new List<OpenInterestSummary>();

            foreach (XmlNode summaryNode in accountSummaryNode.ChildNodes)
            {
                OpenInterestSummary summaryItem = new OpenInterestSummary();
                summaryItem.Initialize(summaryNode);
                openInterestSummarys.Add(summaryItem);
            }

            return openInterestSummarys;
        }

        internal static List<OpenInterestSummary> GetOpenInterestOrderSummary(XmlNode orderSummaryNode,AccountType accountType)
        {
            List<OpenInterestSummary> orderSummaryItems = new List<OpenInterestSummary>();

            foreach (XmlNode tranNode in orderSummaryNode)
            {
                Guid transactionId = new Guid(tranNode.Attributes["ID"].Value);
                decimal contractSize = decimal.Parse(tranNode.Attributes["ContractSize"].Value);
                Guid instrumentId = new Guid(tranNode.Attributes["InstrumentID"].Value);
                string executeTime = tranNode.Attributes["ExecuteTime"].Value;

                foreach (XmlNode orderNode in tranNode.ChildNodes)
                {
                    Guid orderId = new Guid(orderNode.Attributes["ID"].Value);
                    bool isBuy = bool.Parse(orderNode.Attributes["IsBuy"].Value);
                    decimal lotBalance = decimal.Parse(orderNode.Attributes["LotBalance"].Value);
                    string executePrice = orderNode.Attributes["ExecutePrice"].Value;

                    OpenInterestSummary orderSummaryItem = OrderSummaryItemSetItem(orderId,instrumentId,accountType, executePrice, lotBalance, isBuy, contractSize, executeTime);
                    orderSummaryItems.Add(orderSummaryItem);
                }
            }
            return orderSummaryItems;
        }

        private static OpenInterestSummary OrderSummaryItemSetItem(Guid orderId,Guid instrumentId,AccountType accountType, string executePrice, decimal lotBalance, bool isBuy, decimal contractSize, string executeTime)
        {
            OpenInterestSummary orderSummaryItem = new OpenInterestSummary();
            var executePriceValue = XmlConvert.ToDecimal(executePrice);
            decimal buyLot = isBuy ? lotBalance : decimal.Zero;
            decimal sellLot = !isBuy ? lotBalance : decimal.Zero;
            orderSummaryItem.Id = orderId;
            orderSummaryItem.InstrumentId = instrumentId;
            orderSummaryItem.Code = executeTime;
            orderSummaryItem.BuyLot = buyLot;
            orderSummaryItem.BuyAvgPrice = isBuy ? executePrice : "0";
            orderSummaryItem.BuyContractSize = buyLot * contractSize;
            orderSummaryItem.SellLot = sellLot;
            orderSummaryItem.SellAvgPrice = !isBuy ? executePrice : "0";
            orderSummaryItem.SellContractSize = sellLot * contractSize;
            if (accountType == AccountType.Company)  //Company
            {
                orderSummaryItem.NetLot = sellLot - buyLot;
                orderSummaryItem.NetContractSize = sellLot * contractSize - buyLot * contractSize;
            }
            else
            {
                orderSummaryItem.NetLot = buyLot - sellLot;
                orderSummaryItem.NetContractSize = buyLot * contractSize - sellLot * contractSize;
            }
            orderSummaryItem.NetAvgPrice = isBuy ? executePrice : "-" + executePrice;

            return orderSummaryItem;
        }
        #endregion

        #region Convert TransactionXml

        //internal static ExecutedTransaction GetExecutedTransaction(XmlNode xmlTran)
        //{
        //    Transaction[] transactions;
        //    Order[] orders;
        //    OrderRelation[] orderRelations;

        //    ManagerHelper.Parse(xmlTran, out transactions, out orders, out orderRelations);
        //    ExecutedTransaction executedTran = new ExecutedTransaction(transactions, orders, orderRelations);
        //    return executedTran;
        //}

        //internal static void Parse(XmlNode transactionNode, out Transaction[] transactions, out Order[] orders, out OrderRelation[] orderRelations)
        //{
        //    List<Transaction> transactionList = new List<Transaction>();
        //    List<Order> orderList = new List<Order>();
        //    List<OrderRelation> orderRelationList = new List<OrderRelation>();

        //    ManagerHelper.ParseTransaction(transactionNode, transactionList, orderList, orderRelationList);

        //    transactions = transactionList.ToArray();
        //    orders = orderList.ToArray();
        //    orderRelations = orderRelationList.ToArray();
        //}
        //internal static void ParseTransaction(XmlNode transactionNode, List<Transaction> transactions, List<Order> orders, List<OrderRelation> orderRelations)
        //{
        //    Transaction transaction = new Transaction();
        //    transaction.Initialize(transactionNode);
        //    transactions.Add(transaction);

        //    foreach (XmlNode orderNode in transactionNode.ChildNodes)
        //    {
        //        Order order = new Order();
        //        ManagerHelper.ParseOrder(orderNode, transaction, out order);
        //        orders.Add(order);

        //        foreach (XmlNode orderRelationNode in orderNode.ChildNodes)
        //        {
        //            string openOrderPrice = string.Empty;
        //            string openOrderID = orderRelationNode.Attributes["OpenOrderID"].Value;
        //            Guid openOrderId = new Guid(openOrderID);
        //            decimal closeLot = decimal.Parse(orderRelationNode.Attributes["ClosedLot"].Value);

        //            if (_OrderRelationOpenPrics.ContainsKey(openOrderID))
        //            {
        //                openOrderPrice = _OrderRelationOpenPrics[openOrderID];
        //            }
        //            else
        //            {
        //                openOrderPrice = GetOrderRelationOpenPrice(openOrderID);
        //                _OrderRelationOpenPrics.Add(openOrderID, openOrderPrice);
        //            }

        //            OrderRelation relation = new OrderRelation(order.Id, openOrderId, closeLot, openOrderPrice);
        //            orderRelations.Add(relation);
        //        }
        //    }
        //}

        //public static string GetOrderRelationOpenPrice(string openOrderId)
        //{
        //    string setPrice = string.Empty;
        //    string connectionString = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];
        //    string sql = string.Format("SELECT SetPrice FROM v_Order WHERE ID = N'{0}'", openOrderId);
        //    Object o = iExchange.Common.DataAccess.ExecuteScalar(sql, connectionString);
        //    if (o != null && o != DBNull.Value)
        //    {
        //        setPrice = (string)o;
        //    }
        //    return setPrice;
        //}

        //internal static void ParseOrder(XmlNode orderNode, Transaction transaction, out Order order)
        //{
        //    order = new Order();
        //    order.TransactionId = transaction.Id;
        //    order.Initialize(orderNode);
        //}

        #endregion
    }

    internal static class XmlConvertHelper
    {
        //internal static void Initialize(this Transaction transaction, XmlNode xmlNode)
        //{
        //    foreach (XmlAttribute attribute in xmlNode.Attributes)
        //    {
        //        String nodeName = attribute.Name;
        //        String nodeValue = attribute.Value;
        //        if (nodeName == "ID")
        //        {
        //            transaction.Id = new Guid(nodeValue);
        //        }
        //        else if (nodeName == "Code")
        //        {
        //            transaction.Code = nodeValue;
        //        }
        //        else if (nodeName == "Type")
        //        {
        //            transaction.Type = (TransactionType)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName == "SubType")
        //        {
        //            transaction.SubType = (TransactionSubType)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName == "Phase")
        //        {
        //            transaction.Phase = (OrderPhase)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName == "BeginTime")
        //        {
        //            transaction.BeginTime = DateTime.Parse(nodeValue);
        //        }
        //        else if (nodeName == "EndTime")
        //        {
        //            transaction.EndTime = DateTime.Parse(nodeValue);
        //        }
        //        else if (nodeName == "ExpireType")
        //        {
        //            transaction.ExpireType = (ExpireType)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName == "SubmitTime")
        //        {
        //            transaction.SubmitTime = DateTime.Parse(nodeValue);
        //        }
        //        else if (nodeName == "SubmitorID")
        //        {
        //            transaction.SubmitorId = new Guid(nodeValue);
        //        }
        //        else if (nodeName == "ExecuteTime")
        //        {
        //            transaction.ExecuteTime = DateTime.Parse(nodeValue);
        //        }
        //        else if (nodeName == "OrderType")
        //        {
        //            transaction.OrderType = (OrderType)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName == "ContractSize")
        //        {
        //            transaction.ContractSize = decimal.Parse(nodeValue);
        //        }
        //        else if (nodeName == "AccountID")
        //        {
        //            transaction.AccountId = new Guid(nodeValue);
        //        }
        //        else if (nodeName == "InstrumentID")
        //        {
        //            transaction.InstrumentId = new Guid(nodeValue);
        //        }
        //        else if (nodeName == "ErrorCode")
        //        {
        //            transaction.Error = (TransactionError)Enum.Parse(typeof(TransactionError), nodeValue);
        //        }
        //        else if (nodeName == "AssigningOrderID" && !string.IsNullOrEmpty(nodeValue))
        //        {
        //            transaction.AssigningOrderId = new Guid(nodeValue);
        //        }
        //        else if (nodeName == "InstrumentCategory")
        //        {
        //            transaction.InstrumentCategory = (InstrumentCategory)Enum.Parse(typeof(InstrumentCategory), nodeValue);
        //        }
        //    }
        //}

        //internal static void Initialize(this Order order, XmlNode xmlNode)
        //{
        //    foreach (XmlAttribute attribute in xmlNode.Attributes)
        //    {
        //        string nodeName = attribute.Name;
        //        string nodeValue = attribute.Value;
        //        if (nodeName.Equals("ID"))
        //        {
        //            order.Id = new Guid(nodeValue);
        //        }
        //        else if (nodeName.Equals("Code"))
        //        {
        //            order.Code = nodeValue;
        //        }
        //        else if (nodeName.Equals("Lot"))
        //        {
        //            order.Lot = decimal.Parse(nodeValue);
        //        }
        //        else if (nodeName == "MinLot")
        //        {
        //            if (!string.IsNullOrEmpty(nodeValue))
        //            {
        //                order.MinLot = decimal.Parse(nodeValue);
        //            }
        //        }
        //        else if (nodeName.Equals("IsOpen"))
        //        {
        //            order.IsOpen = bool.Parse(nodeValue);
        //        }
        //        else if (nodeName.Equals("IsBuy"))
        //        {
        //            order.IsBuy = bool.Parse(nodeValue);
        //        }
        //        else if (nodeName.Equals("SetPrice"))
        //        {
        //            order.SetPrice = nodeValue;
        //        }
        //        else if (nodeName.Equals("ExecutePrice"))
        //        {
        //            order.ExecutePrice = nodeValue;
        //        }
        //        else if (nodeName.Equals("BestPrice"))
        //        {
        //            order.BestPrice = nodeValue;
        //        }
        //        else if (nodeName.Equals("BestTime"))
        //        {
        //            order.BestTime = DateTime.Parse(nodeValue);
        //        }
        //        else if (nodeName.Equals("TradeOption"))
        //        {
        //            order.TradeOption = (TradeOption)(int.Parse(nodeValue));
        //        }
        //        else if (nodeName.Equals("DQMaxMove"))
        //        {
        //            order.DQMaxMove = int.Parse(nodeValue);
        //        }
        //        else if (nodeName.Equals("HitCount"))
        //        {
        //            order.HitCount = short.Parse(nodeValue);
        //        }
        //    }
        //}

        internal static void Initialize(this OpenInterestSummary openInterestSummary, XmlNode xmlNode)
        {
            foreach (XmlAttribute attribute in xmlNode.Attributes)
            {
                String nodeName = attribute.Name;
                String nodeValue = attribute.Value;
                if (nodeName == "ID")
                {
                    Guid id;
                    Guid.TryParse(nodeValue, out id);
                    if (id == Guid.Empty)
                    {
                        openInterestSummary.OriginCode = nodeValue;
                        openInterestSummary.IsOrigin = true;
                    }
                    else
                    {
                        openInterestSummary.IsOrigin = false;
                        openInterestSummary.Id = Guid.Parse(nodeValue);
                    }
                }
                else if (nodeName == "Code")
                {
                    openInterestSummary.Code = nodeValue;
                }
                else if (nodeName == "Type")
                {
                    openInterestSummary.AccountType = (AccountType)(int.Parse(nodeValue));
                }
                else if (nodeName == "MinNumeratorUnit")
                {
                    openInterestSummary.MinNumeratorUnit = int.Parse(nodeValue);
                }
                else if (nodeName == "MaxDenominator")
                {
                    openInterestSummary.MaxDenominator = int.Parse(nodeValue); ;
                }
                else if (nodeName == "BuyLot")
                {
                    openInterestSummary.BuyLot = decimal.Parse(nodeValue); ;
                }
                else if (nodeName == "AvgBuyPrice")
                {
                    openInterestSummary.BuyAvgPrice = nodeValue; ;
                }
                else if (nodeName == "BuyContractSize")
                {
                    openInterestSummary.BuyContractSize = decimal.Parse(nodeValue); ;
                }
                else if (nodeName == "SellLot")
                {
                    openInterestSummary.SellLot = decimal.Parse(nodeValue); ;
                }
                else if (nodeName == "AvgSellPrice")
                {
                    openInterestSummary.SellAvgPrice = nodeValue; ;
                }
                else if (nodeName == "SellContractSize")
                {
                    openInterestSummary.SellContractSize = decimal.Parse(nodeValue); ;
                }
                else if (nodeName == "NetLot")
                {
                    openInterestSummary.NetLot = decimal.Parse(nodeValue); ;
                }
                else if (nodeName == "AvgNetPrice")
                {
                    openInterestSummary.NetAvgPrice = nodeValue; ;
                }
                else if (nodeName == "NetContractSize")
                {
                    openInterestSummary.NetContractSize = decimal.Parse(nodeValue); ;
                }
            }
        }
    }
}