using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass
{
    public sealed class RiskData
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RiskData));
        private BusinessItem<decimal> _necessary;
        private BusinessItem<decimal> _netNecessary;
        private BusinessItem<decimal> _hedgeNecessary;
        private BusinessItem<decimal> _necessaryFillingOpenOrder;
        private BusinessItem<decimal> _necessaryFillingCloseOrder;

        private BusinessItem<decimal> _minEquityAvoidRiskLevel1;
        private BusinessItem<decimal> _minEquityAvoidRiskLevel2;
        private BusinessItem<decimal> _minEquityAvoidRiskLevel3;

        private BusinessItem<decimal> _tradePLFloat;
        private BusinessItem<decimal> _interestPLFloat;
        private BusinessItem<decimal> _storagePLFloat;
        private BusinessItem<decimal> _valueAsMargin;

        private BusinessItem<decimal> _tradePLNotValued;
        private BusinessItem<decimal> _interestPLNotValued;
        private BusinessItem<decimal> _storagePLNotValued;

        private BusinessItem<decimal> _lockOrderTradePLFloat;
        private BusinessItem<decimal> _feeForCutting;
        private BusinessItem<decimal> _riskCredit;

        private BusinessItem<decimal> _partialPaymentPhysicalNecessary;

        private BusinessItem<decimal> _totalPaidAmount;

        public decimal Necessary
        {
            get { return this._necessary.Value; }
            set
            {
                this._necessary.SetValue(value);
            }
        }

        internal decimal NetNecessary
        {
            get { return this._netNecessary.Value; }
            set { this._netNecessary.SetValue(value); }
        }

        internal decimal HedgeNecessary
        {
            get { return this._hedgeNecessary.Value; }
            set { this._hedgeNecessary.SetValue(value); }
        }

        internal decimal MinEquityAvoidRiskLevel1
        {
            get { return this._minEquityAvoidRiskLevel1.Value; }
            set { this._minEquityAvoidRiskLevel1.SetValue(value); }
        }

        internal decimal MinEquityAvoidRiskLevel2
        {
            get { return this._minEquityAvoidRiskLevel2.Value; }
            set { this._minEquityAvoidRiskLevel2.SetValue(value); }
        }

        internal decimal MinEquityAvoidRiskLevel3
        {
            get { return this._minEquityAvoidRiskLevel3.Value; }
            set { this._minEquityAvoidRiskLevel3.SetValue(value); }
        }

        internal decimal NecessaryFillingOpenOrder
        {
            get { return this._necessaryFillingOpenOrder.Value; }
            set { this._necessaryFillingOpenOrder.SetValue(value); }
        }

        internal decimal NecessaryFillingCloseOrder
        {
            get { return this._necessaryFillingCloseOrder.Value; }
            set { this._necessaryFillingCloseOrder.SetValue(value); }
        }

        internal decimal TradePLFloat
        {
            get { return this._tradePLFloat.Value; }
            set
            {
                this._tradePLFloat.SetValue(value);
            }
        }


        internal decimal InterestPLFloat
        {
            get { return this._interestPLFloat.Value; }
            set { this._interestPLFloat.SetValue(value); }
        }

        internal decimal StoragePLFloat
        {
            get { return this._storagePLFloat.Value; }
            set { this._storagePLFloat.SetValue(value); }
        }

        internal decimal ValueAsMargin
        {
            get { return this._valueAsMargin.Value; }
            set { this._valueAsMargin.SetValue(value); }
        }

        internal decimal TradePLNotValued
        {
            get { return this._tradePLNotValued.Value; }
            set { this._tradePLNotValued.SetValue(value); }
        }

        internal decimal InterestPLNotValued
        {
            get { return this._interestPLNotValued.Value; }
            set { this._interestPLNotValued.SetValue(value); }
        }

        internal decimal StoragePLNotValued
        {
            get { return this._storagePLNotValued.Value; }
            set { this._storagePLNotValued.SetValue(value); }
        }

        internal decimal LockOrderTradePLFloat
        {
            get { return this._lockOrderTradePLFloat.Value; }
            set { this._lockOrderTradePLFloat.SetValue(value); }
        }

        internal decimal FeeForCutting
        {
            get { return this._feeForCutting.Value; }
            set { this._feeForCutting.SetValue(value); }
        }

        internal decimal RiskCredit
        {
            get { return this._riskCredit.Value; }
            set { this._riskCredit.SetValue(value); }
        }

        internal decimal PartialPaymentPhysicalNecessary
        {
            get { return _partialPaymentPhysicalNecessary.Value; }
            set { _partialPaymentPhysicalNecessary.SetValue(value); }
        }

        internal decimal TotalPaidAmount
        {
            get { return this._totalPaidAmount.Value; }
            set { this._totalPaidAmount.SetValue(value); }
        }

        internal RiskData(BusinessRecord parent)
        {
            this._necessary = BusinessItemFactory.Create("Necessary", 0m, PermissionFeature.Sound, parent);
            this._netNecessary = BusinessItemFactory.Create("NetNecessary", 0m, PermissionFeature.Dumb, parent);
            this._hedgeNecessary = BusinessItemFactory.Create("HedgeNecessary", 0m, PermissionFeature.Dumb, parent);
            this._minEquityAvoidRiskLevel1 = BusinessItemFactory.Create("MinEquityAvoidRiskLevel1", 0m, PermissionFeature.Dumb, parent);
            this._minEquityAvoidRiskLevel2 = BusinessItemFactory.Create("MinEquityAvoidRiskLevel2", 0m, PermissionFeature.Dumb, parent);
            this._minEquityAvoidRiskLevel3 = BusinessItemFactory.Create("MinEquityAvoidRiskLevel3", 0m, PermissionFeature.Dumb, parent);

            this._necessaryFillingOpenOrder = BusinessItemFactory.Create("NecessaryFillingOpenOrder", 0m, PermissionFeature.Dumb, parent);
            this._necessaryFillingCloseOrder = BusinessItemFactory.Create("NecessaryFillingCloseOrder", 0m, PermissionFeature.Dumb, parent);

            this._tradePLFloat = BusinessItemFactory.Create("TradePLFloat", 0m, PermissionFeature.Dumb, parent);
            this._interestPLFloat = BusinessItemFactory.Create("InterestPLFloat", 0m, PermissionFeature.Dumb, parent);
            this._storagePLFloat = BusinessItemFactory.Create("StoragePLFloat", 0m, PermissionFeature.Dumb, parent);
            this._valueAsMargin = BusinessItemFactory.Create("ValueAsMargin", 0m, PermissionFeature.Dumb, parent);

            this._tradePLNotValued = BusinessItemFactory.Create("TradePLNotValued", 0m, PermissionFeature.Dumb, parent);
            this._interestPLNotValued = BusinessItemFactory.Create("InterestPLNotValued", 0m, PermissionFeature.Dumb, parent);
            this._storagePLNotValued = BusinessItemFactory.Create("StoragePLNotValued", 0m, PermissionFeature.Dumb, parent);

            this._lockOrderTradePLFloat = BusinessItemFactory.Create("LockOrderTradePLFloat", 0m, PermissionFeature.Dumb, parent);
            this._feeForCutting = BusinessItemFactory.Create("FeeForCutting", 0m, PermissionFeature.Dumb, parent);
            this._riskCredit = BusinessItemFactory.Create("RiskCredit", 0m, PermissionFeature.Dumb, parent);
            _partialPaymentPhysicalNecessary = BusinessItemFactory.Create("PartialPaymentPhysicalNecessary", 0m, PermissionFeature.Dumb, parent);
            _totalPaidAmount = BusinessItemFactory.Create("TotalPaidAmount", 0m, PermissionFeature.Dumb, parent);
        }

        internal void Reset(FundData other)
        {
            this.Necessary = other.RiskData.Necessary;
            this.NetNecessary = other.RiskData.NetNecessary;
            this.HedgeNecessary = other.RiskData.HedgeNecessary;
            this.MinEquityAvoidRiskLevel1 = other.RiskData.MinEquityAvoidRiskLevel1;
            this.MinEquityAvoidRiskLevel2 = other.RiskData.MinEquityAvoidRiskLevel2;
            this.MinEquityAvoidRiskLevel3 = other.RiskData.MinEquityAvoidRiskLevel3;
            this.NecessaryFillingOpenOrder = other.RiskData.NecessaryFillingOpenOrder;
            this.NecessaryFillingCloseOrder = other.RiskData.NecessaryFillingCloseOrder;

            this.TradePLFloat = other.RiskData.TradePLFloat;
            this.InterestPLFloat = other.RiskData.InterestPLFloat;
            this.StoragePLFloat = other.RiskData.StoragePLFloat;

            this.ValueAsMargin = other.RiskData.ValueAsMargin;

            this.TradePLNotValued = other.RiskData.TradePLNotValued;
            this.InterestPLNotValued = other.RiskData.InterestPLNotValued;
            this.StoragePLNotValued = other.RiskData.StoragePLNotValued;

            this.LockOrderTradePLFloat = other.RiskData.LockOrderTradePLFloat;
            this.FeeForCutting = other.RiskData.FeeForCutting;
            this.RiskCredit = other.RiskData.RiskCredit;
            this.PartialPaymentPhysicalNecessary = other.RiskData.PartialPaymentPhysicalNecessary;
            this.TotalPaidAmount = other.RiskData.TotalPaidAmount;
        }

        internal void Clear()
        {
            this.ClearFloatingPL();
            this.ClearNecessary();
            this.LockOrderTradePLFloat = 0m;
            this.RiskCredit = 0m;
            this.FeeForCutting = 0m;
        }


        internal void ClearNecessary()
        {
            this.ValueAsMargin = 0m;
            this.Necessary = 0m;
            this.HedgeNecessary = 0m;
            this.NetNecessary = 0m;
            this.NecessaryFillingCloseOrder = 0m;
            this.NecessaryFillingOpenOrder = 0m;
            this.MinEquityAvoidRiskLevel1 = 0m;
            this.MinEquityAvoidRiskLevel2 = 0m;
            this.MinEquityAvoidRiskLevel3 = 0m;
            this.PartialPaymentPhysicalNecessary = 0m;
            this.TotalPaidAmount = 0m;
        }

        internal void ClearFloatingPL()
        {
            this.TradePLFloat = 0m;
            this.InterestPLFloat = 0m;
            this.StoragePLFloat = 0m;
        }


        internal void Add(RiskData other, CurrencyRate currencyRate = null)
        {
            this.Necessary += this.AddByExchange(other.Necessary, currencyRate);
            this.NetNecessary += this.AddByExchange(other.NetNecessary, currencyRate);
            this.HedgeNecessary += this.AddByExchange(other.HedgeNecessary, currencyRate);
            this.MinEquityAvoidRiskLevel1 += this.AddByExchange(other.MinEquityAvoidRiskLevel1, currencyRate);
            this.MinEquityAvoidRiskLevel2 += this.AddByExchange(other.MinEquityAvoidRiskLevel2, currencyRate);
            this.MinEquityAvoidRiskLevel3 += this.AddByExchange(other.MinEquityAvoidRiskLevel3, currencyRate);
            this.NecessaryFillingOpenOrder += this.AddByExchange(other.NecessaryFillingOpenOrder, currencyRate);
            this.NecessaryFillingCloseOrder += this.AddByExchange(other.NecessaryFillingCloseOrder, currencyRate);
            this.TradePLFloat += this.AddByExchange(other.TradePLFloat, currencyRate);
            this.InterestPLFloat += this.AddByExchange(other.InterestPLFloat, currencyRate);
            this.StoragePLFloat += this.AddByExchange(other.StoragePLFloat, currencyRate);

            this.ValueAsMargin += this.AddByExchange(other.ValueAsMargin, currencyRate);

            this.TradePLNotValued += this.AddByExchange(other.TradePLNotValued, currencyRate);
            this.InterestPLNotValued += this.AddByExchange(other.InterestPLNotValued, currencyRate);
            this.StoragePLNotValued += this.AddByExchange(other.StoragePLNotValued, currencyRate);

            this.LockOrderTradePLFloat += this.AddByExchange(other.LockOrderTradePLFloat, currencyRate);
            this.FeeForCutting += this.AddByExchange(other.FeeForCutting, currencyRate);
            this.RiskCredit += this.AddByExchange(other.RiskCredit, currencyRate);
            this.PartialPaymentPhysicalNecessary += this.AddByExchange(other.PartialPaymentPhysicalNecessary, currencyRate);
            this.TotalPaidAmount += this.AddByExchange(other.TotalPaidAmount, currencyRate);
        }

        private decimal AddByExchange(decimal value, CurrencyRate currencyRate)
        {
            return currencyRate.AddByExchange(value);
        }
    }
}