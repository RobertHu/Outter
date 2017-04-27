using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
{
    public interface IOrderXmlService
    {
        XmlElement ToXmlNode(XmlDocument xmlTran, XmlElement tranNode);
        void GetExecuteXmlString(StringBuilder stringBuilder, DateTime executeTradeDay);
    }


    public abstract class OrderXmlServiceBase : IOrderXmlService
    {
        protected Order _order;
        protected OrderXmlServiceBase(Order order)
        {
            _order = order;
        }

        public virtual XmlElement ToXmlNode(XmlDocument xmlTran, XmlElement tranNode)
        {
            XmlElement orderNode = xmlTran.CreateElement("Order");
            tranNode.AppendChild(orderNode);

            orderNode.SetAttribute("ID", XmlConvert.ToString(_order.Id));
            if (_order.OriginCode != null) orderNode.SetAttribute("Code", _order.OriginCode);
            if (_order.Code != null) orderNode.SetAttribute("Code", _order.Code);
            orderNode.SetAttribute("Phase", XmlConvert.ToString((int)_order.Phase));
            orderNode.SetAttribute("TradeOption", XmlConvert.ToString((int)_order.TradeOption));
            orderNode.SetAttribute("IsOpen", XmlConvert.ToString(_order.IsOpen));
            orderNode.SetAttribute("IsBuy", XmlConvert.ToString(_order.IsBuy));
            if (_order.BlotterCode != null) orderNode.SetAttribute("BlotterCode", _order.BlotterCode);
            if (_order.InterestValueDate != null)
            {
                orderNode.SetAttribute("InterestValueDate", XmlConvert.ToString(_order.InterestValueDate.Value, DateTimeFormat.Xml));
            }

            if (_order.SetPrice != null) orderNode.SetAttribute("SetPrice", (string)_order.SetPrice);
            if (_order.SetPrice2 != null) orderNode.SetAttribute("SetPrice2", (string)_order.SetPrice2);
            if (_order.SetPriceMaxMovePips != 0) orderNode.SetAttribute("SetPriceMaxMovePips", XmlConvert.ToString(_order.SetPriceMaxMovePips));
            if (_order.DQMaxMove != 0) orderNode.SetAttribute("DQMaxMove", XmlConvert.ToString(_order.DQMaxMove));

            if (_order.ExecutePrice != null) orderNode.SetAttribute("ExecutePrice", (string)_order.ExecutePrice);
            if (_order.AutoLimitPrice != null) orderNode.SetAttribute("AutoLimitPrice", (string)_order.AutoLimitPrice);
            if (_order.AutoStopPrice != null) orderNode.SetAttribute("AutoStopPrice", (string)_order.AutoStopPrice);

            orderNode.SetAttribute("Lot", XmlConvert.ToString(_order.Lot));
            orderNode.SetAttribute("OriginalLot", XmlConvert.ToString(_order.OriginalLot));
            orderNode.SetAttribute("LotBalance", XmlConvert.ToString(_order.LotBalance));

            orderNode.SetAttribute("CommissionSum", XmlConvert.ToString(_order.CommissionSum));
            orderNode.SetAttribute("LevySum", XmlConvert.ToString(_order.LevySum));
            orderNode.SetAttribute("InterestPerLot", XmlConvert.ToString(_order.InterestPerLot));
            orderNode.SetAttribute("StoragePerLot", XmlConvert.ToString(_order.StoragePerLot));
            if (_order.LivePrice != null) orderNode.SetAttribute("LivePrice", (string)_order.LivePrice);

            if (_order.IsOpen)
            {
                orderNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(_order.InterestPLFloat));
                orderNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(_order.StoragePLFloat));
                orderNode.SetAttribute("TradePLFloat", XmlConvert.ToString(_order.TradePLFloat));
            }

            orderNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(_order.InterestPLNotValued));
            orderNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(_order.StoragePLNotValued));
            orderNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(_order.TradePLNotValued));
            if (_order.FeeSettings.IsValued)
            {
                orderNode.SetAttribute("DayInterestPLNotValued", _order.FeeSettings.InterestNotValuedString);
                orderNode.SetAttribute("DayStoragePLNotValued", _order.FeeSettings.StorageNotValuedString);
            }
            orderNode.SetAttribute("PlacedByRiskMonitor", XmlConvert.ToString(_order.PlacedByRiskMonitor));
            return orderNode;
        }

        public void GetExecuteXmlString(StringBuilder stringBuilder, DateTime executeTradeDay)
        {
            if (_order.Phase == OrderPhase.Canceled) return;
            this.InnerGetExecuteXmlString(stringBuilder, executeTradeDay);
            this.AddEndTag(stringBuilder);
        }

        protected virtual void InnerGetExecuteXmlString(StringBuilder stringBuilder, DateTime executeTradeDay)
        {
            stringBuilder.AppendFormat("<Order ID='{0}'", XmlConvert.ToString(_order.Id));
            if (_order.Code != null) stringBuilder.AppendFormat(" Code='{0}'", _order.Code);
            stringBuilder.AppendFormat(" TradeOption='{0}' IsOpen='{1}' IsBuy='{2}'", XmlConvert.ToString((int)_order.TradeOption), XmlConvert.ToString(_order.IsOpen), XmlConvert.ToString(_order.IsBuy));
            stringBuilder.AppendFormat(" InterestValueDate='{0}'", XmlConvert.ToString(_order.InterestValueDate ?? DateTime.MinValue, DateTimeFormat.Xml));

            if (_order.SetPrice != null) stringBuilder.AppendFormat(" SetPrice='{0}'", (string)_order.SetPrice);
            if (_order.SetPrice2 != null) stringBuilder.AppendFormat(" SetPrice2='{0}'", (string)_order.SetPrice2);
            if (_order.SetPriceMaxMovePips != 0) stringBuilder.AppendFormat(" SetPriceMaxMovePips='{0}'", XmlConvert.ToString(_order.SetPriceMaxMovePips));
            if (_order.DQMaxMove != 0) stringBuilder.AppendFormat(" DQMaxMove='{0}'", XmlConvert.ToString(_order.DQMaxMove));
            stringBuilder.AppendFormat(" ExecutePrice='{0}'", (string)_order.ExecutePrice);
            stringBuilder.AppendFormat(" ExecuteTradeDay='{0}'", XmlConvert.ToString(executeTradeDay, DateTimeFormat.Xml));
            if (_order.AutoLimitPrice != null) stringBuilder.AppendFormat(" AutoLimitPrice='{0}'", (string)_order.AutoLimitPrice);
            if (_order.AutoStopPrice != null) stringBuilder.AppendFormat(" AutoStopPrice='{0}'", (string)_order.AutoStopPrice);
            stringBuilder.AppendFormat(" OriginalLot='{0}'", XmlConvert.ToString(_order.OriginalLot));
            stringBuilder.AppendFormat(" Lot='{0}' LotBalance='{1}'", XmlConvert.ToString(_order.Lot), XmlConvert.ToString(_order.LotBalance));
            stringBuilder.AppendFormat(" CommissionSum='{0}' LevySum='{1}'", XmlConvert.ToString(_order.CommissionSum), XmlConvert.ToString(_order.LevySum));
            stringBuilder.AppendFormat(" LivePrice='{0}' InterestPerLot='{1}'", (string)_order.LivePrice, XmlConvert.ToString(_order.InterestPerLot));
            stringBuilder.AppendFormat(" StoragePerLot='{0}' InterestPLFloat='{1}'", XmlConvert.ToString(_order.StoragePerLot), XmlConvert.ToString(_order.InterestPLFloat));
            stringBuilder.AppendFormat(" StoragePLFloat='{0}' TradePLFloat='{1}'", XmlConvert.ToString(_order.StoragePLFloat), XmlConvert.ToString(_order.TradePLFloat));
            if (_order.FeeSettings.IsValued)
            {
                stringBuilder.AppendFormat(" DayInterestNotValued='{0}' DayStorageNotValued='{1}'", _order.FeeSettings.InterestNotValuedString, _order.FeeSettings.StorageNotValuedString);
            }
            stringBuilder.AppendFormat(" PlacedByRiskMonitor='{0}'", XmlConvert.ToString(_order.PlacedByRiskMonitor));
        }

        private void AddEndTag(StringBuilder stringBuilder)
        {
            stringBuilder.Append(">");
        }
    }


    public sealed class GeneralOrderXmlService : OrderXmlServiceBase
    {
        internal GeneralOrderXmlService(Order order)
            : base(order) { }

    }

}
