using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Physical.Delivery
{
    internal sealed class DeliveryModel : BusinessItemBuilder
    {
        private PhysicalOrder _owner;
        private BusinessItem<decimal> _deliveryLockLot;

        internal DeliveryModel(PhysicalOrder owner)
            : base(owner)
        {
            _owner = owner;
            _deliveryLockLot = this.CreateSoundItem("DeliveryLockLot", 0m);
        }

        internal decimal DeliveryLockLot
        {
            get { return _deliveryLockLot.Value; }
            set { _deliveryLockLot.SetValue(value); }
        }


        internal void LockForDelivery(decimal deliveryLot)
        {
            this.DeliveryLockLot += deliveryLot;
            Quotation quotation = _owner.Owner.AccountInstrument.GetQuotation();
            Price price = quotation == null ? _owner.ExecutePrice : (_owner.IsBuy ? quotation.SellPrice : quotation.BuyPrice);
            decimal valueAsMargin;
            MarketValueCalculator.CalculateMarketValue(_owner, price, null, out valueAsMargin);
            var account = _owner.Owner.Owner;
            if (account.IsMultiCurrency)
            {
                var fund = account.GetOrCreateFund(_owner.Owner.CurrencyId);
                fund.AddValueAsMargin(valueAsMargin);
            }
            else
            {
                var fund = account.GetOrCreateFund(account.Setting().CurrencyId);
                var currencyRate = Settings.Setting.Default.GetCurrencyRate(_owner.Owner.CurrencyId, account.Setting().CurrencyId);
                fund.AddValueAsMargin(currencyRate.Exchange(valueAsMargin));
            }
        }



    }
}
