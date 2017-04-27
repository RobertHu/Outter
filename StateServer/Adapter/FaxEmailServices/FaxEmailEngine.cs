using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using Protocal;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using iExchange.Common;
using System.Xml;

namespace iExchange.StateServer.Adapter.FaxEmailServices
{
    internal sealed class FaxEmailEngine
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FaxEmailEngine));
        private EmailNotifier _emailNotifier;

        internal static readonly FaxEmailEngine Default = new FaxEmailEngine();

        static FaxEmailEngine() { }
        private FaxEmailEngine() { }


        internal void Initialize(string connectionString)
        {
            _emailNotifier = new EmailNotifier(connectionString);
        }

        private bool CanNotify()
        {
            return Settings.SettingManager.Default.SystemParameter.EnableEmailNotify;
        }


        internal void NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyPasswordChanged, customerId = {0}, loginName = {1}, newPassword= {2}", customerId, loginName, newPassword);
            _emailNotifier.NotifyPasswordChanged(customerId, loginName, newPassword);
        }

        internal void NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyTelephonePinReset customerId={0}, accountId = {1}, verificationCode = {2}", customerId, accountId, verificationCode);
            _emailNotifier.NotifyTelephonePinReset(customerId, accountId, verificationCode);
        }

        internal void NotifyBalanceChanged(Account account, DateTime time, string currencyCode, decimal balance)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyBalanceChanged  time = {0}, currencyCode= {1}, balance = {2}", time, currencyCode, balance);
            _emailNotifier.NotifyBalanceChanged(account, time, currencyCode, balance);
        }

        internal void NotifyApplyDelivery(Guid deliveryRequestId)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyApplyDelivery  deliveryRequestId = {0}", deliveryRequestId);
            _emailNotifier.NotifyApplyDelivery(deliveryRequestId);
        }

        internal void NotifyOrderDeleted(Guid orderID)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyOrderDeleted  orderId = {0}", orderID);
            _emailNotifier.NotifyOrderDeleted(orderID);
        }

        internal void NotifyAccountRisk(AccountRisk accountRisk)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyAccountRisk {0}", accountRisk);
            _emailNotifier.NotifyAccountRisk(accountRisk);
        }

        internal void NotifyExecution(Guid orderId)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyExecution {0}", orderId);
            _emailNotifier.NotifyExecution(orderId);
        }

        internal void NotifyResetAccountRisk(Account account, DateTime time)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyResetAccountRisk {0}", account);
            _emailNotifier.NotifyResetAccountRisk(account, time);
        }

        internal void NotifyTradeDayReset(DateTime tradeDay)
        {
            if (!this.CanNotify()) return;
            Logger.InfoFormat("NotifyTradeDayReset tradeDay = {0}", tradeDay);
            _emailNotifier.NotifyTradeDayReset(tradeDay);
        }

    }
}