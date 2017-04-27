using iExchange.Common;
using ProtoBuf;
using Protocal.Physical;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Protocal
{
    [ProtoContract]
    [ServiceContract]
    public interface IBroadcastService
    {
        [OperationContract]
        void Broadcast(Command command);

        [OperationContract]
        bool Test();
    }


    [ServiceContract]
    public interface ITransactionServerService
    {
        [OperationContract]
        TransactionError Place(TransactionData tranData);

        [OperationContract(Name = "PlaceAndGetTranCode")]
        TransactionError Place(TransactionData tranData, out string tranCode);

        [OperationContract]
        long PlaceBatchOrders(List<TransactionData> trans);

        [OperationContract]
        TransactionError Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason);

        [OperationContract]
        string GetInitializeData(List<Guid> accountIds);

        [OperationContractAttribute(AsyncPattern = true)]
        IAsyncResult BeginGetInitializeData(List<Guid> accountIds, AsyncCallback callback, object asyncState);

        string EndGetInitializeData(IAsyncResult result);


        [OperationContract]
        TransactionError ApplyDelivery(DeliveryRequestData data);


        [OperationContract]
        List<Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId);

        [OperationContract]
        TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, TerminateData terminateData);

        [OperationContract]
        List<OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType);

        [OperationContract]
        bool Test();

        [OperationContract]
        TransactionError DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest);

        [OperationContract]
        [XmlSerializerFormat]
        bool SetPriceAlerts(Guid submitorId, XmlNode priceAlertsNode);

        [OperationContract]
        bool ChangeLeverage(Guid accountId, int leverage, out decimal necessary);

        [OperationContract]
        void SetDailyClosePrice(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations);

        [OperationContract]
        void SetDailyClosePrices(List<InstrumentDailyClosePriceInfo> dailyClosePrices);
    }


   


}
