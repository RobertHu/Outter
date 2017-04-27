using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using Newtonsoft.Json;
using Protocal;
using Protocal.Physical;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.DB;

namespace Core.TransactionServer.Agent.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class TransactionServerService : ITransactionServerService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionServerService));

        TransactionError ITransactionServerService.Place(TransactionData tranData)
        {
            return this.Server.Place(tranData);
        }

        TransactionError ITransactionServerService.Place(TransactionData tranData, out string tranCode)
        {
            return this.Server.Place(tranData, out tranCode);
        }

        TransactionError ITransactionServerService.Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason)
        {
            return this.Server.Cancel(token, accountId, tranId, cancelReason);
        }

        string ITransactionServerService.GetInitializeData(List<Guid> accountIds)
        {
            return this.Server.GetInitializeData(accountIds);
        }

        TransactionError ITransactionServerService.ApplyDelivery(DeliveryRequestData data)
        {
            return this.Server.ApplyDelivery(data);
        }

        List<OrderInstalmentData> ITransactionServerService.GetOrderInstalments(Guid orderId)
        {
            return this.Server.GetOrderInstalments(orderId);
        }

        TransactionError ITransactionServerService.InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, TerminateData terminateData)
        {
            return this.Server.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData);
        }

        List<OrderQueryData> ITransactionServerService.QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return DBRepository.Default.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType);
        }

        bool ITransactionServerService.Test()
        {
            return true;
        }

        TransactionError ITransactionServerService.DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return this.Server.DeleteOrder(accountId, orderId, isPayForInstalmentDebitInterest, null);
        }

        bool ITransactionServerService.SetPriceAlerts(Guid submitorId, System.Xml.XmlNode priceAlertsNode)
        {
            return this.Server.SetPriceAlerts(submitorId, priceAlertsNode);
        }

        private Server Server { get { return ServerFacade.Default.Server; } }

        public bool ChangeLeverage(Guid accountId, int leverage, out decimal necessary)
        {
            return this.Server.ChangeLeverage(accountId, leverage, out necessary);
        }

        void ITransactionServerService.SetDailyClosePrice(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            Logger.InfoFormat("received SetDailyClosePrice instrumentId = {0}, tradeDay = {1}", instrumentId, tradeDay);
            Prices.ClosePriceManager.Default.Add(new Prices.Notify(instrumentId, tradeDay, closeQuotations));
        }

        public long PlaceBatchOrders(List<TransactionData> trans)
        {
            return this.Server.PlaceBatchOrders(trans);
        }

        public IAsyncResult BeginGetInitializeData(List<Guid> accountIds, AsyncCallback callback, object asyncState)
        {
            return new CompletedAsyncResult<string>(this.Server.GetInitializeData(accountIds));
        }

        public string EndGetInitializeData(IAsyncResult result)
        {
            var completedAsynResult = result as CompletedAsyncResult<string>;
            return completedAsynResult.Data;
        }


        public void SetDailyClosePrices(List<InstrumentDailyClosePriceInfo> dailyClosePrices)
        {
            Logger.InfoFormat("received SetDailyClosePrices dailyClosePrices count = {0}", dailyClosePrices.Count);
            if (dailyClosePrices != null && dailyClosePrices.Count > 0)
            {
                foreach (var eachDailyClosePrice in dailyClosePrices)
                {
                    Logger.InfoFormat("received SetDailyClosePrices instrumentId = {0}, tradeDay = {1}", eachDailyClosePrice.InstrumentId, eachDailyClosePrice.TradeDay);
                    Prices.ClosePriceManager.Default.Add(new Prices.Notify(eachDailyClosePrice.InstrumentId, eachDailyClosePrice.TradeDay, eachDailyClosePrice.Quotations));
                }
            }
        }
    }

    internal class CompletedAsyncResult<T> : IAsyncResult
    {
        private T _data;

        public CompletedAsyncResult(T data)
        { this._data = data; }

        public T Data
        { get { return _data; } }

        public object AsyncState
        { get { return (object)_data; } }

        public WaitHandle AsyncWaitHandle
        { get { throw new Exception("The method or operation is not implemented."); } }

        public bool CompletedSynchronously
        { get { return true; } }

        public bool IsCompleted
        { get { return true; } }
    }
}
