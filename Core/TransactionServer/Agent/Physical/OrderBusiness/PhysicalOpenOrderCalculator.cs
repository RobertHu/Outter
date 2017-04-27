using Core.TransactionServer.Agent.BLL.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.OrderBusiness.Calculator;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalOpenOrderCalculator : OpenOrderCalculatorBase
    {
        private PhysicalOrder _physicalOrder;
        private PhysicalOrderSettings _physicalSettings;
        internal PhysicalOpenOrderCalculator(PhysicalOrder order, PhysicalOrderSettings physicalSettings, OpenOrderServiceFactoryBase openOrderServiceFactory)
            : base(order, physicalSettings, openOrderServiceFactory)
        {
            _physicalOrder = order;
            _physicalSettings = physicalSettings;
        }

        internal void UpdateLotBalanceForDelivery(decimal lot)
        {
            if (!_physicalOrder.IsExecuted)
            {
                throw new ApplicationException(string.Format("Invalid Phase: Physical Order = {0} of {1}, deltaLotBalance = {2}", _physicalOrder.Id, _physicalOrder.Owner, lot));
            }
            if (_physicalOrder.DeliveryLockLot < lot)
            {
                throw new TransactionServerException(TransactionError.ExceedOpenLotBalance, string.Format("Physical Order = {0} of {1}, deltaLotBalance = {2}", _physicalOrder.Id, _physicalOrder.Owner, lot));
            }
            _physicalSettings.DeliveryModel.DeliveryLockLot -= lot;
        }


        public override decimal CanBeClosedLot
        {
            get
            {
                if (_physicalOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery)
                {
                    return _physicalOrder.DeliveryLockLot + _physicalOrder.LotBalance;
                }
                return _physicalOrder.LotBalance;
            }
        }
    }
}
