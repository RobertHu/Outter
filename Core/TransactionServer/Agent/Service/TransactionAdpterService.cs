using Core.TransactionServer.Agent.DB;
using iExchange.Common;
using Protocal;
using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class TransactionAdapterService : ISystemController
    {
        TransactionError ISystemController.Place(TransactionData tranData)
        {
            return this.Server.Place(tranData);
        }

        TransactionError ISystemController.Place(TransactionData tranData, out string tranCode)
        {
            return this.Server.Place(tranData, out tranCode);
        }

        TransactionError ISystemController.Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason)
        {
            return this.Server.Cancel(token, accountId, tranId, cancelReason);
        }

        TransactionError ISystemController.Delete(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return this.Server.DeleteOrder(accountId, orderId, isPayForInstalmentDebitInterest, null);
        }

        string ISystemController.GetInitializeData(List<Guid> accountIds)
        {
            return this.Server.GetInitializeData(accountIds);
        }

        TransactionError ISystemController.ApplyDelivery(DeliveryRequestData data, out string code, out string balance, out string usableMargin)
        {
            return this.Server.ApplyDelivery(data, out code, out balance, out usableMargin);
        }

        bool ISystemController.CancelDelivery(Guid userId, Guid deliveryRequestId, string title, string notifyMessage, out Guid accountId, out int status)
        {
            return this.Server.CancelDelivery(userId, deliveryRequestId, out accountId, out status);
        }

        bool ISystemController.NotifyDelivery(Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage, out Guid accountId)
        {
            return this.Server.NotifyDelivery(deliveryRequestId, availableDeliveryTime, title, notifyMessage, out accountId);
        }

        List<OrderInstalmentData> ISystemController.GetOrderInstalments(Guid orderId)
        {
            return this.Server.GetOrderInstalments(orderId);
        }

        TransactionError ISystemController.PrePayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, TerminateData terminateData)
        {
            return this.Server.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData);
        }

        List<OrderQueryData> ISystemController.QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return DBRepository.Default.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType);
        }

        void ISystemController.Update(AppType appType, string update)
        {
            throw new NotImplementedException();
        }

        TransactionError ISystemController.Book(Token token, TransactionBookData tranData, bool preserveCalculation)
        {
            return this.Server.Book(token, tranData, preserveCalculation);
        }

        TransactionError ISystemController.Execute(Guid accountId, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            return this.Server.Execute(accountId, tranID, buyPrice, sellPrice, lot, executedOrderID);
        }

        bool ISystemController.SetPriceAlerts(Guid submitorId, System.Xml.XmlNode priceAlertsNode)
        {
            return this.Server.SetPriceAlerts(submitorId, priceAlertsNode);
        }

        TransactionError ISystemController.ApplyTransfer(Guid userId, Guid sourceAccountID, Guid sourceCurrencyID, decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount, decimal rate, DateTime expireDate)
        {
            return this.Server.ApplyTransfer(userId, sourceAccountID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
        }

        TransactionError ISystemController.AcceptTransfer(Guid userId, Guid transferID)
        {
            return this.Server.AcceptTransfer(userId, transferID);
        }

        TransactionError ISystemController.DeclineTransfer(Guid userId, Guid transferID)
        {
            return this.Server.DeclineTransfer(userId, transferID);
        }

        AlertLevel[] ISystemController.ResetAlertLevel(Guid[] accountIDs)
        {
            return this.Server.ResetAlertLevel(accountIDs);
        }

        void ISystemController.ResetHit(Dictionary<Guid, List<Guid>> accountPerOrders)
        {
            this.Server.ResetHit(accountPerOrders);
        }

        TransactionError ISystemController.Assign(TransactionData tranData)
        {
            throw new NotImplementedException();
        }

        TransactionError ISystemController.MultipleClose(Guid accountId, Guid[] orderIds)
        {
            return this.Server.MultipleClose(accountId, orderIds);
        }

        private Server Server { get { return ServerFacade.Default.Server; } }


        TransactionError ISystemController.AcceptPlace(Guid accountID, Guid tranID)
        {
            return this.Server.AcceptPlace(accountID, tranID);
        }

        bool ISystemController.ChangeLeverage(Guid accountId, int leverage, out decimal necessary)
        {
            return this.Server.ChangeLeverage(accountId, leverage, out necessary);
        }


        bool ISystemController.Deposit(Protocal.DataTypes.PayRecord deposit, out bool canResetAlertLevel)
        {
            throw new NotImplementedException();
        }

        bool ISystemController.Withdraw(Protocal.DataTypes.PayRecord deposit)
        {
            throw new NotImplementedException();
        }

        bool ISystemController.ClearDeposit(Guid clearedDepositId, bool successed)
        {
            throw new NotImplementedException();
        }

        public void GetAccountInstrumentPrice(Guid accountId, Guid instrumentId, out string buyPrice, out string sellPrice)
        {
            this.Server.GetAccountInstrumentPrice(accountId, instrumentId, out buyPrice, out sellPrice);
        }


        public void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            this.Server.NotifyDeliveryApproved(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime);
        }


        public bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId)
        {
            return this.Server.NotifyDeliveried(accountId, deliveryRequestId);
        }

        public TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<InstalmentData> instalments)
        {
            return this.Server.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalments);
        }


        public void QuoteAnswer(string anwser)
        {
            throw new NotImplementedException();
        }


        public void SetNews(List<News> news)
        {
            throw new NotImplementedException();
        }


        public void SetNews(List<Protocal.Commands.Notify.News> news)
        {
            throw new NotImplementedException();
        }

        public void Chat(string content)
        {
            throw new NotImplementedException();
        }


        public AccountFloatingStatus GetAccountFloatingStatus(Guid accountId)
        {
            return this.Server.GetAccountFloatingStatus(accountId);
        }


        public TransactionError DeleteByCancelDelivery(Guid accountId, Guid orderId, Guid deliveryRequestId, bool isPayForInstalmentDebitInterest)
        {
            return this.Server.DeleteOrder(accountId, orderId, isPayForInstalmentDebitInterest, deliveryRequestId);
        }


        public Guid[] Rehit(Guid[] orderIds, Guid[] accountIds)
        {
            return this.Server.Rehit(orderIds, accountIds);
        }


        public string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            return this.Server.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
        }

        public string GetAllAccountsInitData()
        {
            return this.Server.GetAllAccountsInitData();
        }
    }
}
