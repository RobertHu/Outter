using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Xml;

using iExchange.Common;
using System.Collections.Generic;

namespace iExchange.StateServer
{
    /// <summary>
    /// Summary description for Service.
    /// </summary>
    [WebService(Namespace = "http://www.omnicare.com/StateServer/")]
    public class Service : System.Web.Services.WebService
    {
        public Service()
        {
            //CODEGEN: This call is required by the ASP.NET Web Services Designer
            InitializeComponent();
        }

        #region Component Designer generated code

        //Required by the Web Services Designer 
        private IContainer components = null;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        protected StateServer StateServer
        {
            get { return (StateServer)Application["StateServer"]; }
        }


        [WebMethod(Description = "Only for service test")]
        public string HelloWorld()
        {
            //System.Threading.Thread.Sleep(TimeSpan.FromMinutes(30));
            return "Hello World";
        }

        [WebMethod(Description = "Get this.StateServer's inner state.")]
        public XmlNode GetState()
        {
            return this.StateServer.GetState();
        }

        [WebMethod(Description = "Client must use this method to register itself before using this service")]
        public bool Register(Token token, string url)
        {
            return this.StateServer.Register(token, url);
        }

        [WebMethod(Description = "Client must use this method to Unregister itself after using this service")]
        public bool UnRegister(Token token, string url)
        {
            return this.StateServer.UnRegister(token, url);
        }

        /*
        [WebMethod(Description="NOTE: password is hash of the password user inputed")]
        public bool Login(ref Token token,string loginID,byte[] password)
        {
            StateServer this.StateServer=(StateServer)Application["StateServer"];
            if(loginID==null)
            {
                return this.StateServer.Login(token);
            }
            else
            {
                return this.StateServer.Login(ref token,loginID,password);
            }
        }
*/

        [WebMethod(Description = "Login")]
        public bool Login(Token token)
        {
            return this.StateServer.Login(token);
        }

        [WebMethod(Description = "NOTE: it's recommand to logout for security")]
        public bool Logout(Token token)
        {
            return this.StateServer.Logout(token);
        }

        [WebMethod(Description = "NOTE: password is hash of the password user inputed")]
        public bool ChangePassword(Token token, byte[] oldPassword, byte[] newPassword)
        {
            return this.StateServer.ChangePassword(token, oldPassword, newPassword);
        }

        [WebMethod(Description = "Get all init data and current commandSequence")]
        public DataSet GetInitData(Token token, XmlNode permittedKeys, out int commandSequence)
        {
            return this.StateServer.GetInitData(token, permittedKeys, out commandSequence);
        }

        [WebMethod(Description = "Get all init data and current commandSequence")]
        public DataSet GetInitData2(Token token, XmlNode permittedKeys, XmlNode permittedKeys2, out int commandSequence)
        {
            return this.StateServer.GetInitData2(token, permittedKeys, permittedKeys2, out commandSequence);
        }

        [WebMethod(Description = "QuotationColector send lost quotation to QuotationServer")]
        public bool ReplayQuotation(Token token, string quotation)
        {
            return this.StateServer.ReplayQuotation(token, quotation);
        }

        [WebMethod(Description = "QuotationColector and DealingConsole send quotation to QuotationServer")]
        public bool SetQuotation(Token token, string quotation)
        {
            return this.StateServer.SetQuotation(token, quotation);
        }

        [WebMethod(Description = "Flush all quotations in quotationServer memory to database")]
        public bool FlushQuotations(Token token, string quotation)
        {
            return this.StateServer.FlushQuotations(token);
        }

        [WebMethod(Description = "QuotationColector send best limit to QuotationServer")]
        public bool SetBestLimit(Token token, BestLimit[] bestLimits)
        {
            return this.StateServer.SetBestLimit(token, bestLimits);
        }

        [WebMethod(Description = "DealingConsole discard quotation to QuotationServer")]
        public bool DiscardQuotation(Token token, Guid instrumentID)
        {
            return this.StateServer.DiscardQuotation(token, instrumentID);
        }

        //<Instrument ID="" OriginType="" HitTimes="" ... />
        [WebMethod(Description = "DealingConsole update instrument parameters")]
        public bool UpdateInstrument(Token token, XmlNode instrument)
        {
            return this.StateServer.UpdateInstrument(token, instrument);
        }

        [WebMethod(Description = "DealingConsole update DealingPolicyDetail")]
        public bool UpdateDealingPolicyDetail(Token token, XmlNode dealingPolicyDetail)
        {
            return this.StateServer.UpdateDealingPolicyDetail(token, dealingPolicyDetail);
        }

        [WebMethod(Description = "DealingConsole update CustomerPolicy")]
        public bool UpdateCustomerPolicy(Token token, XmlNode customerPolicyNodes)
        {
            return this.StateServer.UpdateCustomerPolicy(token, customerPolicyNodes);
        }
        [WebMethod(Description = "DealingConsole update EmployeePolicy")]
        public bool UpdateEmployeePolicy(Token token, XmlNode employeePolicyNodes)
        {
            return this.StateServer.UpdateEmployeePolicy(token, employeePolicyNodes);
        }

        //<QuotePolicy ID="" InstrumentID="" PriceType="" ... />
        [WebMethod(Description = "DealingConsole update QuotePolicy parameters")]
        public bool UpdateQuotePolicy(Token token, XmlNode quotePolicy, out int error)
        {
            return this.StateServer.UpdateQuotePolicy(token, quotePolicy, out error);
        }

        [WebMethod(Description = "DealingConsole update QuotePolicies parameter")]
        public bool UpdateQuotePolicies(Token token, XmlNode quotePolicies, out int error)
        {
            return this.StateServer.UpdateQuotePolicies(token, quotePolicies, out error);
        }

        [WebMethod(Description = "DealingConsole and TradingConsole get instrument for Setting")]
        public DataSet GetInstrumentForSetting(Token token)
        {
            return this.StateServer.GetInstrumentForSetting(token);
        }
        //<InstrumentSetting>
        //	<Instrument ID="" Sequence="">
        //	<Instrument ID="" Sequence="">
        //</InstrumentSetting>
        [WebMethod(Description = "DealingConsole and TradingConsole update InstrumentSetting")]
        public DataSet UpdateInstrumentSetting(Token token, XmlNode instrumentSetting)
        {
            return this.StateServer.UpdateInstrumentSetting(token, instrumentSetting);
        }

        [WebMethod(Description = "DealingConsole and TradingConsole update InstrumentSetting")]
        public DataSet UpdateInstrumentSettingSupportMulitQuotePolicy(Token token, XmlNode instrumentSetting)
        {
            return this.StateServer.UpdateInstrumentSetting(token, null, instrumentSetting, true);
        }

        //Added by Michael on 2008-04-17
        [WebMethod(Description = "TradingMonitor update InstrumentSetting")]
        public DataSet UpdateInstrumentSetting2(Token token, XmlNode permittedKeys, XmlNode instrumentSetting)
        {
            return this.StateServer.UpdateInstrumentSetting(token, permittedKeys, instrumentSetting);
        }

        //<Orders>
        //	<Order ID="" HitCount="" BestPrice="" BestTime="" />
        //	<Order ID="" HitCount="" BestPrice="" BestTime="" />
        //</Orders>
        [WebMethod(Description = "DealingConsole update Order's HitCount,BestPrice,BestTime")]
        public bool UpdateOrder(Token token, XmlNode orders)
        {
            return this.StateServer.UpdateOrder(token, orders);
        }

        //<UpdateAccountLock AgentAccountID="" >
        //	<Account ID="" IsLocked="true" />
        //	<Account ID="" IsLocked="false" />
        //</UpdateAccountLock>
        [WebMethod(Description = "TradingConsole set accounts lock state")]
        public bool UpdateAccountLock(Token token, XmlNode accountLockChanges)
        {
            return this.StateServer.UpdateAccountLock(token, accountLockChanges);
        }

        [WebMethod(Description = "TradingConsole quote price to DealingConsole")]
        public void Quote(Token token, Guid instrumentID, double quoteLot, int BSStatus)
        {
            int setPriceMaxMovePips = 0;
            this.StateServer.Quote(token, instrumentID, quoteLot, BSStatus, setPriceMaxMovePips);
        }

        [WebMethod(Description = "TradingConsole Quote Price with SetPriceMaxMovePips to DealingConsole")]
        public void QuoteWithSetPriceMaxMovePips(Token token, Guid instrumentID, double quoteLot, int BSStatus, int setPriceMaxMovePips)
        {
            this.StateServer.Quote(token, instrumentID, quoteLot, BSStatus, setPriceMaxMovePips);
        }

        [WebMethod(Description = "TradingConsole quote2 price to DealingConsole")]
        public void Quote2(Token token, Guid instrumentID, double buyQuoteLot, double sellQuoteLot, int tick)
        {
            this.StateServer.Quote2(token, instrumentID, buyQuoteLot, sellQuoteLot, tick);
        }

        [WebMethod(Description = "TradingConsole cancel quote price to DealingConsole")]
        public void CancelQuote(Token token, Guid instrumentID, double buyQuoteLot, double sellQuoteLot)
        {
            this.StateServer.CancelQuote(token, instrumentID, buyQuoteLot, sellQuoteLot);
        }

        [WebMethod(Description = "RiskMonitor chat to Trading Console")]
        public void Chat(Token token, XmlNode message)
        {
            this.StateServer.Chat(token, message);
        }

        //		<Instrument ID="" Origin="">
        //			<Customer ID="" Ask="" Bid="" QuoteLot=""/>
        //			<Customer ID="" Ask="" Bid="" QuoteLot=""/>
        //		</Instrument>
        [WebMethod(Description = "DealingConsole answer TradingConsole")]
        public void Answer(Token token, XmlNode quotation)
        {
            this.StateServer.Answer(token, quotation);
        }

        //From TradingConsole
        //<Transaction ID="" AccountID="" InstrumentID="" Type="" OrderType="" BeginTime="" EndTime="" SubmitTime="" SubmitorID="" >
        //	<Order ID="" TradeOption="" IsOpen="" IsBuy="" SetPrice="" Lot="" >
        //		<OrderRelation OpenOrderID="" ClosedLot="" />
        //		<OrderRelation OpenOrderID="" ClosedLot="" />
        //	</Order>
        //</Transaction>	
        [WebMethod(Description = "TradingConsole place transaction")]
        public TransactionError Place(Token token, XmlNode tran, out string tranCode)
        {
            return this.StateServer.Place(token, tran, out tranCode);
        }


        [WebMethod(Description = "Delivery physical for Trader, Physical Termianl")]
        public TransactionError ApplyDelivery(Token token, ref XmlNode deliveryRequire, out string code, out string balance, out string usableMargin)
        {
            return this.StateServer.ApplyDelivery(token, ref deliveryRequire, out code, out balance, out usableMargin);
        }

        [WebMethod(Description = "Cancel Delivery physical for Physical Termianl")]
        public bool CancelDelivery(Token token, Guid deliveryRequestId, string title, string notifyMessage)
        {
            return this.StateServer.CancelDelivery(token, deliveryRequestId, title, notifyMessage);
        }

        [WebMethod(Description = "Physical delivery request approved")]
        public bool NotifyDeliveryApproved(Token token, Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            return this.StateServer.NotifyDeliveryApproved(token, accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime, title, notifyMessage);
        }

        [WebMethod(Description = "Physical is ready to delivery for Physical Termianl")]
        public bool NotifyDelivery(Token token, Guid deliveryRequestId, DateTime deliveryTime, string title, string notifyMessage)
        {
            return this.StateServer.NotifyDelivery(token, deliveryRequestId, deliveryTime, title, notifyMessage);
        }

        [WebMethod(Description = "Physical notify instrument is delivered")]
        public bool NotifyDelivered(Token token, Guid deliveryRequestId, Guid accountId)
        {
            return this.StateServer.NotifyDelivered(token, deliveryRequestId, accountId);
        }


        [WebMethod]
        public bool NotifyScrapDeposit(Token token, XmlNode scrapDeposit, string title, string notifyMessage)
        {
            return this.StateServer.NotifyScrapDeposit(token, scrapDeposit, title, notifyMessage);
        }

        [WebMethod]
        public bool NotifyScrapDepositCanceled(Token token, XmlNode scrapDepositCancel)
        {
            return this.StateServer.NotifyScrapDepositCanceled(token, scrapDepositCancel);
        }

        [WebMethod]
        public TransactionError Cancel(Token token, Guid tranID, CancelReason cancelReason)
        {
            return this.StateServer.Cancel(token, tranID, cancelReason);
        }

        //Added by Michael on 2005-11-24
        [WebMethod(Description = "Send email")]
        public void Email(Token token, string typeDesc, Guid transactionID, string fromEmail, string toEmails, string subject, string body)
        {
            this.StateServer.Email(token, typeDesc, transactionID, fromEmail, toEmails, subject, body);
        }

        //Added by Michael on 2005-04-06
        [WebMethod(Description = "DealingConsole Reject CancelLmtOrder")]
        public TransactionError RejectCancelLmtOrder(Token token, Guid tranID, Guid accountID)
        {
            return this.StateServer.RejectCancelLmtOrder(token, tranID, accountID);
        }

        [WebMethod(Description = "DealingConsole execute transaction")]
        public TransactionError Execute(Token token, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID, out XmlNode xmlTran)
        {
            return this.StateServer.Execute(token, tranID, buyPrice, sellPrice, lot, executedOrderID, out xmlTran);
        }

        [WebMethod(Description = "TradingConsole multipleClose order")]
        public TransactionError MultipleClose(Token token, Guid[] orderIds, out XmlNode xmlTran, out XmlNode xmlAccount)
        {
            return this.StateServer.MultipleClose(token, orderIds, out xmlTran, out xmlAccount);
        }

        [WebMethod(Description = "TradingConsole Assign order")]
        public TransactionError Assign(Token token, ref XmlNode xmlTran, out XmlNode xmlAccount, out XmlNode xmlInstrument)
        {
            return this.StateServer.Assign(token, ref xmlTran, out xmlAccount, out xmlInstrument);
        }

        [WebMethod(Description = "Book transaction")]
        public TransactionError Book(Token token, ref XmlNode xmlTran, bool preserveCalculation, out XmlNode xmlAccount, out XmlNode xmlAffectedOrders)
        {
            return this.StateServer.Book(token, ref xmlTran, preserveCalculation, out xmlAccount, out xmlAffectedOrders);
        }

        [WebMethod(Description = "RiskMonitor delete order already executed")]
        public TransactionError Delete(Token token, bool notifyByEmail, bool isPayForInstalmentDebitInterest, Guid orderID, out XmlNode affectedOrders, out XmlNode xmlAccount)
        {
            return this.StateServer.Delete(token, orderID, notifyByEmail, isPayForInstalmentDebitInterest, out affectedOrders, out xmlAccount);
        }

        [WebMethod(Description = "RiskMonitor delete order already executed")]
        public TransactionError DeleteForNewVersion(Token token, Guid accountId, Guid orderID, bool notifyByEmail, bool isPayForInstalmentDebitInterest)
        {
            return this.StateServer.DeleteForNewVersion(token, notifyByEmail, isPayForInstalmentDebitInterest, accountId, orderID);
        }

        [WebMethod(Description = "Dealer Reset Hit")]
        public void ResetHit(Token token, Guid[] orderIDs)
        {
            this.StateServer.ResetHit(token, orderIDs);
        }

        [WebMethod(Description = "RiskMonitor Reset AlertLevel")]
        public bool ResetAlertLevel(Token token, Guid[] accountIDs)
        {
            return this.StateServer.ResetAlertLevel(token, accountIDs);
        }

        [WebMethod(Description = "Get Accounts and Transactions of the Accounts")]
        public XmlNode GetAccounts(Token token, Guid[] accountIDs, bool includeTransactions)
        {
            return this.StateServer.GetAccounts(token, accountIDs, includeTransactions, false);
        }

        [WebMethod]
        public XmlNode GetAccounts5(Token token, Guid[] accountIDs, Guid[] instrumentIDs, bool includeTransactions)
        {
            return this.StateServer.GetAccounts(token, accountIDs, instrumentIDs, includeTransactions);
        }

        [WebMethod]
        public XmlNode GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            return this.StateServer.GetOpenInterestInstrumentSummary(token, isGroupByOriginCode, blotterCodeSelecteds);
        }

        [WebMethod]
        public XmlNode GetOpenInterestSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            return this.StateServer.GetOpenInterestSummary(token, accountIDs, instrumentIDs, blotterCodeSelecteds);
        }

        [WebMethod]
        public XmlNode GetOpenInterestSummaryOrderList(Token token, Guid accountId, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            return this.StateServer.GetOpenInterestSummaryOrderList(token, accountId, instrumentIDs, blotterCodeSelecteds);
        }

        [WebMethod]
        public XmlNode GetGroupNetPosition(Token token, string permissionName, Guid[] accountIDs, Guid[] instrumentIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            return this.StateServer.GetGroupNetPosition(token, permissionName, accountIDs, instrumentIDs, showActualQuantity, blotterCodeSelecteds);
        }

        [WebMethod]
        public XmlNode GetGroupNetPositionInstrument(Token token, string permissionName, Guid accountId, Guid instrumentId, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            return this.StateServer.GetGroupNetPositionInstrument(token, permissionName, accountId, instrumentId, showActualQuantity, blotterCodeSelecteds);
        }

        [WebMethod(Description = "Get Accounts and Cut Transactions of the Accounts")]
        public XmlNode GetAccountsForCut(Token token, Guid[] accountIDs, bool includeTransactions)
        {
            return this.StateServer.GetAccounts(token, accountIDs, includeTransactions, true);
        }

        [WebMethod]
        public XmlNode GetAccountsForInit(Guid[] accountIDs)
        {
            return this.StateServer.GetAccountsForInit(null, accountIDs);
        }

        [WebMethod]
        public XmlNode GetOrdersForGetAutoPrice(Guid[] orderIDs)
        {
            return this.StateServer.GetOrdersForGetAutoPrice(orderIDs);
        }

        [WebMethod(Description = "Backoffice change the paramters")]
        public bool Update(Token token, XmlNode update)
        {
            return this.StateServer.Update(token, update);
        }

        [WebMethod]
        public DataSet RedoLimitOrder(Token token, XmlNode orderIds, out TransactionError returnValue)
        {
            return this.StateServer.RedoLimitOrder(token, orderIds, out returnValue);
        }

        [WebMethod(Description = "TNU batch change the paramters")]
        public bool UpdateFromTNU(XmlNode[] updates)
        {
            bool result = true;
            Token token = new Token(Guid.Empty, UserType.System, AppType.BackOffice);

            foreach (XmlNode node in updates)
            {
                if (!this.StateServer.Update(token, node))
                {
                    result = false;
                }
            }
            return result;
        }

        [WebMethod(Description = "Backoffice change the paramters")]
        public bool Update1(string sXml)
        {
            try
            {
                Token token = new Token(Guid.Empty, UserType.System, AppType.BackOffice);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(sXml);
                XmlNode update = xmlDoc.DocumentElement;
                return this.StateServer.Update(token, update);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        [WebMethod]
        public bool SetActiveSource(Token token, string quotationSource)
        {
            return this.StateServer.SetActiveSource(token, quotationSource);
        }

        //Added by Michael on 2008-05-26
        [WebMethod(true)]
        public bool SetActiveSourceInstrument(Token token, XmlNode sourceInstrumentNodes)
        {
            return this.StateServer.SetActiveSourceInstrument(token, sourceInstrumentNodes);
        }

        [WebMethod]
        public Guid[] VerifyTransaction(Token token, Guid[] transactionIDs)
        {
            return this.StateServer.VerifyTransaction(token, transactionIDs);
        }

        //Added by Michael on 2005-02-23
        [WebMethod(Description = "Get Accounts for JakartaFX use")]
        public DataSet GetAccounts2(Token token)
        {
            return this.StateServer.GetAccounts(token);
        }

        //Added by Michael on 2005-02-23
        //different region datetime will error at the different PC(Client & Server PC)
        [WebMethod(Description = "Get Orders for JakartaFX use")]
        //public DataSet GetOrders(Token token,XmlNode xmlAccounts,DateTime tradeDay)
        public DataSet GetOrders(Token token, XmlNode xmlAccounts, string tradeDay)
        {
            return this.StateServer.GetOrders(token, xmlAccounts, tradeDay);
        }


        [WebMethod(Description = "DealingConsole accept transaction placing")]
        public TransactionError AcceptPlace(Token token, Guid tranID)
        {
            return this.StateServer.AcceptPlace(token, tranID);
        }

        /// <returns>		
        ///		<Account ID="" Balance="" Equity="" Necessary="">
        ///			<Instrument ID="" BuyLotBalanceSum="" SellLotBalanceSum="" />
        ///		</Account>
        /// </returns>
        [WebMethod(Description = "DealingConsole get account info before confirm")]
        public XmlNode GetAcountInfo(Token token, Guid tranID)
        {
            return this.StateServer.GetAcountInfo(token, tranID);
        }

        [WebMethod(Description = "RiskMonitor Balance Necessary Equity Exclude Alerted when init data")]
        public XmlNode GetMemoryBalanceNecessaryEquityExcludeAlerted(Token token)
        {
            return this.StateServer.GetMemoryBalanceNecessaryEquityExcludeAlerted(token);
        }

        //add by Korn 2008-03-24
        [WebMethod(Description = "QuotationColector send newsCollection to TradingConsole")]
        public bool SetNews(Token token, News[] newsCollection)
        {
            return this.StateServer.SetNews(token, newsCollection);
        }

        [WebMethod(true)]
        public void UpdateHighLow(Token token, string ip, Guid instrumentId, bool isOriginHiLo, string newInput, bool isUpdateHigh, out int batchProcessId, out string instrumentCode, out bool highBid, out bool lowBid, out DateTime updateTime, out int returnValue, out string errorMessage)
        {
            this.StateServer.UpdateHighLow(token, ip, instrumentId, isOriginHiLo, newInput, isUpdateHigh, out batchProcessId, out instrumentCode, out highBid, out lowBid, out updateTime, out returnValue, out errorMessage);
        }

        [WebMethod(true)]
        public void RestoreHighLow(Token token, string ip, int batchProcessId, out Guid instrumentId, out string instrumentCode, out string newInput, out bool isUpdateHigh, out bool highBid, out bool lowBid, out int returnValue, out string errorMessage)
        {
            this.StateServer.RestoreHighLow(token, ip, batchProcessId, out instrumentId, out instrumentCode, out newInput, out isUpdateHigh, out highBid, out lowBid, out returnValue, out errorMessage);
        }

        [WebMethod(Description = "DealingConsole send fixed OverridedQuotationHistory to QuotationServer,include action: Insert,Delete,Modify")]
        public bool FixOverridedQuotationHistory(Token token, string quotation, bool needApplyAutoAdjustPoints)
        {
            return this.StateServer.FixOverridedQuotationHistory(token, quotation, needApplyAutoAdjustPoints);
        }

        //unused
        //add by Korn 2008-9-6 
        [WebMethod(Description = "DealingConsole send modified HistoryQuotation to QuotationServer")]
        public bool SetHistoryQuotation(Token token, DateTime tradeDay, string quotation, bool needApplyAutoAdjustPoints)
        {
            return this.StateServer.SetHistoryQuotation(token, tradeDay, quotation, needApplyAutoAdjustPoints);
        }

        //add by Korn 2008-9-6
        [WebMethod(Description = "DealingConsole send modified OverridedQuotationHighLow to QuotationServer")]
        public bool UpdateOverridedQuotationHighLow(Token token, Guid instrumentID, string quotation)
        {
            return this.StateServer.UpdateOverridedQuotationHighLow(token, instrumentID, quotation);
        }

        [WebMethod]
        public XmlNode GetAccountStatus(Token token, Guid accountId, Guid instrumentId, OrderType orderType, bool needOutputPrice, out string buyPrice, out string sellPrice)
        {
            return this.StateServer.GetAccountStatus(token, accountId, instrumentId, orderType, needOutputPrice, out buyPrice, out sellPrice);
        }

        [WebMethod]
        public Common.MatchInfoCommand[] GetMatchInfoCommands(Guid[] instrumentIds)
        {
            return this.StateServer.GetMatchInfoCommands(instrumentIds);
        }

        [WebMethod]
        public bool ChangeLeverage(Token token, Guid accountId, int leverage, out decimal necessary)
        {
            return this.StateServer.ChangeLeverage(token, accountId, leverage, out necessary);
        }

        [WebMethod]
        public bool NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            return this.StateServer.NotifyPasswordChanged(customerId, loginName, newPassword);
        }

        [WebMethod(true)]
        public TransactionError InstalmentPayoff(Token token, Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, string instalmentXml, string terminateXml)
        {
            return this.StateServer.InstalmentPayoff(token, submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalmentXml, terminateXml);
        }

        [WebMethod(true)]
        public TransactionError InstalmentUndoPayoff(Token token, Guid submitorId, Guid accountId, string undoInstalmentXml)
        {
            return this.StateServer.InstalmentUndoPayoff(token, submitorId, accountId, undoInstalmentXml);
        }

        [WebMethod(true)]
        public bool SetPriceAlerts(Token token, XmlNode priceAlertsNode)
        {
            return this.StateServer.SetPriceAlerts(token, priceAlertsNode);
        }

        [WebMethod]
        public bool NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            return this.StateServer.NotifyTelephonePinReset(customerId, accountId, verificationCode);
        }

        [WebMethod(true)]
        public TransactionError ApplyTransfer(Token token, Guid sourceAccountID, Guid sourceCurrencyID,
            decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
            decimal rate, DateTime expireDate)
        {
            return this.StateServer.ApplyTransfer(token, sourceAccountID, sourceCurrencyID,
                    sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
        }

        [WebMethod(true)]
        public TransactionError AcceptTransfer(Token token, Guid transferID)
        {
            return this.StateServer.AcceptTransfer(token, transferID);
        }

        [WebMethod(true)]
        public TransactionError DeclineTransfer(Token token, Guid transferID)
        {
            return this.StateServer.DeclineTransfer(token, transferID);
        }

        [WebMethod(Description = "Dealer hit orders manually")]
        public Guid[] Rehit(Token token, Guid[] orderIDs, string[] hitPrices)
        {
            return this.StateServer.Rehit(token, orderIDs, hitPrices);
        }

        [WebMethod(Description = "Get last cycle minute chart data")]
        public MinuteChartData[] GetMinuteChartData(Token token, Guid? instrumentId, Guid? quotePolicyId)
        {
            return this.StateServer.GetMinuteChartData(instrumentId, quotePolicyId);
        }

        [WebMethod(true)]
        public void RecordLoginInfo(Guid userId, Common.AppType appType, string ip)
        {
            AppDebug.LogEvent("StateServer", string.Format("RecordLoginInfo userId = {0}, appType = {1}, ip = {2}", userId, appType, ip), EventLogEntryType.Information);
        }

        [WebMethod(true)]
        public void RecordLogoutInfo(Guid userId, Common.AppType appType)
        {
            AppDebug.LogEvent("StateServer", string.Format("RecordLogoutInfo userId = {0}, appType = {1}", userId, appType), EventLogEntryType.Information);
        }

        [WebMethod(true)]
        public void UpdateLoginInfo(Common.TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            //<LoginInfos>
            // <LoginInfo UserID="" LoginTime="" IpAddress="" AppType=""/>
            //</LoginInfos>
            this.StateServer.UpdateLoginInfo(appType, onlineXml, expireTime);
        }

        [WebMethod(true)]
        public void SetDailyClosePrices(Token token, List<InstrumentDailyClosePriceInfo> closeQuotations)
        {
            this.StateServer.SetDailyClosePrices(token, closeQuotations);
        }
    }
}
