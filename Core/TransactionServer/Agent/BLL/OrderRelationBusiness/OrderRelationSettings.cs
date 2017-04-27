using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.OrderRelationBusiness
{
    public class OrderRelationSettings : BusinessItemBuilder
    {
        private BusinessItem<Guid> _id;
        private BusinessItem<Guid> _closeOrderId;
        private BusinessItem<Guid> _openOrderId;
        private BusinessItem<decimal> _closedLot;
        private BusinessItem<DateTime?> _closeTime;
        private BusinessItem<DateTime?> _valueTime;
        private BusinessItem<int> _decimals;
        private BusinessItem<decimal> _rateIn;
        private BusinessItem<decimal> _rateOut;
        private BusinessItem<decimal> _commission;
        private BusinessItem<decimal> _levy;
        private BusinessItem<decimal> _otherFee;
        private BusinessItem<decimal> _interestPL;
        private BusinessItem<decimal> _storagePL;
        private BusinessItem<decimal> _tradePL;

        private BusinessItem<DateTime?> _openOrderExecuteTime;
        private BusinessItem<string> _openOrderExecutePrice;
        private BusinessItem<decimal> _estimateCloseCommissionOfOpenOrder;
        private BusinessItem<decimal> _estimateCloseLevyOfOpenOrder;

        private Order _closeOrder;

        internal OrderRelationSettings(OrderRelation owner, Order closeOrder, OrderRelationConstructParams constructParams)
            : base(owner)
        {
            _closeOrder = closeOrder;
            this.Parse(constructParams);
        }

        private void Parse(OrderRelationConstructParams constructParams)
        {
            _id = this.CreateKey("ID", constructParams.Id);
            _openOrderId = this.CreateKey(OrderRelationBusinessItemNames.OpenOrderId, constructParams.OpenOrder.Id);
            _closeOrderId = this.CreateKey(OrderRelationBusinessItemNames.CloseOrderId, _closeOrder.Id);
            _closedLot = this.CreateReadonlyItem(OrderRelationBusinessItemNames.ClosedLot, constructParams.ClosedLot);
            _closeTime = this.CreateSoundItem(OrderRelationBusinessItemNames.CloseTime, constructParams.CloseTime);
            _valueTime = this.CreateSoundItem(OrderRelationBusinessItemNames.ValueTime, constructParams.ValueTime);
            _decimals = this.CreateSoundItem(OrderRelationBusinessItemNames.Decimals, constructParams.Decimals);
            _rateIn = this.CreateSoundItem(OrderRelationBusinessItemNames.RateIn, constructParams.RateIn);
            _rateOut = this.CreateSoundItem(OrderRelationBusinessItemNames.RateOut, constructParams.RateOut);
            _commission = this.CreateSoundItem(OrderRelationBusinessItemNames.Commission, constructParams.Commission);
            _levy = this.CreateSoundItem(OrderRelationBusinessItemNames.Levy, constructParams.Levy);
            _otherFee = this.CreateSoundItem("OtherFee", constructParams.OtherFee);
            _interestPL = this.CreateSoundItem(OrderRelationBusinessItemNames.InterestPL, constructParams.InterestPL);
            _storagePL = this.CreateSoundItem(OrderRelationBusinessItemNames.StoragePL, constructParams.StoragePL);
            _tradePL = this.CreateSoundItem(OrderRelationBusinessItemNames.TradePL, constructParams.TradePL);
            _openOrderExecuteTime = this.CreateReadonlyItem("OpenOrderExecuteTime", constructParams.OpenOrderExecuteTime);
            _openOrderExecutePrice = this.CreateReadonlyItem("OpenOrderExecutePrice", constructParams.OpenOrderExecutePrice);
            _estimateCloseCommissionOfOpenOrder = this.CreateSoundItem("EstimateCloseCommissionOfOpenOrder", constructParams.EstimateCloseCommissionOfOpenOrder);
            _estimateCloseLevyOfOpenOrder = this.CreateSoundItem("EstimateCloseLevyOfOpenOrder", constructParams.EstimateCloseLevyOfOpenOrder);
        }

        internal Guid Id
        {
            get { return _id.Value; }
            set { _id.SetValue(value); }
        }

        internal decimal ClosedLot
        {
            get { return this._closedLot.Value; }
            set { _closedLot.SetValue(value); }
        }

        internal DateTime? CloseTime
        {
            get { return _closeTime.Value; }
            set { this._closeTime.SetValue(value); }
        }

        public DateTime? ValueTime
        {
            get { return this._valueTime.Value; }
            set { _valueTime.SetValue(value); }
        }

        public int Decimals
        {
            get { return this._decimals.Value; }
            set { _decimals.SetValue(value); }
        }

        public decimal RateIn
        {
            get { return this._rateIn.Value; }
            set { _rateIn.SetValue(value); }
        }

        public decimal RateOut
        {
            get { return this._rateOut.Value; }
            set { _rateOut.SetValue(value); }
        }

        public decimal Commission
        {
            get { return _commission.Value; }
            set { _commission.SetValue(value); }
        }

        public decimal Levy
        {
            get { return _levy.Value; }
            set { _levy.SetValue(value); }
        }

        public decimal OtherFee
        {
            get { return _otherFee.Value; }
            set { _otherFee.SetValue(value); }
        }

        public decimal InterestPL
        {
            get { return _interestPL.Value; }
            set { _interestPL.SetValue(value); }
        }

        public decimal StoragePL
        {
            get { return _storagePL.Value; }
            set { _storagePL.SetValue(value); }
        }

        public decimal TradePL
        {
            get { return _tradePL.Value; }
            set { _tradePL.SetValue(value); }
        }

        public decimal EstimateCloseCommissionOfOpenOrder
        {
            get { return _estimateCloseCommissionOfOpenOrder.Value; }
            set { _estimateCloseCommissionOfOpenOrder.SetValue(value); }
        }

        public decimal EstimateCloseLevyOfOpenOrder
        {
            get { return _estimateCloseLevyOfOpenOrder.Value; }
            set { _estimateCloseLevyOfOpenOrder.SetValue(value); }
        }

    }

}
