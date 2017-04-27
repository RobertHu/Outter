using iExchange.Common;
using Protocal.Physical;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace Protocal.Test
{
    [ServiceContract]
    public interface IServerService
    {
        [OperationContract]
        TransactionError Place(Protocal.TransactionData tranData);

        [OperationContract]
        void PlaceByModel(Protocal.TransactionData tranData);

        [OperationContract]
        string Test();

        [OperationContract]
        string GetInitData(List<Guid> accountIds);

        [OperationContract]
        List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId);

        [OperationContract]
        void DoAccountSystemReset(Guid accountId, DateTime tradeDay);

        [OperationContract]
        [XmlSerializerFormat]
        void Update(AppType appType, XElement updateNode);

        [OperationContract]
        Protocal.TradingInstrument.InstrumentStatus GetInstrumentTradingStatus(Guid instrumentId);

        [OperationContract]
        TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData);

        [OperationContract]
        List<OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType);

        [OperationContract]
        TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, TerminateData terminateData);

        [OperationContract]
        TransactionError PlaceHistoryOrder(Protocal.TransactionData tranData);

        [OperationContract]
        TransactionError DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest);

        [OperationContract]
        bool ExistsTradePolicyId(Guid instrumentId, Guid tradePolicyId);

        [OperationContract]
        Dictionary<Guid, AccountQuotationInfo> GetQuotationCountPerAccount();

        [OperationContract]
        long ExecuteBatchOrders(List<Protocal.Test.ExecuteInfo> executeInfos);


    }
}
