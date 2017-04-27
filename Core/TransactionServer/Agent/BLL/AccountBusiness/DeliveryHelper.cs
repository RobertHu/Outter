using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class DeliveryHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeliveryHelper));

        internal static void ApplyDelivery(Account account, Protocal.Physical.DeliveryRequestData requestData)
        {
            var instrument = Settings.Setting.Default.GetInstrument(requestData.InstrumentId);
            if (string.IsNullOrEmpty(instrument.DeliveryPrice)) throw new TransactionServerException(TransactionError.InvalidPrice);
            Verify(account, requestData);
            DeliveryRequest request = DeliveryRequestManager.Default.Create(account, requestData);
            request.InitPrice(instrument.DeliveryPrice);
            foreach (var eachRelation in requestData.OrderRelations)
            {
                var order = (PhysicalOrder)account.GetOrder(eachRelation.OpenOrderId);
                order.LockForDelivery(eachRelation.DeliveryLot);
            }
            var tradingInstrument = account.GetOrCreateInstrument(requestData.InstrumentId);
            request.DeliveryRequestStatus = DeliveryRequestStatus.Accepted;
            DeliveryRequestManager.Default.Add(request);
        }

        private static void Verify(Account account, Protocal.Physical.DeliveryRequestData requestData)
        {
            if (requestData.OrderRelations == null || requestData.OrderRelations.Count == 0)
            {
                throw new TransactionServerException(TransactionError.InvalidOrderRelation, "Order relation is null or count = 0");
            }

            if ((account.Setting().TradePolicy()[requestData.InstrumentId, null].AllowedPhysicalTradeSides & PhysicalTradeSide.Delivery) != PhysicalTradeSide.Delivery)
            {
                throw new TransactionServerException(TransactionError.OrderTypeIsNotAcceptable);
            }
            VerifyDeliveryLot(requestData);
            foreach (var eachOrderRelation in requestData.OrderRelations)
            {
                var order = (PhysicalOrder)account.GetOrder(eachOrderRelation.OpenOrderId);
                if (order == null || !order.IsBuy || !order.IsOpen
                    || (order.PhysicalTradeSide != PhysicalTradeSide.Buy && order.PhysicalTradeSide != PhysicalTradeSide.Deposit)
                    || order.Instrument().Id != requestData.InstrumentId
                    || (order.IsInstalment && !order.IsPayoff))
                {
                    throw new TransactionServerException(TransactionError.InvalidRelation);
                }

                Logger.InfoFormat("order id = {0}, lotBalance = {1}, lockLot = {2}, deliveryLot = {3}", order.Id, order.LotBalance, order.DeliveryLockLot, eachOrderRelation.DeliveryLot);

                if (order.LotBalance - order.DeliveryLockLot < eachOrderRelation.DeliveryLot)
                {
                    throw new TransactionServerException(TransactionError.ExceedOpenLotBalance);
                }
            }
            var tradingInstrument = account.GetOrCreateInstrument(requestData.InstrumentId);
            if (!account.HasEnoughMoneyToDelivery(tradingInstrument))
            {
                throw new TransactionServerException(TransactionError.MarginIsNotEnough);
            }

            if (requestData.Charge > 0)
            {
                account.AddBalance(requestData.ChargeCurrencyId, -requestData.Charge, null);
                decimal usableMargin = account.SumFund.Equity - account.SumFund.Necessary;
                if (account.SumFund.Balance < 0 || usableMargin < 0)
                {
                    throw new TransactionServerException(TransactionError.BalanceOrEquityIsShort);
                }
            }
        }

        private static void VerifyDeliveryLot(Protocal.Physical.DeliveryRequestData requestData)
        {
            decimal totalDeliveryLot = 0m;
            foreach (var eachRelation in requestData.OrderRelations)
            {
                totalDeliveryLot += eachRelation.DeliveryLot;
            }
            if (totalDeliveryLot != requestData.RequireLot)
            {
                string errorInfo = string.Format("totalDeliveryLotInRelation = {0}, requireLot = {1}, deliveryId = {2}",
                    totalDeliveryLot, requestData.RequireLot, requestData.Id);
                throw new TransactionServerException(TransactionError.InvalidRelation, errorInfo);
            }
        }


    }
}
