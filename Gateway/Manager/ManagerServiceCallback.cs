using SystemController.Factory;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    public class ManagerServiceCallback : IStateServer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ManagerServiceCallback));
        public iExchange.Common.TransactionError Book(iExchange.Common.Token token, string xmlTran, bool preserveCalculation)
        {
            throw new NotImplementedException();
        }

        public bool SwitchPriceState(string[] originCodes, bool enable, out Guid[] affectInstrumentIds)
        {
            throw new NotImplementedException();
        }

        public bool SuspendResume(string[] originCodes, bool resume, out Guid[] affectInstrumentIds)
        {
            throw new NotImplementedException();
        }

        public void Update(iExchange.Common.Token token, string updateXml)
        {
            throw new NotImplementedException();
        }

        public void BroadcastQuotation(iExchange.Common.Token token, iExchange.Common.OriginQuotation[] originQs, iExchange.Common.OverridedQuotation[] overridedQs)
        {
            if (SettingManager.Default.UserManagerPrice)
            {
                Logger.Info("receive quotation");
                var command = CommandFactory.CreateQuotationCommand(originQs, overridedQs);
                Broadcaster.Default.AddCommand(command);
            }
        }

        public void Answer(iExchange.Common.Token token, List<iExchange.Common.Manager.Answer> answerQutos)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.TransactionError AcceptPlace(iExchange.Common.Token token, Guid tranID)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.TransactionError Cancel(iExchange.Common.Token token, Guid tranID, iExchange.Common.CancelReason cancelReason)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.TransactionError RejectCancelLmtOrder(iExchange.Common.Token token, Guid tranID, Guid accountId)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.TransactionError CancelPlace(iExchange.Common.Token token, Guid tranID)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.Manager.TransactionResult Execute(iExchange.Common.Token token, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            throw new NotImplementedException();
        }

        public void ResetHit(iExchange.Common.Token token, Guid[] orderIDs)
        {
            throw new NotImplementedException();
        }

        public bool UpdateInstrument(iExchange.Common.Token token, iExchange.Common.Manager.ParameterUpdateTask parameterUpdateTask)
        {
            throw new NotImplementedException();
        }

        public bool UpdateDealingPolicyDetail(iExchange.Common.Token token, List<Dictionary<string, string>> dealingPolicyDic, List<Dictionary<string, string>> instrumentDic)
        {
            throw new NotImplementedException();
        }

        public bool UpdatePolicyProcess(iExchange.Common.Token token, List<Dictionary<string, string>> customerFileValues, List<Dictionary<string, string>> salesFileValues)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.Manager.AccountInformation GetAcountInfo(iExchange.Common.Token token, Guid tranID)
        {
            throw new NotImplementedException();
        }

        public List<iExchange.Common.Manager.AccountGroupGNP> GetGroupNetPosition(iExchange.Common.Token token, string permissionName, Guid[] accountIDs, Guid[] instrumentIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            throw new NotImplementedException();
        }

        public List<iExchange.Common.Manager.OpenInterestSummary> GetOpenInterestInstrumentSummary(iExchange.Common.Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            throw new NotImplementedException();
        }

        public List<iExchange.Common.Manager.OpenInterestSummary> GetOpenInterestAccountSummary(iExchange.Common.Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            throw new NotImplementedException();
        }

        public List<iExchange.Common.Manager.OpenInterestSummary> GetOpenInterestOrderSummary(iExchange.Common.Token token, Guid accountId, iExchange.Common.AccountType accountType, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            throw new NotImplementedException();
        }

        public string GetAccounts(iExchange.Common.Token token, Guid[] accountIDs, bool includeTransactions)
        {
            throw new NotImplementedException();
        }

        public Guid[] VerifyTransactions(iExchange.Common.Token token, Guid[] tranIDs)
        {
            throw new NotImplementedException();
        }

        public bool ChangeSystemStatus(iExchange.Common.Token token, iExchange.Common.SystemStatus newStatus)
        {
            throw new NotImplementedException();
        }

        public iExchange.Common.TransactionError Delete(iExchange.Common.Token token, Guid orderID, bool notifyByEmail, out System.Xml.XmlNode affectedOrders, out System.Xml.XmlNode xmlAccount)
        {
            throw new NotImplementedException();
        }
    }
}
