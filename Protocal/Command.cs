using iExchange.Common;
using ProtoBuf;
using Protocal.Commands;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Protocal
{
    public enum ModifyType
    {
        None,
        Add,
        Delete,
        Update
    }


    [ProtoContract]
    [ProtoInclude(3, typeof(SettingCommand))]
    [ProtoInclude(4, typeof(MarketCommand))]
    [ProtoInclude(5, typeof(TradingCommand))]
    [ProtoInclude(6, typeof(NotifyCommand))]
    [ProtoInclude(7, typeof(TradeAuditCommand))]
    [ProtoInclude(8, typeof(QuotationCommand))]
    public class Command
    {

        [ProtoMember(1)]
        public iExchange.Common.AppType SourceType { get; set; }

        [ProtoMember(2)]
        public long Sequence { get; set; }

        public bool IsQuotation { get; set; }

    }

    [ProtoContract]
    public sealed class QuotationCommand : Command
    {
        [ProtoMember(3)]
        public OriginQ[] OriginQs { get; set; }
        [ProtoMember(4)]
        public OverridedQ[] OverridedQs { get; set; }
    }


    [ProtoContract]
    [ProtoInclude(31, typeof(TradingCancelByManagerCommand))]
    [ProtoInclude(30, typeof(Commands.Trading.CancelDeliveryCommand))]
    [ProtoInclude(29, typeof(Commands.Trading.NotifyDeliveryCommand))]
    [ProtoInclude(28, typeof(TradingUpdateBalanceCommand))]
    [ProtoInclude(27, typeof(TradingTransferCommand))]
    [ProtoInclude(26, typeof(TradingExecuteCommand))]
    [ProtoInclude(25, typeof(TradingHitCommand))]
    [ProtoInclude(24, typeof(TradingResetAlertLevelCommand))]
    [ProtoInclude(23, typeof(TradingPriceAlertCommand))]
    [ProtoInclude(22, typeof(TradingPrePaymentCommand))]
    [ProtoInclude(21, typeof(TradingAcceptPlaceCommand))]
    public class TradingCommand : Command
    {
        [ProtoMember(3)]
        public string Content { get; set; }

        [ProtoMember(4)]
        public Guid AccountId { get; set; }

        [ProtoMember(5)]
        public bool IsBook { get; set; }
    }


    [ProtoContract]
    [ProtoInclude(12, typeof(ChatNotifyCommand))]
    [ProtoInclude(11, typeof(QuoteAnswerNotifyCommand))]
    public abstract class NotifyCommand : Command
    {
        protected HashSet<Guid> customerIds = null;
        private object customerIdsLock = new object();
        private bool customerIdsCollected = false;

        public bool ShouldSendTo(Guid customerId)
        {
            lock (customerIdsLock)
            {
                if (!customerIdsCollected)
                {
                    this.CollectCustomerIds();
                    customerIdsCollected = true;
                }
                return customerIds == null || customerIds.Contains(customerId);
            }
        }

        protected abstract void CollectCustomerIds();
    }


    [ProtoContract]
    public sealed class ChatNotifyCommand : NotifyCommand
    {
        [ProtoMember(3)]
        public string Content { get; set; }

        protected override void CollectCustomerIds()
        {
            //<Chat ID="" Title="" Content="" ExpireTime="" Publisher="" xmlns=""><Recipients><Customers><Customer ID="" /></Customers></Recipients></Chat>
            //<Chat ID="" Title="" Content="" PublishTime="" />
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(this.Content);
            XmlNode elelemt = doc.DocumentElement;
            if (elelemt.HasChildNodes)
            {
                foreach (XmlNode customerChild in elelemt.ChildNodes[0].ChildNodes[0].ChildNodes)
                {
                    if (customerChild.Attributes["ID"] != null && !string.IsNullOrEmpty(customerChild.Attributes["ID"].Value))
                    {
                        if (customerIds == null) customerIds = new HashSet<Guid>();
                        customerIds.Add(Guid.Parse(customerChild.Attributes["ID"].Value));
                    }
                }
            }
        }

    }

    [ProtoContract]
    public sealed class QuoteAnswerNotifyCommand : NotifyCommand
    {
        [ProtoMember(3)]
        public string Content { get; set; }

        protected override void CollectCustomerIds()
        {
            //<QuoteAnswer>
            //<Instrument ID="" Origin=""><Customer ID="" Ask="" Bid="" QuoteLot="" AnswerLot=""/></Instrument>
            //<Instrument ID="" Origin=""><Customer ID="" Ask="" Bid="" QuoteLot="" AnswerLot=""/></Instrument>
            //</QuoteAnswer>
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(this.Content);
            XmlNode elelemt = doc.DocumentElement;
            foreach (XmlNode instrumentChild in elelemt.ChildNodes)
            {
                foreach (XmlNode customerChild in instrumentChild.ChildNodes)
                {
                    if (customerChild.Attributes["ID"] != null && !string.IsNullOrEmpty(customerChild.Attributes["ID"].Value))
                    {
                        if (this.customerIds == null) customerIds = new HashSet<Guid>();
                        customerIds.Add(Guid.Parse(customerChild.Attributes["ID"].Value));
                    }
                }
            }
        }
    }


    [ProtoContract]
    public sealed class SettingCommand : Command
    {
        [ProtoMember(3)]
        public string Content { get; set; }

        [ProtoMember(4)]
        public AppType AppType { get; set; }
    }


    [ProtoContract]
    public sealed class TradeAuditCommand : Command
    {
        [ProtoMember(3)]
        public Guid Id { get; set; }
        [ProtoMember(4)]
        public Guid UserId { get; set; }
        [ProtoMember(5)]
        public Guid? AccountId { get; set; }
    }


}
