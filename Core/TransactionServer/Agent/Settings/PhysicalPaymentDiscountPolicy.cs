using Core.TransactionServer.Agent.Physical;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public enum DiscountOption
    {
        Progressive = 0,
        Flat = 1
    }

    public enum DiscountBasis
    {
        Percentage = 0,
        UnitAmount = 1
    }

    public class PhysicalPaymentDiscountPolicy
    {
        public Guid ID
        {
            get;
            private set;
        }

        public DiscountOption Option
        {
            get;
            private set;
        }

        public AllowedPaymentForm AllowedPaymentForms
        {
            get;
            private set;
        }

        public DiscountBasis DiscountBasis
        {
            get;
            private set;
        }

        private List<PhysicalPaymentDiscountPolicyDetail> Details
        {
            get;
            set;
        }

        internal PhysicalPaymentDiscountPolicyDetail this[Guid physicalPaymentDiscountDetailId]
        {
            get
            {
                int index = this.Details.FindIndex(delegate(PhysicalPaymentDiscountPolicyDetail item) { return item.ID == physicalPaymentDiscountDetailId; });
                return index == -1 ? null : this.Details[index];
            }
        }

        public PhysicalPaymentDiscountPolicy(IDBRow dataRow)
        {
            this.ID = (Guid)dataRow["ID"];
            this.Option = (DiscountOption)(int)dataRow["Option"];
            this.AllowedPaymentForms = (AllowedPaymentForm)(int)dataRow["AllowedPaymentForm"];
            this.DiscountBasis = (DiscountBasis)(int)dataRow["DiscountBasis"];

            this.Details = new List<PhysicalPaymentDiscountPolicyDetail>();
        }

        public PhysicalPaymentDiscountPolicy(XElement node)
        {
            this.ID = node.AttrToGuid("ID");
            this.Update(node);

            this.Details = new List<PhysicalPaymentDiscountPolicyDetail>();
        }

        public void Update(XElement node)
        {
            foreach (XAttribute attribute in node.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "Option":
                        this.Option = (DiscountOption)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "AllowedPaymentForm":
                        this.AllowedPaymentForms = (AllowedPaymentForm)XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "DiscountBasis":
                        this.DiscountBasis = (DiscountBasis)XmlConvert.ToInt32(attribute.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        internal decimal CalculateDiscount(decimal lot, decimal marketValue, AllowedPaymentForm paymentForm, CurrencyRate currencyRate)
        {
            if ((paymentForm & this.AllowedPaymentForms) != paymentForm || this.Details.Count == 0) return 0;


            if (this.Option == DiscountOption.Flat)
            {
                PhysicalPaymentDiscountPolicyDetail detail = this.Details.FindLast(delegate(PhysicalPaymentDiscountPolicyDetail item) { return lot > item.From; });
                decimal discount = 0;
                if (detail != null)
                {
                    if (this.DiscountBasis == DiscountBasis.UnitAmount)
                    {
                        discount = currencyRate.Exchange(lot * detail.DiscountValue);
                    }
                    else
                    {
                        discount = Math.Round(marketValue * detail.DiscountValue, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                    }
                }
                return discount;
            }
            else if (this.Option == DiscountOption.Progressive)
            {
                decimal discount = 0;

                int index = 0;
                while (index < this.Details.Count && lot > this.Details[index].From)
                {
                    PhysicalPaymentDiscountPolicyDetail detail = this.Details[index];
                    decimal calculateLot = lot - detail.From;
                    if (index < this.Details.Count - 1)
                    {
                        decimal range = (this.Details[index + 1].From - detail.From);
                        calculateLot = Math.Min(calculateLot, range);
                    }

                    decimal calculateValue = (marketValue * calculateLot) / lot;

                    discount += this.DiscountBasis == DiscountBasis.UnitAmount ? calculateLot * detail.DiscountValue : calculateValue * detail.DiscountValue;
                    index++;
                }

                if (this.DiscountBasis == DiscountBasis.UnitAmount)
                {
                    discount = currencyRate.Exchange(discount);
                }
                else
                {
                    discount = Math.Round(discount, currencyRate.TargetCurrency.Decimals, MidpointRounding.AwayFromZero);
                }
                return discount;
            }
            else
            {
                throw new NotSupportedException(string.Format("Option = {0} is not supported", this.Option));
            }
        }

        internal void Add(PhysicalPaymentDiscountPolicyDetail detail)
        {
            int index = this.Details.FindIndex(delegate(PhysicalPaymentDiscountPolicyDetail item) { return item.From > detail.From; });
            if (index == -1)
            {
                this.Details.Add(detail);
            }
            else
            {
                this.Details.Insert(index, detail);
            }
        }

        internal void Remove(PhysicalPaymentDiscountPolicyDetail detail)
        {
            this.Details.Remove(detail);
        }

        internal void Remove(Guid physicalPaymentDiscountPolicyDetailId)
        {
            int index = this.Details.FindIndex(delegate(PhysicalPaymentDiscountPolicyDetail item) { return item.ID == physicalPaymentDiscountPolicyDetailId; });
            if (index >= 0)
            {
                this.Details.RemoveAt(index);
            }
        }
        internal void Update(PhysicalPaymentDiscountPolicyDetail detail)
        {
            int index = this.Details.FindIndex(delegate(PhysicalPaymentDiscountPolicyDetail item) { return item.ID == detail.ID; });
            if (index == -1)
            {
                this.Add(detail);
            }
            else
            {
                this.Details[index] = detail;
            }
        }

        internal void ResortDetails()
        {
            this.Details.Sort(PhysicalPaymentDiscountPolicyDetailComparer.Default);
        }
    }

    public class PhysicalPaymentDiscountPolicyDetailComparer : IComparer<PhysicalPaymentDiscountPolicyDetail>
    {
        public static readonly PhysicalPaymentDiscountPolicyDetailComparer Default = new PhysicalPaymentDiscountPolicyDetailComparer();

        private PhysicalPaymentDiscountPolicyDetailComparer()
        {
        }

        int IComparer<PhysicalPaymentDiscountPolicyDetail>.Compare(PhysicalPaymentDiscountPolicyDetail x, PhysicalPaymentDiscountPolicyDetail y)
        {
            return x.From.CompareTo(y.From);
        }
    }

    public class PhysicalPaymentDiscountPolicyDetail
    {
        public Guid ID
        {
            get;
            private set;
        }

        public Guid PhysicalPaymentDiscountID
        {
            get;
            private set;
        }

        public decimal From
        {
            get;
            private set;
        }

        public decimal DiscountValue
        {
            get;
            private set;
        }

        public PhysicalPaymentDiscountPolicyDetail(IDBRow  dataRow)
        {
            this.ID = (Guid)dataRow["ID"];
            this.PhysicalPaymentDiscountID = (Guid)dataRow["PhysicalPaymentDiscountID"];
            this.From = (decimal)dataRow["From"];
            this.DiscountValue = (decimal)dataRow["DiscountValue"];
        }

        public PhysicalPaymentDiscountPolicyDetail(XElement node)
        {
            this.ID = node.AttrToGuid("ID");
            this.Update(node);
        }

        public void Update(XElement node)
        {
            foreach (XAttribute attribute in node.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "DiscountValue":
                        this.DiscountValue = XmlConvert.ToDecimal(attribute.Value);
                        break;
                    case "PhysicalPaymentDiscountID":
                        this.PhysicalPaymentDiscountID = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "From":
                        this.From = XmlConvert.ToDecimal(attribute.Value);
                        break;
                }
            }
        }
    }

}
