using iExchange.Common;
using iExchange.Common.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SystemController
{
    [ServiceContract(CallbackContract = typeof(IStateServer), SessionMode = SessionMode.Required)]
    public interface IExchangeService
    {
        [OperationContract]
        void Register(string iexchangeCode, bool forQuotation);

        [OperationContract(IsInitiating = false), XmlSerializerFormat]
        void AddCommand(Command command);
    }


    [ServiceContract]
    public interface IStateServer
    {
        [OperationContract]
        TransactionError Book(Token token, string xmlTran, bool preserveCalculation);

        [OperationContract]
        bool SwitchPriceState(string[] originCodes, bool enable, out Guid[] affectInstrumentIds);

        [OperationContract]
        bool SuspendResume(string[] originCodes, bool resume, out Guid[] affectInstrumentIds);

        [OperationContract]
        void Update(Token token, string updateXml);

        [OperationContract(IsOneWay = true)]
        void BroadcastQuotation(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs);

        [OperationContract]
        void Answer(Token token, List<Answer> answerQutos);

        [OperationContract]
        TransactionError AcceptPlace(Token token, Guid tranID);

        [OperationContract]
        TransactionError Cancel(Token token, Guid tranID, CancelReason cancelReason);

        [OperationContract]
        TransactionError RejectCancelLmtOrder(Token token, Guid tranID, Guid accountId);

        [OperationContract]
        TransactionError CancelPlace(Token token, Guid tranID);

        [OperationContract]
        TransactionResult Execute(Token token, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID);

        [OperationContract]
        void ResetHit(Token token, Guid[] orderIDs);

        [OperationContract]
        bool UpdateInstrument(Token token, ParameterUpdateTask parameterUpdateTask);

        [OperationContract]
        bool UpdateDealingPolicyDetail(Token token, List<Dictionary<string, string>> dealingPolicyDic, List<Dictionary<string, string>> instrumentDic);

        [OperationContract]
        bool UpdatePolicyProcess(Token token, List<Dictionary<string, string>> customerFileValues, List<Dictionary<string, string>> salesFileValues);

        [OperationContract]
        AccountInformation GetAcountInfo(Token token, Guid tranID);

        [OperationContract]
        List<AccountGroupGNP> GetGroupNetPosition(Token token, string permissionName, Guid[] accountIDs, Guid[] instrumentIDs, bool showActualQuantity, string[] blotterCodeSelecteds);

        [OperationContract]
        List<OpenInterestSummary> GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds);

        [OperationContract]
        List<OpenInterestSummary> GetOpenInterestAccountSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds);

        [OperationContract]
        List<OpenInterestSummary> GetOpenInterestOrderSummary(Token token, Guid accountId, AccountType accountType, Guid[] instrumentIDs, string[] blotterCodeSelecteds);

        [OperationContract]
        string GetAccounts(Token token, Guid[] accountIDs, bool includeTransactions);

        [OperationContract]
        Guid[] VerifyTransactions(Token token, Guid[] tranIDs);

        [OperationContract]
        bool ChangeSystemStatus(Token token, SystemStatus newStatus);

        [OperationContract, XmlSerializerFormat]
        TransactionError Delete(Token token, Guid orderID, bool notifyByEmail, out XmlNode affectedOrders, out XmlNode xmlAccount);
    }
}
