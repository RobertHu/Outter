using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SystemController.Factory;

namespace SystemController.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class SystemControllerService : Protocal.Communication.HttpCommunicationService<ISystemController>, ISystemController
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(SystemControllerService));

        internal SystemControllerService()
            : base(SettingManager.Default.TransactionAdapterService)
        {
        }

        public iExchange.Common.TransactionError Place(TransactionData tranData)
        {
            return this.Call<TransactionError>(() => this.Service.Place(tranData));
        }

        public iExchange.Common.TransactionError Place(TransactionData tranData, out string tranCode)
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

        public iExchange.Common.TransactionError Delete(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest)
        {
            return this.Call<TransactionError>(() => this.Service.Delete(accountId, orderId, isPayForInstalmentDebitInterest));
        }


        public string GetInitializeData(List<Guid> accountIds)
        {
            return this.Call<string>(() => this.Service.GetInitializeData(accountIds));
        }

        public iExchange.Common.TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData data, out string code, out string balance, out string usableMargin)
        {
            code = balance = usableMargin = null;
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

        public bool CancelDelivery(Guid userId, Guid deliveryRequestId, string title, string notifyMessage, out Guid accountId, out int status)
        {
            status = (int)DeliveryRequestStatus.Cancelled;
            accountId = Guid.Empty;
            try
            {
                if (this.Service.CancelDelivery(userId, deliveryRequestId, title, notifyMessage, out accountId, out status))
                {
                    Broadcaster.Default.AddCommand(new Protocal.Commands.Trading.CancelDeliveryCommand(accountId, deliveryRequestId, status));
                    Broadcaster.Default.AddCommand(new Protocal.SettingCommand() { Content = this.BuildChatMessage(title, notifyMessage) });
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                this.RecoverConnection(ex);
                return false;
            }

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

        public List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId)
        {
            return this.Call<List<Protocal.Physical.OrderInstalmentData>>(() => this.Service.GetOrderInstalments(orderId));
        }

        public iExchange.Common.TransactionError PrePayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return this.Call<TransactionError>(() => this.Service.PrePayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData));
        }

        public List<OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            return this.Call<List<OrderQueryData>>(() => this.Service.QueryOrders(language, customerId, lastDays, accountId, instrumentId, queryType));
        }

        public void Update(AppType appType, string update)
        {
            try
            {
                Logger.InfoFormat("update setting  {0}", update);
                var command = CommandFactory.CreateSettingCommand(appType, update);
                Broadcaster.Default.AddCommand(command);
                InstrumentBLL.InstrumentManager.Default.AddNewInstrument(update);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public iExchange.Common.TransactionError Book(iExchange.Common.Token token, TransactionBookData tranData, bool preserveCalculation)
        {
            Logger.InfoFormat("Book order tranId = {0}", tranData.Id);
            return this.Call<TransactionError>(() => this.Service.Book(token, tranData, preserveCalculation));
        }

        public iExchange.Common.TransactionError Execute(Guid accountId, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            return this.Call<TransactionError>(() => this.Service.Execute(accountId, tranID, buyPrice, sellPrice, lot, executedOrderID));
        }

        public bool SetPriceAlerts(Guid submitorId, System.Xml.XmlNode priceAlertsNode)
        {
            return this.Call<bool>(() => this.Service.SetPriceAlerts(submitorId, priceAlertsNode));
        }

        public iExchange.Common.TransactionError ApplyTransfer(Guid userId, Guid sourceAccountID, Guid sourceCurrencyID, decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount, decimal rate, DateTime expireDate)
        {
            return this.Call<TransactionError>(() => this.Service.ApplyTransfer(userId, sourceAccountID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate));
        }

        public iExchange.Common.TransactionError AcceptTransfer(Guid userId, Guid transferID)
        {
            return this.Call<TransactionError>(() => this.Service.AcceptTransfer(userId, transferID));
        }

        public iExchange.Common.TransactionError DeclineTransfer(Guid userId, Guid transferID)
        {
            return this.Call<TransactionError>(() => this.Service.DeclineTransfer(userId, transferID));
        }

        public iExchange.Common.AlertLevel[] ResetAlertLevel(Guid[] accountIDs)
        {
            return this.Call<AlertLevel[]>(() => this.Service.ResetAlertLevel(accountIDs));
        }

        public void ResetHit(Dictionary<Guid, List<Guid>> accountPerOrders)
        {
            this.Call(() => this.Service.ResetHit(accountPerOrders));
        }

        public iExchange.Common.TransactionError Assign(TransactionData tranData)
        {
            return this.Call<TransactionError>(() => this.Service.Assign(tranData));
        }

        public iExchange.Common.TransactionError MultipleClose(Guid accountId, Guid[] orderIds)
        {
            return this.Call<TransactionError>(() => this.Service.MultipleClose(accountId, orderIds));
        }

        protected override ILog Logger
        {
            get { return _Logger; }
        }


        public TransactionError AcceptPlace(Guid accountID, Guid tranID)
        {
            return this.Call<TransactionError>(() => this.Service.AcceptPlace(accountID, tranID));
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


        public bool Deposit(Protocal.DataTypes.PayRecord deposit, out bool canResetAlertLevel)
        {
            throw new NotImplementedException();
        }

        public bool Withdraw(Protocal.DataTypes.PayRecord deposit)
        {
            throw new NotImplementedException();
        }

        public bool ClearDeposit(Guid clearedDepositId, bool successed)
        {
            throw new NotImplementedException();
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

        public void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            try
            {
                this.Service.NotifyDeliveryApproved(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime, title, notifyMessage);
                Broadcaster.Default.AddCommand(new Protocal.Commands.Trading.NotifyDeliveryCommand(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime));

                ChatNotifyCommand chatCommand = new ChatNotifyCommand
                {
                    Content = BuildChatMessage(title, notifyMessage)
                };
                Broadcaster.Default.AddCommand(chatCommand);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private string BuildChatMessage(string title, string notifyMessage)
        {
            XElement node = new XElement("Chat");
            node.SetAttributeValue("ID", Guid.NewGuid());
            node.SetAttributeValue("Title", title);
            node.SetAttributeValue("Content", title);
            node.SetAttributeValue("PublishTime", XmlConvert.ToString(DateTime.Now, DateTimeFormat.Xml));
            return node.ToString();
        }


        public bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId)
        {
            try
            {
                this.Service.NotifyDeliveried(accountId, deliveryRequestId);
                var command = new Protocal.Commands.Trading.NotifyDeliveryCommand();
                command.DeliveryRequestId = deliveryRequestId;
                command.DeliveryRequestStatus = DeliveryRequestStatus.OrderCreated;
                command.AccountId = accountId;
                Broadcaster.Default.AddCommand(command);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }


        public TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<Protocal.Physical.InstalmentData> instalments)
        {
            return this.Call(() => this.Service.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalments));
        }


        public void QuoteAnswer(string anwser)
        {
            try
            {
                QuoteAnswerNotifyCommand command = new QuoteAnswerNotifyCommand
                {
                    Content = anwser,
                    SourceType = AppType.StateServer
                };
                Broadcaster.Default.AddCommand(command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        public void SetNews(List<Protocal.Commands.Notify.News> news)
        {
            var newsCommand = new Protocal.Commands.Notify.NewsNotifyCommand
            {
                News = news
            };
            Broadcaster.Default.AddCommand(newsCommand);
        }

        public void Chat(string content)
        {
            Broadcaster.Default.AddCommand(new ChatNotifyCommand { Content = content });
        }


        public AccountFloatingStatus GetAccountFloatingStatus(Guid accountId)
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
            return this.Call( () => this.Service.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit));
        }

        public string GetAllAccountsInitData()
        {
            return this.Call(() => this.Service.GetAllAccountsInitData());
        }
    }
}
