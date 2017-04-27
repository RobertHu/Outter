using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal static class PartialExecutor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PartialExecutor));

        internal static XmlNode PartialExecute(Transaction tran, decimal executeLot, bool cancelRemain, out XmlNode toExecuteTranNode)
        {
            toExecuteTranNode = null;
            XmlDocument toExecuteXmlTran = new XmlDocument();
            if (!cancelRemain)
            {
                toExecuteTranNode = CreateExecuteXMLTran(tran, toExecuteXmlTran);
            }
            var toBeRemovedOrderRelations = GetAllToBeRemovedOrderRelations(tran, executeLot, cancelRemain, toExecuteXmlTran, toExecuteTranNode);
            var tranNode = BuildXmlTran(tran, toBeRemovedOrderRelations);
            return tranNode;
        }

        private static XmlNode BuildXmlTran(Transaction tran, List<OrderRelation> removedOrderRelations)
        {
            XmlDocument xmlTran = new XmlDocument();
            XmlElement tranNode = xmlTran.CreateElement("Transaction");
            xmlTran.AppendChild(tranNode);
            tranNode.SetAttribute("ID", XmlConvert.ToString(tran.Id));
            foreach (Order order in tran.Orders)
            {
                if (order.Phase == OrderPhase.Canceled) continue;

                XmlElement orderNode = xmlTran.CreateElement("Order");
                tranNode.AppendChild(orderNode);

                orderNode.SetAttribute("ID", XmlConvert.ToString(order.Id));
                orderNode.SetAttribute("Lot", XmlConvert.ToString(order.Lot));
                orderNode.SetAttribute("LotBalance", XmlConvert.ToString(order.LotBalance));

                if (order.IsOpen) continue;

                foreach (OrderRelation orderRelation in order.OrderRelations)
                {
                    XmlElement orderRelationNode = xmlTran.CreateElement("OrderRelation");
                    orderNode.AppendChild(orderRelationNode);

                    orderRelationNode.SetAttribute("OpenOrderID", XmlConvert.ToString(orderRelation.OpenOrderId));
                    orderRelationNode.SetAttribute("ClosedLot", XmlConvert.ToString(orderRelation.ClosedLot));
                }

                foreach (OrderRelation orderRelation in removedOrderRelations)
                {
                    XmlElement orderRelationNode = xmlTran.CreateElement("RemovedOrderRelation");
                    orderNode.AppendChild(orderRelationNode);

                    orderRelationNode.SetAttribute("OpenOrderID", XmlConvert.ToString(orderRelation.OpenOrderId));
                }
            }
            return tranNode;
        }

        private static XmlNode CreateExecuteXMLTran(Transaction tran, XmlDocument toExecuteXmlTran)
        {
            XmlNode toExecuteTranNode = toExecuteXmlTran.CreateElement("Transaction");
            toExecuteXmlTran.AppendChild(toExecuteTranNode);
            ((XmlElement)toExecuteTranNode).SetAttribute("ID", XmlConvert.ToString(Guid.NewGuid()));
            ((XmlElement)toExecuteTranNode).SetAttribute("Type", XmlConvert.ToString((int)tran.Type));
            ((XmlElement)toExecuteTranNode).SetAttribute("Phase", XmlConvert.ToString((int)tran.Phase));
            ((XmlElement)toExecuteTranNode).SetAttribute("OrderType", XmlConvert.ToString((int)tran.OrderType));
            ((XmlElement)toExecuteTranNode).SetAttribute("ContractSize", XmlConvert.ToString(tran.ContractSize));
            ((XmlElement)toExecuteTranNode).SetAttribute("AccountID", XmlConvert.ToString(tran.AccountId));
            ((XmlElement)toExecuteTranNode).SetAttribute("InstrumentID", XmlConvert.ToString(tran.InstrumentId));
            ((XmlElement)toExecuteTranNode).SetAttribute("BeginTime", XmlConvert.ToString(tran.BeginTime, DateTimeFormat.Xml));
            ((XmlElement)toExecuteTranNode).SetAttribute("EndTime", XmlConvert.ToString(tran.EndTime, DateTimeFormat.Xml));
            ((XmlElement)toExecuteTranNode).SetAttribute("ExpireType", XmlConvert.ToString((int)tran.ExpireType));
            ((XmlElement)toExecuteTranNode).SetAttribute("SubmitTime", XmlConvert.ToString(tran.SubmitTime, DateTimeFormat.Xml));
            ((XmlElement)toExecuteTranNode).SetAttribute("SubmitorID", XmlConvert.ToString(tran.SubmitorId));
            ((XmlElement)toExecuteTranNode).SetAttribute("ApproverID", XmlConvert.ToString(tran.ApproverId ?? Guid.Empty));
            if (tran.SourceOrderId != Guid.Empty)
            {
                ((XmlElement)toExecuteTranNode).SetAttribute("AssigningOrderID", XmlConvert.ToString(tran.SourceOrderId ?? Guid.Empty));
            }
            return toExecuteTranNode;
        }

        private static List<OrderRelation> GetAllToBeRemovedOrderRelations(Transaction tran, decimal executeLot, bool cancelRemain, XmlDocument toExecuteXmlTran, XmlNode toExecuteTranNode)
        {
            List<OrderRelation> toBeRemovedOrderRelations = new List<OrderRelation>();
            foreach (Order order in tran.Orders)
            {
                Logger.Warn(string.Format("Change Order lot: id = {0}; old lot = {1}; new lot = {2}; approverID ={3}", order.Id, order.Lot, executeLot, tran.ApproverId));
                order.PartialExecute(executeLot, cancelRemain, toExecuteXmlTran, toExecuteTranNode, toBeRemovedOrderRelations);
            }
            return toBeRemovedOrderRelations;
        }
    }
}
