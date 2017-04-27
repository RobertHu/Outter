using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Periphery.Facades;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.Delivery
{
    internal static class DeliveryManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DeliveryManager));

        internal static bool CancelDelivery(Guid deliveryRequestId, Account account, out int status)
        {
            Logger.InfoFormat("CancelDelivery deliveryRequestId = {0} accountId = {1}", deliveryRequestId, account.Id);

            status = (int)DeliveryRequestStatus.Cancelled;
            try
            {
                bool result = false;
                DeliveryRequest deliveryRequest = DeliveryRequestManager.Default[deliveryRequestId];
                bool hasFilledShortSellOrders = account.HasFilledShortSellOrders(deliveryRequest.InstrumentId);
                if (hasFilledShortSellOrders)
                {
                    result = CancelDeliveryWithShortSellOrders(deliveryRequest);
                }
                else
                {
                    result = deliveryRequest.Cancel();
                }

                status = (int)deliveryRequest.DeliveryRequestStatus;
                if (result)
                {
                    if (!hasFilledShortSellOrders)
                    {
                        foreach (DeliveryRequestOrderRelation deliveryRequestOrderRelation in deliveryRequest.DeliveryRequestOrderRelations)
                        {
                            Physical.PhysicalOrder order = (Physical.PhysicalOrder)account.GetOrder(deliveryRequestOrderRelation.OpenOrderId);
                            order.LockForDelivery(-deliveryRequestOrderRelation.DeliveryLot);
                        }

                        if (deliveryRequest.Charge != 0)
                        {
                            account.AddBalance(deliveryRequest.ChargeCurrencyId.Value, deliveryRequest.Charge, null);
                            Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateUpdateBalanceCommand(account.Id, deliveryRequest.ChargeCurrencyId.Value, deliveryRequest.Charge, Protocal.ModifyType.Add));
                        }
                    }

                    DeliveryRequestManager.Default.Remove(deliveryRequest);
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat(deliveryRequestId.ToString() + " failed:\r\n" + e.ToString());
                return false;
            }
        }

        private static bool CancelDeliveryWithShortSellOrders(DeliveryRequest deliveryRequest)
        {
            List<Transaction> executedTransactions = new List<Transaction>();
            List<Transaction> transactions = new List<Transaction>();

            Account account = TradingSetting.Default.GetAccount(deliveryRequest.AccountId);
            var instrument = Settings.Setting.Default.GetInstrument(deliveryRequest.InstrumentId);
            var tradePolicyDetail = Settings.Setting.Default.GetTradePolicy(account.Setting().TradePolicyId)[instrument.Id, null];
            TransactionError error = TransactionError.OK;

            foreach (DeliveryRequestOrderRelation relation in deliveryRequest.DeliveryRequestOrderRelations)
            {
                Order openOrder = account.GetOrder(relation.OpenOrderId);
                transactions.Add(account.CreateCloseTranAndOrder(openOrder, relation, deliveryRequest));

                transactions.Add(account.CreateOpenTranAndOrder(openOrder, relation, deliveryRequest));
            }

            foreach (var eachTran in transactions)
            {
                eachTran.ExecuteDirectly(new Engine.ExecuteContext(account.Id, eachTran.Id, Engine.ExecuteStatus.Filled, eachTran.CreateOrderPriceInfo()) { IsFreeFee = true, IsFreeValidation = true });
            }

            return error == TransactionError.OK;
        }

        private static Transaction CreateOpenTranAndOrder(this Account account, Order openOrder, DeliveryRequestOrderRelation relation, DeliveryRequest deliveryRequest)
        {
            Transaction transaction = TransactionFacade.CreateCancelDeliveryWithShortSellTran(account, deliveryRequest.InstrumentId, openOrder.Owner.ContractSize(null));
            CancelDeliveryWithShortSellOrderParam param = new CancelDeliveryWithShortSellOrderParam
            {
                IsBuy = true,
                IsOpen = true,
                SetPrice = openOrder.SetPrice,
                ExecutePrice = openOrder.ExecutePrice,
                Lot = relation.DeliveryLot,
                LotBalance = relation.DeliveryLot,
                PhysicalRequestId = deliveryRequest.Id,
                TradeOption = openOrder.TradeOption
            };
            Order order = OrderFacade.Default.CreateCancelDeliveryWithShortSellOrder(transaction, param);
            return transaction;
        }



        private static Transaction CreateCloseTranAndOrder(this Account account, Order openOrder, DeliveryRequestOrderRelation relation, DeliveryRequest deliveryRequest)
        {
            Transaction transaction = TransactionFacade.CreateCancelDeliveryWithShortSellTran(account, deliveryRequest.InstrumentId, openOrder.Owner.ContractSize(null));
            var orderRelationRecord = new OrderRelationRecord(account.GetOrder(relation.OpenOrderId), relation.DeliveryLot);
            CancelDeliveryWithShortSellOrderParam param = new CancelDeliveryWithShortSellOrderParam
            {
                IsBuy = false,
                IsOpen = false,
                SetPrice = openOrder.SetPrice,
                ExecutePrice = openOrder.ExecutePrice,
                Lot = relation.DeliveryLot,
                LotBalance = 0m,
                PhysicalRequestId = deliveryRequest.Id,
                TradeOption = iExchange.Common.TradeOption.Invalid,
                OrderRelations = new List<OrderRelationRecord>() { orderRelationRecord }
            };
            Order order = OrderFacade.Default.CreateCancelDeliveryWithShortSellOrder(transaction, param);
            return transaction;
        }


    }
}
