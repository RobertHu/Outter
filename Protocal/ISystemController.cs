using iExchange.Common;
using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace Protocal
{
    [ServiceContract]
    public interface ISystemController
    {
        [OperationContract]
        TransactionError Place(TransactionData tranData);

        [OperationContract(Name = "PlaceAndGetTranCode")]
        TransactionError Place(TransactionData tranData, out string tranCode);

        [OperationContract]
        TransactionError Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason);

        [OperationContract]
        TransactionError Delete(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest);

        [OperationContract]
        string GetInitializeData(List<Guid> accountIds);

        [OperationContract]
        TransactionError AcceptPlace(Guid accountID, Guid tranID);

        [OperationContract]
        TransactionError ApplyDelivery(DeliveryRequestData data, out string code, out string balance, out string usableMargin);

        [OperationContract]
        bool CancelDelivery(Guid userId, Guid deliveryRequestId, string title, string notifyMessage, out Guid accountId, out int status);

        [OperationContract]
        bool NotifyDelivery(Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage, out Guid accountId);

        [OperationContract]
        List<Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId);

        [OperationContract]
        TransactionError PrePayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, TerminateData terminateData);

        [OperationContract]
        TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<InstalmentData> instalments);

        [OperationContract]
        List<OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType);

        [OperationContract]
        void Update(AppType appType, string update);

        [OperationContract]
        TransactionError Book(Token token, TransactionBookData tranData, bool preserveCalculation);

        [OperationContract]
        TransactionError Execute(Guid accountId, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID);

        [OperationContract]
        [XmlSerializerFormat]
        bool SetPriceAlerts(Guid submitorId, XmlNode priceAlertsNode);

        [OperationContract]
        TransactionError ApplyTransfer(Guid userId, Guid sourceAccountID, Guid sourceCurrencyID,
            decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
            decimal rate, DateTime expireDate);

        [OperationContract]
        TransactionError AcceptTransfer(Guid userId, Guid transferID);

        [OperationContract]
        TransactionError DeclineTransfer(Guid userId, Guid transferID);

        [OperationContract]
        AlertLevel[] ResetAlertLevel(Guid[] accountIDs);

        [OperationContract]
        void ResetHit(Dictionary<Guid, List<Guid>> accountPerOrders);

        [OperationContract]
        TransactionError Assign(TransactionData tranData);

        [OperationContract]
        TransactionError MultipleClose(Guid accountId, Guid[] orderIds);

        [OperationContract]
        bool Deposit(DataTypes.PayRecord deposit, out bool canResetAlertLevel);

        [OperationContract]
        bool Withdraw(DataTypes.PayRecord deposit);

        [OperationContract]
        bool ClearDeposit(Guid clearedDepositId, bool successed);

        [OperationContract]
        bool ChangeLeverage(Guid accountId, int leverage, out decimal necessary);

        [OperationContract]
        void GetAccountInstrumentPrice(Guid accountId, Guid instrumentId, out string buyPrice, out string sellPrice);

        [OperationContract]
        void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage);

        [OperationContract]
        bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId);

        [OperationContract]
        void QuoteAnswer(string anwser);

        [OperationContract]
        void SetNews(List<Protocal.Commands.Notify.News> news);

        [OperationContract]
        void Chat(string content);

        [OperationContract]
        AccountFloatingStatus GetAccountFloatingStatus(Guid accountId);

        [OperationContract]
        iExchange.Common.TransactionError DeleteByCancelDelivery(Guid accountId, Guid orderId, Guid deliveryRequestId, bool isPayForInstalmentDebitInterest);

        [OperationContract]
        Guid[] Rehit(Guid[] orderIds, Guid[] accountIds);

        [OperationContract]
        string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit);
    }


    [DataContract]
    public sealed class AccountFloatingStatus
    {
        public AccountFloatingStatus()
        {
            this.FundStatus = new List<FundStatus>();
            this.OrderStatus = new List<OrderFloatingStatus>();
        }
        [DataMember]
        public decimal FloatingPL { get; set; }
        [DataMember]
        public decimal Equity { get; set; }
        [DataMember]
        public decimal Necessary { get; set; }

        [DataMember]
        public List<FundStatus> FundStatus { get; set; }

        [DataMember]
        public List<OrderFloatingStatus> OrderStatus { get; set; }

    }

    [DataContract]
    public sealed class OrderFloatingStatus
    {
        [DataMember]
        public Guid ID { get; set; }
        [DataMember]
        public string LivePrice { get; set; }
        [DataMember]
        public decimal FloatingPL { get; set; }
        [DataMember]
        public decimal Necessary { get; set; }
    }

    [DataContract]
    public sealed class FundStatus
    {
        [DataMember]
        public Guid CurrencyId { get; set; }
        [DataMember]
        public decimal Equity { get; set; }
        [DataMember]
        public decimal FloatingPL { get; set; }
        [DataMember]
        public decimal Necessary { get; set; }
    }

}
