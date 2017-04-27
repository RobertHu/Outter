using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using System.Threading;
using CommonPrice = iExchange.Common.Price;
using System.Collections.Concurrent;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Framework;
using System.Diagnostics;
using log4net;
using Protocal;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Hit;

namespace Core.TransactionServer.Agent.Quotations
{
    public sealed class QuotePolicy2QuotationDict : Dictionary<Guid, Quotation>
    {
        private const int QuotePolicyToQuotationFactor = 10;
        public QuotePolicy2QuotationDict() : base(QuotePolicyToQuotationFactor) { }

        internal void AddQuotation(Guid id, Quotation quotation)
        {
            if (!this.ContainsKey(id))
            {
                this.Add(id, quotation);
            }
            else
            {
                this[id] = quotation;
            }
        }
    }


    public sealed class QuotationBulk
    {
        private List<KeyValuePair<Guid, QuotePolicy2QuotationDict>> _instrument2QuotationsDict = new List<KeyValuePair<Guid, QuotePolicy2QuotationDict>>(17);
        private DateTime _timeStamp;

        internal QuotationBulk(OverridedQ[] overridedQs)
        {
            this.Initialize(overridedQs);
        }

        internal QuotationBulk(Dictionary<Guid, QuotePolicy2QuotationDict> source)
        {
            _timeStamp = Market.MarketManager.Now;
            foreach (var eachItem in source)
            {
                _instrument2QuotationsDict.Add(new KeyValuePair<Guid, QuotePolicy2QuotationDict>(eachItem.Key, eachItem.Value));
            }
        }


        internal List<KeyValuePair<Guid, QuotePolicy2QuotationDict>> Quotations
        {
            get { return _instrument2QuotationsDict; }
        }


        private void Initialize(OverridedQ[] overridedQs)
        {
            _timeStamp = Market.MarketManager.Now;
            foreach (OverridedQ eachOverridedQuotation in overridedQs)
            {
                Quotation quotation = Quotation.Create(eachOverridedQuotation);
                if (quotation == null) continue;
                QuotePolicy2QuotationDict quotationList;

                if (!this.TryGetValue(eachOverridedQuotation.InstrumentID, out quotationList))
                {
                    quotationList = new QuotePolicy2QuotationDict();
                    _instrument2QuotationsDict.Add(new KeyValuePair<Guid, QuotePolicy2QuotationDict>(eachOverridedQuotation.InstrumentID, quotationList));
                }
                if (quotationList.ContainsKey(eachOverridedQuotation.QuotePolicyID)) throw new ApplicationException("Can't have more than one price for one instrument + quotepolicy in a batch of quotation");
                quotationList.Add(eachOverridedQuotation.QuotePolicyID, quotation);
            }
        }

        internal DateTime Timestamp
        {
            get { return _timeStamp; }
        }

        public QuotePolicy2QuotationDict GetQuotations(Guid instrumentId)
        {
            QuotePolicy2QuotationDict result = null;
            this.TryGetValue(instrumentId, out result);
            return result;
        }

        internal bool TryGetQuotation(Guid instrumentId, IQuotePolicyProvider quotePolicyProvider, out Quotation quotation)
        {
            quotation = null;
            QuotePolicy2QuotationDict quotations = null;
            if (this.TryGetValue(instrumentId, out quotations))
            {
                quotation = quotePolicyProvider.Get<Quotation>(delegate(Guid id, out Quotation q)
                  {
                      return quotations.TryGetValue(id, out q);
                  });
            }
            return quotation != null;
        }

        internal bool TryGetValue(Guid instrumentId, out QuotePolicy2QuotationDict quotationList)
        {
            quotationList = null;
            for (int i = 0; i < _instrument2QuotationsDict.Count; i++)
            {
                var eachItem = _instrument2QuotationsDict[i];
                if (eachItem.Key.Equals(instrumentId))
                {
                    quotationList = eachItem.Value;
                    return true;
                }
            }
            return false;
        }
    }



    public sealed class Quotation
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Quotation));

        public static Price GetBuyPrice(bool isNormal, Price ask, Price bid)
        {
            return isNormal ? bid : ask;
        }

        public static Price GetSellPrice(bool isNormal, Price ask, Price bid)
        {
            return isNormal ? ask : bid;
        }

        public static Price GetBuyPriceForReset(bool isNormal, Price ask, Price bid)
        {
            return isNormal ? ask : bid;
        }

        public static Price GetSellPriceForReset(bool isNormal, Price ask, Price bid)
        {
            return !isNormal ? ask : bid;
        }

        public static Quotation CreateByRehit(Price ask, Price bid)
        {
            return new Quotation() { Ask = ask, Bid = bid };
        }

        private Quotation()
        {
        }

        internal Guid InstrumentId
        {
            get;
            private set;
        }

        internal DateTime Timestamp
        {
            get;
            private set;
        }

        internal bool IsNormal
        {
            get;
            private set;
        }

        internal CommonPrice BuyPrice
        {
            get
            {
                return GetBuyPrice(this.IsNormal, this.Ask, this.Bid);
            }
        }

        internal CommonPrice SellPrice
        {
            get
            {
                return GetSellPrice(this.IsNormal, this.Ask, this.Bid);
            }
        }

        internal CommonPrice Ask
        {
            get;
            private set;
        }

        internal CommonPrice Bid
        {
            get;
            private set;
        }

        internal CommonPrice High
        {
            get;
            private set;
        }

        internal CommonPrice Low
        {
            get;
            private set;
        }

        internal void Merge(Quotation quotation)
        {
            if (this.Timestamp <= quotation.Timestamp)
            {
                this.Timestamp = quotation.Timestamp;
                if (quotation.Ask != null) this.Ask = quotation.Ask;
                if (quotation.Bid != null) this.Bid = quotation.Bid;
                if (quotation.High != null) this.High = quotation.High;
                if (quotation.Low != null) this.Low = quotation.Low;
            }
            else
            {
                if (this.Ask == null) this.Ask = quotation.Ask;
                if (this.Bid == null) this.Bid = quotation.Bid;
                if (this.High == null) this.High = quotation.High;
                if (this.Low == null) this.Low = quotation.Low;
            }
        }

        internal QuotationTrend CalculateTrend(Quotation other)
        {
            if (other.Bid == this.Bid)
            {
                return QuotationTrend.Identical;
            }
            else if (this.Bid > other.Bid)
            {
                return QuotationTrend.Up;
            }
            else
            {
                return QuotationTrend.Down;
            }
        }


        internal PriceCompareResult Compare(Price price, bool isBuy)
        {
            var marketPrice = HitCommon.CalculateMarketPrice(isBuy, this);
            int compareResult = Price.Compare(marketPrice, price, !this.IsNormal);
            PriceCompareResult result;
            if (compareResult == 0)
            {
                result = PriceCompareResult.Fair;
            }
            else if (compareResult > 0)
            {
                result = isBuy ? PriceCompareResult.Worse : PriceCompareResult.Better;
            }
            else
            {
                result = isBuy ? PriceCompareResult.Better : PriceCompareResult.Worse;
            }
            return result;
        }

        internal static Quotation Create(OverridedQ overridedQuotation)
        {
            return Quotation.Create(overridedQuotation.InstrumentID, overridedQuotation.Ask,
                      overridedQuotation.Bid, overridedQuotation.High, overridedQuotation.Low, overridedQuotation.Timestamp, Settings.Setting.Default);
        }


        public static Quotation Create(Guid instrumentId, string ask, string bid,
            string high, string low, DateTime timestamp, Setting quotationParameterProvider)
        {
            QuotationParameter quotationParameter = quotationParameterProvider.GetQuotationParameter(instrumentId);
            if (quotationParameter == QuotationParameter.Invalid) return null;
            Quotation quotation = new Quotation();
            quotation.IsNormal = quotationParameter.IsNormal;
            quotation.InstrumentId = instrumentId;
            quotation.Timestamp = timestamp;

            quotation.Ask = CommonPrice.CreateInstance(ask, quotationParameter.Numerator, quotationParameter.Denominator);
            quotation.Bid = CommonPrice.CreateInstance(bid, quotationParameter.Numerator, quotationParameter.Denominator);
            quotation.High = CommonPrice.CreateInstance(high, quotationParameter.Numerator, quotationParameter.Denominator);
            quotation.Low = CommonPrice.CreateInstance(low, quotationParameter.Numerator, quotationParameter.Denominator);
            return quotation;
        }

    }
}