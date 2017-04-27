using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data;
using System.Xml.Linq;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{

    internal enum ExchangeDirection
    {
        RateIn,
        RateOut
    }

    public sealed class CurrencyRate
    {
        private Currency sourceCurrency;
        private Currency targetCurrency;
        decimal rateIn;
        decimal rateOut;
        private Guid? dependingInstrumentId;
        private bool inverted;

        #region Common internal properties definition
        internal Currency SourceCurrency
        {
            get { return this.sourceCurrency; }
        }
        internal Currency TargetCurrency
        {
            get { return this.targetCurrency; }
        }
        public decimal RateIn
        {
            get { return this.rateIn; }
        }
        public decimal RateOut
        {
            get { return this.rateOut; }
        }
        internal Guid? DependingInstrumentId
        {
            get { return this.dependingInstrumentId; }
        }
        internal bool Inverted
        {
            get { return this.inverted; }
        }
        #endregion Common internal properties definition

        /// <summary>
        /// 为了创建CurrencyRate.ONE
        /// </summary>
        private CurrencyRate()
        {
        }

        public override string ToString()
        {
            return string.Format("sourceCurrencyId = {0}, targetCurrencyId = {1}, rateIn = {2}, rateOut = {3}", this.sourceCurrency.Id, this.targetCurrency.Id, this.rateIn, this.rateOut);
        }


        internal CurrencyRate(XElement currencyRateNode, Currency sourceCurrency, Currency targetCurrency)
        {
            this.sourceCurrency = sourceCurrency;
            this.targetCurrency = targetCurrency;
            this.Update(currencyRateNode);
        }
        internal CurrencyRate(IDBRow currencyRateRow, Currency sourceCurrency, Currency targetCurrency)
        {
            this.sourceCurrency = sourceCurrency;
            this.targetCurrency = targetCurrency;
            this.rateIn = (decimal)(double)currencyRateRow["RateIn"];
            this.rateOut = (decimal)(double)currencyRateRow["RateOut"];
            this.dependingInstrumentId = currencyRateRow["DependingInstrumentId"] == DBNull.Value ? null : (Guid?)currencyRateRow["DependingInstrumentId"];
            this.inverted = currencyRateRow["Inverted"] == DBNull.Value ? false : (bool)currencyRateRow["Inverted"];
        }

        internal CurrencyRate(Currency sourceCurrency, Currency targetCurrency, decimal rateIn, decimal rateOut, Guid? dependingInstrumentId, bool inverted)
        {
            this.sourceCurrency = sourceCurrency;
            this.targetCurrency = targetCurrency;
            this.rateIn = rateIn;
            this.rateOut = rateOut;
            this.dependingInstrumentId = dependingInstrumentId;
            this.inverted = inverted;
        }

        internal void Update(decimal rateIn, decimal rateOut)
        {
            this.rateIn = rateIn;
            this.rateOut = rateOut;
        }

        internal decimal Exchange(decimal amount)
        {
            return this.Exchange(amount, true, MidpointRounding.AwayFromZero);
        }

        internal decimal Exchange(decimal amount, bool isDirect)
        {
            return this.Exchange(amount, isDirect, MidpointRounding.AwayFromZero);
        }

        internal decimal Exchange(decimal amount, ExchangeDirection direction)
        {
            var rate = direction == ExchangeDirection.RateIn ? this.rateIn : this.rateOut;
            var roundedAmount = Math.Round(amount, this.sourceCurrency.Decimals, MidpointRounding.AwayFromZero);
            var exchangedValue = roundedAmount * rate;
            return Math.Round(exchangedValue, this.targetCurrency.Decimals, MidpointRounding.AwayFromZero);
        }

        private decimal Exchange(decimal amount, bool isDirect, MidpointRounding midpointRounding)
        {
            if (isDirect)
            {
                return Math.Round(Math.Round(amount, this.sourceCurrency.Decimals, midpointRounding) * (amount > 0 ? this.rateIn : this.rateOut), this.targetCurrency.Decimals, midpointRounding);
            }
            else
            {
                return Math.Round(Math.Round(amount, this.targetCurrency.Decimals, midpointRounding) / (amount > 0 ? this.rateIn : this.rateOut), this.sourceCurrency.Decimals, midpointRounding);
            }
        }



        internal bool Update(XElement rateNode)
        {
            foreach (var attribute in rateNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "RateIn":
                        this.rateIn = (decimal)XmlConvert.ToDouble(attribute.Value);
                        break;
                    case "RateOut":
                        this.rateOut = (decimal)XmlConvert.ToDouble(attribute.Value);
                        break;
                    case "DependingInstrumentId":
                        if (!string.IsNullOrEmpty(attribute.Value.Trim()))
                        {
                            this.dependingInstrumentId = XmlConvert.ToGuid(attribute.Value);
                        }
                        else
                        {
                            this.dependingInstrumentId = null;
                        }
                        break;
                    case "Inverted":
                        if (!string.IsNullOrEmpty(attribute.Value.Trim()))
                        {
                            this.inverted = XmlConvert.ToBoolean(attribute.Value);
                        }
                        break;
                }
            }

            return true;
        }
    }
}