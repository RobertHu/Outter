using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal static class TransactionXmlService
    {
        public static XmlNode ToXmlNode(Transaction tran, bool isForReport = false)
        {
            List<Order> orders = new List<Order>(tran.Orders.Count());
            foreach (Order order in tran.Orders)
            {
                orders.Add(order);
            }

            XmlDocument xmlTran = new XmlDocument();
            XmlElement tranNode = xmlTran.CreateElement("Transaction");
            xmlTran.AppendChild(tranNode);

            tranNode.SetAttribute("ID", XmlConvert.ToString(tran.Id));
            if (tran.Code != null) tranNode.SetAttribute("Code", tran.Code);
            tranNode.SetAttribute("Type", XmlConvert.ToString((int)tran.Type));
            tranNode.SetAttribute("SubType", XmlConvert.ToString((int)tran.SubType));
            tranNode.SetAttribute("Phase", XmlConvert.ToString((int)tran.Phase));
            tranNode.SetAttribute("OrderType", XmlConvert.ToString((int)tran.OrderType));
            tranNode.SetAttribute("InstrumentCategory", XmlConvert.ToString((int)tran.InstrumentCategory));
            tranNode.SetAttribute("ContractSize", XmlConvert.ToString(tran.ContractSize));
            tranNode.SetAttribute("AccountID", XmlConvert.ToString(tran.AccountId));
            tranNode.SetAttribute("InstrumentID", XmlConvert.ToString(tran.InstrumentId));
            tranNode.SetAttribute("BeginTime", XmlConvert.ToString(tran.BeginTime, DateTimeFormat.Xml));
            tranNode.SetAttribute("EndTime", XmlConvert.ToString(tran.EndTime, DateTimeFormat.Xml));
            tranNode.SetAttribute("ExpireType", XmlConvert.ToString((int)tran.ExpireType));
            tranNode.SetAttribute("SubmitTime", XmlConvert.ToString(tran.SubmitTime, DateTimeFormat.Xml));
            if (tran.ExecuteTime != DateTime.MinValue)
            {
                tranNode.SetAttribute("ExecuteTime", XmlConvert.ToString(tran.ExecuteTime.Value, DateTimeFormat.Xml));
            }
            tranNode.SetAttribute("SubmitorID", XmlConvert.ToString(tran.Submitor.Id));
            tranNode.SetAttribute("ApproverID", XmlConvert.ToString(tran.ApproverId.Value));
            if (tran.SourceOrderId != null)
                tranNode.SetAttribute("AssigningOrderID", XmlConvert.ToString(tran.SourceOrderId.Value));

            if (isForReport)
            {
                tranNode.SetAttribute("NumeratorUnit", Convert.ToString(tran.SettingInstrument.NumeratorUnit));
                tranNode.SetAttribute("Denominator", Convert.ToString(tran.SettingInstrument.Denominator));
            }

            foreach (Order order in orders)
            {
                if (order.Phase == OrderPhase.Canceled) continue;
                var orderNode = order.ToXmlNode(xmlTran, tranNode);
                if (order.IsOpen) continue;
                foreach (var orderRelation in order.OrderRelations)
                {
                    orderRelation.XmlService.ToXmlNode(xmlTran, orderNode);
                }
            }
            return tranNode;
        }

        public static XmlElement GetExecuteXmlElement(Transaction tran, DateTime? executeTradeDay = null)
        {
            XmlDocument document = new XmlDocument();
            if (executeTradeDay == null)
            {
                executeTradeDay = Settings.SettingManager.Default.Setting.GetTradeDay().Day;
            }
            document.LoadXml(InnerGetExecuteXmlString(tran, executeTradeDay.Value));
            return (XmlElement)document.GetElementsByTagName("Transaction")[0];
        }

        public static string GetExecuteXmlString(Transaction tran)
        {
            DateTime executeTradeDay = Settings.SettingManager.Default.Setting.GetTradeDay().Day;
            return InnerGetExecuteXmlString(tran, executeTradeDay);
        }

        private static string InnerGetExecuteXmlString(Transaction tran, DateTime executeTradeDay)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<Transaction ID='{0}' Code='{1}'", XmlConvert.ToString(tran.Id), tran.Code);
            stringBuilder.AppendFormat(" Type='{0}' SubType='{1}'", XmlConvert.ToString((int)tran.Type), XmlConvert.ToString((int)tran.SubType));
            stringBuilder.AppendFormat(" Phase='{0}' OrderType='{1}'", XmlConvert.ToString((int)tran.Phase), XmlConvert.ToString((int)tran.OrderType));
            stringBuilder.AppendFormat(" InstrumentCategory='{0}'", XmlConvert.ToString((int)tran.InstrumentCategory));
            stringBuilder.AppendFormat(" ContractSize='{0}' AccountID='{1}'", XmlConvert.ToString(tran.ContractSize), XmlConvert.ToString(tran.AccountId));
            stringBuilder.AppendFormat(" InstrumentID='{0}' BeginTime='{1}'", XmlConvert.ToString(tran.InstrumentId), XmlConvert.ToString(tran.BeginTime, DateTimeFormat.Xml));
            stringBuilder.AppendFormat(" EndTime='{0}' ExpireType='{1}'", XmlConvert.ToString(tran.EndTime, DateTimeFormat.Xml), XmlConvert.ToString((int)tran.ExpireType));
            DateTime executeTime = tran.ExecuteTime == null ? executeTradeDay : tran.ExecuteTime.Value;
            stringBuilder.AppendFormat(" SubmitTime='{0}' ExecuteTime='{1}'", XmlConvert.ToString(tran.SubmitTime, DateTimeFormat.Xml), XmlConvert.ToString(executeTime, DateTimeFormat.Xml));
            stringBuilder.AppendFormat(" SubmitorID='{0}' ApproverID='{1}'", XmlConvert.ToString(tran.SubmitorId), XmlConvert.ToString(tran.ApproverId ?? Guid.Empty));
            if (tran.SourceOrderId != null)
            {
                stringBuilder.AppendFormat(" AssigningOrderID='{0}'", XmlConvert.ToString(tran.SourceOrderId.Value));
            }
            stringBuilder.AppendFormat(">");
            foreach (Order order in tran.Orders)
            {
                order.GetExecuteXmlString(stringBuilder, executeTradeDay);
                if (!order.IsOpen)
                {
                    foreach (OrderRelation orderRelation in order.OrderRelations)
                    {
                        orderRelation.XmlService.GetExecuteXmlString(stringBuilder);
                    }
                }
                stringBuilder.Append("</Order>");
            }
            stringBuilder.Append("</Transaction>");
            return stringBuilder.ToString();
        }

    }
}
