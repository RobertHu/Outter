using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator
{
    internal class OrderFloating
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderFloating));

        private BusinessItem<Price> _livePrice;
        private BusinessItem<decimal> _interestPLFloat;
        private BusinessItem<decimal> _storagePLFloat;
        private BusinessItem<decimal> _tradePLFloat;
        protected Order _owner;
        protected CalculateParams _calculateParams;

        internal OrderFloating(Order order, CalculateParams calculateParams)
        {
            _owner = order;
            _calculateParams = calculateParams;
            _interestPLFloat = BusinessItemFactory.Create("InterestPLFloat", 0m, PermissionFeature.Dumb, order);
            _storagePLFloat = BusinessItemFactory.Create("StoragePLFloat", 0m, PermissionFeature.Dumb, order);
            _tradePLFloat = BusinessItemFactory.Create("TradePLFloat", 0m, PermissionFeature.Dumb, order);
            _livePrice = BusinessItemFactory.Create("LivePrice", (Price)null, PermissionFeature.Dumb, order);
        }

        public decimal InterestPLFloat
        {
            get { return _interestPLFloat.Value; }
            set { _interestPLFloat.SetValue(value); }
        }

        public decimal StoragePLFloat
        {
            get { return _storagePLFloat.Value; }
            set { _storagePLFloat.SetValue(value); }
        }

        public decimal TradePLFloat
        {
            get { return _tradePLFloat.Value; }
            set { _tradePLFloat.SetValue(value); }
        }

        public decimal Necessary { get; private set; }

        public Price LivePrice
        {
            get { return _livePrice.Value; }
            set { _livePrice.SetValue(value); }
        }

        public void Calculate(Quotation quotation)
        {
            if (quotation == null) return;
            Debug.Assert(_calculateParams != null);
            _calculateParams.Update(quotation);
            this.InnerCalculate(quotation);
            _calculateParams.IsCalculated = true;
        }

        public void CalculateFloatPLForcely(Quotation quotation)
        {
            this.CalculateTradePL(quotation);
        }

        public void CalculateNecessary(decimal lot)
        {
            this.Necessary = NecessaryCalculator.CalculateNecessary(_owner, null, lot, null);
        }


        protected virtual void InnerCalculate(Quotation quotation)
        {
            if (this.NeedCalculateTradePL())
            {
                this.CalculateTradePL(quotation);
            }

            if (this.NeedCalculateNecessary())
            {
                this.CalculateNecessary(quotation);
            }

            if (this.NeedCalculateInterestAndStoragePL())
            {
                this.CalculateStoragePL();
                this.CalculateInterestPL();
            }
        }


        internal void CalculateTradePL(Quotation quotation)
        {
            this.TradePLFloat = TradePLCalculator.Calculate(_owner, quotation, null);
            if (_calculateParams.ChangedItem.HasFlag(ChangedItem.Quotation))
            {
                this.CalculateLivePrice(quotation);
            }
        }

        protected void CalculateNecessary(Quotation quotation)
        {
            this.Necessary = NecessaryCalculator.CalculateNecessary(_owner, quotation, null);
        }

        protected void CalculateInterestPL()
        {
            this.InterestPLFloat = _owner.CurrencyRate.Exchange(_owner.LotBalance * _owner.InterestPerLot);
        }

        protected void CalculateStoragePL()
        {
            this.StoragePLFloat = _owner.CurrencyRate.Exchange(_owner.LotBalance * _owner.StoragePerLot);
        }

        protected virtual bool NeedCalculateNecessary()
        {
            bool result = false;
            result |= this.NeedCalculateCommon();
            bool quotationChanged = _calculateParams.ChangedItem.Include(ChangedItem.Quotation);
            result |= (quotationChanged && _calculateParams.MarginFormula.MarketPriceInvolved());
            result |= _calculateParams.ChangedItem.Include(ChangedItem.MarginFormula);
            return result;
        }

        protected virtual bool NeedCalculateTradePL()
        {
            bool quotationChanged = _calculateParams.ChangedItem.Include(ChangedItem.Quotation);
            return this.NeedCalculateCommon() || quotationChanged;
        }

        protected virtual bool NeedCalculateInterestAndStoragePL()
        {
            return this.NeedCalculateCommon();
        }

        protected void CalculateLivePrice(Quotation quotation)
        {
            this.LivePrice = _owner.IsBuy ? quotation.BuyPrice : quotation.SellPrice;
        }

        protected bool NeedCalculateCommon()
        {
            bool currencyRateChanged = _calculateParams.ChangedItem.Include(ChangedItem.CurrecnyRate);
            bool lotBalanceChanged = _calculateParams.ChangedItem.Include(ChangedItem.LotBalance);
            return !_calculateParams.IsCalculated || currencyRateChanged || lotBalanceChanged;
        }

    }
}
