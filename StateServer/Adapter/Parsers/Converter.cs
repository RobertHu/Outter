using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml;
using Protocal;
using Protocal.Physical;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using iExchange.StateServer.Adapter.Parsers;
using System.Reflection;
using log4net;

namespace iExchange.StateServer.Adapter
{
    internal static class XmlHelper
    {
        internal static XmlNode ToXmlNode(this string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return null;
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.FirstChild;
        }
    }






    internal static class Converter
    {
        internal static TransactionData ToTransactionData(Token token, XmlNode tran)
        {
            string tranCode = string.Empty;
            Protocal.TransactionData tranData = TransactionParser.ParseTransaction(tran);
            tranData.PlaceByRiskMonitor = token.AppType == AppType.RiskMonitor ? true : false;
            tranData.Id = Guid.Parse(tran.Attributes["ID"].Value);
            tranData.Orders = new List<OrderData>();

            InstrumentCategory instrumentCategory = InstrumentCategory.Forex;
            if (tran.Attributes["InstrumentCategory"] != null)
            {
                instrumentCategory = (InstrumentCategory)Enum.Parse(typeof(InstrumentCategory), tran.Attributes["InstrumentCategory"].Value);
            }

            foreach (XmlNode orderNode in tran.ChildNodes)
            {
                if (orderNode.Name == "Order")
                {
                    OrderData orderData = null;
                    if (tranData.OrderType == OrderType.BinaryOption)
                    {
                        orderData = OrderDataHelper.InitializeBOData(orderNode);
                    }
                    else if (instrumentCategory == InstrumentCategory.Forex)
                    {
                        orderData = OrderDataHelper.Initialize(orderNode);
                    }
                    else
                    {
                        orderData = OrderDataHelper.InitializePhysicalData(orderNode);
                    }
                    tranData.Orders.Add(orderData);
                }
            }

            return tranData;
        }


        internal static TransactionBookData ToTransactionBookData(XmlNode tranNode)
        {
            string tranCode = string.Empty;
            Guid? orderBatchInstructionId;
            TransactionBookData tranData = TransactionParser.ParseTransactionBookData(tranNode, out orderBatchInstructionId);
            tranData.Id = Guid.Parse(tranNode.Attributes["ID"].Value);
            tranData.Orders = new List<OrderBookData>();

            var instrument = InstrumentManager.Default.Get(tranData.InstrumentId);
            InstrumentCategory instrumentCategory = instrument.Category;

            foreach (XmlNode orderNode in tranNode.ChildNodes)
            {
                if (orderNode.Name == "Order")
                {
                    OrderBookData orderData = null;
                    if (instrumentCategory == InstrumentCategory.Forex)
                    {
                        orderData = OrderDataHelper.InitializeBookData(orderNode);
                    }
                    else
                    {
                        orderData = OrderDataHelper.InitalizePhysicalBookData(orderNode);
                    }
                    orderData.OrderBatchInstructionID = orderBatchInstructionId;
                    tranData.Orders.Add(orderData);
                }
            }
            tranData.PlaceByRiskMonitor = true;
            return tranData;
        }

        internal static TransactionData ToTransactionAssignData(XmlNode tranNode)
        {
            throw new NotImplementedException();
        }

        internal static Protocal.Physical.DeliveryRequestData ToDeliveryRequestData(XmlNode deliveryRequestNode)
        {
            return DeliveryRequestParser.Parser(deliveryRequestNode);
        }


        internal static Protocal.Physical.TerminateData ToTerminateData(string terminateXml)
        {
            var result = new Protocal.Physical.TerminateData();
            XElement root = XElement.Parse(terminateXml);
            XElement node = root.Element("Order");
            result.OrderId = node.AttrToGuid("ID");
            result.TerminateFee = node.AttrToDecimal("TerminateFee");
            result.Amount = node.AttrToDecimal("Amount");
            result.SourceTerminateFee = XmlConvert.ToDecimal(node.Attribute("SourceTerminateFee").Value.Replace(",", ""));
            result.SourceCurrencyId = node.HasAttribute("SourceCurrencyId") ? node.AttrToGuid("SourceCurrencyId") : Guid.Empty;
            result.SourceAmount = node.AttrToDecimal("SourceAmount");
            result.CurrencyRate = node.AttrToDecimal("CurrencyRate");
            result.IsPayOff = node.AttrToBoolean("IsPayOff");
            return result;
        }

        internal static List<Protocal.Physical.InstalmentData> ToInstalmentData(string instalmentXml)
        {
            var result = new List<Protocal.Physical.InstalmentData>();
            if (string.IsNullOrEmpty(instalmentXml)) return result;
            XElement root = XElement.Parse(instalmentXml);
            foreach (XElement eachInstalmentNode in root.Elements("Instalment"))
            {
                var instalment = new Protocal.Physical.InstalmentData();
                instalment.OrderID = eachInstalmentNode.AttrToGuid("OrderID");
                instalment.Sequence = eachInstalmentNode.AttrToInt32("Sequence");
                instalment.InterestRate = eachInstalmentNode.AttrToDecimal("InterestRate");
                instalment.Principal = eachInstalmentNode.AttrToDecimal("Principal");
                instalment.Interest = eachInstalmentNode.AttrToDecimal("Interest");
                instalment.DebitInterest = eachInstalmentNode.AttrToDecimal("DebitInterest");
                instalment.SourceCurrencyId = eachInstalmentNode.AttrToGuid("SourceCurrencyId");
                instalment.SourceAmount = eachInstalmentNode.AttrToDecimal("SourceAmount");
                instalment.CurrencyRate = eachInstalmentNode.AttrToDecimal("CurrencyRate");
                instalment.ExecuteTime = eachInstalmentNode.AttrToDateTime("ExecuteTime");
                instalment.Lot = eachInstalmentNode.AttrToDecimal("Lot");
                instalment.LotBalance = eachInstalmentNode.AttrToDecimal("LotBalance");
                instalment.CurrencyId = eachInstalmentNode.AttrToGuid("CurrencyId");
                instalment.PaymentDateTimeOnPlan = eachInstalmentNode.AttrToDateTime("PaymentDateTimeOnPlan");
                instalment.PaidDateTime = eachInstalmentNode.AttrToDateTime("PaidDateTime");
                instalment.IsPayOff = eachInstalmentNode.AttrToBoolean("IsPayOff");
                instalment.PaidPledge = eachInstalmentNode.AttrToDecimal("PaidPledge");
                result.Add(instalment);
            }
            return result;
        }

        internal static XmlNode ToAccountsForInit(string newTransactionServerInitializeData)
        {
            XElement sourceElement = XElement.Parse(newTransactionServerInitializeData);
            XElement resultElement = new XElement("Accounts");

            foreach (XElement accountElement in sourceElement.Elements("Account"))
            {
                XElement newAccountElement = new XElement("Account");
                resultElement.Add(newAccountElement);
                foreach (XAttribute attribute in accountElement.Attributes())
                {
                    if (attribute.Name.LocalName.StartsWith("MinEquityAvoidRiskLevel"))
                    {
                        string name = attribute.Name.LocalName.Replace("MinEquityAvoidRiskLevel", "Necessary");
                        newAccountElement.SetAttributeValue(name, attribute.Value);
                    }
                    else
                    {
                        newAccountElement.SetAttributeValue(attribute.Name, attribute.Value);
                        if (attribute.Name == "Necessary")
                        {
                            newAccountElement.SetAttributeValue("Necessary0", attribute.Value);
                        }
                    }
                }

                XElement fundsElement = accountElement.Element("Funds");
                foreach (XElement fundElement in fundsElement.Elements("Fund"))
                {
                    XElement currencyElement = new XElement("Currency");
                    newAccountElement.Add(currencyElement);

                    foreach (XAttribute attribute in fundElement.Attributes())
                    {
                        if (!attribute.Name.LocalName.StartsWith("MinEquityAvoidRiskLevel"))
                        {
                            if (attribute.Name == "CurrencyID")
                            {
                                currencyElement.SetAttributeValue("ID", attribute.Value);
                            }
                            else
                            {
                                currencyElement.SetAttributeValue(attribute.Name, attribute.Value);
                            }
                        }
                    }
                }

                XElement transElement = accountElement.Element("Transactions");
                if (transElement == null) continue;

                foreach (XElement tranElement in transElement.Elements("Transaction"))
                {
                    foreach (XElement orderElement in transElement.Element("Orders").Elements("Order"))
                    {
                        XElement newOrderElement = new XElement("Order");
                        newOrderElement.SetAttributeValue("ID", orderElement.Attribute("ID").Value);

                        newOrderElement.SetAttributeValue("InterestPLFloat", orderElement.Attribute("InterestPLFloat").Value);
                        newOrderElement.SetAttributeValue("StoragePLFloat", orderElement.Attribute("StoragePLFloat").Value);
                        newOrderElement.SetAttributeValue("TradePLFloat", orderElement.Attribute("TradePLFloat").Value);
                        newOrderElement.SetAttributeValue("Necessary", orderElement.Attribute("Necessary").Value);

                        if (orderElement.Attribute("ValueAsMargin") != null)
                        {
                            newOrderElement.SetAttributeValue("ValueAsMargin", orderElement.Attribute("ValueAsMargin").Value);
                        }

                        newOrderElement.SetAttributeValue("LivePrice", orderElement.Attribute("LivePrice").Value);

                        if (orderElement.Attribute("AutoStopPrice") != null)
                        {
                            newOrderElement.SetAttributeValue("AutoStopPrice", orderElement.Attribute("AutoStopPrice").Value);
                        }

                        if (orderElement.Attribute("AutoLimitPrice") != null)
                        {
                            newOrderElement.SetAttributeValue("AutoLimitPrice", orderElement.Attribute("AutoLimitPrice").Value);
                        }
                    }
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(resultElement.ToString());
            return doc.DocumentElement;
        }
    }


    internal sealed class AttrInfo
    {
        public AttrInfo(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
            this.IsRequired = true;
            this.DefaultValue = null;
        }

        internal string Name { get; private set; }
        internal Type Type { get; private set; }
        internal string FieldName { get; set; }
        internal object DefaultValue { get; set; }
        internal bool IsRequired { get; set; }
    }


    internal static class AttrsParser
    {
        internal static void Parse(object owner, List<AttrInfo> attrs, XmlNode node)
        {
            foreach (var eachAttr in attrs)
            {
                var propertyInfo = owner.GetPropertyInfo(string.IsNullOrEmpty(eachAttr.FieldName) ? eachAttr.Name : eachAttr.FieldName);
                if (eachAttr.IsRequired && eachAttr.DefaultValue == null && !node.HasAttribute(eachAttr.Name))
                {
                    throw new ArgumentException(string.Format("attrName = {0} is not founed, node = {1}", eachAttr.Name, node.OuterXml));
                }
                if (node.HasAttribute(eachAttr.Name))
                {
                    propertyInfo.SetValue(owner, node.GetAttrValue(eachAttr.Name, eachAttr.Type), null);
                }
                else
                {
                    propertyInfo.SetValue(owner, eachAttr.DefaultValue, null);
                }
            }
        }

        private static PropertyInfo GetPropertyInfo(this object owner, string name)
        {
            return owner.GetType().GetProperty(name);
        }
    }



    internal static class TransactionParser
    {
        private static List<AttrInfo> _attrInfos = new List<AttrInfo>
            {
                new AttrInfo("ID", typeof(Guid)){ FieldName= "Id"},
                new AttrInfo("AccountID", typeof(Guid)){FieldName = "AccountId"},
                new AttrInfo("InstrumentID", typeof(Guid)){FieldName = "InstrumentId"},
                new AttrInfo("Type", typeof(TransactionType)),
                new AttrInfo("SubType", typeof(TransactionSubType)),
                new AttrInfo("OrderType", typeof(OrderType)),
                new AttrInfo("BeginTime", typeof(DateTime)),
                new AttrInfo("EndTime", typeof(DateTime)),
                new AttrInfo("ExpireType", typeof(ExpireType)){DefaultValue = ExpireType.GTD},
                new AttrInfo("SubmitTime", typeof(DateTime)),
                new AttrInfo("SubmitorID", typeof(Guid)){FieldName = "SubmitorId"},
                new AttrInfo("AssigningOrderID", typeof(Guid?)){FieldName = "SourceOrderId", IsRequired =false},
                new AttrInfo("DisableLmtVariation", typeof(bool)){ DefaultValue =false},
            };

        internal static TransactionData ParseTransaction(XmlNode tranNode)
        {
            return (TransactionData)ParseTransactionData(tranNode, false, _attrInfos);
        }

        internal static TransactionBookData ParseTransactionBookData(XmlNode tranNode, out Guid? orderBatchInstructionId)
        {
            orderBatchInstructionId = null;
            var attrInfos = new List<AttrInfo>(_attrInfos)
            {
                new AttrInfo("ExecuteTime", typeof(DateTime)),
                new AttrInfo("CheckMargin", typeof(bool)){DefaultValue = false},
                new AttrInfo("ApproverID", typeof(Guid) ){FieldName ="ApproverId"}
            };
            TransactionBookData bookData = (TransactionBookData)ParseTransactionData(tranNode, true, attrInfos);
            if (tranNode.HasAttribute("OrderBatchInstructionID"))
            {
                orderBatchInstructionId = XmlConvert.ToGuid(tranNode.GetAttrValue("OrderBatchInstructionID"));
            }
            return bookData;
        }

        private static TransactionCommonData ParseTransactionData(XmlNode tranNode, bool isBook, List<AttrInfo> infos)
        {
            TransactionCommonData tranData;
            if (isBook)
            {
                tranData = new TransactionBookData();
            }
            else
            {
                tranData = new TransactionData();
            }
            AttrsParser.Parse(tranData, infos, tranNode);
            return tranData;
        }




    }
}