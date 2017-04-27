using iExchange.Common;
using Protocal;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.PriceAlert
{
    public enum AlertCondition
    {
        LessThanBid,
        GreaterThanBid,
        LessThanAsk,
        GreaterThanAsk
    }

    public enum ModifyFlag
    {
        Add,
        Update,
        Remove
    }

    public sealed class Alert
    {
        public Guid Id { get; private set; }
        public Guid InstrumentId { get; private set; }
        public AlertCondition Condition { get; private set; }
        public Price Price { get; private set; }
        public DateTime ExpireTime { get; private set; }
        public Guid UserId { get; private set; }
        public AlertState State { get; private set; }
        public Guid? QuotePolicyId { get; private set; }
        public Price HitPrice { get; private set; }
        public DateTime HitPriceTimestamp { get; private set; }

        public static Alert Create(Guid userId, XmlNode node, IQuotationSetterProvider quotationSetterProvider)
        {
            Alert alert = new Alert();
            alert.Id = Guid.Parse(node.Attributes["ID"].Value);
            alert.UserId = userId;
            alert.InstrumentId = Guid.Parse(node.Attributes["InstrumentID"].Value);
            alert.Condition = (AlertCondition)(int.Parse(node.Attributes["Condition"].Value));
            alert.ExpireTime = DateTime.Parse(node.Attributes["ExpirationTime"].Value);
            alert.QuotePolicyId = Guid.Parse(node.Attributes["QuotePolicyID"].Value);
            IQuotationSetter setter = quotationSetterProvider.Get(alert.InstrumentId);
            alert.Price = new Price(node.Attributes["Price"].Value, setter.NumeratorUnit, setter.Denominator);
            alert.State = AlertState.Pending;

            return alert;
        }

        public static Alert Create(IDBRow row, IQuotationSetterProvider quotationSetterProvider)
        {
            Alert alert = new Alert();

            alert.Id = (Guid)row["ID"];
            alert.UserId = (Guid)row["UserID"];
            alert.QuotePolicyId = row.GetColumn<Guid?>("QuotePolicyID");
            alert.InstrumentId = (Guid)row["InstrumentID"];
            alert.Condition = (AlertCondition)((int)row["Condition"]);
            alert.ExpireTime = (DateTime)row["ExpirationTime"];
            IQuotationSetter setter = quotationSetterProvider.Get(alert.InstrumentId);
            alert.Price = new Price((string)row["Price"], setter.NumeratorUnit, setter.Denominator);
            alert.State = (AlertState)((int)row["State"]);

            return alert;
        }

        internal void Update(XmlNode node, IQuotationSetterProvider quotationSetterProvider)
        {
            //if (node.Attributes["UserID"] != null)
            //{
            //    this.UserId = Guid.Parse(node.Attributes["UserID"].Value);
            //    AppDebug.LogEvent("TransactionServer", string.Format("Alert.Update new userid={0}", node.Attributes["UserID"].Value), EventLogEntryType.Information);
            //}
            if (node.Attributes["InstrumentID"] != null) this.InstrumentId = new Guid(node.Attributes["InstrumentID"].Value);
            if (node.Attributes["Condition"] != null) this.Condition = (AlertCondition)(int.Parse(node.Attributes["Condition"].Value));
            if (node.Attributes["ExpirationTime"] != null) this.ExpireTime = DateTime.Parse(node.Attributes["ExpirationTime"].Value);

            if (node.Attributes["QuotePolicyID"] != null)
            {
                this.QuotePolicyId = Guid.Parse(node.Attributes["QuotePolicyID"].Value);
            }

            if (node.Attributes["Price"] != null)
            {
                IQuotationSetter setter = quotationSetterProvider.Get(this.InstrumentId);
                this.Price = new Price(node.Attributes["Price"].Value, setter.NumeratorUnit, setter.Denominator);
            }
            this.State = AlertState.Pending;
        }

        internal bool Hit(Quotation quotaion)
        {
            bool isHit = false;
            if (this.Condition == AlertCondition.LessThanBid)
            {
                isHit = this.Price > quotaion.Bid;
                if (isHit) this.HitPrice = quotaion.Bid;
            }
            else if (this.Condition == AlertCondition.GreaterThanBid)
            {
                isHit = this.Price < quotaion.Bid;
                if (isHit) this.HitPrice = quotaion.Bid;
            }
            else if (this.Condition == AlertCondition.LessThanAsk)
            {
                isHit = this.Price > quotaion.Ask;
                if (isHit) this.HitPrice = quotaion.Ask;
            }
            else if (this.Condition == AlertCondition.GreaterThanAsk)
            {
                isHit = this.Price < quotaion.Ask;
                if (isHit) this.HitPrice = quotaion.Ask;
            }

            if (isHit)
            {
                this.State = AlertState.Hit;
                this.HitPriceTimestamp = quotaion.Timestamp;
            }

            return isHit;
        }

        internal bool Expire(DateTime now)
        {
            if (now >= this.ExpireTime)
            {
                this.State = AlertState.Expired;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void BuildUpdateXmlString(System.Text.StringBuilder builder)
        {
            builder.AppendFormat("<PriceAlert ID=\"{0}\"", this.Id);
            builder.AppendFormat(" State=\"{0}\"", (int)this.State);
            builder.AppendFormat(" ModifyFlag=\"{0}\"", (int)ModifyFlag.Update);
            if (this.State == AlertState.Hit)
            {
                builder.AppendFormat(" HitPrice=\"{0}\"", (string)this.HitPrice);
                builder.AppendFormat(" HitTime=\"{0}\"", this.HitPriceTimestamp.ToString(DateTimeFormat.Xml));
            }

            builder.Append(" />");
        }

        public override string ToString()
        {
            return string.Format("Id={0} InstrumentId={1} Price={2} Condition={3} State={4}",
                this.Id, this.InstrumentId, this.Price, this.Condition, this.State);
        }
    }
}
