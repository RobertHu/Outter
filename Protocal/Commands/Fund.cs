using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Commands
{
    public class Fund : XmlFillable<Fund>
    {
        public Fund(Account owner, Guid currencyID, string currencyCode)
        {
            this.Owner = owner;
            this.CurrencyId = currencyID;
            this.CurrencyCode = currencyCode;
        }

        public Account Owner { get; private set; }
        public Guid CurrencyId { get; private set; }
        public string CurrencyCode { get; private set; }

        public decimal Balance { get; set; }
        public decimal FrozenFund { get; set; }
        public decimal Necessary { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal Equity { get; set; }

        public decimal TradePLFloat { get; set; }
        public decimal InterestPLFloat { get; set; }
        public decimal StoragePLFloat { get; set; }
        public decimal ValueAsMargin { get; set; }
        public decimal TradePLNotValued { get; set; }
        public decimal InterestPLNotValued { get; set; }
        public decimal StoragePLNotValued { get; set; }
        public decimal LockOrderTradePLFloat { get; set; }
        public decimal FeeForCutting { get; set; }
        public decimal RiskCredit { get; set; }
        public decimal PartialPaymentPhysicalNecessary { get; set; }

        public void Initialize(XElement fundElement)
        {
            this.ParseFund(fundElement);
        }

        public void Update(XElement fundElement)
        {
            this.ParseFund(fundElement);
        }

        private void ParseFund(XElement fundElement)
        {
            this.InitializeProperties(fundElement);
        }


        protected override void InnerInitializeProperties(XElement element)
        {
            this.FillProperty(m => m.Balance);
            this.FillProperty(m => m.TotalPaidAmount);
            this.FillProperty(m => m.TotalDeposit);
            this.FillProperty(m => m.FrozenFund);
            this.FillProperty(m => m.Necessary);
            this.FillProperty(m => m.ValueAsMargin);
            this.FillProperty(m => m.TradePLNotValued);
            this.FillProperty(m => m.InterestPLNotValued);
            this.FillProperty(m => m.StoragePLNotValued);
            this.FillProperty(m => m.LockOrderTradePLFloat);
            this.FillProperty(m => m.FeeForCutting);
            this.FillProperty(m => m.RiskCredit);
            this.FillProperty(m => m.PartialPaymentPhysicalNecessary);
            this.FillProperty(m => m.Equity);
        }
    }

}
