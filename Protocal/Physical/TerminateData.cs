using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Physical
{
    [DataContract]
    public sealed class TerminateData
    {
        [DataMember]
        public Guid OrderId { get; set; }

        [DataMember]
        public decimal TerminateFee { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public decimal SourceTerminateFee { get; set; }

        [DataMember]
        public Guid SourceCurrencyId { get; set; }

        [DataMember]
        public decimal SourceAmount { get; set; }

        [DataMember]
        public bool IsPayOff { get; set; }

        [DataMember]
        public decimal CurrencyRate { get; set; }

        public override string ToString()
        {
            return this.GetType().ToString(this);
        }
    }


    [DataContract]
    public sealed class InstalmentData
    {
        [DataMember]
        public Guid OrderID { get; set; }
        [DataMember]
        public int Sequence { get; set; }
        [DataMember]
        public decimal InterestRate { get; set; }
        [DataMember]
        public decimal Principal { get; set; }
        [DataMember]
        public decimal Interest { get; set; }
        [DataMember]
        public decimal DebitInterest { get; set; }
        [DataMember]
        public Guid SourceCurrencyId { get; set; }
        [DataMember]
        public decimal SourceAmount { get; set; }
        [DataMember]
        public decimal CurrencyRate { get; set; }
        [DataMember]
        public DateTime ExecuteTime { get; set; }
        [DataMember]
        public decimal Lot { get; set; }
        [DataMember]
        public decimal LotBalance { get; set; }
        [DataMember]
        public Guid CurrencyId { get; set; }
        [DataMember]
        public DateTime PaymentDateTimeOnPlan { get; set; }
        [DataMember]
        public DateTime PaidDateTime { get; set; }
        [DataMember]
        public bool IsPayOff { get; set; }
        [DataMember]
        public decimal PaidPledge { get; set; }

        public override string ToString()
        {
            return this.GetType().ToString(this);
        }

    }


    public static class ToStringHelper
    {
        public static string ToString(this Type type, object source)
        {
            StringBuilder sb = new StringBuilder(200);
            Debug.WriteLine(type.ToString());
            var properties = type.GetProperties();
            Debug.WriteLine(properties.Length);
            foreach (var eachProperty in properties)
            {
                sb.AppendFormat("{0}={1},", eachProperty.Name, eachProperty.GetValue(source, null));
            }
            return sb.ToString(0, sb.Length - 1);
        }
    }


}
