using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using log4net;
using iExchange.Common;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data;
using iExchange.StateServer.Adapter.CommonTypes;

namespace iExchange.StateServer.Adapter.FaxEmailServices
{
    internal class EmailNotifier
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(EmailNotifier));
        private string _ConnectionString;
        private  FaxEmail.FaxEmailService  _EmailService;
        private Queue<object> _ToBeSendEntities = new Queue<object>();
        private object _ToBeSendEntitiesLock = new object();
        private AutoResetEvent _HasToBeSendEntityEvent = new AutoResetEvent(false);
        private AutoResetEvent _StopEvent = new AutoResetEvent(false);
        private Thread _SendEmailThread;


        public EmailNotifier(string connectionString)
        {
            this._ConnectionString = connectionString;

            this._SendEmailThread = new Thread(this.SendEmail);
            this._SendEmailThread.IsBackground = true;
            this._SendEmailThread.Start();
        }

        private void SendEmail()
        {
            try
            {
                this._EmailService = new FaxEmail.FaxEmailService();
                AutoResetEvent[] events = new AutoResetEvent[] { this._StopEvent, this._HasToBeSendEntityEvent };

                while (true)
                {
                    int eventIndex = WaitHandle.WaitAny(events);
                    if (eventIndex == 0) break;

                    if (eventIndex == 1)
                    {
                        while (true)
                        {
                            object emailEntity = null;
                            lock (this._ToBeSendEntitiesLock)
                            {
                                if (this._ToBeSendEntities.Count > 0)
                                {
                                    emailEntity = this._ToBeSendEntities.Dequeue();
                                }
                            }
                            if (emailEntity == null)
                            {
                                break;
                            }
                            else
                            {
                                this.Send(emailEntity);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error("EmailNotifier.SendEmail", exception);
            }
        }

        private void Send(object emailEntity)
        {
            try
            {
                Execution execution = emailEntity as Execution;
                if (execution != null)
                {
                    Logger.InfoFormat("TransactonServer.NotifyExecution(before send) OrderId= {0}", execution.OrderId);
                    this._EmailService.NotifyOrderExecuted(execution.OrderId);
                    Logger.InfoFormat("TransactonServer.NotifyExecution(after send) OrderId= {0}", execution.OrderId);
                    return;
                }

                AccountRisk accountRisk = emailEntity as AccountRisk;
                if (accountRisk != null)
                {
                    this._EmailService.NotifyRiskLevelChanged(accountRisk.ToRiskLevelChangedInfo());
                    return;
                }

                BalanceInfo balanceInfo = emailEntity as BalanceInfo;
                if (balanceInfo != null)
                {
                    this._EmailService.NotifyBalanceChanged(balanceInfo);
                    return;
                }

                OrderDeleted orderDeleted = emailEntity as OrderDeleted;
                if (orderDeleted != null)
                {
                    this._EmailService.NotifyOrderDeleted(orderDeleted.OrderId);
                    return;
                }

                TradeDayReset tradeDayReset = emailEntity as TradeDayReset;
                if (tradeDayReset != null)
                {
                    this._EmailService.NotifyResetStatement(tradeDayReset.TradeDay.Day);
                    this.SetTradeDayResetEmailGenerated(tradeDayReset.TradeDay.Day);
                    return;
                }

                PasswordResetInfo passwordResetInfo = emailEntity as PasswordResetInfo;
                if (passwordResetInfo != null)
                {
                    this._EmailService.NotifyPasswordReset(passwordResetInfo);
                }

                VerificationCodeInfo verificationCodeInfo = emailEntity as VerificationCodeInfo;
                if (verificationCodeInfo != null)
                {
                    this._EmailService.NotifyTelephonePinReset(verificationCodeInfo);
                }

                ApplyDelivery applyDelivery = emailEntity as ApplyDelivery;
                if (applyDelivery != null)
                {
                    this._EmailService.NotifyDeliveryListing(applyDelivery.DeliveryRequestId);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(emailEntity.ToString(), exception);
            }
        }

        private void SetTradeDayResetEmailGenerated(DateTime tradeDay)
        {
            using (SqlConnection connection = new SqlConnection(this._ConnectionString))
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "P_SetResetEmailGenerated";
                command.Parameters.Add(new SqlParameter("@tradeDay", tradeDay));

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        internal void NotifyExecution(Guid orderId)
        {
            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(new Execution(orderId));
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyTradeDayReset(DateTime tradeDay)
        {
            Thread thread = new Thread(delegate()
            {
                string sql = "EXEC P_GetResetEmailNotGeneratedTradeDays";
                DataSet dataSet = DataAccess.GetData(sql, this._ConnectionString);

                bool hasTradeDay = dataSet.Tables[0].Rows.Count > 0;
                if (hasTradeDay)
                {
                    lock (this._ToBeSendEntitiesLock)
                    {
                        foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                        {
                            TradeDay tradeDay2 = new TradeDay(dataRow);
                            Logger.Info(string.Format("NotifyTradeDayReset, will notify {0} if it is not equal to TradeDay = {1}", tradeDay2.Day, tradeDay));
                            if (tradeDay2.Day != tradeDay)
                            {
                                Logger.Info("NotifyTradeDayReset " + tradeDay2.Day.ToString());
                                this._ToBeSendEntities.Enqueue(new TradeDayReset(tradeDay2));
                            }
                        }
                    }
                    this._HasToBeSendEntityEvent.Set();
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        internal void NotifyResetAccountRisk(Account account, DateTime time)
        {
            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(AccountRisk.Create(account, time, (int)AlertLevel.Normal));
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyAccountRisk(AccountRisk accountRisk)
        {
            //DateTime alertTime = DateTime.Parse(accountNode.Attributes["AlertTime"].Value);
            //decimal balance = decimal.Parse(accountNode.Attributes["Balance"].Value);
            //decimal necessary = decimal.Parse(accountNode.Attributes["Necessary"].Value);
            //decimal equity = decimal.Parse(accountNode.Attributes["Equity"].Value);

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(accountRisk);
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void Stop()
        {
            this._StopEvent.Set();
        }

        internal void NotifyBalanceChanged(Account account, DateTime time, string currencyCode, decimal balance)
        {
            BalanceInfo balanceInfo = new BalanceInfo
            {
                AccountCode = account.Code,
                AccountID = account.Id,
                TradeDay = time,
                Amount = (double)Math.Abs(balance),
                CurrencyCode = currencyCode,
                ClientName = account.CustomerName,
                ChangeSubject = balance > 0 ? BalanceChangeSubject.Deposit : BalanceChangeSubject.Withdrawal
            };

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(balanceInfo);
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            PasswordResetInfo info = new PasswordResetInfo
            {
                CustomerID = customerId,
                LoginName = loginName,
                Password = newPassword
            };

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(info);
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyOrderDeleted(Guid orderID)
        {
            OrderDeleted orderDeleted = new OrderDeleted(orderID);

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(orderDeleted);
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyApplyDelivery(Guid deliveryRequestId)
        {
            ApplyDelivery applyDelivery = new ApplyDelivery(deliveryRequestId);

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(applyDelivery);
            }
            this._HasToBeSendEntityEvent.Set();
        }

        internal void NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            VerificationCodeInfo info = new VerificationCodeInfo
            {
                AccountId = accountId,
                VerificationCode = verificationCode
            };

            lock (this._ToBeSendEntitiesLock)
            {
                this._ToBeSendEntities.Enqueue(info);
            }
            this._HasToBeSendEntityEvent.Set();
        }
    }

    public class OrderDeleted
    {
        public OrderDeleted(Guid orderId)
        {
            this.OrderId = orderId;
        }

        public Guid OrderId
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("OrderDeleted orderId={0}", this.OrderId);
        }
    }

    public class ApplyDelivery
    {
        public ApplyDelivery(Guid deliveryRequestId)
        {
            this.DeliveryRequestId = deliveryRequestId;
        }

        public Guid DeliveryRequestId
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("ApplyDelivery deliveryRequestId={0}", this.DeliveryRequestId);
        }
    }

    public class Execution
    {
        public Execution(Guid orderId)
        {
            this.OrderId = orderId;
        }

        public Guid OrderId
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("Execution orderId={0}", this.OrderId);
        }
    }

    public class TradeDayReset
    {
        public TradeDayReset(TradeDay tradeDay)
        {
            this.TradeDay = tradeDay;
        }

        public TradeDay TradeDay
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("TradeDayReset tradeDay={0}", this.TradeDay.Day);
        }
    }

    public class AccountRisk
    {
        public AccountRisk(Guid accountID, string clientName, string accountName, string currencyCode, DateTime tradeDay, int riskLevel, double equity, double margin)
            : this(accountID, clientName, accountName, currencyCode, tradeDay, riskLevel, equity, margin, null)
        {
        }

        public AccountRisk(Guid accountID, string clientName, string accountName, string currencyCode, DateTime tradeDay, int riskLevel, double equity, double margin, string[] cutOrders)
        {
            this.AccountID = accountID;
            this.ClientName = clientName;
            this.AccountName = accountName;
            this.CurrencyCode = currencyCode;
            this.TradeDay = tradeDay;
            this.RiskLevel = riskLevel;
            this.Equity = equity;
            this.Margin = margin;
            this.TransactionCodes = cutOrders;
        }

        public Guid AccountID { get; private set; }
        public string ClientName { get; private set; }
        public string AccountName { get; private set; }
        public string CurrencyCode { get; private set; }
        public DateTime TradeDay { get; private set; }
        public int RiskLevel { get; private set; }
        public double Equity { get; private set; }
        public double Margin { get; private set; }
        public string[] TransactionCodes { get; private set; }

        public override string ToString()
        {
            return string.Format("AccountRisk accountId={0}, tradeDay={1}, riskLevel={2}", this.AccountID, this.TradeDay.Day, this.RiskLevel);
        }

        public iExchange.Common.RiskLevelChangedInfo ToRiskLevelChangedInfo()
        {
            return new RiskLevelChangedInfo
            {
                AccountID = this.AccountID,
                AccountName = this.AccountName,
                ClientName = this.ClientName,
                CurrencyCode = this.CurrencyCode,
                TradeDay = this.TradeDay,
                RiskLevel = this.RiskLevel,
                Equity = this.Equity,
                InitialMargin = Margin,
                TransactionCodes = this.TransactionCodes
            };
        }

        internal static AccountRisk Create(Account account, DateTime time, Transaction[] cutTrans)
        {
            AccountRisk accountRisk = AccountRisk.Create(account, time, (int)account.AlertLevel);

            if (cutTrans != null && cutTrans.Length > 0)
            {
                List<string> codes = new List<string>(cutTrans.Length);
                foreach (Transaction tran in cutTrans)
                {
                    foreach (Order order in tran.Orders)
                    {
                        codes.Add(order.Code);
                    }
                }
                accountRisk.TransactionCodes = codes.ToArray();
            }
            else
            {
                accountRisk.TransactionCodes = null;
            }

            return accountRisk;
        }

        internal static AccountRisk Create(Account account, DateTime time, int riskLevel)
        {
            return new AccountRisk(account.Id, account.CustomerName, account.Code, account.CurrencyCode, time, riskLevel, (double)account.Equity, (double)account.Necessary);
        }
    }
}