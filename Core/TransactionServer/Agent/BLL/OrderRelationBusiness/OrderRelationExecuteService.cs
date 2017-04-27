using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public class OrderRelationExecuteService
    {
        private OrderRelation _owner;
        private OrderRelationSettings _settings;
        internal OrderRelationExecuteService(OrderRelation owner, OrderRelationSettings settings)
        {
            _owner = owner;
            _settings = settings;
        }

        internal void PartialExecute(ref decimal remainLot, bool cancelRemain, XmlDocument toExecuteXmlTran, XmlNode toExecuteOrderNode)
        {
            if (_owner.OpenOrder == null)
            {
                throw new TransactionServerException(TransactionError.OpenOrderNotExists);
            }
            if (remainLot > 0)
            {
                var closeLot = Math.Min(_owner.ClosedLot, remainLot);
                if (!cancelRemain && closeLot < _owner.ClosedLot)
                {
                    var closeLotToExecute = _owner.ClosedLot - closeLot;
                    this.AppendNewOrderRelationNode(toExecuteXmlTran, toExecuteOrderNode, closeLotToExecute);
                }
                _settings.ClosedLot = closeLot;
                remainLot -= closeLot;
            }
            else
            {
                if (!cancelRemain)
                {
                    this.AppendNewOrderRelationNode(toExecuteXmlTran, toExecuteOrderNode, _owner.ClosedLot);
                }
            }
        }

        private void AppendNewOrderRelationNode(XmlDocument toExecuteXmlTran, XmlNode toExecuteOrderNode, decimal closedLot)
        {
            XmlElement orderRelationNode = toExecuteXmlTran.CreateElement("OrderRelation");
            toExecuteOrderNode.AppendChild(orderRelationNode);
            orderRelationNode.SetAttribute("OpenOrderID", XmlConvert.ToString(_owner.OpenOrderId));
            orderRelationNode.SetAttribute("ClosedLot", XmlConvert.ToString(closedLot));
        }
    }
}
