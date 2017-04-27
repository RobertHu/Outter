using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SystemController.InstrumentBLL;

namespace SystemController.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal sealed class TransactionService : Protocal.Communication.HttpCommunicationService<Protocal.ITransactionServerService>, Protocal.ITransactionServerService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(TransactionService));

        internal TransactionService()
            : base(SettingManager.Default.TransactionServiceUrl)
        {
        }

        public iExchange.Common.TransactionError Place(Protocal.TransactionData tranData)
        {
            return this.Call<TransactionError>(() => this.Service.Place(tranData));
        }

        public iExchange.Common.TransactionError Place(Protocal.TransactionData tranData, out string tranCode)
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

        public iExchange.Common.TransactionError Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason)
        {
            return this.Call<TransactionError>(() => this.Service.Cancel(token, accountId, tranId, cancelReason));
        }

        public string GetInitializeData(List<Guid> accountIds)
        {
            return this.Call<string>(() =>
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    string result = this.Service.GetInitializeData(accountIds);
                    watch.Stop();
                    Logger.InfoFormat("get initialize data cost time = {0} ", watch.ElapsedMilliseconds);
                    return result;
                });
        }

        public iExchange.Common.TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData data)
        {
            return this.Call<TransactionError>(() => this.Service.ApplyDelivery(data));
        }

        public List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId)
        {
            return this.Call<List<Protocal.Physical.OrderInstalmentData>>(() => this.Service.GetOrderInstalments(orderId));
        }

        public iExchange.Common.TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return this.Call<TransactionError>(() => this.Service.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData));
        }

        public List<Protocal.OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return this.Call<List<Protocal.OrderQueryData>>(() => this.Service.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType));
        }

        public bool Test()
        {
            return this.Call<bool>(() => this.Service.Test());
        }

        public iExchange.Common.TransactionError DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return this.Call<TransactionError>(() => this.Service.DeleteOrder(accountId, orderId, isPayForInstalmentDebitInterest));
        }

        public bool SetPriceAlerts(Guid submitorId, System.Xml.XmlNode priceAlertsNode)
        {
            return this.Call<bool>(() => this.Service.SetPriceAlerts(submitorId, priceAlertsNode));
        }

        protected override ILog Logger
        {
            get { return _Logger; }
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

        public void SetDailyClosePrice(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            Logger.WarnFormat("begin SetDailyClosePrice instrumentId = {0}, tradeDay = {1}", instrumentId, tradeDay);
            this.Call(() => this.Service.SetDailyClosePrice(instrumentId, tradeDay, closeQuotations));
            this.ProcessForSetDailyClosePrice(instrumentId, tradeDay);
        }

        public void SetDailyClosePrices(List<InstrumentDailyClosePriceInfo> dailyClosePrices)
        {
            if (dailyClosePrices == null || dailyClosePrices.Count == 0) return;
            foreach (var eachDailyClosePrice in dailyClosePrices)
            {
                Logger.WarnFormat("begin SetDailyClosePrices instrumentId = {0}, tradeDay = {1}", eachDailyClosePrice.InstrumentId, eachDailyClosePrice.TradeDay);
            }

            this.Call(() => this.Service.SetDailyClosePrices(dailyClosePrices));
            foreach (var eachDailyClosePrice in dailyClosePrices)
            {
                this.ProcessForSetDailyClosePrice(eachDailyClosePrice.InstrumentId, eachDailyClosePrice.TradeDay);
            }
        }

        private void ProcessForSetDailyClosePrice(Guid instrumentId, DateTime tradeDay)
        {
            Logger.WarnFormat("after SetDailyClosePrice instrumentId = {0}, tradeDay = {1}", instrumentId, tradeDay);
            InstrumentTradingStatusKeeper.Default.AddInstrumentStatus(instrumentId, Protocal.TradingInstrument.InstrumentStatus.DayCloseQuotationReceived, DateTime.Now, tradeDay);
        }


        public long PlaceBatchOrders(List<Protocal.TransactionData> trans)
        {
            return this.Call(() => this.Service.PlaceBatchOrders(trans));
        }


        public IAsyncResult BeginGetInitializeData(List<Guid> accountIds, AsyncCallback callback, object asyncState)
        {
            return this.Call(() => this.Service.BeginGetInitializeData(accountIds, callback, asyncState));
        }

        public string EndGetInitializeData(IAsyncResult result)
        {
            return this.Call(() => this.Service.EndGetInitializeData(result));
        }


    }
}
