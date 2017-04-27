using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using Protocal;
using System.Xml;
using iExchange.Common;
using System.Diagnostics;
using iExchange.StateServer.Adapter.OuterService;
using System.Configuration;
using log4net;
using Protocal.Commands;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace iExchange.StateServer.Adapter
{
    internal interface ICommandBroadcast
    {
        void BroadcastCommands(Token token, iExchange.Common.Command[] commands);
        void BoardcastBookResult(Token token, XmlNode xmlTran, XmlNode xmlAccount, XmlNode xmlAffectedOrders);
        void OnTranExecutedForNewVersion(XmlNode xmlTran);
    }

    internal class TransactionAdaptor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionAdaptor));

        private SystemControllerProxy _proxy;
        private OuterTransactionService _outerService;
        private string _connectionString;
        private QuotationManager _quotationManager;

        private ISystemController SystemController
        {
            get { return _proxy; }
        }

        internal TransactionAdaptor(string systemControllerUrl, string quotationServiceUrl, ICommandBroadcast commandBroadcast, string connectionString)
        {
            Logger.InfoFormat("in constructor,systemControllerUrl = {0}", systemControllerUrl);
            _proxy = new SystemControllerProxy(systemControllerUrl);
            _outerService = new OuterTransactionService(connectionString, _proxy);
            _connectionString = connectionString;
            CommandManager.Default.Initialize(commandBroadcast, connectionString, this);
            this.InitializeDBData(connectionString);
            this.RegisterToSystemController();
            _quotationManager = new QuotationManager(quotationServiceUrl);
            DBHelper.Initialize(connectionString);
        }

        internal Protocal.Commands.Transaction GetTran(Guid tranId)
        {
            Guid accountId;
            if (!TransactionMapping.Default.GetAccountId(tranId, out accountId)) return null;
            Protocal.Commands.Account account;
            if (!AccountRepository.Default.TryGet(accountId, out account)) return null;
            return account.GetTran(tranId);
        }


        private void InitializeDBData(string connectionString)
        {
            var initData = Protocal.DataBaseHelper.GetData("[Trading].GetInitDataForTransactionAdapter", connectionString, new string[] { "Instrument", "SystemParameter" }, (List<DBParameter>)null);

            InstrumentManager.Default.Initialize(initData.Tables["Instrument"]);
            Settings.SettingManager.Default.Initialize(initData);
        }


        private void RegisterToSystemController()
        {
            string gatewayServiceUrl = ConfigurationManager.AppSettings["GatewayUrl"];
            string commandCollectorUrl = ConfigurationManager.AppSettings["CommandCollectorUrl"];

            Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        try
                        {
                            var channel = Protocal.ChannelFactory.CreateHttpChannel<Protocal.IGatewayService>(gatewayServiceUrl);
                            channel.Register(commandCollectorUrl, AppType.StateServer);
                            Logger.WarnFormat("Register to system controller {0} sucessfully", gatewayServiceUrl);
                            break;
                        }
                        catch (Exception exception)
                        {
                            Logger.WarnFormat("Register to system controller {0} failed :{1}", gatewayServiceUrl, exception);
                            Thread.Sleep(1000);
                        }
                    }
                });
        }


        internal bool NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            return _outerService.NotifyTelephonePinReset(customerId, accountId, verificationCode);
        }

        internal bool NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            Logger.InfoFormat("NotifyPasswordChanged customerId = {0}, loginName = {1}, newPassword = {2}", customerId, loginName, newPassword);
            return _outerService.NotifyPasswordChanged(customerId, loginName, newPassword);
        }


        internal XmlNode GetAccountsForInit(Guid[] accountIDs)
        {
            if (accountIDs == null || accountIDs.Length == 0) return null;
            string initData = this.SystemController.GetInitializeData(new List<Guid>(accountIDs));
            if (string.IsNullOrEmpty(initData))
            {
                StringBuilder sb = new StringBuilder(200);
                foreach (var eachAccountId in accountIDs)
                {
                    sb.AppendFormat(string.Format("{0}, ", eachAccountId));
                }
                Logger.ErrorFormat("GetAccountsForInit initData is null, accountIds = {0}", sb.ToString());
                return null;
            }
            return CommandManager.Default.FillAndGetInitData(initData);
        }

        internal TransactionError Place(Token token, XmlNode xmlTran, out string tranCode)
        {
            tranCode = null;
            try
            {
                Logger.InfoFormat("place xml = {0}", xmlTran.OuterXml);
                TransactionData tranData = Converter.ToTransactionData(token, xmlTran);
                return this.SystemController.Place(tranData, out tranCode);
            }
            catch (Exception exception)
            {
                this.HandleInvokeException(exception);

                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError Book(Token token, XmlNode xmlTran, bool preserveCalculation)
        {
            try
            {
                Logger.InfoFormat("book order {0}", xmlTran.OuterXml);
                var bookData = Converter.ToTransactionBookData(xmlTran);
                return this.SystemController.Book(token, bookData, preserveCalculation);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal bool ChangeLeverage(Token token, Guid accountId, int leverage, out decimal necessary)
        {
            necessary = 0m;
            try
            {
                Logger.InfoFormat("ChangeLeverage, accountId = {0}, leverage = {1}", accountId, leverage);
                return this.SystemController.ChangeLeverage(accountId, leverage, out necessary);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return false;
            }
        }


        internal TransactionError ApplyDelivery(XmlNode deliveryRequire, out string code, out string balance, out string usableMargin)
        {
            code = balance = usableMargin = null;
            try
            {
                var deliveryData = Converter.ToDeliveryRequestData(deliveryRequire);
                return this.SystemController.ApplyDelivery(deliveryData, out code, out balance, out usableMargin);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal bool CancelDelivery(Guid userId, Guid deliveryRequestId, string title, string message, out Guid accountId, out int status)
        {
            accountId = Guid.Empty;
            status = 100;
            try
            {
                return this.SystemController.CancelDelivery(userId, deliveryRequestId, title, message, out accountId, out status);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return false;
            }
        }

        internal bool NotifyDelivery(Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage, out Guid accountId)
        {
            accountId = Guid.Empty;
            try
            {
                return this.SystemController.NotifyDelivery(deliveryRequestId, availableDeliveryTime, title, notifyMessage, out accountId);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return false;
            }
        }


        internal bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId)
        {
            try
            {
                return this.SystemController.NotifyDeliveried(accountId, deliveryRequestId);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return false;
            }
        }


        internal TransactionError Delete(Guid? accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            try
            {
                if (accountId == null)
                {
                    TransactionError error;
                    accountId = this.GetAccountId(orderId, out error);
                    if (error != TransactionError.OK) return error;
                }
                Logger.InfoFormat("Delete order accountId={0}, orderId = {1}", accountId.Value, orderId);
                return this.SystemController.Delete(accountId.Value, orderId, isPayForInstalmentDebitInterest);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }




        internal TransactionError DeleteByCancelDelivery(Guid? accountId, Guid orderId, Guid deliveryRequestId, bool isPayForInstalmentDebitInterest)
        {
            if (accountId == null)
            {
                TransactionError error;
                accountId = this.GetAccountId(orderId, out error);
                if (error != TransactionError.OK)
                {
                    return error;
                }
            }
            Logger.InfoFormat("DeleteByCancelDelivery accountId={0}, orderId = {1}, deliveryRequestId = {2}", accountId.Value, orderId, deliveryRequestId);
            return this.SystemController.DeleteByCancelDelivery(accountId.Value, orderId, deliveryRequestId, isPayForInstalmentDebitInterest);
        }


        private Guid? GetAccountId(Guid orderId, out TransactionError errorCode)
        {
            Guid result;
            errorCode = TransactionError.OK;
            if (!Protocal.Commands.TransactionMapping.Default.GetAccountIdByOrder(orderId, out result))
            {
                errorCode = TransactionError.OpenOrderNotExists;
                return null;
            }
            return result;
        }

        internal TransactionError Cancel(Token token, Guid tranID, CancelReason cancelReason)
        {
            try
            {
                Guid accountId;
                if (Protocal.Commands.TransactionMapping.Default.GetAccountId(tranID, out accountId))
                {
                    return this.SystemController.Cancel(token, accountId, tranID, cancelReason);
                }
                else
                {
                    return TransactionError.TransactionNotExists;
                }
            }
            catch (Exception exception)
            {
                this.HandleInvokeException(exception);

                return TransactionError.RuntimeError;
            }
        }


        internal TransactionError Execute(Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            try
            {
                Guid accountId;
                if (!Protocal.Commands.TransactionMapping.Default.GetAccountId(tranID, out accountId))
                {
                    return TransactionError.TransactionNotExists;
                }
                return this.SystemController.Execute(accountId, tranID, buyPrice, sellPrice, lot, executedOrderID);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, string instalmentXml, string terminateXml)
        {
            try
            {
                if (!string.IsNullOrEmpty(instalmentXml))
                {
                    Logger.InfoFormat("InstalmentPayoff accountId= {0}, currencyId= {1},sumSourcePaymentAmount = {2}, sumSourceTerminateFee = {3}  instalmentXml = {4}", accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalmentXml);
                    return this.SystemController.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, Converter.ToInstalmentData(instalmentXml));
                }
                else if (!string.IsNullOrEmpty(terminateXml))
                {
                    Logger.InfoFormat("InstalmentPayoff accountId= {0}, currencyId= {1},sumSourcePaymentAmount = {2}, sumSourceTerminateFee = {3}  terminateXml = {4}", accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateXml);
                    return this.SystemController.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, Converter.ToTerminateData(terminateXml));
                }
                else
                {
                    return TransactionError.RuntimeError;
                }

            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError AcceptPlace(Guid tranID)
        {
            try
            {
                Guid accountId;
                if (!Protocal.Commands.TransactionMapping.Default.GetAccountId(tranID, out accountId))
                {
                    return TransactionError.TransactionNotExists;
                }
                return this.SystemController.AcceptPlace(accountId, tranID);

            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal bool SetPriceAlerts(Guid submitorId, XmlNode priceAlertsNode)
        {
            try
            {
                return this.SystemController.SetPriceAlerts(submitorId, priceAlertsNode);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return false;
            }
        }

        internal TransactionError ApplyTransfer(Guid userId, Guid sourceAccountID, Guid sourceCurrencyID,
           decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
           decimal rate, DateTime expireDate)
        {
            try
            {
                return this.SystemController.ApplyTransfer(userId, sourceAccountID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError AcceptTransfer(Guid userId, Guid transferID)
        {
            try
            {
                return this.SystemController.AcceptTransfer(userId, transferID);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError DeclineTransfer(Guid userId, Guid transferID)
        {
            try
            {
                return this.SystemController.DeclineTransfer(userId, transferID);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal AlertLevel[] ResetAlertLevel(Token token, Guid[] accountIDs)
        {
            try
            {
                this.NotifyFaxEmailEngine(accountIDs);
                var result = this.SystemController.ResetAlertLevel(accountIDs);
                foreach (var eachAccountId in accountIDs)
                {
                    DBHelper.ResetDBAlertLevel(token.UserID, eachAccountId);
                }
                return result;
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }



        private void NotifyFaxEmailEngine(Guid[] accountIDs)
        {
            foreach (Guid eachAccountId in accountIDs)
            {
                Account account = (Account)AccountRepository.Default.Get(eachAccountId);
                if (account == null) continue;
                FaxEmailServices.FaxEmailEngine.Default.NotifyResetAccountRisk(account, DateTime.Now);
            }
        }


        internal void ResetHit(Guid[] orderIDs)
        {
            try
            {
                if (orderIDs == null || orderIDs.Length == 0) return;
                this.SystemController.ResetHit(this.GetAccountPerOrders(orderIDs));
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        internal Guid[] Rehit(Guid[] orderIDs, Guid[] accountIDs)
        {
            try
            {
                Logger.InfoFormat("Rehit orderId = {0} , accountId = {1}", orderIDs[0], accountIDs[0]);
                return this.SystemController.Rehit(orderIDs, accountIDs);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal XmlNode GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            try
            {
                Logger.InfoFormat("GetAccountsProfitWithin minProfit = {0}, includeMinProfit = {1}, maxProfit = {2}, includeMaxProfit = {3}", minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                string result = this.SystemController.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                if (!string.IsNullOrEmpty(result)) return null;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result);
                return doc.DocumentElement;
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal TransactionError Assign(XmlNode xmlTran)
        {
            try
            {
                return this.SystemController.Assign(Converter.ToTransactionAssignData(xmlTran));
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError MultipleClose(Guid[] orderIds)
        {
            try
            {
                Guid accountId;
                Protocal.Commands.TransactionMapping.Default.GetAccountIdByOrder(orderIds[0], out accountId);
                return this.SystemController.MultipleClose(accountId, orderIds);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return TransactionError.RuntimeError;
            }
        }

        internal void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            try
            {
                this.SystemController.NotifyDeliveryApproved(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime, title, notifyMessage);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        internal XmlNode GetAccountInfo(Token token, Guid tranID)
        {
            try
            {
                return _outerService.GetAcountInfo(token, tranID);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }


        internal XmlNode GetMemoryBalanceNecessaryEquityExcludeAlerted(Token token)
        {
            try
            {
                return _outerService.GetMemoryBalanceNecessaryEquityExcludeAlerted(token);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal Guid[] VerifyTransaction(Token token, Guid[] transactionIDs, out XmlNode[] xmlTrans, out XmlNode[] xmlAccounts)
        {
            xmlTrans = null;
            xmlAccounts = null;
            try
            {
                return _outerService.VerifyTransaction(token, transactionIDs, out xmlTrans, out xmlAccounts);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal XmlNode GetOpenInterestSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                return this._outerService.GetOpenInterestSummary(token, accountIDs, instrumentIDs, blotterCodeSelecteds);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal XmlNode GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            try
            {
                return _outerService.GetOpenInterestInstrumentSummary(token, isGroupByOriginCode, blotterCodeSelecteds);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }

        internal XmlNode GetOpenInterestSummaryOrderList(Guid accountId, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                return _outerService.GetOpenInterestSummaryOrderList(accountId, instrumentIDs, blotterCodeSelecteds);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
                return null;
            }
        }


        internal void Update(Token token, XmlNode updateNode)
        {
            try
            {
                Settings.SettingManager.Default.Update(updateNode);
                this.SystemController.Update(token.AppType, updateNode.OuterXml);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        internal void QuoteAnswer(string anwser)
        {
            try
            {
                this.SystemController.QuoteAnswer(anwser);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }


        internal void Chat(string content)
        {
            try
            {
                this.SystemController.Chat(content);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        internal void SetNews(News[] newsCollection)
        {
            try
            {
                var result = new List<Protocal.Commands.Notify.News>(newsCollection.Length);
                foreach (var eachNews in newsCollection)
                {
                    result.Add(new Protocal.Commands.Notify.News()
                    {
                        CategoryId = eachNews.CategoryId,
                        Contents = eachNews.Contents,
                        ExpireTime = eachNews.ExpireTime,
                        Id = eachNews.Id,
                        IsExpired = eachNews.IsExpired,
                        Language = eachNews.Language,
                        ModifyTime = eachNews.ModifyTime,
                        PublisherId = eachNews.PublisherId,
                        PublishTime = eachNews.PublishTime,
                        Title = eachNews.Title
                    });
                }
                this.SystemController.SetNews(result);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        internal void SetQuotation(OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            try
            {
                _quotationManager.Add(new QuotationPair(originQs, overridedQs));
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }
        }


        private Dictionary<Guid, List<Guid>> GetAccountPerTrans(Guid[] tranIDs)
        {
            return this.GetAccountPerModelsCommon(tranIDs, (Guid id, out Guid accountId) => Protocal.Commands.TransactionMapping.Default.GetAccountId(id, out accountId));
        }


        private Dictionary<Guid, List<Guid>> GetAccountPerOrders(Guid[] orderIDs)
        {
            return this.GetAccountPerModelsCommon(orderIDs, (Guid id, out Guid accountId) => Protocal.Commands.TransactionMapping.Default.GetAccountIdByOrder(id, out accountId));
        }

        private Dictionary<Guid, List<Guid>> GetAccountPerModelsCommon(Guid[] ids, TryGetMappingAccountIdHandle handle)
        {
            Dictionary<Guid, List<Guid>> result = new Dictionary<Guid, List<Guid>>();
            foreach (var eachId in ids)
            {
                Guid accountId;
                if (handle(eachId, out accountId))
                {
                    List<Guid> models;
                    if (!result.TryGetValue(accountId, out models))
                    {
                        models = new List<Guid>();
                        result.Add(accountId, models);
                    }
                    models.Add(eachId);
                }
            }
            return result;
        }

        public XmlNode GetAccountStatus(Guid accountId, Guid instrumentId, OrderType orderType, bool needOutputPrice, out string buyPrice, out string sellPrice)
        {
            return _outerService.GetAccountStatus(accountId, instrumentId, orderType, needOutputPrice, out buyPrice, out sellPrice);
        }

        public XmlNode GetGroupNetPositionForManager(Token token, string permissionName, Guid[] accountGroupIDs, Guid[] instrumentGroupIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            return _outerService.GetGroupNetPositionForManager(token, permissionName, accountGroupIDs, instrumentGroupIDs, showActualQuantity, blotterCodeSelecteds);
        }

        public bool ChangeSystemStatus(Token token, SystemStatus newStatus)
        {
            return true;
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, bool includeTransactions, bool onlyCutOrder)
        {
            return _outerService.GetAccounts(token, accountIDs, new Guid[] { Guid.Empty }, includeTransactions, onlyCutOrder);
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, Guid[] instrumentIDs, bool includeTransactions)
        {
            return _outerService.GetAccounts(token, accountIDs, instrumentIDs, includeTransactions, false);
        }

        internal void SetDailyClosePrice(Token token, TradingDailyQuotation[] quotations)
        {
            try
            {
                //this.SystemController.SetDailyClosePrice(userId, transferID);
            }
            catch (Exception ex)
            {
                this.HandleInvokeException(ex);
            }
        }

        private void HandleInvokeException(Exception exception)
        {
            Logger.Error(exception);
        }

        private delegate bool TryGetMappingAccountIdHandle(Guid id, out Guid accountId);

    }


}