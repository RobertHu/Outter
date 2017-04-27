using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Xml;
using System.Data;
using Protocal;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;
using System.Xml.Linq;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public struct InstalmentPeriod
    {
        private int _period;
        private InstalmentFrequence _frequence;

        internal InstalmentPeriod(int period, InstalmentFrequence frequence)
        {
            _period = period;
            _frequence = frequence;
        }


        internal int Period
        {
            get { return _period; }
        }

        internal InstalmentFrequence Frequence
        {
            get { return _frequence; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            InstalmentPeriod other = (InstalmentPeriod)obj;
            return other.Period == this.Period && other.Frequence == this.Frequence;
        }

        public override int GetHashCode()
        {
            return this.Period.GetHashCode() ^ this.Frequence.GetHashCode();
        }

        public static bool operator ==(InstalmentPeriod period1, InstalmentPeriod period2)
        {
            if (object.ReferenceEquals(period1, period2)) return true;
            if ((object)period1 == null || (object)period2 == null) return false;
            return period1.Equals(period2);
        }

        public static bool operator !=(InstalmentPeriod period1, InstalmentPeriod period2)
        {
            return !(period1 == period2);
        }
    }

    public enum AdvancePaymentOption
    {
        DisallowAll,
        AllowAll,
        AllowInstalment,
        AllowPrepayment
    }

    public sealed class InstalmentPolicy
    {
        private Dictionary<InstalmentPeriod, InstalmentPolicyDetail> _instalmentPolicyDetails = new Dictionary<InstalmentPeriod, InstalmentPolicyDetail>();

        internal InstalmentPolicy(IDBRow dataRow)
        {
            this.Id = (Guid)dataRow["Id"];
            this.ValueDiscountAsMargin = (decimal)dataRow["ValueDiscountAsMargin"];
            this.CloseOption = (InstalmentCloseOption)((int)dataRow["AllowClose"]);
            this.IsDownPayAsFirstPay = (bool)dataRow["IsDownPayAsFirstPay"];
            this.AdvancePaymentOption = (AdvancePaymentOption)((int)dataRow["AdvancePayment"]);
        }

        internal InstalmentPolicy(XElement xmlNode)
        {
            this.Update(xmlNode);
        }

        internal Guid Id
        {
            get;
            private set;
        }

        internal decimal ValueDiscountAsMargin
        {
            get;
            private set;
        }

        internal AdvancePaymentOption AdvancePaymentOption { get; private set; }

        internal InstalmentCloseOption CloseOption { get; private set; }

        internal bool IsDownPayAsFirstPay { get; private set; }

        internal void Update(XElement xmlNode)
        {
            foreach (XAttribute attribute in xmlNode.Attributes())
            {
                if (attribute.Name == "ID")
                {
                    this.Id = Guid.Parse(attribute.Value);
                }
                else if (attribute.Name == "ValueDiscountAsMargin")
                {
                    this.ValueDiscountAsMargin = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "AllowClose")
                {
                    this.CloseOption = (InstalmentCloseOption)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "AdvancePayment")
                {
                    this.AdvancePaymentOption = (AdvancePaymentOption)(int.Parse(attribute.Value));
                }
            }
        }

        internal void Add(InstalmentPolicyDetail instalmentPolicyDetail)
        {
            if (!_instalmentPolicyDetails.ContainsKey(instalmentPolicyDetail.Period))
            {
                this._instalmentPolicyDetails.Add(instalmentPolicyDetail.Period, instalmentPolicyDetail);
            }
        }

        internal InstalmentPolicyDetail Get(InstalmentPeriod period)
        {
            InstalmentPolicyDetail result;
            _instalmentPolicyDetails.TryGetValue(period, out result);
            return result;
        }

        internal void Remove(int period, InstalmentFrequence frequence)
        {
            if (_instalmentPolicyDetails.ContainsKey(new InstalmentPeriod(period, frequence)))
            {
                _instalmentPolicyDetails.Remove(new InstalmentPeriod(period, frequence));
            }
        }

    }

    internal sealed class InstalmentPolicyDetail
    {
        internal InstalmentPolicyDetail(IDBRow  dataRow)
        {
            this.InstalmentPolicyId = dataRow.GetColumn<Guid>("InstalmentPolicyId");
            this.IsActive = (bool)dataRow["IsActive"];
            this.InstalmentFeeType = (InstalmentFeeType)dataRow.GetColumn<int>("AdministrationFeeBase");
            this.InstalmentFeeRate = dataRow.GetColumn<decimal>("AdministrationFee");
            this.PrepaymentFeeType = (PrepaymentFeeType)dataRow.GetColumn<int>("ContractTerminateType");
            this.PrepaymentFeeRate = dataRow.GetColumn<decimal>("ContractTerminateFee");
            this.DownPaymentBasis = (DownPaymentBasis)dataRow.GetColumn<int>("DownPaymentBasis");

            this.LatePaymentAutoCutDay = dataRow.GetColumn<int>("LatePaymentAutoCutDay");
            this.AutoCutPenaltyValue = dataRow.GetColumn<decimal>("AutoCutPenaltyValue");
            this.AutoCutPenaltyBase = (AutoCutPenaltyBase)dataRow.GetColumn<int>("AutoCutPenaltyBase");
            this.InterestRate = dataRow.GetColumn<decimal>("InterestRate");

            this.ClosePenaltyValue = dataRow.GetColumn<decimal>("ClosePenaltyValue");
            this.ClosePenaltyBase = (ClosePenaltyBase)dataRow.GetColumn<int>("ClosePenaltyBase");
            this.DebitInterestRatio = dataRow.GetColumn<decimal>("DebitInterestRatio");
            this.DebitFreeDays = dataRow.GetColumn<int>("DebitFreeDays");
            this.DebitInterestType = dataRow.GetColumn<int>("DebitInterestType");

            int period = dataRow.GetColumn<int>("Period");
            InstalmentFrequence frequence = (InstalmentFrequence)dataRow.GetColumn<int>("Frequence");
            this.Period = new InstalmentPeriod(period, frequence);
        }

        internal InstalmentPolicyDetail(DB.DBMapping.InstalmentPolicyDetail detail)
        {
            this.InstalmentPolicyId = detail.InstalmentPolicyId;
            this.IsActive = detail.IsActive;
            this.InstalmentFeeType = (InstalmentFeeType)detail.AdministrationFeeBase;
            this.InstalmentFeeRate = detail.AdministrationFee;
            this.PrepaymentFeeType = (PrepaymentFeeType)detail.ContractTerminateType;
            this.PrepaymentFeeRate = detail.ContractTerminateFee;
            this.DownPaymentBasis = (DownPaymentBasis)detail.DownPaymentBasis;

            this.LatePaymentAutoCutDay = detail.LatePaymentAutoCutDay;
            this.AutoCutPenaltyValue = detail.AutoCutPenaltyValue;
            this.AutoCutPenaltyBase = (AutoCutPenaltyBase)detail.AutoCutPenaltyBase;
            this.InterestRate = detail.InterestRate;

            this.ClosePenaltyValue = detail.ClosePenaltyValue;
            this.ClosePenaltyBase = (ClosePenaltyBase)detail.ClosePenaltyBase;
            this.DebitInterestRatio = detail.DebitInterestRatio;
            this.DebitFreeDays = detail.DebitFreeDays;
            this.DebitInterestType = detail.DebitInterestType;

            int period = detail.Period;
            InstalmentFrequence frequence = (InstalmentFrequence)detail.Frequence;
            this.Period = new InstalmentPeriod(period, frequence);
        }

        internal InstalmentPolicyDetail(XElement xmlNode)
        {
            this.Update(xmlNode);
        }

        internal Guid InstalmentPolicyId { get; private set; }

        internal InstalmentPeriod Period { get; private set; }

        internal decimal InterestRate { get; private set; }

        internal InstalmentFeeType InstalmentFeeType { get; private set; }

        internal decimal InstalmentFeeRate { get; private set; }

        internal PrepaymentFeeType PrepaymentFeeType { get; private set; }

        internal decimal PrepaymentFeeRate { get; private set; }

        internal DownPaymentBasis DownPaymentBasis { get; private set; }

        internal int LatePaymentAutoCutDay { get; private set; }

        internal AutoCutPenaltyBase AutoCutPenaltyBase { get; private set; }

        internal decimal AutoCutPenaltyValue { get; private set; }

        internal ClosePenaltyBase ClosePenaltyBase { get; private set; }

        internal decimal ClosePenaltyValue { get; private set; }

        internal decimal DebitInterestRatio { get; private set; }

        internal int DebitFreeDays { get; private set; }

        internal int DebitInterestType { get; private set; }

        public bool IsActive { get; private set; }

        internal void Update(XElement xmlNode)
        {
            foreach (XAttribute attribute in xmlNode.Attributes())
            {
                if (attribute.Name == "InstalmentPolicyId")
                {
                    this.InstalmentPolicyId = Guid.Parse(attribute.Value);
                }
                else if (attribute.Name == "IsActive")
                {
                    this.IsActive = XmlConvert.ToBoolean(attribute.Value);
                }
                else if (attribute.Name == "Period")
                {
                    int period = int.Parse(attribute.Value);
                    this.Period = new InstalmentPeriod(period, this.Period == null ? InstalmentFrequence.Month : this.Period.Frequence);
                }
                else if (attribute.Name == "Frequence")
                {
                    InstalmentFrequence frequence = (InstalmentFrequence)(int.Parse(attribute.Value));
                    this.Period = new InstalmentPeriod(this.Period == null ? 0 : this.Period.Period, frequence);
                }
                else if (attribute.Name == "DownPaymentBasis")
                {
                    this.DownPaymentBasis = (DownPaymentBasis)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "AdministrationFeeBase")
                {
                    this.InstalmentFeeType = (InstalmentFeeType)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "AdministrationFee")
                {
                    this.InstalmentFeeRate = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "ContractTerminateType")
                {
                    this.PrepaymentFeeType = (PrepaymentFeeType)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "ContractTerminateFee")
                {
                    this.PrepaymentFeeRate = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "AutoCutPenaltyBase")
                {
                    this.AutoCutPenaltyBase = (AutoCutPenaltyBase)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "AutoCutPenaltyValue")
                {
                    this.AutoCutPenaltyValue = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "LatePaymentAutoCutDay")
                {
                    this.LatePaymentAutoCutDay = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "ClosePenaltyBase")
                {
                    this.ClosePenaltyBase = (ClosePenaltyBase)(int.Parse(attribute.Value));
                }
                else if (attribute.Name == "ClosePenaltyValue")
                {
                    this.ClosePenaltyValue = decimal.Parse(attribute.Value);
                }

            }
        }
    }
}