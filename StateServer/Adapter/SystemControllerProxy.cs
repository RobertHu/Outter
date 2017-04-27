using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using iExchange.Common;

namespace iExchange.StateServer.Adapter
{
    public sealed class SystemControllerProxy : Protocal.Communication.HttpCommunicationService<Protocal.ISystemController>, Protocal.ISystemController
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(SystemControllerProxy));

        public SystemControllerProxy(string serviceUrl)
            : base(serviceUrl) { }

        public Common.TransactionError AcceptPlace(Guid accountID, Guid tranID)
        {
            return this.Call<TransactionError>(() => this.Service.AcceptPlace(accountID, tranID));
        }

        public Common.TransactionError AcceptTransfer(Guid userId, Guid transferID)
        {
            return this.Call<TransactionError>(() => this.Service.AcceptTransfer(userId, transferID));
        }

        public Common.TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData data, out string code, out string balance, out string usableMargin)
        {
            code = string.Empty;
            balance = string.Empty;
            usableMargin = string.Empty;
            try
            {
                return this.Service.ApplyDelivery(data, out code, out balance, out  usableMargin);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return TransactionError.RuntimeError;
            }
        }

        public Common.TransactionError ApplyTransfer(Guid userId, Guid sourceAccountID, Guid sourceCurrencyID, decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount, decimal rate, DateTime expireDate)
        {
            return this.Call<TransactionError>(() => this.Service.ApplyTransfer(userId, sourceAccountID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate));
        }

        public Common.TransactionError Assign(Protocal.TransactionData tranData)
        {
            return this.Call<TransactionError>(() => this.Service.Assign(tranData));
        }

        public Common.TransactionError Book(Common.Token token, Protocal.TransactionBookData tranData, bool preserveCalculation)
        {
            return this.Call<TransactionError>(() => this.Service.Book(token, tranData, preserveCalculation));
        }

        public Common.TransactionError Cancel(Token token, Guid accountId, Guid tranId, Common.CancelReason cancelReason)
        {
            return this.Call<TransactionError>(() => this.Service.Cancel(token, accountId, tranId, cancelReason));
        }

        public bool CancelDelivery(Guid userId, Guid deliveryRequestId, string title, string message, out Guid accountId, out int status)
        {
            status = (int)DeliveryRequestStatus.Cancelled;
            accountId = Guid.Empty;
            try
            {
                return this.Service.CancelDelivery(userId, deliveryRequestId, title, message, out accountId, out status);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return false;
            }
        }

        public bool ChangeLeverage(Guid accountId, int leverage, out decimal necessary)
        {
            necessary = 0m;
            try
            {
                return this.Service.ChangeLeverage(accountId, leverage, out necessary);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return false;
            }
        }

        public bool ClearDeposit(Guid clearedDepositId, bool successed)
        {
            return this.Call<bool>(() => this.Service.ClearDeposit(clearedDepositId, successed));
        }

        public Common.TransactionError DeclineTransfer(Guid userId, Guid transferID)
        {
            return this.Call<TransactionError>(() => this.Service.DeclineTransfer(userId, transferID));
        }

        public Common.TransactionError Delete(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return this.Call<TransactionError>(() => this.Service.Delete(accountId, orderId, isPayForInstalmentDebitInterest));
        }

        public bool Deposit(Protocal.DataTypes.PayRecord deposit, out bool canResetAlertLevel)
        {
            throw new NotImplementedException();
        }

        public Common.TransactionError Execute(Guid accountId, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            return this.Call<TransactionError>(() => this.Service.Execute(accountId, tranID, buyPrice, sellPrice, lot, executedOrderID));
        }

        public string GetInitializeData(List<Guid> accountIds)
        {
            return this.Call<string>(() => this.Service.GetInitializeData(accountIds));
        }

        public List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId)
        {
            return this.Call<List<Protocal.Physical.OrderInstalmentData>>(() => this.Service.GetOrderInstalments(orderId));
        }

        public Common.TransactionError MultipleClose(Guid accountId, Guid[] orderIds)
        {
            return this.Call<TransactionError>(() => this.Service.MultipleClose(accountId, orderIds));
        }

        public bool NotifyDelivery(Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage, out Guid accountId)
        {
            accountId = Guid.Empty;
            try
            {
                return this.Service.NotifyDelivery(deliveryRequestId, availableDeliveryTime, title, notifyMessage, out accountId);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return false;
            }
        }

        public Common.TransactionError Place(Protocal.TransactionData tranData, out string tranCode)
        {
            tranCode = string.Empty;
            try
            {
                return this.Service.Place(tranData, out tranCode);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return TransactionError.RuntimeError;
            }
        }

        public void GetAccountInstrumentPrice(Guid accountId, Guid instrumentId, out string buyPrice, out string sellPrice)
        {
            buyPrice = string.Empty;
            sellPrice = string.Empty;
            try
            {
                this.Service.GetAccountInstrumentPrice(accountId, instrumentId, out buyPrice, out sellPrice);
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
            }
        }

        public Common.TransactionError Place(Protocal.TransactionData tranData)
        {
            return this.Call<TransactionError>(() => this.Service.Place(tranData));
        }

        public List<Protocal.OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return this.Call<List<Protocal.OrderQueryData>>(() => this.Service.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType));
        }

        public Common.AlertLevel[] ResetAlertLevel(Guid[] accountIDs)
        {
            return this.Call<AlertLevel[]>(() => this.Service.ResetAlertLevel(accountIDs));
        }

        public void ResetHit(Dictionary<Guid, List<Guid>> accountPerOrders)
        {
            this.Call(() => this.Service.ResetHit(accountPerOrders));
        }

        public bool SetPriceAlerts(Guid submitorId, System.Xml.XmlNode priceAlertsNode)
        {
            return this.Call<bool>(() => this.Service.SetPriceAlerts(submitorId, priceAlertsNode));
        }

        public void Update(Common.AppType appType, string update)
        {
            this.Call(() => this.Service.Update(appType, update));
        }

        public bool Withdraw(Protocal.DataTypes.PayRecord deposit)
        {
            throw new NotImplementedException();
        }

        protected override log4net.ILog Logger
        {
            get { return _Logger; }
        }


        public void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            this.Call(() => this.Service.NotifyDeliveryApproved(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime, title, notifyMessage));
        }


        public bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId)
        {
            return this.Call(() => this.Service.NotifyDeliveried(accountId, deliveryRequestId));
        }


        public TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<Protocal.Physical.InstalmentData> instalments)
        {
            return this.Call<TransactionError>(() => this.Service.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalments));
        }

        public TransactionError PrePayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return this.Call<TransactionError>(() => this.Service.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData));
        }


        public void QuoteAnswer(string anwser)
        {
            this.Call(() => this.Service.QuoteAnswer(anwser));
        }


        public void Chat(string content)
        {
            this.Call(() => this.Service.Chat(content));
        }

        public void SetNews(List<Protocal.Commands.Notify.News> news)
        {
            this.Call(() => this.Service.SetNews(news));
        }


        public Protocal.AccountFloatingStatus GetAccountFloatingStatus(Guid accountId)
        {
            return this.Call(() => this.Service.GetAccountFloatingStatus(accountId));
        }


        public TransactionError DeleteByCancelDelivery(Guid accountId, Guid orderId, Guid deliveryRequestId, bool isPayForInstalmentDebitInterest)
        {
            return this.Call(() => this.Service.DeleteByCancelDelivery(accountId, orderId, deliveryRequestId, isPayForInstalmentDebitInterest));
        }


        public Guid[] Rehit(Guid[] orderIds, Guid[] accountIds)
        {
            return this.Call(() => this.Service.Rehit(orderIds, accountIds));
        }


        public string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            return this.Call(() => this.Service.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit));
        }
    }

    public sealed class QuotationServiceProxy : Protocal.Communication.CommunicationServiceByEndPointName<Protocal.IQuotationService>, Protocal.IQuotationService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(QuotationServiceProxy));

        internal QuotationServiceProxy(string url, string endpointName)
            : base(url, endpointName)
        {
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        public void SetQuotation(Protocal.OriginQ[] originQs, Protocal.OverridedQ[] overridedQs)
        {
            this.Call(() => this.Service.SetQuotation(originQs, overridedQs));
        }
    }

}