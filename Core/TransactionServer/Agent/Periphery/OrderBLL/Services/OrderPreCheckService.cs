using Core.TransactionServer.Agent.BLL.PreCheck;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Services
{
    internal abstract class OrderPreCheckServiceBase
    {
        protected Order _order;

        protected OrderPreCheckServiceBase(Order order)
        {
            _order = order;
        }

        internal abstract decimal CalculatePrecheckBalance();

        internal bool ShouldCalculateFilledMarginAndQuantity(bool isBuy)
        {
            return _order.IsBuy != isBuy && _order.Phase == OrderPhase.Executed && _order.IsOpen && _order.LotBalance > 0 && _order.IsRisky;
        }

        internal MarginAndQuantityResult CalculateUnfilledMarginAndQuantityForOrder(Price price, decimal? effectiveLot)
        {
            decimal lot = effectiveLot ?? _order.Lot;
            if (_order.Phase == OrderPhase.Placed || _order.Phase == OrderPhase.Placing)
            {
                decimal necessary = _order.CalculatePreCheckNecessary(price, lot);
                decimal quantity = this.CalculateQuantity(lot);
                return this.CreateMarginAndQuantityResult(_order.IsBuy, necessary, quantity);
            }
            return new MarginAndQuantityResult();
        }


        internal decimal CalculatePreCheckNecessary(Price preCheckPrice, decimal? effectiveLot)
        {
            decimal result = 0m;
            if (preCheckPrice != null)
            {
                var lot = effectiveLot.HasValue ? effectiveLot.Value : _order.Lot;
                result = NecessaryCalculator.CalculateNecessary(_order, preCheckPrice, lot, null);
            }
            else
            {
                var price = _order.SetPrice == null ? _order.ExecutePrice : _order.SetPrice;
                result = NecessaryCalculator.CalculateNecessary(_order, price, _order.Lot, null);
            }
            return result;
        }

        internal MarginAndQuantityResult CalculateFilledMarginAndQuantity(bool isBuy, Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            decimal remainLot = this.GetRemainLot(remainFilledLotPerOrderDict);
            decimal margin = this.CalculateMargin(remainLot);
            decimal quantity = this.CalculateQuantity(remainLot);
            return this.CreateMarginAndQuantityResult(_order.IsBuy, margin, quantity);
        }

        private decimal GetRemainLot(Dictionary<Guid, decimal> remainFilledLotPerOrderDict)
        {
            decimal remainLot = 0m;
            if (remainFilledLotPerOrderDict == null || !remainFilledLotPerOrderDict.TryGetValue(_order.Id, out remainLot))
            {
                remainLot = _order.LotBalance;
            }
            return remainLot;
        }

        protected virtual MarginAndQuantityResult CreateMarginAndQuantityResult(bool isBuy, decimal margin, decimal quantity)
        {
            var result = new MarginAndQuantityResult();
            result.AddMarginAndQuantity(isBuy, margin, quantity);
            return result;
        }

        private decimal CalculateMargin(decimal remainLot)
        {
            return remainLot > 0 ? NecessaryCalculator.CalculateNecessary(_order, null, remainLot, null) : _order.Necessary;
        }

        protected virtual decimal CalculateQuantity(decimal remainLot)
        {
            decimal lot = remainLot > 0 ? remainLot : _order.LotBalance;
            return lot * _order.Owner.ContractSize(null);
        }

    }


    internal sealed class OrderPreCheckService : OrderPreCheckServiceBase
    {
        internal OrderPreCheckService(Order order)
            : base(order)
        {
        }

        internal override decimal CalculatePrecheckBalance()
        {
            return 0m;
        }

    }


    internal sealed class PhysicalOrderPreCheckService : OrderPreCheckServiceBase
    {
        internal PhysicalOrderPreCheckService(Physical.PhysicalOrder order)
            : base(order)
        {
        }

        internal override decimal CalculatePrecheckBalance()
        {
            var physicalOrder = (Physical.PhysicalOrder)_order;
            decimal result = 0m;
            if (physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Buy && _order.IsOpen && (_order.Phase == OrderPhase.Placed || _order.Phase == OrderPhase.Placing))
            {
                var quotation = _order.Owner.AccountInstrument.GetQuotation();
                var price = _order.SetPrice == null ? quotation.BuyPrice : _order.SetPrice;
                decimal marketValue = MarketValueCalculator.CalculateValue(_order.Owner.SettingInstrument().TradePLFormula, _order.Lot, price, _order.Owner.TradePolicyDetail().DiscountOfOdd, _order.Owner.TradePolicyDetail().ContractSize);
                if (physicalOrder.IsInstalment)
                {
                    decimal instalmentAdministrationFee = physicalOrder.CalculateInstalmentAdministrationFee(marketValue);
                    decimal downPayment = physicalOrder.CalculatePaidAmountForPledge(marketValue);
                    result = instalmentAdministrationFee + downPayment;
                }
                else
                {
                    result = marketValue;
                }
            }
            return result;
        }

        protected override MarginAndQuantityResult CreateMarginAndQuantityResult(bool isBuy, decimal margin, decimal quantity)
        {
            var result = new MarginAndQuantityResult();
            result.AddMarginAndQuantity(isBuy, this.IsPartialPhysicalOrder(), margin, quantity);
            return result;
        }

        protected override decimal CalculateQuantity(decimal remainLot)
        {
            return remainLot > 0 ? (this.IsPartialPhysicalOrder() ? remainLot : remainLot * _order.Owner.ContractSize(null)) :
                (this.IsPartialPhysicalOrder() ? _order.LotBalance : _order.LotBalance * _order.Owner.ContractSize(null));
        }

        private bool IsPartialPhysicalOrder()
        {
            return ((Physical.PhysicalOrder)_order).IsPartialPaymentPhysicalOrder;
        }

    }

    internal sealed class BOOrderPreCheckService : OrderPreCheckServiceBase
    {
        internal BOOrderPreCheckService(BinaryOption.Order order)
            : base(order)
        {
        }

        internal override decimal CalculatePrecheckBalance()
        {
            if (_order.IsOpen && (_order.Phase == OrderPhase.Placed || _order.Phase == OrderPhase.Placing))
            {
                return _order.Lot;
            }
            else
            {
                return 0m;
            }
        }
    }




}
