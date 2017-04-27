using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.Market;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class ServerService : Protocal.Test.IServerService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerService));

        public TransactionError Place(Protocal.TransactionData tranData)
        {
            try
            {
                return ServerFacade.Default.Server.Place(tranData);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return TransactionError.RuntimeError;
            }
        }


        public string Test()
        {
            return "From Server";
        }


        public string GetInitData(List<Guid> accountIds)
        {
            return ServerFacade.Default.Server.GetInitializeData(accountIds);
        }


        public void PlaceByModel(Protocal.TransactionData tranData)
        {
            ServerFacade.Default.Server.Place(tranData);
        }

        public List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId)
        {
            return ServerFacade.Default.Server.GetOrderInstalments(orderId);
        }

        public void DoReset(Guid instrumentId, DateTime tradeDay)
        {
        }

        public Protocal.TradingInstrument.InstrumentStatus GetInstrumentTradingStatus(Guid instrumentId)
        {
            var instrument = TradingSetting.Default.GetInstrument(instrumentId);
            return instrument.TradingStatus.Status;
        }

        public void Update(AppType appType, XElement updateNode)
        {
            ServerFacade.Default.Server.Update(appType, updateNode);
        }


        public TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData)
        {
            return ServerFacade.Default.Server.ApplyDelivery(requestData);
        }


        public void DoAccountSystemReset(Guid accountId, DateTime tradeDay)
        {
            var account = TradingSetting.Default.GetAccount(accountId);
            account.DoSystemReset(tradeDay);
        }

        public List<Protocal.OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return DBRepository.Default.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType);
        }


        public TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return ServerFacade.Default.Server.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData);
        }


        public TransactionError PlaceHistoryOrder(Protocal.TransactionData tranData)
        {
            return TransactionError.OK;
            //return ServerFacade.Default.Server.Book(tranData);
        }


        public TransactionError DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return ServerFacade.Default.Server.DeleteOrder(accountId, orderId, isPayForInstalmentDebitInterest,null);
        }

        public bool ExistsTradePolicyId(Guid instrumentId, Guid tradePolicyId)
        {
            return Settings.Setting.Default.GetTradePolicyDetail(instrumentId, tradePolicyId, null) != null;
        }

        public Dictionary<Guid, Protocal.Test.AccountQuotationInfo> GetQuotationCountPerAccount()
        {
            throw new NotImplementedException();
        }


        public long ExecuteBatchOrders(List<Protocal.Test.ExecuteInfo> executeInfos)
        {
            return ServerFacade.Default.Server.ExecuteBatchOrders(executeInfos);
        }
   
    }


}
