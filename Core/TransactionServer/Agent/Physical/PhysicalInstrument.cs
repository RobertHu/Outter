using Core.TransactionServer.Agent.Quotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Physical.InstrumentBusiness;

namespace Core.TransactionServer.Agent.Physical
{
    internal class PhysicalInstrument : Instrument
    {
        private PhysicalLotCalculator _physicalLotCalculator;
        private PhysicalInstrumentCalculator _physicalInstrumentCalculator;

        internal PhysicalInstrument(Account owner, Guid id, QuotationBulk initQuotation, InstrumentServiceFactory factory)
            : base(owner, id, initQuotation, factory)
        {
            _physicalLotCalculator = this.LotCalculator as PhysicalLotCalculator;
            _physicalInstrumentCalculator = this.Calculator as PhysicalInstrumentCalculator;
        }

        public decimal TotalBuyLotBalanceForPartialPaymentPhysicalOrder
        {
            get
            {
                return _physicalLotCalculator.CalculateBuyLotBalanceForPartialPaymentPhysicalOrder();
            }
        }

        public decimal TotalSellLotBalanceForPartialPaymentPhysicalOrder
        {
            get
            {
                return _physicalLotCalculator.CalculateSellLotBalanceForPartialPaymentPhysicalOrder();
            }
        }

        public decimal TotalBuyMarginForPartialPaymentPhysicalOrder
        {
            get
            {
                return _physicalInstrumentCalculator.CalculateBuyMarginForPartialPaymentPhysicalOrder();
            }
        }

        public decimal TotalSellMarginForPartialPaymentPhysicalOrder
        {
            get
            {
                return _physicalInstrumentCalculator.CalculateSellMarginForPartialPaymentPhysicalOrder();
            }
        }

    }
}
