using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Xml;
using ManagerService.Exchange;
using iExchange.Common;
using iExchange.Common.Manager;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Configuration;

namespace iExchange.StateServer.Manager
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Callback : IStateServer
    {
        private Adapter.KickoutServiceProxy _proxy = new Adapter.KickoutServiceProxy(ConfigurationManager.AppSettings["TraderService_KickoutServiceUrl"]);

        private class InstrumentState
        {
            public Guid InstrumentId;
            public bool? IsPriceEnabled;
            public bool? IsAutoEnablePrice;
        }

        public TransactionError Book(Token token, string xmlTran, bool preserveCalculation)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlTran);
            XmlNode xmlOrder = doc.FirstChild;
            XmlNode xmlAccount;
            XmlNode xmlAffectedOrders;
            return Global.StateServer.Book(token, ref xmlOrder, preserveCalculation, out xmlAccount, out xmlAffectedOrders);
        }

        public TransactionError[] BookOrders(Token token, string[] xmlTrans)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement[] xmlTrans2 = new XmlElement[xmlTrans.Length];
            for (int index = 0; index < xmlTrans.Length; index++)
            {
                doc.LoadXml(xmlTrans[index]);
                xmlTrans2[index] = doc.DocumentElement;
            }
            return Global.StateServer.Book(token, xmlTrans2);
        }

        public bool ResetServerAndKickoffAllTrader(Token token, int timeout)
        {
            try
            {
                return Global.StateServer.ResetServerAndKickoffAllTrader(token, timeout);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.ResetServerAndKickoffAllTrader", string.Format("{0}", ex), EventLogEntryType.Error);
                return false;
            }
        }

        public bool SwitchPriceState(string[] originCodes, bool enable, out Guid[] affectInstrumentIds)
        {
            affectInstrumentIds = null;
            List<Guid> affectInstrumentIdList = new List<Guid>();
            try
            {
                string sql = "SELECT ID,IsPriceEnabled FROM Instrument WHERE OriginCode=@originCode";
                List<InstrumentState> updateItems = new List<InstrumentState>();
                foreach (string originCode in originCodes)
                {
                    DataAccessHelper.ExecuteReader(sql, CommandType.Text, delegate(SqlDataReader reader)
                    {
                        while (reader.Read())
                        {
                            InstrumentState instrumentState = new InstrumentState { InstrumentId = (Guid)reader["ID"], IsPriceEnabled = (bool)reader["IsPriceEnabled"] };
                            if (instrumentState.IsPriceEnabled == enable) continue;
                            instrumentState.IsPriceEnabled = enable;
                            updateItems.Add(instrumentState);
                        }
                    }, new SqlParameter("@originCode", originCode));
                }

                if (updateItems.Count > 0)
                {
                    XmlDocument doc = new XmlDocument();
                    XmlNode instruments = doc.CreateNode(XmlNodeType.Element, "Instruments", null);
                    XmlAttribute attribute;
                    foreach (InstrumentState instrumentState in updateItems)
                    {
                        XmlNode instrument = doc.CreateNode(XmlNodeType.Element, "Instrument", null);
                        attribute = doc.CreateAttribute("ID");
                        attribute.Value = instrumentState.InstrumentId.ToString();
                        instrument.Attributes.Append(attribute);
                        attribute = doc.CreateAttribute("IsPriceEnabled");
                        attribute.Value = XmlConvert.ToString(instrumentState.IsPriceEnabled.Value);
                        instrument.Attributes.Append(attribute);
                        instruments.AppendChild(instrument);
                        affectInstrumentIdList.Add(instrumentState.InstrumentId);
                    }

                    Token token = new Token(Guid.Empty, UserType.System, AppType.Manager);
                    if (Global.StateServer.UpdateInstrument(token, instruments))
                    {
                        affectInstrumentIds = affectInstrumentIdList.ToArray();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.SwitchPriceState",
                    string.Format("originCodes:{0},enable:{1}\r\n{2}", string.Join(",", originCodes), enable, ex.ToString()), EventLogEntryType.Error);
            }
            return false;
        }

        public bool SuspendResume(string[] originCodes, bool resume, out Guid[] affectInstrumentIds)
        {
            affectInstrumentIds = null;
            List<Guid> affectInstrumentIdList = new List<Guid>();
            try
            {
                string sql = "SELECT ID,IsPriceEnabled,IsAutoEnablePrice FROM Instrument WHERE OriginCode=@originCode";
                List<InstrumentState> updateItems = new List<InstrumentState>();
                foreach (string originCode in originCodes)
                {
                    DataAccessHelper.ExecuteReader(sql, CommandType.Text, delegate(SqlDataReader reader)
                    {
                        while (reader.Read())
                        {
                            InstrumentState instrumentState = new InstrumentState() { InstrumentId = (Guid)reader["ID"], IsPriceEnabled = (bool)reader["IsPriceEnabled"], IsAutoEnablePrice = (bool)reader["IsAutoEnablePrice"] };
                            if (instrumentState.IsPriceEnabled == resume && instrumentState.IsAutoEnablePrice == resume) continue;
                            instrumentState.IsPriceEnabled = instrumentState.IsPriceEnabled == resume ? null : (bool?)resume;
                            instrumentState.IsAutoEnablePrice = instrumentState.IsAutoEnablePrice == resume ? null : (bool?)resume;
                            updateItems.Add(instrumentState);
                        }
                    }, new SqlParameter("@originCode", originCode));
                }
                if (updateItems.Count > 0)
                {
                    Token token = new Token(Guid.Empty, UserType.System, AppType.Manager);
                    if (Global.StateServer.UpdateInstrument(token, this.GetInstrumentUpdateXmlNode(updateItems, affectInstrumentIdList)))
                    {
                        affectInstrumentIds = affectInstrumentIdList.ToArray();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.SuspendResume",
                    string.Format("originCodes:{0},resume:{1}\r\n{2}", string.Join(",", originCodes), resume, ex.ToString()), EventLogEntryType.Error);
            }
            return false;
        }

        private XmlNode GetInstrumentUpdateXmlNode(List<InstrumentState> instrumentStates, List<Guid> affectInstrumentIdList)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode instruments = doc.CreateNode(XmlNodeType.Element, "Instruments", null);
            XmlAttribute attribute;
            foreach (var instrumentState in instrumentStates)
            {
                XmlNode instrument = doc.CreateNode(XmlNodeType.Element, "Instrument", null);
                attribute = doc.CreateAttribute("ID");
                attribute.Value = instrumentState.InstrumentId.ToString();
                instrument.Attributes.Append(attribute);

                if (instrumentState.IsPriceEnabled.HasValue)
                {
                    attribute = doc.CreateAttribute("IsPriceEnabled");
                    attribute.Value = XmlConvert.ToString(instrumentState.IsPriceEnabled.Value);
                    instrument.Attributes.Append(attribute);
                }

                if (instrumentState.IsAutoEnablePrice.HasValue)
                {
                    attribute = doc.CreateAttribute("IsAutoEnablePrice");
                    attribute.Value = XmlConvert.ToString(instrumentState.IsAutoEnablePrice.Value);
                    instrument.Attributes.Append(attribute);
                }
                instruments.AppendChild(instrument);
                affectInstrumentIdList.Add(instrumentState.InstrumentId);
            }
            return instruments;
        }


        public void Update(Token token, string updateXml)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(updateXml);
                Global.StateServer.Update(token, xmlDocument.DocumentElement);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Update",
                    string.Format("udpateNode:{0}\r\n{2}", updateXml, ex.ToString()), EventLogEntryType.Error);
            }
        }

        public void BroadcastQuotation(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            try
            {
                Global.StateServer.Broadcast(token, originQs, overridedQs);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.BroadcastQuotation",
                    string.Format("udpateNode:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public bool UpdateInstrument(Token token, ParameterUpdateTask parameterUpdateTask)
        {
            try
            {
                XmlNode instrumentNode = ManagerHelper.GetInstrumentParametersXml(parameterUpdateTask);
                return Global.StateServer.UpdateInstrument(token, instrumentNode);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.UpdateInstrument",
                    string.Format("UpdateInstrument:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateDealingPolicyDetail(Token token, List<Dictionary<string, string>> dealingPolicyDic, List<Dictionary<string, string>> instrumentDic)
        {
            try
            {
                bool isDealingPolicy = true;
                bool isInstrument = true;
                if (dealingPolicyDic.Count > 0)
                {
                    XmlNode dealingPolicyNode = ManagerHelper.GetUpdateNodeFromDic(dealingPolicyDic, "DealingPolicyDetail");
                    isDealingPolicy = Global.StateServer.UpdateDealingPolicyDetail(token, dealingPolicyNode);
                }

                if (instrumentDic.Count > 0)
                {
                    XmlNode instrumentNode = ManagerHelper.GetUpdateNodeFromDic(instrumentDic, "Instrument");
                    isInstrument = Global.StateServer.UpdateInstrument(token, instrumentNode);
                }

                return (isDealingPolicy && isInstrument);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.UpdateDealingPolicyDetail",
                    string.Format("UpdateDealingPolicyDetail:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdatePolicyProcess(Token token, List<Dictionary<string, string>> customerFileValues, List<Dictionary<string, string>> salesFileValues)
        {
            bool isOk = false;
            bool isCustomerOk = true;
            bool isSalesOk = true;

            try
            {

                if (customerFileValues != null && customerFileValues.Count > 0)
                {
                    isCustomerOk = false;
                    XmlNode customerPolicyNode = ManagerHelper.GetUpdateNodeFromDic(customerFileValues, "Customer");
                    isCustomerOk = Global.StateServer.UpdateCustomerPolicy(token, customerPolicyNode);
                }
                if (salesFileValues != null && salesFileValues.Count > 0)
                {
                    isSalesOk = false;
                    XmlNode employeePolicyNode = ManagerHelper.GetUpdateNodeFromDic(salesFileValues, "Employee");
                    isSalesOk = Global.StateServer.UpdateEmployeePolicy(token, employeePolicyNode);
                }
                isOk = (isCustomerOk && isSalesOk);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.UpdatePolicyProcess",
                    string.Format("UpdatePolicyProcess:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return false;
            }
            return isOk;
        }

        public void Answer(Token token, List<Answer> answerQutos)
        {
            try
            {
                foreach (Answer answer in answerQutos)
                {
                    List<Answer> answers = new List<Common.Manager.Answer>();
                    answers.Add(answer);
                    XmlNode quotation = ManagerHelper.ConvertQuotationXml(answers);
                    Global.StateServer.Answer(token, quotation);
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Answer",
                    string.Format("Answer:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public TransactionError AcceptPlace(Token token, Guid tranID)
        {
            try
            {
                return Global.StateServer.AcceptPlace(token, tranID);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.AcceptPlace",
                    string.Format("AcceptPlace:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError Cancel(Token token, Guid tranID, CancelReason cancelReason)
        {
            try
            {
                return Global.StateServer.Cancel(token, tranID, cancelReason);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Cancel",
                    string.Format("Cancel:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError RejectCancelLmtOrder(Token token, Guid tranID, Guid accountId)
        {
            try
            {
                return Global.StateServer.RejectCancelLmtOrder(token, tranID, accountId);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.RejectCancelLmtOrder",
                    string.Format("RejectCancelLmtOrder:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError CancelPlace(Token token, Guid tranID)
        {
            try
            {
                return TransactionError.OK;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.CancelPlace",
                    string.Format("CancelPlace:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionResult Execute(Token token, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            try
            {
                TransactionResult transactionResult = new TransactionResult();
                XmlNode xmlTran = null;
                transactionResult.TransactionError = Global.StateServer.Execute(token, tranID, buyPrice, sellPrice, lot, executedOrderID, out xmlTran);
                //transactionResult.ExecutedTransaction = ManagerHelper.GetExecutedTransaction(xmlTran);
                return transactionResult;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Execute",
                    string.Format("Execute:Error tranID:{0},\r\n{1}", tranID, ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public void ResetHit(Token token, Guid[] orderIds)
        {
            try
            {
                Global.StateServer.ResetHit(token, orderIds);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.ResetHit",
                    string.Format("ResetHit:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public AccountInformation GetAcountInfo(Token token, Guid tranID)
        {
            try
            {
                XmlNode accountInforNode = Global.StateServer.GetAcountInfo(token, tranID);
                return ManagerHelper.GetAcountInfo(accountInforNode);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetAcountInfo",
                    string.Format("ResetHit:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetAccountsProfitWithin(Token token, decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            try
            {
                XmlNode node = Global.StateServer.GetAccountsProfitWithin(token, minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                //return ManagerHelper.GetGroupNetPosition(groupNetPositionXml);
                return node;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetAccountsProfitWithin",
                   string.Format("GetGroupNetPosition:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetGroupNetPosition(Token token, string permissionName, Guid[] accountIDs, Guid[] instrumentIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            try
            {
                XmlNode groupNetPositionXml = Global.StateServer.GetGroupNetPositionForManager(token, permissionName, accountIDs, instrumentIDs, showActualQuantity, blotterCodeSelecteds);
                //return ManagerHelper.GetGroupNetPosition(groupNetPositionXml);
                return groupNetPositionXml;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetGroupNetPosition",
                   string.Format("GetGroupNetPosition:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public List<OpenInterestSummary> GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            try
            {
                XmlNode instrumentSummaryNode = Global.StateServer.GetOpenInterestInstrumentSummary(token, isGroupByOriginCode, blotterCodeSelecteds);
                return ManagerHelper.GetOpenInterestInstrumentSummary(instrumentSummaryNode);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetOpenInterestInstrumentSummary",
                   string.Format("GetOpenInterestInstrumentSummary:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public List<OpenInterestSummary> GetOpenInterestAccountSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                XmlNode accountSummaryNode = Global.StateServer.GetOpenInterestSummary(token, accountIDs, instrumentIDs, blotterCodeSelecteds);
                return ManagerHelper.GetOpenInterestAccountSummary(accountSummaryNode);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetOpenInterestAccountSummary",
                   string.Format("GetOpenInterestAccountSummary:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public List<OpenInterestSummary> GetOpenInterestOrderSummary(Token token, Guid accountId, AccountType accountType, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                XmlNode orderSummaryNode = Global.StateServer.GetOpenInterestSummaryOrderList(token, accountId, instrumentIDs, blotterCodeSelecteds);
                return ManagerHelper.GetOpenInterestOrderSummary(orderSummaryNode, accountType);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetOpenInterestOrderSummary",
                   string.Format("GetOpenInterestOrderSummary:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public string GetAccounts(Token token, Guid[] accountIDs, bool includeTransactions)
        {
            try
            {
                XmlNode accountNodes = Global.StateServer.GetAccounts(token, accountIDs, includeTransactions, false);
                if (accountNodes != null)
                {
                    return accountNodes.OuterXml.ToString();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetAccounts",
                  string.Format("GetAccounts:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public Guid[] VerifyTransactions(Token token, Guid[] tranIDs)
        {
            return Global.StateServer.VerifyTransaction(token, tranIDs);
        }


        public bool ChangeSystemStatus(Token token, SystemStatus newStatus)
        {
            return Global.StateServer.ChangeSystemStatus(token, newStatus);
        }

        public TransactionError Delete(Token token, Guid orderID, bool notifyByEmail, out XmlNode affectedOrders, out XmlNode xmlAccount)
        {
            return Global.StateServer.Delete(token, orderID, notifyByEmail, false, out affectedOrders, out xmlAccount);
        }

        public void BrodcastTradingDailyQuotation(Token token, Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            try
            {
                Global.StateServer.SetDailyClosePrice(token, instrumentId, tradeDay, closeQuotations);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.SetDailyClosePrice",
                    string.Format("SetDailyClosePrice:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public void BrodcastTradingDailyQuotations(Token token, List<InstrumentDailyClosePriceInfo> closeQuotations)
        {
            try
            {
                Global.StateServer.SetDailyClosePrices(token, closeQuotations);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.SetDailyClosePrices",
                    string.Format("SetDailyClosePrices:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public Guid[] Rehit(Common.Token token, Guid[] orderIDs, string[] hitPrices, Guid[] accountIDs)
        {
            try
            {
                return Global.StateServer.Rehit(token, orderIDs, hitPrices, accountIDs);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Rehit",
                    string.Format("Rehit:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand()
        {
            try
            {
                return Global.StateServer.GetTradingInstrumentStatusCommand();
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.GetTradingInstrumentStatusCommand",
                    string.Format("GetTradingInstrumentStatusCommand:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public void BrodcastMinuteChartDataCommand(Token token, MinuteChartData[] minuteChartDatas)
        {
            try
            {
                Global.StateServer.BrodcastMinuteChartDataCommand(token, minuteChartDatas);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.BrodcastMinuteChartDataCommand",
                    string.Format("BrodcastMinuteChartDataCommand:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public bool Kickoff(Token token, Guid[] customerId, Guid[] employeeId)
        {
            try
            {
                return this.Kickoff(customerId, employeeId);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.Kickoff",
                    string.Format("Kickoff:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return false;
            }
        }

        private bool Kickoff(Guid[] customerId, Guid[] employeeId)
        {
            bool kickSuccess = true;
            try
            {
                if (customerId != null)
                {
                    foreach (var customer in customerId)
                    {
                        _proxy.Kickout(customer);
                    }
                }

                if (employeeId != null)
                {
                    foreach (var employee in employeeId)
                    {
                        _proxy.Kickout(employee);
                    }
                }

                kickSuccess = Global.StateServer.KickoffSL(customerId, employeeId);

                AppDebug.LogEvent("StateServer.Kickoff", string.Format("kickoff completed. customerid {0}, employeeid {1}, success= {2}.",
                    customerId == null ? string.Empty : string.Join(",", customerId), employeeId == null ? string.Empty : string.Join(",", employeeId), kickSuccess), EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.Kickoff",
                    string.Format("Kickoff:Error\r\n{0}", e.ToString()), EventLogEntryType.Error);
                return false;
            }
            return kickSuccess;
        }

        public bool UpdateCustomerDisallowlogin(Token token, List<Dictionary<string, string>> customerFileValues, List<Dictionary<string, string>> salesFileValues)
        {
            bool isOk = false;
            bool isCustomerOk = true;
            bool isSalesOk = true;

            try
            {
                if (customerFileValues != null && customerFileValues.Count > 0)
                {
                    isCustomerOk = false;
                    XmlNode customerPolicyNode = ManagerHelper.GetUpdateNodeFromDic(customerFileValues, "Customer");
                    isCustomerOk = Global.StateServer.UpdateCustomeDisallowLogin(token, customerPolicyNode);
                }
                if (salesFileValues != null && salesFileValues.Count > 0)
                {
                    isSalesOk = false;
                    XmlNode employeePolicyNode = ManagerHelper.GetUpdateNodeFromDic(salesFileValues, "Employee");
                    isSalesOk = Global.StateServer.UpdateEmployeeDisallowLogin(token, employeePolicyNode);
                }
                isOk = (isCustomerOk && isSalesOk);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerCallback.UpdateCustomerDisallowlogin",
                    string.Format("UpdateCustomerDisallowlogin:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
                return false;
            }
            return isOk;
        }

    }
}
