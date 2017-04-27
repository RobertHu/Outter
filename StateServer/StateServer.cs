using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Services.Protocols;
using System.Web.Script.Serialization;
using System.Xml;

using iExchange.Common;
using Framework.Collections;
using System.ServiceModel;
using System.Web.Services.Description;
using iExchange.Common.Client;
using System.Collections.Concurrent;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using iExchange.StateServer.OrderMapping;
using System.Web.Hosting;
using iExchange.StateServer.Manager;
using iExchange.StateServer.Adapter;
using System.Xml.Linq;
using Protocal.TypeExtensions;

namespace iExchange.StateServer
{
    /*
     *	Bisiness rules:
     * 	Instrument-->QuotationServer
     * 		QuotationCollector-->QuotationServer
     * 		DealingConsole-->QuotationServer		
     * 
     * 	Account-->TransactionServer
     * 		TradingConsole-->TransactionServer
     * 		DealingConsole-->TransactionServer
     *		RiskMonitor-->TransactionServer		
     * 	Instrument-->TransactionServer
     * 		QuotationServer-->TransactionServer
     * 
     * 	Instrument-->DealingConsole
     *		QuotationServer-->DealingConsole
     *		TradingConsole-->DealingConsole	
     *		TransactionServer-->DealingConsole
     * 
     * 	Account-->TradingConsole
     *		
     * 	Instrument-->TradingConsole	
     * 
     * 	Account-->RiskMonitor
     *		QuotationServer-->RiskMonitor
     *		TransactionServer-->RiskMonitor
     * 
     */

    /// <summary>
    /// Summary description for Proxy.
    /// </summary>
    public class StateServer : ICommandBroadcast
    {
        public static readonly Token Token = new Token(Guid.Empty, UserType.System, AppType.StateServer);

        private ReaderWriterLock rwLock = new ReaderWriterLock();
        private bool _UseManager = false;

        //Support Omnibus function
        //private Hashtable accountMapping = new Hashtable();
        private StateServerService linkedStateServer;

        private iExchange.StateServer.PolamediaServer.Authentication polamediaServer;
        private iExchange.StateServer.FileExchangeServer.Service fileExchangeServer;

        private string stateServerID;
        private string connectionString;
        private string securityConnectionString;
        private int commandSequence;

        private OrderMapping.Booker orderMappingBooker;

        //for performance error!
        //private static Hashtable initCommands;

        //Server now only implement 1
        private ArrayList quotationServers;
        private ArrayList transactionServers;
        private TransactionAdaptor transactionAdaptor;

        private TransactionAdaptor _transactionAdapterTester;
        //Client
        private ArrayList dealingConsoles;
        private ArrayList tradingConsoles;
        private SLTraderManager slTraderManager;
        private ArrayList riskMonitors;
        private ArrayList tradingMonitors;
        //	private ArrayList mobiles;
        private QuotationCollector.Service2 quotationCollector;

        private Hashtable tokens = new Hashtable();

        private Hashtable instrumentMaps;
        private Hashtable accountMaps;
        private Hashtable transactionMaps;

        private Dictionary<string, Guid> instrumentCode2ID;
        private Hashtable orderToTransaction;
        private Dictionary<Guid, string> instrumentId2MappingCode = new Dictionary<Guid, string>();

        private MatchInfoCommandBuffer matchInfoCommandBuffer;

        private Protocal.ITransactionServerService _TransactionServerServiceProxy;

        private string _GateWayServiceUrl = string.Empty;
        private Protocal.IGatewayService _GatewayServiceChanel;

        private InitCommand GetInitCommand(AppType appType, string language)
        {
            InitCommand initCommand = new InitCommand();

            switch (appType)
            {
                case AppType.TradingConsole:
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForTradingConsole");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;
                    initCommand.Parameters = new String[] { "@customerID", "@permittedKeys", "@xmlInstruments" };
                    initCommand.Command.Parameters.Add("@customerID", SqlDbType.UniqueIdentifier);
                    initCommand.Command.Parameters.Add("@permittedKeys", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@xmlInstruments", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@language", SqlDbType.NChar);
                    initCommand.Command.Parameters["@language"].Value = language;
                    initCommand.TableNames = new string[]{
														   "TradeDay",	
														   "SystemParameter",
														   "Currency",
														   "CurrencyRate",
														   "Instrument",
														   "TradingTime",	
														   "TradePolicy",
                                                           "TradePolicyDetail",
                                                           "DeliveryCharge",
                                                           "VolumeNecessary",
                                                           "VolumeNecessaryDetail",
                                                           "DealingPolicy",
                                                           "DealingPolicyDetail",
                                                           "BOBetType",
                                                           "BOPolicy",
                                                           "BOPolicyDetail",
														   "Customer",
														   "Account",
														   "Quotation",
														   "AccountAgentHistory",
														   "AccountCurrency",
														   "Transaction",
														   "Order",
                                                           "OrderRelation",
                                                           "DayPLNotValued",
                                                           "OrderModification",
														   "Message",
														   "Settings",
                                                           "OrganizationName",
                                                           "QuotePolicyDetail",
                                                           "BestPending",
                                                           "TimeAndSale",
                                                           "InstrumentGroupState",
                                                           "InstrumentState",
                                                           "PaymentInstructionRemark",
                                                           "DeliveryRequest",
                                                           "DeliveryRequestOrderRelation",
                                                           "DeliveryRequestSpecification",
                                                           "DeliverySpecification",
                                                           "DeliverySpecificationDetail",
                                                           "ScrapDeposit",
                                                           "ScrapInstrument",
                                                           "DeliveryHolidays",
                                                           "InstalmentPolicy",
                                                           "InstalmentPolicyDetail",
                                                           "PriceAlert",
                                                           "PhysicalPaymentDiscount",
                                                           "PhysicalPaymentDiscountDetail",
                                                           "InstrumentUnit"
													   };
                    break;
                case AppType.DealingConsoleServer:
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForDealingConsoleServer");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;

                    initCommand.TableNames = new string[]{
														   "Account",
														   "Customer",
					};
                    break;
                case AppType.TradingMonitor:
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForTradingMonitor");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;
                    initCommand.Parameters = new String[] { "@tradingMonitorID", "@permittedKeys", "@permittedKeys2", "@xmlInstruments" };
                    initCommand.Command.Parameters.Add("@tradingMonitorID", SqlDbType.UniqueIdentifier);
                    initCommand.Command.Parameters.Add("@permittedKeys", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@permittedKeys2", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@xmlInstruments", SqlDbType.NText);

                    initCommand.TableNames = new string[]{
														   "TradeDay",
														   "Instrument",
														   "Account",
														   "Employee",
														   "Order"
													   };
                    break;
                case AppType.DealingConsole:
                    //DealingConsole
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForDealingConsole");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;
                    initCommand.Parameters = new String[] { "@dealerID", "@permittedKeys", "@xmlInstruments" };
                    initCommand.Command.Parameters.Add("@dealerID", SqlDbType.UniqueIdentifier);
                    initCommand.Command.Parameters.Add("@permittedKeys", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@xmlInstruments", SqlDbType.NText);

                    initCommand.TableNames = new string[]{
														   "TradeDay",
														   //"QuotationSource",
                                                           "SourceInstrument",
														   "SystemParameter",
                                                           "DealerParameterGroupDetail",
														   "Customer",
                                                           "AccountGroup",
														   "Account",
														   "QuotePolicy",
														   "QuotePolicyDetail",
														   "TradePolicy",
														   "TradePolicyDetail",
														   "Instrument",
														   "TradingTime",
														   "OverridedQuotation",
														   "OriginQuotation",
														   "Order",
                                                           "OrderRelation",
                                                           "OrderModification",
														   "Settings"
													   };
                    break;
                case AppType.RiskMonitor:
                    AppDebug.LogEvent("StateServer", "[GetInitCommand] construct sqlCommand for risk monitor", EventLogEntryType.Information);
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForRiskMonitor");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;
                    initCommand.Parameters = new String[] { "@riskMonitorID", "@permittedKeys" };
                    initCommand.Command.Parameters.Add("@riskMonitorID", SqlDbType.UniqueIdentifier);
                    initCommand.Command.Parameters.Add("@permittedKeys", SqlDbType.NText);
                    initCommand.TableNames = new string[]{
														   "TradeDay",
														   "SystemParameter",
														   "Currency",
														   "CurrencyRate",
														   "TradePolicy",
														   "TradePolicyDetail",
                                                           "VolumeNecessary",
                                                           "VolumeNecessaryDetail",
                                                           "PhysicalPaymentDiscount",
                                                           "PhysicalPaymentDiscountDetail",
														   "Instrument",
														   "Customer",
														   //"TradingTime",
                                                           "AccountGroup",
														   "Account",
														   "AccountBalance",
														   "OriginQuotation",
														   "OverridedQuotation",
														   "Transaction",
														   "Order",
                                                           "DeliveryRequestOrderRelation",
														   "OrderRelation",
														   "AccountEx",
                                                           "Settings" 
													   };
                    break;
                case AppType.Mobile:
                    initCommand.Command = new SqlCommand("dbo.P_GetInitDataForMobile");
                    initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;
                    initCommand.Parameters = new String[] { "@customerID", "@permittedKeys", "@xmlInstruments" };
                    initCommand.Command.Parameters.Add("@customerID", SqlDbType.UniqueIdentifier);
                    initCommand.Command.Parameters.Add("@permittedKeys", SqlDbType.NText);
                    initCommand.Command.Parameters.Add("@xmlInstruments", SqlDbType.NText);
                    initCommand.TableNames = new string[]{
														   "TradeDay",	
														   "Instrument",
														   "TradingTime",	
														   "Quotation"
													   };
                    break;
            }

            return initCommand;
        }

        private void InitializeInstruments(string connectionString)
        {
            try
            {
                string sql = "SELECT ID, Code, MappingCode, NumeratorUnit, Denominator,CurrencyID,Category FROM Instrument";
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = new SqlCommand(sql, new SqlConnection(connectionString));
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);

                this.instrumentId2MappingCode.Clear();
                this.instrumentCode2ID.Clear();
                foreach (DataRow dr in dataSet.Tables[0].Rows)
                {
                    Guid instrumentId = (Guid)dr["ID"];
                    string code = (string)dr["Code"];
                    string mappingCode = (string)dr["MappingCode"];
                    this.instrumentId2MappingCode[instrumentId] = mappingCode;
                    this.instrumentCode2ID[code] = instrumentId;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Warning);
            }
        }

        private void LinkStateServer(string connectionString)
        {
            try
            {
                AppDebug.LogEvent("StateServer", "LinkStateServer", EventLogEntryType.Information);

                string linkedStateServerUrl = System.Configuration.ConfigurationManager.AppSettings["iExchange.StateServer.LinkedStateServer.Service"];
                if (!string.IsNullOrEmpty(linkedStateServerUrl))
                {
                    this.linkedStateServer = new StateServerService();
                    this.linkedStateServer.Url = linkedStateServerUrl;
                }

                /*lock (this.accountMapping)
                {
                    SqlDataAdapter dataAdapter = new SqlDataAdapter();
                    string sql = "SELECT AccountID, LinkedAccountID, IsLocal, IsOpposite, QuantityFactor FROM CrossAccountMapping WHERE QuantityFactor >0";
                    dataAdapter.SelectCommand = new SqlCommand(sql, new SqlConnection(connectionString));
                    DataSet dataSet = new DataSet();
                    dataAdapter.Fill(dataSet);

                    AppDebug.LogEvent("StateServer", string.Format("LinkStateServer, count of account mapping={0}", dataSet.Tables[0].Rows.Count), EventLogEntryType.Information);
                    this.accountMapping.Clear();
                    foreach (DataRow dr in dataSet.Tables[0].Rows)
                    {
                        LinkedAccount linkedAccount = new LinkedAccount();
                        linkedAccount.AccountID = (Guid)dr["AccountID"];
                        linkedAccount.LinkedAccountID = (Guid)dr["LinkedAccountID"];
                        linkedAccount.IsLocal = (bool)dr["IsLocal"];
                        linkedAccount.IsOpposite = (bool)dr["IsOpposite"];
                        linkedAccount.QuantityFactor = (double)dr["QuantityFactor"];

                        if (!this.accountMapping.ContainsKey(linkedAccount.AccountID))
                        {
                            this.accountMapping.Add(linkedAccount.AccountID, new ArrayList());
                        }
                        ArrayList linkedAccounts = (ArrayList)this.accountMapping[linkedAccount.AccountID];
                        linkedAccounts.Add(linkedAccount);
                    }
                }*/
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Warning);
            }
        }

        public StateServer(string stateServerID, string connectionString, string serviceUrl, string securityConnectionString)
        {
            this.stateServerID = stateServerID;
            this.connectionString = connectionString;
            this.securityConnectionString = securityConnectionString;
            this.commandSequence = 0;

            string useManager = ConfigurationManager.AppSettings["UseManager"];
            this._UseManager = !string.IsNullOrEmpty(useManager) && bool.Parse(useManager);
#if TEST
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["TransactionAdaptorServiceUrl"]))
            {
                this.transactionAdaptor = new TransactionAdaptor(ConfigurationManager.AppSettings["TransactionAdaptorServiceUrl"], this);
            }
            this.InitializeInstruments(connectionString);
#else

            this._GateWayServiceUrl = ConfigurationManager.AppSettings["GatewayUrl"];

            if (!this._UseManager)
            {
                this.quotationServers = new ArrayList();
                QuotationServer.Service quotationServer = new QuotationServer.Service();
                int oldTimeout = quotationServer.Timeout;

                quotationServer.Timeout = 5 * 60 * 1000;
                serviceUrl = ConfigurationManager.AppSettings["Servcie2URLForQuotationServer"];
                if (string.IsNullOrEmpty(serviceUrl)) throw new ApplicationException("Servcie2URLForQuotationServer can't be null or empty");
                quotationServer.RegisterSoapHeader = new RegisterSoapHeader(serviceUrl);
                Token token = new Token(Guid.Empty, UserType.System, AppType.StateServer);
                quotationServer.Register(token, serviceUrl);
                if (!quotationServer.FlushQuotations(token))
                {
                    throw new Exception("QuotationServer.FlushQuotations failed");
                }

                quotationServer.Timeout = oldTimeout;
                this.quotationServers.Add(quotationServer);
            }

            this.transactionServers = new ArrayList();
            TransactionServer.Service transactionServer = null;
            if (ShouldUserTransactionServer())
            {
                transactionServer = new TransactionServer.Service();
                serviceUrl = ConfigurationManager.AppSettings["Servcie2URLForTransactionServer"];
                if (string.IsNullOrEmpty(serviceUrl)) throw new ApplicationException("Servcie2URLForTransactionServer can't be null or empty");
                transactionServer.RegisterSoapHeaderValue = new RegisterSoapHeader(serviceUrl);
                this.transactionServers.Add(transactionServer);
                QuotationBroadcastHelper.Add(this, transactionServer);
            }
            this.InitializeTransactionAdapter();

            this.InitializeTransactionServerServiceChannel();
            this.tradingConsoles = new ArrayList();
            this.riskMonitors = new ArrayList();
            this.tradingMonitors = new ArrayList();
            this.slTraderManager = new SLTraderManager();
            string tradingConsoleSLCommandCollectorUrl = ConfigurationManager.AppSettings["TradingConsoleSLCommandCollectorUrl"];
            if (!string.IsNullOrEmpty(tradingConsoleSLCommandCollectorUrl))
            {
                this.slTraderManager.CreateChannelForTraderSL(tradingConsoleSLCommandCollectorUrl);
            }
            else
            {
                throw new ApplicationException("TradingConsoleSLCommandCollectorUrl can't be null or empty");
            }

            string tradingConsoleInnerServiceUrl = ConfigurationManager.AppSettings["TradingConsoleInnerServiceUrl"];
            if (!string.IsNullOrEmpty(tradingConsoleInnerServiceUrl))
            {
                this.CreateService<TradingConsole.Service2>(tradingConsoleInnerServiceUrl, this.tradingConsoles);
            }
            else
            {
                throw new ApplicationException("TradingConsoleInnerServiceUrl can't be null or empty");
            }

            this.dealingConsoles = new ArrayList();
            if (!this._UseManager)
            {
                string dealingConsoleInnerServiceUrl = ConfigurationManager.AppSettings["DealingConsoleInnerServiceUrl"];
                if (!string.IsNullOrEmpty(dealingConsoleInnerServiceUrl))
                {
                    this.CreateService<DealingConsole.Service2>(dealingConsoleInnerServiceUrl, this.dealingConsoles);
                }
                else
                {
                    throw new ApplicationException("DealingConsoleInnerServiceUrl can't be null or empty");
                }
            }

            string riskMonitorInnerServiceUrl = ConfigurationManager.AppSettings["RiskMonitorInnerServiceUrl"];
            if (!string.IsNullOrEmpty(riskMonitorInnerServiceUrl))
            {
                this.CreateService<RiskMonitor.Service2>(riskMonitorInnerServiceUrl, this.riskMonitors);
            }
            else
            {
                throw new ApplicationException("RiskMonitorInnerServiceUrl can't be null or empty");
            }

            string tradingMonitorInnerServiceUrl = ConfigurationManager.AppSettings["TradingMonitorInnerServiceUrl"];
            if (!string.IsNullOrEmpty(tradingMonitorInnerServiceUrl))
            {
                this.CreateService<TradingMonitor.Service2>(tradingMonitorInnerServiceUrl, this.tradingMonitors);
            }
            else
            {
                throw new ApplicationException("TradingMonitorInnerServiceUrl can't be null or empty");
            }

            //		this.mobiles=new ArrayList();

            this.instrumentMaps = new Hashtable();
            this.accountMaps = new Hashtable();
            this.transactionMaps = new Hashtable();
            this.instrumentCode2ID = new Dictionary<string, Guid>();
            this.orderToTransaction = new Hashtable();

            this.quotationCollector = new QuotationCollector.Service2();
            this.quotationCollector.CookieContainer = new System.Net.CookieContainer();
            this.matchInfoCommandBuffer = new MatchInfoCommandBuffer();

            this.LinkStateServer(connectionString);
            this.InitializeInstruments(connectionString);

            this.LinkPolamediaServer();

            this.LinkFileExchangeServer();

            if (ShouldUserTransactionServer())
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        transactionServer.Register(new Token(Guid.Empty, UserType.System, AppType.StateServer), serviceUrl);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", string.Format("Resgiter transaction server {0} failed: {1}", serviceUrl, e.ToString()), EventLogEntryType.Error);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }
#endif

            string cachePath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "OrderMapping");
            this.orderMappingBooker = new Booker(this.connectionString, this.Book);
            this.orderMappingBooker.Start();
        }

        private void InitializeTransactionAdapter()
        {
            string isTest = ConfigurationManager.AppSettings["IsTransactionAdaptorTest"];
            string serviceUrl = ConfigurationManager.AppSettings["TransactionAdaptorServiceUrl"];
            string quotationServiceUrl = ConfigurationManager.AppSettings["SystemController_QuotationServiceUrl"];
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                bool testing = string.IsNullOrEmpty(isTest) ? false : bool.Parse(isTest);
                if (!testing)
                {
                    this.transactionAdaptor = new TransactionAdaptor(serviceUrl, quotationServiceUrl, this, this.connectionString);
                }
                else
                {
                    _transactionAdapterTester = new TransactionAdaptor(serviceUrl, quotationServiceUrl, this, this.connectionString);
                }
            }
        }

        public static bool ShouldUserTransactionServer()
        {
            string adapterTest = ConfigurationManager.AppSettings["IsTransactionAdaptorTest"];
            string serviceUrl = ConfigurationManager.AppSettings["TransactionAdaptorServiceUrl"];
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                if (string.IsNullOrEmpty(adapterTest) || !bool.Parse(adapterTest))
                {
                    return false;
                }
                else
                {
                    if (bool.Parse(adapterTest))
                    {
                        return true;
                    }
                }
            }
            return true;
        }


        public bool IsUseManager { get { return this._UseManager; } }

        private void LinkPolamediaServer()
        {
            try
            {
                this.polamediaServer = new iExchange.StateServer.PolamediaServer.Authentication();
                this.polamediaServer.Start();
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.LinkPolamediaServer", e.ToString(), EventLogEntryType.Warning);
            }
        }

        private void LinkFileExchangeServer()
        {
            try
            {
                this.fileExchangeServer = new iExchange.StateServer.FileExchangeServer.Service();
                this.fileExchangeServer.Start();
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.FileExchangeServer", e.ToString(), EventLogEntryType.Warning);
            }
        }

        //		private TransactionMap GetTransactionMap(Guid id,bool isTransaction)
        //		{
        //
        //		}

        public XmlNode GetState()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode stateServer = xmlDoc.CreateElement("StateServer");
            xmlDoc.AppendChild(stateServer);

            foreach (DealingConsole.Service2 dealingConsole2 in this.dealingConsoles)
            {
                XmlElement dealingConsole2Node = xmlDoc.CreateElement("DealingConsole2");
                stateServer.AppendChild(dealingConsole2Node);

                dealingConsole2Node.SetAttribute("Url", dealingConsole2.Url);
            }

            foreach (TradingConsole.Service2 tradingConsole2 in this.tradingConsoles)
            {
                XmlElement tradingConsole2Node = xmlDoc.CreateElement("TradingConsole2");
                stateServer.AppendChild(tradingConsole2Node);

                tradingConsole2Node.SetAttribute("Url", tradingConsole2.Url);
            }

            foreach (RiskMonitor.Service2 riskMonitor2 in this.riskMonitors)
            {
                XmlElement riskMonitor2Node = xmlDoc.CreateElement("RiskMonitor2");
                stateServer.AppendChild(riskMonitor2Node);

                riskMonitor2Node.SetAttribute("Url", riskMonitor2.Url);
            }

            foreach (TradingMonitor.Service2 tradingMonitor2 in this.tradingMonitors)
            {
                XmlElement tradingMonitor2Node = xmlDoc.CreateElement("TradingMonitor2");
                stateServer.AppendChild(tradingMonitor2Node);

                tradingMonitor2Node.SetAttribute("Url", tradingMonitor2.Url);
            }

            /*
            foreach(Mobile.Service2 mobile2 in this.mobiles)
            {
                XmlElement mobile2Node=xmlDoc.CreateElement("Mobile2");
                stateServer.AppendChild(mobile2Node);
				
                mobile2Node.SetAttribute("Url",mobile2.Url);
            }*/

            return stateServer;
        }

        private void CreateService<ServiceType>(string urls, ArrayList services)
            where ServiceType : System.Web.Services.Protocols.SoapHttpClientProtocol, new()
        {
            string[] urlArray = urls.Split(';');
            foreach (string url in urlArray)
            {
                if (string.IsNullOrEmpty(url)) continue;

                bool exists = false;
                foreach (ServiceType dealingConsole2 in services)
                {
                    if (dealingConsole2.Url.Equals(url, StringComparison.InvariantCultureIgnoreCase))
                    {
                        exists = true;
                        break;
                    };
                }

                if (!exists)
                {
                    ServiceType dealingConsole = new ServiceType();
                    dealingConsole.Url = url;
                    services.Add(dealingConsole);
                }
            }
        }

        public bool Register(Token token, string url)
        {
            try
            {
                if (token.AppType != AppType.TradingConsoleSilverLight) url = System.Web.HttpUtility.UrlDecode(url);

                this.rwLock.AcquireWriterLock(Timeout.Infinite);

                bool registered = true;
                switch (token.AppType)
                {
                    case AppType.DealingConsole:
                        //this.CreateService<DealingConsole.Service2>(url, this.dealingConsoles);
                        break;
                    case AppType.TradingConsole:
                        //this.CreateService<TradingConsole.Service2>(url, this.tradingConsoles);
                        break;
                    case AppType.TradingConsoleSilverLight:
                        url = this.slTraderManager.CreateChannelForTraderSL(url);
                        break;
                    case AppType.RiskMonitor:
                        //this.CreateService<RiskMonitor.Service2>(url, this.riskMonitors);
                        break;
                    case AppType.TradingMonitor:
                        //this.CreateService<TradingMonitor.Service2>(url, this.tradingMonitors);
                        break;
                    /*	case AppType.Mobile:
                            foreach(Mobile.Service2 mobile2 in this.mobiles)
                            {
                                if(string.Compare(mobile2.Url,url,true)==0) return true;
                            }

                            Mobile.Service2 mobile=new Mobile.Service2();
                            mobile.Url=url;
                            this.mobiles.Add(mobile);
                            break;
                     */
                    default:
                        registered = false;
                        break;
                }

                AppDebug.LogEvent("StateServer", "Register " + url, EventLogEntryType.Information);
                return registered;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Error);
                return false;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }

        private void StartTraderService(string serviceUrl)
        {
            try
            {
                WebClient client = new WebClient();
                client.OpenRead(serviceUrl);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StaterServer", "Failed to start the service by visiting url at " + serviceUrl
                    + Environment.NewLine + e.ToString(), EventLogEntryType.Warning);
            }
        }

        public bool UnRegister(Token token, string url)
        {
            url = System.Web.HttpUtility.UrlDecode(url);

            try
            {
                this.rwLock.AcquireWriterLock(Timeout.Infinite);
                switch (token.AppType)
                {
                    case AppType.DealingConsole:
                        //for (int i = 0; i < this.dealingConsoles.Count; i++)
                        //{
                        //    DealingConsole.Service2 dealingConsole = (DealingConsole.Service2)this.dealingConsoles[i];
                        //    if (dealingConsole.Url.Equals(url, StringComparison.InvariantCultureIgnoreCase))
                        //    {
                        //        this.dealingConsoles.RemoveAt(i);
                        //        break;
                        //    }
                        //}
                        break;
                    case AppType.TradingConsole:
                        //for (int i = 0; i < this.tradingConsoles.Count; i++)
                        //{
                        //    TradingConsole.Service2 tradingConsole = (TradingConsole.Service2)this.tradingConsoles[i];
                        //    if (tradingConsole.Url.Equals(url, StringComparison.InvariantCultureIgnoreCase))
                        //    {
                        //        this.tradingConsoles.RemoveAt(i);
                        //        break;
                        //    }
                        //}
                        break;
                    /*	case AppType.Mobile:
                            foreach(Mobile.Service2 mobile in this.mobiles)
                            {
                                if(string.Compare(mobile.Url,url,true)==0)
                                {
                                    this.mobiles.Remove(mobile);
                                    break;
                                }
                            }
                            break;
                     */
                    case AppType.StateServer:
                        foreach (DealingConsole.Service2 dealingConsole in this.dealingConsoles)
                        {
                            try
                            {
                                dealingConsole.UnRegister(token);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        this.dealingConsoles.Clear();

                        foreach (TradingConsole.Service2 tradingConsole in this.tradingConsoles)
                        {
                            try
                            {
                                tradingConsole.UnRegister(token);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        this.tradingConsoles.Clear();

                        /*	foreach(Mobile.Service2 mobile in this.mobiles)
                            {
                                try
                                {
                                    mobile.UnRegister(token);
                                }
                                catch(Exception e)
                                {
                                    AppDebug.LogEvent("StateServer",e.ToString(),EventLogEntryType.Error);
                                }
                            }
                            this.mobiles.Clear();
                         */

                        break;
                }

                AppDebug.LogEvent("StateServer", string.Format("UnRegister {0} at {1}", token.AppType, url), EventLogEntryType.Information);
                return true;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", string.Format("UnRegister {0} at {1} Exception:\r\n {2}", token.AppType, url, exception), EventLogEntryType.Warning);
                return false;
            }
            finally
            {
                this.rwLock.ReleaseWriterLock();
            }
        }


        /*
        public bool Login(ref Token token,string loginID,byte[] password)
        {
            try
            {
                SqlCommand command=new SqlCommand("dbo.P_Login");
                command.CommandType=System.Data.CommandType.StoredProcedure;
                command.Parameters.Add("@loginID",null);
                command.Parameters.Add("@password",null);
                command.Parameters.Add("@userID",SqlDbType.UniqueIdentifier);
                command.Parameters["@userID"].Direction=ParameterDirection.Output;
		
                //command
                command.Parameters["@loginID"].Value=loginID;
                command.Parameters["@password"].Value=password;
                command.Connection=new SqlConnection(this.securityConnectionString);

                command.Connection.Open();
                command.ExecuteNonQuery();
                command.Connection.Close();

                if(command.Parameters["@userID"].Value==DBNull.Value) return false;
                token.UserID=(Guid)command.Parameters["@userID"].Value;

                this.tokens[token]=token;
                this.BroadcastLoginState(token,true);

                return true;
            }
            catch(Exception e)
            {
                AppDebug.LogEvent("StateServer",e.ToString(),EventLogEntryType.Error);
                return false;
            }
        }
*/

        public bool Login(Token token)
        {
            try
            {
                this.tokens[token] = token;
                Task task = Task.Factory.StartNew(() =>
                {
                    if (!Checker.AllowMultipleLogin)
                    {
                        this.KickoutPredecessor(token.UserID);
                    }
                    this.BroadcastLoginState(token, true);
                });

                if (!task.Wait(35000))
                {
                    AppDebug.LogEvent("StateServer", "KickoutPredecessor can't complete in 35 seconds", EventLogEntryType.Warning);
                }

                return true;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool KickoutPredecessor(Guid userId)
        {
            try
            {
                this.slTraderManager.KickoutPredecessor(userId);

                foreach (TradingConsole.Service2 tradingConsole in this.tradingConsoles)
                {
                    try
                    {
                        tradingConsole.BeginKickoutPredecessor(userId, null, null);
                    }
                    catch (Exception exception)
                    {
                        AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Warning);
                    }
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Warning);
                return false;
            }
            return true;
        }

        public bool KickoffSL(Guid[] customerId, Guid[] employeeId)
        {
            bool kickSuccess = true;
            try
            {
                if (customerId != null)
                {
                    foreach (var customer in customerId)
                    {
                        KickoutPredecessor(customer);
                    }
                }

                if (employeeId != null)
                {
                    foreach (var employee in employeeId)
                    {
                        KickoutPredecessor(employee);
                    }
                }

                AppDebug.LogEvent("StateServer.KickoffSL", string.Format("kickoff completed. customerid {0}, employeeid {1}, success= {2}.",
                    customerId == null ? string.Empty : string.Join(",", customerId), employeeId == null ? string.Empty : string.Join(",", employeeId), kickSuccess), EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.KickoffSL",
                    string.Format("Kickoff:Error\r\n{0}", e.ToString()), EventLogEntryType.Error);
                return false;
            }
            return kickSuccess;
        }

        public bool Logout(Token token)
        {
            Token token2 = (Token)this.tokens[token];
            if (token2 != null && token.SessionID == token2.SessionID)
            {
                try
                {
                    this.tokens.Remove(token);
                    this.BroadcastLoginState(token, false);
                    return true;
                }
                catch (Exception e)
                {
                    AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    return false;
                }
            }
            else
            {
                AppDebug.LogEvent("StateServer", "Logout Failed:" + token.ToString(), EventLogEntryType.Warning);
                return false;
            }
        }

        public bool ChangePassword(Token token, byte[] oldPassword, byte[] newPassword)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(this.securityConnectionString))
                {
                    SqlCommand command = sqlConnection.CreateCommand();
                    command.CommandText = "dbo.P_ChangePassword2";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                    command.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add("@userID", SqlDbType.UniqueIdentifier);
                    command.Parameters.Add("@oldPassword", SqlDbType.VarBinary);
                    command.Parameters.Add("@newPassword", SqlDbType.VarBinary);

                    //command
                    command.Parameters["@userID"].Value = token.UserID;
                    command.Parameters["@oldPassword"].Value = oldPassword;
                    command.Parameters["@newPassword"].Value = newPassword;

                    sqlConnection.Open();
                    command.ExecuteNonQuery();

                    if ((int)command.Parameters["@RETURN_VALUE"].Value == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public DataSet GetInitData(Token token, XmlNode permittedKeys, out int commandSequence)
        {
            return this.GetInitData(token, permittedKeys, out commandSequence, null);
        }

        public DataSet GetInitData2(Token token, XmlNode permittedKeys, XmlNode permittedKeys2, out int commandSequence)
        {
            return this.GetInitData2(token, permittedKeys, permittedKeys2, out commandSequence, null);
        }

        private DataSet GetInitData2(Token token, XmlNode permittedKeys, XmlNode permittedKeys2, out int commandSequence, XmlNode selector)
        {
            try
            {
                InitCommand initCommand = this.GetInitCommand(token.AppType, token.Language);

                //command
                SqlCommand command = initCommand.Command;

                switch (token.AppType)
                {
                    case AppType.TradingMonitor:
                        command.Parameters[initCommand.Parameters[0]].Value = token.UserID;
                        if (permittedKeys != null)
                        {
                            command.Parameters[initCommand.Parameters[1]].Value = permittedKeys.OuterXml;
                        }
                        else
                        {
                            command.Parameters[initCommand.Parameters[1]].Value = null;
                        }
                        if (permittedKeys2 != null)
                        {
                            command.Parameters[initCommand.Parameters[2]].Value = permittedKeys2.OuterXml;
                        }
                        else
                        {
                            command.Parameters[initCommand.Parameters[2]].Value = null;
                        }
                        if (selector != null)
                        {
                            command.Parameters[initCommand.Parameters[3]].Value = selector.OuterXml;
                        }
                        else if (command.Parameters.Count == 4)
                        {
                            command.Parameters[initCommand.Parameters[3]].Value = null;
                        }
                        break;
                    default:
                        break;
                }

                command.Connection = new SqlConnection(connectionString);

                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);

                if (dataSet.Tables.Count != initCommand.TableNames.Length)
                    throw new ApplicationException("Get initial data failed");

                //modify table name
                string[] tableNames = initCommand.TableNames;
                for (int i = 0; i < dataSet.Tables.Count; i++)
                {
                    dataSet.Tables[i].TableName = tableNames[i];
                }

                commandSequence = this.commandSequence;

                return dataSet;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                throw e;
            }
        }

        public DataSet GetInitData(Token token, XmlNode permittedKeys, out int commandSequence, XmlNode selector)
        {
            try
            {
                InitCommand initCommand = this.GetInitCommand(token.AppType, token.Language);

                //command
                SqlCommand command = initCommand.Command;

                switch (token.AppType)
                {
                    case AppType.DealingConsoleServer:
                        break;
                    default:
                        command.Parameters[initCommand.Parameters[0]].Value = token.UserID;
                        if (permittedKeys != null)
                        {
                            command.Parameters[initCommand.Parameters[1]].Value = permittedKeys.OuterXml;
                        }
                        else
                        {
                            command.Parameters[initCommand.Parameters[1]].Value = null;
                        }

                        if (selector != null)
                        {
                            command.Parameters[initCommand.Parameters[2]].Value = selector.OuterXml;
                        }
                        else
                        {
                            if (command.Parameters.Count == 3)
                            {
                                command.Parameters[initCommand.Parameters[2]].Value = null;
                            }
                        }

                        break;
                }

                command.Connection = new SqlConnection(connectionString);
                command.CommandTimeout = 300;
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);

                if (dataSet.Tables.Count != initCommand.TableNames.Length)
                {
                    throw new ApplicationException(string.Format("GetInitData failed:token={0}, dataSet.Tables.Count={1}, initCommand.TableNames.Length={2}", token, dataSet.Tables.Count, initCommand.TableNames.Length));
                }

                //modify table name
                string[] tableNames = initCommand.TableNames;
                for (int i = 0; i < dataSet.Tables.Count; i++)
                {
                    dataSet.Tables[i].TableName = tableNames[i];
                }

                //if (token.AppType == AppType.TradingConsole) this.AddCmeInitPendingItems(dataSet);

                commandSequence = this.commandSequence;

                return dataSet;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", string.Format("{0}\n{1}", token, e.ToString()), EventLogEntryType.Error);
                throw e;
            }
        }

        //unused
        public bool SetActiveSource(Token token, string quotationSource)
        {
            bool isSucceed = false;

            if (token.AppType != AppType.DealingConsole) return isSucceed;

            try
            {
                //Authentication
                QuotationCollector.Authentication quotationCollector = new QuotationCollector.Authentication();
                quotationCollector.CookieContainer = new CookieContainer();
                if (this.QuotationCollectorLogin(quotationCollector))
                {
                    //Set
                    this.quotationCollector.CookieContainer = quotationCollector.CookieContainer;
                    isSucceed = this.quotationCollector.SetActiveSource(quotationSource);

                    if (isSucceed)
                    {
                        int commandSequence = Interlocked.Increment(ref this.commandSequence);
                        QuotationSourceCommand quotationSourceCommand = new QuotationSourceCommand(commandSequence);
                        quotationSourceCommand.SourceName = quotationSource;
                        this.BroadcastCommand(token, quotationSourceCommand, AppType.DealingConsole);

                        //Save DB
                        string sql = string.Format("Exec dbo.P_SetActiveSource '{0}'", quotationSource);
                        isSucceed = DataAccess.UpdateDB(sql, this.connectionString);
                        if (isSucceed == false)
                        {
                            AppDebug.LogEvent("StateServer", sql, EventLogEntryType.Warning);
                        }
                    }
                    else
                    {
                        AppDebug.LogEvent("StateServer", "quotationCollector.SetActiveSource", EventLogEntryType.Warning);
                    }
                }
                else
                {
                    AppDebug.LogEvent("StateServer", "Logon to QuotationCollector failed", EventLogEntryType.Warning);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        private bool QuotationCollectorLogin(QuotationCollector.Authentication quotationCollector)
        {
            string quotationCollectorLoginUserId = ConfigurationSettings.AppSettings["QuotationCollectorLoginUserId"];
            string quotationCollectorLoginPassword = ConfigurationSettings.AppSettings["QuotationCollectorLoginPassword"];
            return quotationCollector.Login(quotationCollectorLoginUserId, quotationCollectorLoginPassword);
        }

        //Added by Michael on 2008-05-26
        public bool SetActiveSourceInstrument(Token token, XmlNode sourceInstrumentNodes)
        {
            bool isSucceed = false;

            if (token.AppType != AppType.DealingConsole) return isSucceed;

            try
            {
                //Authentication
                QuotationCollector.Authentication quotationCollector = new QuotationCollector.Authentication();
                quotationCollector.CookieContainer = new CookieContainer();
                if (this.QuotationCollectorLogin(quotationCollector))
                {
                    //Set
                    this.quotationCollector.CookieContainer = quotationCollector.CookieContainer;
                    isSucceed = this.quotationCollector.SetActiveSourceInstrument(sourceInstrumentNodes);

                    if (isSucceed)
                    {
                        int commandSequence = Interlocked.Increment(ref this.commandSequence);
                        SourceInstrumentCommand sourceInstrumentCommand = new SourceInstrumentCommand(commandSequence);
                        sourceInstrumentCommand.Content = sourceInstrumentNodes;
                        this.BroadcastCommand(token, sourceInstrumentCommand, AppType.DealingConsole);

                        //Save DB
                        string sql = string.Format("Exec dbo.P_UpdateSourceInstrument '{0}'", sourceInstrumentNodes.OuterXml);
                        isSucceed = DataAccess.UpdateDB(sql, this.connectionString);
                        AppDebug.LogEvent("AAAAAAA", sourceInstrumentNodes.OuterXml.ToString(), EventLogEntryType.Information);
                        if (isSucceed == false)
                        {
                            AppDebug.LogEvent("StateServer", sql, EventLogEntryType.Warning);
                        }
                    }
                    else
                    {
                        AppDebug.LogEvent("StateServer", "quotationCollector.SetActiveSourceInstrument", EventLogEntryType.Warning);
                    }
                }
                else
                {
                    AppDebug.LogEvent("StateServer", "Logon to QuotationCollector failed", EventLogEntryType.Warning);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        public bool ReplayQuotation(Token token, string quotation)
        {
            bool isSucceed = false;
            OriginQuotation[] originQs = null;
            OverridedQuotation[] overridedQs = null;

            switch (token.AppType)
            {
                case AppType.QuotationCollector:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.SetQuotation(token, quotation, out originQs, out overridedQs);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if (originQs != null || overridedQs != null)
                {
                    this.BroadcastQuotationToTransactionServer(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), originQs, overridedQs);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        public bool FlushQuotations(Token token)
        {
            try
            {
                if (this._UseManager)
                {
                    return ManagerClient.FlushQuotations();
                }
                else
                {
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    return quotationServer.FlushQuotations(token);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool SetQuotation(Token token, string quotation)
        {
            if (this._UseManager) return true;

            bool isSucceed = false;
            OriginQuotation[] originQs = null;
            OverridedQuotation[] overridedQs = null;

            switch (token.AppType)
            {
                case AppType.QuotationCollector:
                case AppType.DealingConsole:
                case AppType.RiskMonitor:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.SetQuotation(token, quotation, out originQs, out overridedQs);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if (originQs != null || overridedQs != null)
                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), originQs, overridedQs);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        public bool DiscardQuotation(Token token, Guid instrumentID)
        {
            bool isSucceed = false;
            OriginQuotation[] originQs = null;
            OverridedQuotation[] overridedQs = null;

            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.DiscardQuotation(token, instrumentID, out originQs, out overridedQs);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if (originQs != null || overridedQs != null)
                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), originQs, overridedQs);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;

        }
        public DataSet GetInstrumentForSetting(Token token)
        {
            try
            {
                string sql = null;
                if (token.AppType == AppType.DealingConsole)
                {
                    sql = string.Format("SELECT * FROM FT_GetInstrumentForSetting('{0}','{1}')", token.UserID, "Dealing");
                }
                else if (token.AppType == AppType.TradingConsole)
                {
                    sql = string.Format("SELECT * FROM FT_GetInstrumentForTradingSetting2('{0}','{1}', '{2}')", token.UserID, "Trading", token.Language);
                }
                else if (token.AppType == AppType.Mobile)
                {
                    sql = string.Format("SELECT * FROM FT_GetInstrumentForSetting('{0}','{1}')", token.UserID, "Mobile");
                }
                if (sql == null) return null;

                DataSet instrument = DataAccess.GetData(sql, this.connectionString);
                if (instrument.Tables[0].Rows.Count == 0)
                {
                    return null;
                }
                else
                {
                    instrument.Tables[0].TableName = "Instrument";
                    return instrument;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public bool UpdateInstrument(Token token, XmlNode instrument)
        {
            try
            {
                //Save to DB
                string instrumentXml = DataAccess.ConvertToSqlXml(instrument.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateInstrument '{0}','{1}'", token.UserID, instrumentXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(instrument, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateDealingPolicyDetail(Token token, XmlNode dealingPolicyDetail)
        {
            try
            {
                //Save to DB
                string dealingPolicyDetailXml = DataAccess.ConvertToSqlXml(dealingPolicyDetail.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateDealingPolicyDetail '{0}','{1}'", token.UserID, dealingPolicyDetailXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(dealingPolicyDetail, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateCustomerPolicy(Token token, XmlNode customerPolicyNodes)
        {
            try
            {
                //Save to DB
                string customerPolicyXml = DataAccess.ConvertToSqlXml(customerPolicyNodes.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateCustomerPolicy '{0}','{1}'", token.UserID, customerPolicyXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(customerPolicyNodes, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateEmployeePolicy(Token token, XmlNode employeePolicyNodes)
        {
            try
            {
                //Save to DB
                string employeePolicyXml = DataAccess.ConvertToSqlXml(employeePolicyNodes.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateEmployeePolicy '{0}','{1}'", token.UserID, employeePolicyXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(employeePolicyNodes, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        //private void AddCmeInitPendingItems(DataSet dataSet)
        //{
        //    List<Guid> instruments = new List<Guid>();
        //    foreach (DataRow instrumentRow in dataSet.Tables["Instrument"].Rows)
        //    {
        //        instruments.Add((Guid)instrumentRow["ID"]);
        //    }
        //    List<MatchInfoCommand> matchInfoCommands = this.matchInfoCommandBuffer.GetCommands(instruments);
        //    if (matchInfoCommands.Count > 0)
        //    {
        //        DataTable bestPending = dataSet.Tables["BestPending"];
        //        foreach (MatchInfoCommand matchInfo in matchInfoCommands)
        //        {
        //            for (int i = matchInfo.BestSells.Length - 1; i >= 0; i--)
        //            {
        //                DataRow dataRow = bestPending.NewRow();
        //                bestPending.Rows.Add(dataRow);
        //                dataRow["OrganizationId"] = Guid.Empty;
        //                dataRow["InstrumentId"] = matchInfo.InstrumentId;
        //                dataRow["Sequence"] = i;
        //                dataRow["IsBuy"] = false;
        //                dataRow["Price"] = matchInfo.BestSells[i].Price == null ? string.Empty : matchInfo.BestSells[i].Price;
        //                dataRow["Quantity"] = (double)matchInfo.BestSells[i].Quantity;
        //            }

        //            for (int i = 0; i < matchInfo.BestBuys.Length; i++)
        //            {
        //                DataRow dataRow = bestPending.NewRow();
        //                bestPending.Rows.Add(dataRow);
        //                dataRow["OrganizationId"] = Guid.Empty;
        //                dataRow["InstrumentId"] = matchInfo.InstrumentId;
        //                dataRow["Sequence"] = i;
        //                dataRow["IsBuy"] = true;
        //                dataRow["Price"] = matchInfo.BestBuys[i].Price == null ? string.Empty : matchInfo.BestBuys[i].Price;
        //                dataRow["Quantity"] = (double)matchInfo.BestBuys[i].Quantity;
        //            }
        //        }
        //        bestPending.AcceptChanges();
        //    }
        //}

        //Added by Michael on 2008-09-05
        private void UpdateRelationQuotePolicyDetail(Token token, XmlNode quotePolicy)
        {
            //Notify Relation QuotePolicyDetail For IsOriginHiLo & PriceType                    
            //Get Relation Records From DB
            bool needNotifyRelation = false;

            foreach (XmlAttribute attribute in quotePolicy.Attributes)
            {
                switch (attribute.Name)
                {
                    case "IsOriginHiLo":
                    case "":
                        needNotifyRelation = true;
                        break;
                }
            }
            if (needNotifyRelation)
            {
                Guid quotePolicyID = XmlConvert.ToGuid(quotePolicy.Attributes["QuotePolicyID"].Value);
                Guid instrumentID = XmlConvert.ToGuid(quotePolicy.Attributes["InstrumentID"].Value);

                string sql = string.Format(@"SELECT QuotePolicyID,InstrumentID,PriceType,IsOriginHiLo
                                    FROM dbo.QuotePolicyDetail
                                    WHERE QuotePolicyID IN 
                                        (
                                            SELECT QuotePolicyID FROM dbo.QuotePolicyDetail
                                            WHERE InstrumentID = '{1}'
                                        )
                                        AND InstrumentID = '{1}' 
					                    AND QuotePolicyID <> '{0}'"
                    , quotePolicyID, instrumentID);
                DataSet dataSet = DataAccess.GetData(sql, this.connectionString);
                if (!(dataSet == null
                    || dataSet.Tables.Count <= 0
                    || dataSet.Tables[0].Rows.Count <= 0))
                {
                    foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                    {
                        Guid quotePolicyID2 = (Guid)dataRow["QuotePolicyID"];
                        bool isOriginHiLo = (bool)dataRow["IsOriginHiLo"];
                        byte priceType = (byte)dataRow["PriceType"];

                        XmlDocument doc = new XmlDocument();
                        XmlNode quotePolicyDetailRelation = doc.CreateNode(XmlNodeType.Element, "QuotePolicyDetail", null);
                        XmlAttribute attribute = doc.CreateAttribute("QuotePolicyID");
                        attribute.Value = quotePolicyID2.ToString();
                        quotePolicyDetailRelation.Attributes.Append(attribute);

                        attribute = doc.CreateAttribute("InstrumentID");
                        attribute.Value = instrumentID.ToString();
                        quotePolicyDetailRelation.Attributes.Append(attribute);

                        attribute = doc.CreateAttribute("IsOriginHiLo");
                        attribute.Value = isOriginHiLo ? "true" : "false";
                        quotePolicyDetailRelation.Attributes.Append(attribute);

                        attribute = doc.CreateAttribute("PriceType");
                        attribute.Value = priceType.ToString();
                        quotePolicyDetailRelation.Attributes.Append(attribute);

                        XmlElement update = doc.CreateElement("Update");
                        XmlElement modify = doc.CreateElement("Modify");
                        modify.AppendChild(quotePolicyDetailRelation);
                        update.AppendChild(modify);

                        this.Update(token, update, true);
                    }
                }
            }
        }

        public bool UpdateQuotePolicy(Token token, XmlNode quotePolicy, out int error)
        {
            error = -1;
            QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
            try
            {
                bool isSucceed = quotationServer.UpdateQuotePolicy(token, quotePolicy, out error);
                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(quotePolicy, true));
                    update.AppendChild(modify);

                    this.Update(token, update);

                    //Added by Michael on 2008-09-05
                    this.UpdateRelationQuotePolicyDetail(token, quotePolicy);

                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateQuotePolicies(Token token, XmlNode quotePolicies, out int error)
        {
            error = -1;
            QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
            try
            {
                bool isSucceed = quotationServer.UpdateQuotePolicies(token, quotePolicies, out error);
                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(quotePolicies, true));
                    update.AppendChild(modify);

                    this.Update(token, update);

                    //Added by Michael on 2008-09-05
                    foreach (XmlNode quotePolicy in quotePolicies.ChildNodes)
                    {
                        this.UpdateRelationQuotePolicyDetail(token, quotePolicy);
                    }
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public DataSet UpdateInstrumentSetting(Token token, XmlNode permittedKeys, XmlNode instrumentSetting, bool supportMultiQuotePolicy = false)
        {
            try
            {
                DataSet instrumentSettingChanges;
                XmlNode instruments = this.UpdateInstrumentSetting(token, instrumentSetting, out instrumentSettingChanges);

                if (token.AppType == AppType.DealingConsole)
                {
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    quotationServer.UpdateInstrumentDealer(token, instrumentSettingChanges);
                }

                if (!instruments.HasChildNodes) return null;
                int commandSequence;

                //Modified by Michael on 2005-06-10
                if (token.AppType == AppType.TradingConsole)
                {
                    return this.GetInstrumentsForTradingConsole(token, instruments, supportMultiQuotePolicy);
                }
                else if (token.AppType == AppType.Mobile)
                {
                    return supportMultiQuotePolicy ? this.GetInstrumentsForTradingConsole(token, instruments, supportMultiQuotePolicy) : this.GetInstrumentsForMobile(token, instruments);
                }
                else if (token.AppType == AppType.TradingMonitor)
                {
                    return this.GetInitData2(token, permittedKeys, null, out commandSequence, instruments);
                }
                else
                {
                    return this.GetInitData(token, permittedKeys, out commandSequence, instruments);
                }

            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public DataSet UpdateInstrumentSetting(Token token, XmlNode instrumentSetting)
        {
            return this.UpdateInstrumentSetting(token, null, instrumentSetting);
        }

        //Added by Michael on 2005-06-10
        private DataSet GetInstrumentsForMobile(Token token, XmlNode instruments)
        {
            SqlCommand command = new SqlCommand("dbo.P_GetInstrumentForMobile");
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add("@userID", null);
            command.Parameters.Add("@xmlInstrumentIDs", null);

            //command
            command.Parameters["@userID"].Value = token.UserID;
            command.Parameters["@xmlInstrumentIDs"].Value = instruments.OuterXml;
            command.Connection = new SqlConnection(this.connectionString);

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            dataAdapter.SelectCommand = command;
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            if (dataSet.Tables.Count > 0)
                dataSet.Tables[0].TableName = "Instrument";
            if (dataSet.Tables.Count > 1)
                dataSet.Tables[1].TableName = "TradingTime";
            if (dataSet.Tables.Count > 2)
                dataSet.Tables[3].TableName = "Quotation";

            return dataSet;
        }

        //Added by Michael on 2005-06-10
        private DataSet GetInstrumentsForTradingConsole(Token token, XmlNode instruments, bool supportMultiQuotePolicy = false)
        {
            SqlCommand command = new SqlCommand("dbo.P_GetInstrumentForTradingConsole");
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add("@userID", null);
            command.Parameters.Add("@xmlInstrumentIDs", null);
            command.Parameters.Add("@language", null);
            //command
            command.Parameters["@userID"].Value = token.UserID;
            command.Parameters["@xmlInstrumentIDs"].Value = instruments.OuterXml;
            command.Parameters["@language"].Value = string.IsNullOrEmpty(token.Language) ? "ENG" : token.Language;
            if (supportMultiQuotePolicy)
            {
                command.Parameters.Add("@isSupportMultiQuotePolicy", "1");
            }
            command.Connection = new SqlConnection(this.connectionString);

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            dataAdapter.SelectCommand = command;
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            if (dataSet.Tables.Count > 0)
                dataSet.Tables[0].TableName = "Instrument";
            if (dataSet.Tables.Count > 1)
                dataSet.Tables[1].TableName = "TradingTime";
            if (dataSet.Tables.Count > 2)
                dataSet.Tables[2].TableName = "TradePolicyDetail";
            if (dataSet.Tables.Count > 3)
                dataSet.Tables[3].TableName = "DeliveryCharge";
            if (dataSet.Tables.Count > 4)
                dataSet.Tables[4].TableName = "VolumeNecessary";
            if (dataSet.Tables.Count > 5)
                dataSet.Tables[5].TableName = "VolumeNecessaryDetail";
            if (dataSet.Tables.Count > 6)
                dataSet.Tables[6].TableName = "InstalmentPolicy";
            if (dataSet.Tables.Count > 7)
                dataSet.Tables[7].TableName = "InstalmentPolicyDetail";
            if (dataSet.Tables.Count > 8)
                dataSet.Tables[8].TableName = "Quotation";
            if (dataSet.Tables.Count > 9)
                dataSet.Tables[9].TableName = "QuotePolicyDetail";
            if (dataSet.Tables.Count > 10)
                dataSet.Tables[10].TableName = "BestPending";
            if (dataSet.Tables.Count > 11)
                dataSet.Tables[11].TableName = "TimeAndSale";
            if (dataSet.Tables.Count > 12)
                dataSet.Tables[12].TableName = "PhysicalPaymentDiscount";
            if (dataSet.Tables.Count > 13)
                dataSet.Tables[13].TableName = "PhysicalPaymentDiscountDetail";

            if (dataSet.Tables.Count > 14)
                dataSet.Tables[14].TableName = "BOBetType";
            if (dataSet.Tables.Count > 15)
                dataSet.Tables[15].TableName = "BOPolicy";
            if (dataSet.Tables.Count > 16)
                dataSet.Tables[16].TableName = "BOPolicyDetail";
            return dataSet;
        }

        private XmlNode UpdateInstrumentSetting(Token token, XmlNode instrumentSetting, out DataSet instrumentSettingChanges)
        {
            SqlCommand command = new SqlCommand("dbo.P_UpdateInstrumentSetting");
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add("@userID", null);
            command.Parameters.Add("@appType", null);
            command.Parameters.Add("@xmlInstrumentSetting", null);

            //command
            command.Parameters["@userID"].Value = token.UserID;
            command.Parameters["@appType"].Value = token.AppType;
            command.Parameters["@xmlInstrumentSetting"].Value = instrumentSetting.OuterXml;
            command.Connection = new SqlConnection(this.connectionString);

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            dataAdapter.SelectCommand = command;
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            if (token.AppType == AppType.DealingConsole)
            {
                instrumentSettingChanges = dataSet;
            }
            else
            {
                instrumentSettingChanges = null;
            }

            //Create New Instruments
            XmlDocument instruments = new XmlDocument();
            XmlElement instrumentsNode = instruments.CreateElement("Instruments");
            instruments.AppendChild(instrumentsNode);

            foreach (DataRow instrumentSettingRow in dataSet.Tables[0].Rows)
            {
                if (instrumentSettingRow["UserID"] == DBNull.Value) continue;

                XmlElement instrumentNode = instruments.CreateElement("Instrument");
                instrumentNode.SetAttribute("ID", XmlConvert.ToString((Guid)instrumentSettingRow["InstrumentID"]));
                instrumentsNode.AppendChild(instrumentNode);
            }

            return instruments.DocumentElement;
        }

        public bool UpdateOrder(Token token, XmlNode orders)
        {
            //Save to DB
            string sql = string.Format("Exec dbo.P_UpdateOrder '{0}','{1}'", token.UserID, orders.OuterXml);
            return DataAccess.UpdateDB(sql, this.connectionString);
        }

        public XmlNode GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetOpenInterestInstrumentSummary(token, isGroupByOriginCode, blotterCodeSelecteds);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetOpenInterestInstrumentSummary(token, isGroupByOriginCode, blotterCodeSelecteds);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetOpenInterestInstrumentSummary", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetOpenInterestSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetOpenInterestSummary(token, accountIDs, instrumentIDs, blotterCodeSelecteds);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetOpenInterestSummary(token, accountIDs, instrumentIDs, blotterCodeSelecteds);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetOpenInterestSummary", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetOpenInterestSummaryOrderList(Token token, Guid accountId, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetOpenInterestSummaryOrderList(accountId, instrumentIDs, blotterCodeSelecteds);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetOpenInterestSummaryOrderList(token, accountId, instrumentIDs, blotterCodeSelecteds);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetOpenInterestSummaryOrderList", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetGroupNetPosition(Token token, string permissionName, Guid[] accountIDs, Guid[] instrumentIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            try
            {
                return transactionServer.GetGroupNetPosition(token, permissionName, accountIDs, instrumentIDs, showActualQuantity, blotterCodeSelecteds);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetGroupNetPosition", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetAccountsProfitWithin(Token token, decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                }
                else
                {
                    return transactionServer.GetAccountsProfitWithin(token, minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetAccountsProfitWithin", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetGroupNetPositionForManager(Token token, string permissionName, Guid[] accountGroupIDs, Guid[] instrumentGroupIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetGroupNetPositionForManager(token, permissionName, accountGroupIDs, instrumentGroupIDs, showActualQuantity, blotterCodeSelecteds);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetGroupNetPositionForManager(token, permissionName, accountGroupIDs, instrumentGroupIDs, showActualQuantity, blotterCodeSelecteds);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetGroupNetPosition", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetGroupNetPositionInstrument(Token token, string permissionName, Guid accountId, Guid instrumentId, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            try
            {
                return transactionServer.GetGroupNetPositionInstrument(token, permissionName, accountId, instrumentId, showActualQuantity, blotterCodeSelecteds);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetGroupNetPositionInstrument", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, bool includeTransactions, bool onlyCutOrder)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetAccounts(token, accountIDs, includeTransactions, onlyCutOrder);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetAccounts4(token, accountIDs, includeTransactions, onlyCutOrder);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, Guid[] instrumentIDs, bool includeTransactions)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetAccounts(token, accountIDs, instrumentIDs, includeTransactions);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.GetAccounts5(token, accountIDs, instrumentIDs, includeTransactions);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }


        public XmlNode GetAccountsForInit(Token token, Guid[] accountIDs)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.GetAccountsForInit(accountIDs);
                }
                else
                {
                    return this.GetTransactionServer(Guid.Empty).GetAccountsForInit(accountIDs);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public XmlNode GetOrdersForGetAutoPrice(Guid[] orderIDs)
        {
            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            try
            {
                return transactionServer.GetOrdersForGetAutoPrice(orderIDs);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public bool UpdateAccountLock(Token token, XmlNode accountLockChanges)
        {
            Guid agentAccountID = XmlConvert.ToGuid(accountLockChanges.Attributes["AgentAccountID"].Value);

            //Save to DB
            bool isSucceed = false;
            try
            {
                SqlCommand command = new SqlCommand("dbo.P_UpdateAccountAgentHistory");
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                command.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;

                command.Parameters.Add("@customerID", SqlDbType.UniqueIdentifier);
                command.Parameters.Add("@agentAccountID", SqlDbType.UniqueIdentifier);
                command.Parameters.Add("@xmlLockChanges", SqlDbType.NText);
                command.Parameters.Add("@hasAccountsLocked", SqlDbType.Bit);
                command.Parameters["@hasAccountsLocked"].Direction = ParameterDirection.Output;

                //command
                command.Parameters["@customerID"].Value = token.UserID;
                command.Parameters["@agentAccountID"].Value = agentAccountID;
                string xmlLockChanges = DataAccess.ConvertToSqlXml(accountLockChanges.OuterXml);
                command.Parameters["@xmlLockChanges"].Value = xmlLockChanges;
                command.Connection = new SqlConnection(connectionString);

                //
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);

                if ((int)command.Parameters["@RETURN_VALUE"].Value == 1)
                {
                    isSucceed = true;
                }
                else
                {
                    isSucceed = false;
                }

                if (isSucceed)
                {
                    accountLockChanges.RemoveAll();
                    foreach (DataRow account in dataSet.Tables[0].Rows)
                    {
                        XmlElement accountNode = accountLockChanges.OwnerDocument.CreateElement("Account");
                        accountLockChanges.AppendChild(accountNode);

                        accountNode.SetAttribute("ID", XmlConvert.ToString((Guid)account["ID"]));
                        if ((int)account["IsLocked"] == 1)
                        {
                            accountNode.SetAttribute("IsLocked", XmlConvert.ToString(true));
                        }
                        else
                        {
                            accountNode.SetAttribute("IsLocked", XmlConvert.ToString(false));
                        }
                    }
                    ((XmlElement)accountLockChanges).SetAttribute("AgentAccountID", XmlConvert.ToString(agentAccountID));
                    ((XmlElement)accountLockChanges).SetAttribute("HasAccountsLocked", XmlConvert.ToString((bool)command.Parameters["@hasAccountsLocked"].Value));
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }

            if (isSucceed) this.UpdateAccountLockNotify(token, accountLockChanges);
            return isSucceed;
        }

        private void UpdateAccountLockNotify(Token token, XmlNode accountLockChanges)
        {
            //notify transaction server
            foreach (TransactionServer.Service transactionServer in this.transactionServers)
            {
                try
                {
                    AsyncCallback cb = new AsyncCallback(this.UpdateAccountLockNotifyCallback);
                    transactionServer.BeginUpdateAccountLock(token, accountLockChanges, cb, transactionServer);
                }
                catch (Exception e)
                {
                    AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                }
            }

            //notify tradingConsole
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            UpdateAccountLockCommand updateAccountLockCommand = new UpdateAccountLockCommand(commandSequence);
            updateAccountLockCommand.Content = accountLockChanges;
            this.BroadcastCommand(token, updateAccountLockCommand, AppType.DealingConsole);
        }

        private void UpdateAccountLockNotifyCallback(IAsyncResult ar)
        {
            try
            {
                if (ar.AsyncState is TransactionServer.Service)
                {
                    TransactionServer.Service transactionServer = (TransactionServer.Service)ar.AsyncState;
                    transactionServer.EndUpdateAccountLock(ar);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        public void Chat(Token token, XmlNode message)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            ChatCommand chatCommand = new ChatCommand(commandSequence);

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement chatNode = xmlDoc.CreateElement("Chat");
            xmlDoc.AppendChild(chatNode);

            chatNode.SetAttribute("ID", message.Attributes["ID"].Value);
            chatNode.SetAttribute("Title", message.Attributes["Title"].Value);
            chatNode.SetAttribute("Content", message.Attributes["Content"].Value);
            chatNode.SetAttribute("PublishTime", XmlConvert.ToString(DateTime.Now, DateTimeFormat.Xml));
            chatCommand.Content = chatNode;

            if (message["Recipients"]["AccountGroups"] != null)
            {
                chatCommand.AccountGroupIDs = new Guid[message["Recipients"]["AccountGroups"].ChildNodes.Count];
                int i = 0;
                foreach (XmlNode accountGroup in message["Recipients"]["AccountGroups"].ChildNodes)
                {
                    chatCommand.AccountGroupIDs[i++] = XmlConvert.ToGuid(accountGroup.Attributes["ID"].Value);

                }
            }
            if (message["Recipients"]["Customers"] != null)
            {
                chatCommand.CustomerIDs = new Guid[message["Recipients"]["Customers"].ChildNodes.Count];
                int i = 0;
                foreach (XmlNode customer in message["Recipients"]["Customers"].ChildNodes)
                {
                    chatCommand.CustomerIDs[i++] = XmlConvert.ToGuid(customer.Attributes["ID"].Value);

                }
            }

            if (this.transactionAdaptor != null)
            {
                this.transactionAdaptor.Chat(message.OuterXml);
            }

            this.BroadcastCommand(token, chatCommand, AppType.TradingConsole);
        }

        public void Quote(Token token, Guid instrumentID, double quoteLot, int BSStatus, int setPriceMaxMovePips)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            QuoteCommand quoteCommand = new QuoteCommand(commandSequence);
            quoteCommand.InstrumentID = instrumentID;
            quoteCommand.QuoteLot = quoteLot;
            quoteCommand.CustomerID = token.UserID;
            quoteCommand.BSStatus = BSStatus;
            quoteCommand.SetPriceMaxMovePips = setPriceMaxMovePips;
            this.BroadcastCommand(token, quoteCommand, AppType.DealingConsole);
        }

        public void Quote2(Token token, Guid instrumentID, double buyQuoteLot, double sellQuoteLot, int tick)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            Quote2Command quoteCommand = new Quote2Command(commandSequence);
            quoteCommand.InstrumentID = instrumentID;
            quoteCommand.BuyQuoteLot = buyQuoteLot;
            quoteCommand.SellQuoteLot = sellQuoteLot;
            quoteCommand.Tick = tick;
            quoteCommand.CustomerID = token.UserID;
            this.BroadcastCommand(token, quoteCommand, AppType.DealingConsole);
        }


        public void CancelQuote(Token token, Guid instrumentID, double buyQuoteLot, double sellQuoteLot)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            CancelQuote2Command cancelQuoteCommand = new CancelQuote2Command(commandSequence);
            cancelQuoteCommand.InstrumentID = instrumentID;
            cancelQuoteCommand.BuyQuoteLot = buyQuoteLot;
            cancelQuoteCommand.SellQuoteLot = sellQuoteLot;
            cancelQuoteCommand.CustomerID = token.UserID;
            this.BroadcastCommand(token, cancelQuoteCommand, AppType.DealingConsole);
        }

        public void Answer(Token token, XmlNode quotation)
        {
            //Save to DB
            string sql = string.Format("Exec dbo.P_AddQuoteQuotation '{0}','{1}'", token.UserID, quotation.OuterXml);
            if (!DataAccess.UpdateDB(sql, this.connectionString)) return;

            if (this.transactionAdaptor != null)
            {
                XElement answerRoot = new XElement("QuoteAnswer");
                answerRoot.Add(XElement.Load(quotation.CreateNavigator().ReadSubtree()));
                this.transactionAdaptor.QuoteAnswer(answerRoot.ToString());
            }
            //
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            AnswerCommand answerCommand = new AnswerCommand(commandSequence);
            answerCommand.InstrumentID = XmlConvert.ToGuid(quotation.Attributes["ID"].Value);
            answerCommand.Content = quotation;
            this.BroadcastCommand(token, answerCommand, AppType.TradingConsole);
        }

        public TransactionError Place(Token token, XmlNode xmlTran, out string tranCode)//tranCode may be LivePrice when error = InvalidPrice
        {
            tranCode = null;

            try
            {
                PlaceExtraInfo extraInfo = null;
                XmlNode xmlHitOrders = null;
                TransactionError error = TransactionError.OK;
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.Place(token, xmlTran, out tranCode);
                }
                else
                {
                    error = this.GetTransactionServer(Guid.Empty).Place(token, ref xmlTran, out extraInfo, out xmlHitOrders);
                }

                if (extraInfo == null || extraInfo.DeferToAuotFill)
                {
                    if (error == TransactionError.OK || (extraInfo != null && extraInfo.DeferToAuotFill))
                    {
                        int commandSequence = Interlocked.Increment(ref this.commandSequence);

                        tranCode = xmlTran.Attributes["Code"].Value;

                        PlaceCommand placeCommand;
                        placeCommand = new PlaceCommand(commandSequence);
                        placeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                        placeCommand.AccountID = XmlConvert.ToGuid(xmlTran.Attributes["AccountID"].Value);

                        XmlDocument xmlDoc = new XmlDocument();
                        XmlNode content = xmlDoc.CreateElement("Place");
                        xmlDoc.AppendChild(content);
                        placeCommand.Content = content;

                        content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                        if (extraInfo == null || !extraInfo.DeferToAuotFill) this.BroadcastCommand(token, placeCommand, AppType.DealingConsole);
                        this.BroadcastCommand(token, placeCommand, AppType.TradingConsole);
                        if (xmlHitOrders != null)
                        {
                            commandSequence = Interlocked.Increment(ref this.commandSequence);
                            HitCommand hitCommand = new HitCommand(commandSequence);
                            hitCommand.Content = xmlHitOrders;
                            this.BroadcastCommand(token, hitCommand, AppType.DealingConsole);
                        }
                    }
                }
                else
                {
                    Guid tranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);
                    if (extraInfo.Error == TransactionError.SplittedForHasShortSell)
                    {
                        error = TransactionError.OK;
                    }
                    else
                    {
                        this.BroadcastExecuteResult(token, tranID, extraInfo.Error, extraInfo.XmlTran, extraInfo.XmlAccount);
                    }
                }

                if (error == TransactionError.InvalidPrice && xmlTran.Attributes["LivePriceToCompare"] != null)
                {
                    tranCode = xmlTran.Attributes["LivePriceToCompare"].Value;
                }
                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                tranCode = null;
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError AcceptPlace(Token token, Guid tranID)
        {
            if (token.AppType != AppType.DealingConsole && token.AppType != AppType.Manager) return TransactionError.RuntimeError;

            TransactionError error;

            try
            {
                Guid instrumentID, accountID;
                PlaceExtraInfo extraInfo;
                System.Xml.XmlNode xmlHitOrders;

                if (this.transactionAdaptor != null)
                {
                    error = this.transactionAdaptor.AcceptPlace(tranID);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    error = transactionServer.AcceptPlace(token, tranID, out instrumentID, out accountID, out extraInfo, out xmlHitOrders);

                    if (extraInfo == null || extraInfo.DeferToAuotFill)
                    {
                        if (error == TransactionError.OK || (extraInfo != null && extraInfo.DeferToAuotFill))
                        {
                            int commandSequence = Interlocked.Increment(ref this.commandSequence);

                            AcceptPlaceCommand acceptPlaceCommand = new AcceptPlaceCommand(commandSequence);
                            acceptPlaceCommand.InstrumentID = instrumentID;
                            acceptPlaceCommand.AccountID = accountID;
                            acceptPlaceCommand.TransactionID = tranID;
                            acceptPlaceCommand.ErrorCode = error;

                            this.BroadcastCommand(token, acceptPlaceCommand, AppType.DealingConsole);
                            this.BroadcastCommand(token, acceptPlaceCommand, AppType.TradingConsole);

                            if (xmlHitOrders != null)
                            {
                                commandSequence = Interlocked.Increment(ref this.commandSequence);
                                HitCommand hitCommand = new HitCommand(commandSequence);
                                hitCommand.Content = xmlHitOrders;
                                this.BroadcastCommand(token, hitCommand, AppType.DealingConsole);
                            }
                        }
                    }
                    else
                    {
                        this.BroadcastExecuteResult(token, tranID, extraInfo.Error, extraInfo.XmlTran, extraInfo.XmlAccount);
                    }
                }

                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError Cancel(Token token, Guid tranID, CancelReason cancelReason)
        {
            TransactionError error;
            try
            {
                Guid instrumentID, accountID;
                if (this.transactionAdaptor != null)
                {
                    error = this.transactionAdaptor.Cancel(token, tranID, cancelReason);

                    if (token.AppType == AppType.TradingConsole && error == TransactionError.Action_NeedDealerConfirmCanceling)
                    {
                        var tran = this.transactionAdaptor.GetTran(tranID);
                        if (tran != null)
                        {
                            int commandSequence = Interlocked.Increment(ref this.commandSequence);

                            //DealingConsole
                            CancelCommand cancelCommand = new CancelCommand(commandSequence);
                            cancelCommand.InstrumentID = tran.InstrumentId;
                            cancelCommand.AccountID = tran.AccountId;
                            cancelCommand.TransactionID = tranID;
                            cancelCommand.CancelReason = cancelReason;
                            cancelCommand.ErrorCode = error;

                            this.BroadcastCommand(token, cancelCommand, AppType.DealingConsole);
                        }
                    }
                    return error;
                }
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);

                error = transactionServer.Cancel(token, tranID, cancelReason, out instrumentID, out accountID);

                if (token.AppType == AppType.TradingConsole && (error == TransactionError.OK || error == TransactionError.Action_NeedDealerConfirmCanceling))
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);

                    //DealingConsole
                    CancelCommand cancelCommand = new CancelCommand(commandSequence);
                    cancelCommand.InstrumentID = instrumentID;
                    cancelCommand.AccountID = accountID;
                    cancelCommand.TransactionID = tranID;
                    cancelCommand.CancelReason = cancelReason;
                    cancelCommand.ErrorCode = error;

                    this.BroadcastCommand(token, cancelCommand, AppType.DealingConsole);
                }

                if (token.AppType == AppType.DealingConsole || (token.AppType == AppType.TradingConsole && error == TransactionError.OK))
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);

                    //TradingConsole
                    CancelCommand cancelCommand = new CancelCommand(commandSequence);
                    cancelCommand.InstrumentID = instrumentID;
                    cancelCommand.AccountID = accountID;
                    cancelCommand.TransactionID = tranID;
                    cancelCommand.CancelReason = cancelReason;
                    cancelCommand.ErrorCode = error;

                    this.BroadcastCommand(token, cancelCommand, AppType.DealingConsole);
                    this.BroadcastCommand(token, cancelCommand, AppType.TradingConsole);
                }

                //Check NeedCancelCloseTran when execute Command process
                if (token.AppType == AppType.StateServer && error == TransactionError.OK)
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);

                    //DealingConsole/TradingConsole
                    CancelCommand cancelCommand = new CancelCommand(commandSequence);
                    cancelCommand.InstrumentID = instrumentID;
                    cancelCommand.AccountID = accountID;
                    cancelCommand.TransactionID = tranID;
                    cancelCommand.CancelReason = cancelReason;
                    cancelCommand.ErrorCode = error;

                    this.BroadcastCommand(token, cancelCommand, AppType.DealingConsole);
                    this.BroadcastCommand(token, cancelCommand, AppType.TradingConsole);
                }

                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        //Added by Michael on 2005-11-24
        public void Email(Token token, string typeDesc, Guid transactionID, string fromEmail, string toEmails, string subject, string body)
        {
            try
            {
                int commandSequence = Interlocked.Increment(ref this.commandSequence);
                EmailCommand emailCommand = new EmailCommand(commandSequence);
                emailCommand.TypeDesc = typeDesc;
                emailCommand.TransactionID = transactionID;
                emailCommand.FromEmail = fromEmail;
                emailCommand.ToEmails = toEmails;
                emailCommand.Body = body;
                emailCommand.Subject = subject;
                this.BroadcastCommand(token, emailCommand, token.AppType);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer.Email", ex.ToString(), EventLogEntryType.Error);
            }
        }

        //Added by Michael on 2005-04-06
        public TransactionError RejectCancelLmtOrder(Token token, Guid tranID, Guid accountID)
        {
            TransactionError error;
            error = TransactionError.OK;
            try
            {
                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                //DealingConsole
                RejectCancelLmtOrderCommand rejectCancelLmtOrderCommand = new RejectCancelLmtOrderCommand(commandSequence);
                rejectCancelLmtOrderCommand.AccountID = accountID;
                rejectCancelLmtOrderCommand.TransactionID = tranID;
                this.BroadcastCommand(token, rejectCancelLmtOrderCommand, AppType.TradingConsole);

                return error;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public Guid[] VerifyTransaction(Token token, Guid[] tranIDs)
        {
            try
            {
                XmlNode[] xmlTrans, xmlAccounts;
                Guid[] canceledTranIDs;
                if (this.transactionAdaptor != null)
                {
                    canceledTranIDs = this.transactionAdaptor.VerifyTransaction(token, tranIDs, out xmlTrans, out xmlAccounts);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    canceledTranIDs = transactionServer.VerifyTransaction(token, tranIDs, out xmlTrans, out xmlAccounts);
                }
                if (xmlTrans != null && xmlTrans.Length > 0)
                {
                    for (int i = 0; i < xmlTrans.Length; i++)
                    {
                        XmlNode xmlAccount = xmlAccounts[i];
                        XmlNode xmlTran = xmlTrans[i];

                        int commandSequence = Interlocked.Increment(ref this.commandSequence);

                        ExecuteCommand executeCommand = new ExecuteCommand(commandSequence, DebugId.VerifyTransaction);
                        executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                        executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                        executeCommand.TranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);

                        XmlDocument xmlDoc = new XmlDocument();
                        XmlElement content = xmlDoc.CreateElement("Content");
                        executeCommand.Content = content;

                        content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                        content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                        if (token.AppType == AppType.Manager)
                        {
                            this.BroadcastCommand(token, executeCommand, AppType.Manager);
                        }
                        else
                        {
                            this.BroadcastCommand(token, executeCommand, AppType.TradingConsole);
                            this.BroadcastCommand(token, executeCommand, AppType.RiskMonitor);
                            this.BroadcastCommand(token, executeCommand, AppType.TradingMonitor);
                        }
                    }
                }

                if (canceledTranIDs.Length == 0)
                {
                    return null;
                }
                else
                {
                    return canceledTranIDs;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public TransactionError Execute(Token token, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID, out XmlNode xmlTran)
        {
            xmlTran = null;


            try
            {
                XmlNode xmlAccount;
                TransactionError error;
                if (this.transactionAdaptor != null)
                {
                    error = this.transactionAdaptor.Execute(tranID, buyPrice, sellPrice, lot, executedOrderID);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    error = transactionServer.Execute(token, tranID, buyPrice, sellPrice, lot, executedOrderID, out xmlTran, out xmlAccount);

                    this.BroadcastExecuteResult(token, tranID, error, xmlTran, xmlAccount);
                }
                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public void BroadcastExecuteResult(Token token, Guid tranID, TransactionError error, XmlNode xmlTran, XmlNode xmlAccount)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);
            if (tranID == Guid.Empty)
            {
                try
                {
                    tranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);
                }
                catch
                {
                    AppDebug.LogEvent("StateServer", "tranID is empty and can't get tranID from :" + xmlTran == null ? "NULL" : xmlTran.OuterXml, EventLogEntryType.Error);
                }
            }

            if (error != TransactionError.OK)
            {
                if (xmlAccount != null)
                {
                    //TradingConsole
                    ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                    executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                    executeCommand.TranID = tranID;
                    executeCommand.ErrorCode = error;

                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement content = xmlDoc.CreateElement("Content");
                    executeCommand.Content = content;

                    //TradingConsole
                    XmlElement tran2 = xmlDoc.CreateElement("Transaction");
                    content.AppendChild(tran2);
                    tran2.SetAttribute("ID", XmlConvert.ToString(tranID));
                    tran2.SetAttribute("ErrorCode", error.ToString());
                    this.BroadcastCommand(token, executeCommand, AppType.TradingConsole);
                }
            }
            else
            {
                ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                executeCommand.TranID = tranID;

                XmlDocument xmlDoc = new XmlDocument();
                XmlElement content = xmlDoc.CreateElement("Content");
                executeCommand.Content = content;

                content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                this.BroadcastCommand(token, executeCommand, AppType.TradingConsole);
                this.BroadcastCommand(token, executeCommand, AppType.RiskMonitor);
                this.BroadcastCommand(token, executeCommand, AppType.TradingMonitor);

                //Here may be duplicated sending to DealingConsole
                this.BroadcastCommand(token, executeCommand, AppType.DealingConsole);

                ThreadPool.QueueUserWorkItem(this.CancelRelatedInvalidOrdersIfExists, tranID);
                this.OnTranExecuted(xmlTran.Clone());
            }
        }

        private void CancelRelatedInvalidOrdersIfExists(object args)
        {
            Guid tranID = (Guid)args;
            try
            {
                Token token = new Token(Guid.Empty, UserType.System, AppType.StateServer);

                string sql = string.Format("EXEC dbo.P_GetNeedCancelCloseTran '{0}'", tranID);
                DataSet dataSet = DataAccess.GetData(sql, this.connectionString);
                if (dataSet == null || dataSet.Tables.Count <= 0 || dataSet.Tables[0].Rows.Count <= 0) return;
                foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                {
                    try
                    {
                        AppDebug.LogEvent("StateServer", "CancelRelatedInvalidOrdersIfExists, to cancel: " + (Guid)dataRow["TransactionID"], EventLogEntryType.Information);
                        this.Cancel(token, (Guid)dataRow["TransactionID"], CancelReason.OpenOrderIsClosed);
                    }
                    catch (Exception e2)
                    {
                        AppDebug.LogEvent("StateServer", "CancelRelatedInvalidOrdersIfExists" + e2.ToString(), EventLogEntryType.Error);
                    }
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", "CancelRelatedInvalidOrdersIfExists" + e.ToString(), EventLogEntryType.Error);
            }
        }

        public XmlNode GetAcountInfo(Token token, Guid tranID)
        {
            if (this.transactionAdaptor != null)
            {
                return this.transactionAdaptor.GetAccountInfo(token, tranID);
            }
            else
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                return transactionServer.GetAcountInfo(token, tranID);
            }
        }

        public XmlNode GetMemoryBalanceNecessaryEquityExcludeAlerted(Token token)
        {
            if (this.transactionAdaptor != null)
            {
                return this.transactionAdaptor.GetMemoryBalanceNecessaryEquityExcludeAlerted(token);
            }
            else
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                return transactionServer.GetMemoryBalanceNecessaryEquityExcludeAlerted(token);
            }
        }

        public TransactionError MultipleClose(Token token, Guid[] orderIds, out XmlNode xmlTran, out XmlNode xmlAccount)
        {
            xmlAccount = null;
            xmlTran = null;

            TransactionServer.Service transactionServer;
            try
            {
                TransactionError error;
                if (this.transactionAdaptor != null)
                {
                    error = this.transactionAdaptor.MultipleClose(orderIds);
                }
                else
                {
                    transactionServer = this.GetTransactionServer(Guid.Empty);
                    error = transactionServer.MultipleClose(token, orderIds, out xmlTran, out xmlAccount);
                    if (error == TransactionError.OK)
                    {
                        ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                        executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                        executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                        executeCommand.TranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);

                        XmlDocument xmlDoc = new XmlDocument();
                        XmlElement content = xmlDoc.CreateElement("Content");
                        executeCommand.Content = content;

                        content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                        content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                        this.BroadcastCommand(token, executeCommand, AppType.TradingConsole);
                        this.BroadcastCommand(token, executeCommand, AppType.RiskMonitor);
                        this.BroadcastCommand(token, executeCommand, AppType.TradingMonitor);
                        this.BroadcastCommand(token, executeCommand, AppType.DealingConsole);
                        this.OnTranExecuted(xmlTran.Clone());
                    }

                }

                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError Assign(Token token, ref XmlNode xmlTran, out XmlNode xmlAccount, out XmlNode xmlInstrument)
        {
            xmlAccount = null;
            xmlInstrument = null;

            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            try
            {
                XmlNode xmlAgentAccount = null;
                TransactionError error;
                if (this.transactionAdaptor != null)
                {
                    error = this.transactionAdaptor.Assign(xmlTran);
                }
                else
                {
                    error = transactionServer.Assign(token, ref xmlTran, out xmlAccount, out xmlInstrument, out xmlAgentAccount);
                }

                if (error == TransactionError.OK)
                {

                    this.UpdateAccountLockNotify(token, xmlAgentAccount);

                    int commandSequence = Interlocked.Increment(ref this.commandSequence);

                    AssignCommand assignCommand = new AssignCommand(commandSequence);
                    assignCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);

                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement content = xmlDoc.CreateElement("Assign");
                    content.AppendChild(xmlDoc.ImportNode(xmlTran, true));
                    content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                    if (xmlInstrument != null) content.AppendChild(xmlDoc.ImportNode(xmlInstrument, true));

                    assignCommand.Content = content;

                    this.BroadcastCommand(token, assignCommand, AppType.TradingConsole);
                    this.BroadcastCommand(token, assignCommand, AppType.RiskMonitor);
                    this.BroadcastCommand(token, assignCommand, AppType.TradingMonitor);

                    this.OnTranExecuted(xmlTran.Clone());
                }
                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public bool ResetServerAndKickoffAllTrader(Token token, int timeout)
        {
            Stopwatch watch = new Stopwatch();

            watch.Start();

            if (this.transactionAdaptor == null)
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                int oldTimeout = transactionServer.Timeout;
                try
                {
                    transactionServer.Timeout = timeout * 1000;
                    transactionServer.Reset();
                }
                finally
                {
                    transactionServer.Timeout = oldTimeout;
                }
            }
            watch.Stop();
            AppDebug.LogEvent("StateServer", string.Format("ResetServerAndKickoffAllTrader reset transactionserver consume time = {0}", watch.Elapsed.TotalSeconds), EventLogEntryType.Information);

            watch.Restart();
            this.slTraderManager.KickoutAll();
            watch.Stop();
            AppDebug.LogEvent("StateServer", string.Format("ResetServerAndKickoffAllTrader slTraderManager.KickoutAll consume time = {0}", watch.Elapsed.TotalSeconds), EventLogEntryType.Information);

            watch.Restart();
            foreach (TradingConsole.Service2 tradingConsole in this.tradingConsoles)
            {
                try
                {
                    tradingConsole.KickoutPredecessor(Guid.Empty);//Guid.Empty means All user
                }
                catch (Exception exception)
                {
                    AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Warning);
                }
            }
            watch.Stop();
            AppDebug.LogEvent("StateServer", string.Format("ResetServerAndKickoffAllTrader tradingConsole.KickoutPredecessor consume time = {0}", watch.Elapsed.TotalSeconds), EventLogEntryType.Information);

            return true;

        }

        public TransactionError[] Book(Token token, XmlElement[] xmlTrans)
        {
            if (this.transactionAdaptor != null)
            {
                TransactionError[] result = new TransactionError[xmlTrans.Length];
                Parallel.For(0, xmlTrans.Length, i =>
                {
                    result[i] = this.transactionAdaptor.Book(token, xmlTrans[i], false);
                });
                return result;
            }
            else
            {
                return this.BookOrdersForTransactionServer(token, xmlTrans);
            }
        }


        private TransactionError[] BookOrdersForTransactionServer(Token token, XmlElement[] xmlTrans)
        {
            XmlElement[] xmlTranResults = null;
            XmlElement[] xmlAccounts = null;
            XmlElement[] xmlAffectedOrders = null;
            TransactionError[] results = null;

            TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
            int oldTimeout = transactionServer.Timeout;
            try
            {
                int timeout = 1200000;
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]))
                {
                    timeout = int.Parse(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]);
                }
                transactionServer.Timeout = timeout * 2;

                results = transactionServer.BookOrders(token, xmlTrans, timeout / 1000, out xmlTranResults, out xmlAccounts, out xmlAffectedOrders);

                for (int index = 0; index < xmlTrans.Length; index++)
                {
                    if (results[index] == TransactionError.OK)
                    {
                        XmlElement xmlTran = xmlTranResults[index];
                        XmlElement xmlAccount = xmlAccounts[index];
                        XmlElement xmlAffectedOrder = xmlAffectedOrders[index];

                        this.BoardcastBookResult(token, xmlTran, xmlAccount, xmlAffectedOrder);
                    }
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Error);
                results = new TransactionError[xmlTrans.Length];
                for (int index = 0; index < xmlTrans.Length; index++)
                {
                    results[index] = TransactionError.RuntimeError;
                }
            }
            finally
            {
                transactionServer.Timeout = oldTimeout;
            }

            return results;
        }


        public TransactionError Book(Token token, ref XmlNode xmlTran, bool preserveCalculation, out XmlNode xmlAccount, out XmlNode xmlAffectedOrders)
        {
            xmlAccount = null;
            xmlAffectedOrders = null;

            TransactionServer.Service transactionServer = null;

            if (this.transactionAdaptor == null)
            {
                transactionServer = this.GetTransactionServer(Guid.Empty);
            }
            try
            {
                TransactionError error;

                if (xmlTran.Attributes["InstrumentCode"] != null)
                {
                    string code = xmlTran.Attributes["InstrumentCode"].Value;

                    Guid instrumentId = Guid.Empty;
                    if (this.instrumentCode2ID.TryGetValue(code, out instrumentId))
                    {
                        xmlTran.Attributes["InstrumentID"].Value = XmlConvert.ToString(instrumentId);
                    }
                    else
                    {
                        AppDebug.LogEvent("StateServer.Book", string.Format("Can't map [{0}] to a valid instrument, tran={1}", code, xmlTran.OuterXml), EventLogEntryType.Warning);
                        return TransactionError.TransactionCannotBeBooked;
                    }
                }

                int oldTimeout = transactionServer != null ? transactionServer.Timeout : 0;

                try
                {
                    if (this.transactionAdaptor != null)
                    {
                        error = this.transactionAdaptor.Book(token, xmlTran, preserveCalculation);
                    }
                    else
                    {
                        // transactionServer.Timeout = 1200000;
                        int timeout = 1200000;
                        if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]))
                        {
                            timeout = int.Parse(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]);
                        }
                        transactionServer.Timeout = timeout;
                        error = transactionServer.BookWithTimeout(token, ref xmlTran, preserveCalculation, out xmlAccount, out xmlAffectedOrders, timeout / 1000);
                        if (error == TransactionError.OK)
                        {
                            this.BoardcastBookResult(token, xmlTran, xmlAccount, xmlAffectedOrders);
                        }
                    }
                }
                finally
                {
                    if (transactionServer != null)
                    {
                        transactionServer.Timeout = oldTimeout;
                    }
                }
                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public void BoardcastBookResult(Token token, XmlNode xmlTran, XmlNode xmlAccount, XmlNode xmlAffectedOrders)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            Execute2Command execute2Command = new Execute2Command(commandSequence);
            execute2Command.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
            execute2Command.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement content = xmlDoc.CreateElement("Execute2");
            execute2Command.Content = content;
            content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
            content.AppendChild(xmlDoc.ImportNode(xmlTran, true));
            if (xmlAffectedOrders != null)
            {
                content.AppendChild(xmlDoc.ImportNode(xmlAffectedOrders, true));
            }

            this.BroadcastCommand(token, execute2Command, AppType.TradingConsole);
            this.BroadcastCommand(token, execute2Command, AppType.TradingMonitor);
            this.BroadcastCommand(token, execute2Command, AppType.DealingConsole);
            this.BroadcastCommand(token, execute2Command, AppType.RiskMonitor);

            Guid tranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);
            ThreadPool.QueueUserWorkItem(this.CancelRelatedInvalidOrdersIfExists, tranID);

            this.OnTranExecuted(xmlTran.Clone());
        }

        public TransactionError Delete(Token token, Guid orderID, bool notifyByEmail, bool isPayForInstalmentDebitInterest, out XmlNode xmlAffectedOrders, out XmlNode xmlAccount)
        {
            xmlAccount = null;
            xmlAffectedOrders = null;
            TransactionServer.Service transactionServer = null;
            if (this.transactionAdaptor == null)
            {
                transactionServer = this.GetTransactionServer(Guid.Empty);
            }
            try
            {
                Guid instrumentID = Guid.Empty;
                Guid[] affectedDeletedOrders = null;

                int oldTimeout = transactionServer != null ? transactionServer.Timeout : 0;
                // transactionServer.Timeout = 1200000;

                TransactionError error = TransactionError.OK;
                try
                {
                    if (this.transactionAdaptor != null)
                    {
                        error = this.transactionAdaptor.Delete(null, orderID, isPayForInstalmentDebitInterest);
                    }
                    else
                    {
                        int timeout = 1200000;
                        if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]))
                        {
                            timeout = int.Parse(ConfigurationManager.AppSettings["BookTimeoutMilliSeconds"]);
                        }
                        transactionServer.Timeout = timeout;
                        error = transactionServer.Delete2(token, orderID, notifyByEmail, isPayForInstalmentDebitInterest, out xmlAffectedOrders, out xmlAccount, out instrumentID, out affectedDeletedOrders, timeout / 1000);

                        if (error == TransactionError.OK)
                        {
                            Guid accountId = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);

                            this.CreateAndBroadcastDeleteCommand(token, orderID, xmlAffectedOrders, xmlAccount, instrumentID, accountId);
                            if (affectedDeletedOrders != null)
                            {
                                foreach (Guid id in affectedDeletedOrders)
                                {
                                    this.CreateAndBroadcastDeleteCommand(token, id, null, xmlAccount, instrumentID, accountId);
                                }
                            }

                            //this.BeginDeleteToLinkedServer(orderID);
                        }
                    }
                }
                finally
                {
                    if (transactionServer != null)
                    {
                        transactionServer.Timeout = oldTimeout;
                    }
                }
                return error;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError DeleteForNewVersion(Token token, bool notifyByEmail, bool isPayForInstalmentDebitInterest, Guid accountId, Guid orderID)
        {
            if (this.transactionAdaptor == null) return TransactionError.RuntimeError;
            return this.transactionAdaptor.Delete(accountId, orderID, isPayForInstalmentDebitInterest);
        }

        public TransactionError DeleteByCancelDeliveryForNewVersion(Token token, bool notifyByEmail, bool isPayForInstalmentDebitInterest, Guid accountId, Guid orderID, Guid deliveryRequestId)
        {
            if (this.transactionAdaptor == null) return TransactionError.RuntimeError;
            return this.transactionAdaptor.DeleteByCancelDelivery(accountId, orderID, deliveryRequestId, isPayForInstalmentDebitInterest);
        }


        private void CreateAndBroadcastDeleteCommand(Token token, Guid orderID, XmlNode xmlAffectedOrders, XmlNode xmlAccount, Guid instrumentID, Guid accountId)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            DeleteCommand deleteCommand = new DeleteCommand(commandSequence);
            deleteCommand.AccountID = accountId;

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement content = xmlDoc.CreateElement("Delete");
            deleteCommand.Content = content;

            XmlElement deletedOrder = xmlDoc.CreateElement("DeletedOrder");
            content.AppendChild(deletedOrder);
            deletedOrder.SetAttribute("ID", XmlConvert.ToString(orderID));

            if (xmlAffectedOrders != null)
            {
                deleteCommand.InstrumentID = XmlConvert.ToGuid(xmlAffectedOrders["Transaction"].Attributes["InstrumentID"].Value);
                content.AppendChild(xmlDoc.ImportNode(xmlAffectedOrders, true));
            }
            deletedOrder.SetAttribute("AccountID", XmlConvert.ToString(deleteCommand.AccountID));
            deletedOrder.SetAttribute("InstrumentID", XmlConvert.ToString(instrumentID));
            this.BroadcastCommand(token, deleteCommand, AppType.DealingConsole);
            this.BroadcastCommand(token, deleteCommand, AppType.TradingMonitor);

            DeleteCommand deleteCommand2 = new DeleteCommand(commandSequence);
            deleteCommand2.AccountID = deleteCommand.AccountID;
            deleteCommand2.InstrumentID = deleteCommand.InstrumentID;
            deleteCommand2.Content = deleteCommand.Content.CloneNode(true);
            deleteCommand2.Content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
            this.BroadcastCommand(token, deleteCommand2, AppType.TradingConsole);
            this.BroadcastCommand(token, deleteCommand2, AppType.RiskMonitor);
        }

        public void ResetHit(Token token, Guid[] orderIDs)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    this.transactionAdaptor.ResetHit(orderIDs);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    transactionServer.ResetHit(token, orderIDs);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        public bool ResetAlertLevel(Token token, Guid[] accountIDs)
        {

            try
            {
                AlertLevel[] alertLevels;
                if (this.transactionAdaptor != null)
                {
                    alertLevels = this.transactionAdaptor.ResetAlertLevel(token, accountIDs);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    alertLevels = transactionServer.ResetAlertLevel(token, accountIDs);
                }
                if (alertLevels != null)
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);

                    ResetAlertLevelCommand resetAlertLevelCommand = new ResetAlertLevelCommand(commandSequence);
                    resetAlertLevelCommand.AccountIDs = accountIDs;
                    resetAlertLevelCommand.AlterLevels = alertLevels;
                    resetAlertLevelCommand.Content = this.GetAccounts(token, accountIDs, true, false);

                    this.BroadcastCommand(token, resetAlertLevelCommand, AppType.TradingConsole);
                    this.BroadcastCommand(token, resetAlertLevelCommand, AppType.RiskMonitor);
                }

                return true;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false; ;
            }
        }

        public void Reset(Token token, XmlNode resetNode, OverridedQuotation[] overridedQs)
        {
            if (overridedQs != null && overridedQs.Length > 0)
            {
                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                QuotationCommand quotationCommand = new QuotationCommand(commandSequence);
                quotationCommand.OverridedQs = overridedQs;

                this.BroadcastCommand(token, quotationCommand, AppType.TradingConsole);
                this.BroadcastCommand(token, quotationCommand, AppType.DealingConsole);
                this.BroadcastCommand(token, quotationCommand, AppType.RiskMonitor);
                this.BroadcastCommand(token, quotationCommand, AppType.Mobile);
            }


            {
                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                ResetCommand resetCommand = new ResetCommand(commandSequence);
                resetCommand.InstrumentID = XmlConvert.ToGuid(resetNode.Attributes["InstrumentID"].Value);
                resetCommand.Content = resetNode.FirstChild; //Remove the <accounts> level

                this.BroadcastCommand(token, resetCommand, AppType.TradingConsole);
                this.BroadcastCommand(token, resetCommand, AppType.RiskMonitor);
            }
        }

        public void Alert(Token token, XmlNode alertNode)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            AlertCommand alertCommand = new AlertCommand(commandSequence);
            alertCommand.AccountID = XmlConvert.ToGuid(alertNode["AlertAccounts"]["Account"].Attributes["ID"].Value);
            alertCommand.Content = alertNode;

            this.BroadcastCommand(token, alertCommand, AppType.TradingConsole);
            this.BroadcastCommand(token, alertCommand, AppType.RiskMonitor);
        }

        public void Cut(Token token, XmlNode cutNode)
        {
            int commandSequence = Interlocked.Increment(ref this.commandSequence);

            CutCommand cutCommand = new CutCommand(commandSequence);
            cutCommand.AccountID = XmlConvert.ToGuid(cutNode["Account"].Attributes["ID"].Value);
            cutCommand.Content = cutNode;
            this.BroadcastCommand(token, cutCommand, AppType.TradingConsole);
            this.BroadcastCommand(token, cutCommand, AppType.RiskMonitor);

            CutCommand cutCommand2 = new CutCommand(commandSequence);
            cutCommand2.AccountID = cutCommand.AccountID;
            cutCommand2.Content = cutNode["Account"]["Transactions"];
            this.BroadcastCommand(token, cutCommand2, AppType.DealingConsole);
            this.BroadcastCommand(token, cutCommand2, AppType.TradingMonitor);

            this.OnCut(cutCommand2.Content.Clone());
        }

        public void BroadcastCommands(Token token, Command[] commands)
        {
            foreach (Command command in commands)
            {
                if (command == null)
                {
                    AppDebug.LogEvent("StateServer", "BroadcastCommands command is null", EventLogEntryType.Error);
                    continue;
                }

                PlaceCommand placeCommand = command as PlaceCommand;
                if (placeCommand == null || !placeCommand.IsAutoFill)
                {
                    this.BroadcastCommand(token, command, AppType.DealingConsole);
                }
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);
                this.BroadcastCommand(token, command, AppType.PhysicalTerminal);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
            }
        }

        public void BroadcastCommand(Token token, Command[] commands)
        {
            if (token.AppType == AppType.TransactionServer)
            {
                foreach (Command command in commands)
                {
                    this.BroadcastCommand(token, command, AppType.DealingConsole);
                    this.BroadcastCommand(token, command, AppType.TradingConsole);
                    this.BroadcastCommand(token, command, AppType.TradingMonitor);

                    if (command is AccountUpdateCommand) this.BroadcastCommand(token, command, AppType.RiskMonitor);
                }
            }
        }

        public bool BroadcastTradeCommand(Token token, iExchange.Common.External.CME.TradeCommand tradeCommand)
        {
            try
            {
                if (token.AppType == AppType.TransactionServer)
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
                    {
                        connection.Open();
                        SqlCommand sqlCommand = connection.CreateCommand();
                        sqlCommand.CommandText = "Bursa.TradeMessage_Add";
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        SqlCommandBuilder.DeriveParameters(sqlCommand);
                        sqlCommand.Parameters["@messageId"].Value = tradeCommand.TradeMessage.MessageId;
                        sqlCommand.Parameters["@instrumentId"].Value = tradeCommand.TradeMessage.InstrumentId;
                        sqlCommand.Parameters["@timestamp"].Value = tradeCommand.TradeMessage.Timestamp;
                        sqlCommand.Parameters["@price"].Value = tradeCommand.TradeMessage.Price;
                        sqlCommand.Parameters["@aggressorSide"].Value = tradeCommand.TradeMessage.AggressorSide;
                        sqlCommand.Parameters["@volume"].Value = tradeCommand.TradeMessage.Volume;
                        sqlCommand.Parameters["@tradeVolume"].Value = tradeCommand.TradeMessage.TradeVolume;
                        sqlCommand.ExecuteNonQuery();

                        if ((int)sqlCommand.Parameters["@RETURN_VALUE"].Value == 0)
                        {
                            tradeCommand.TradeMessage.TotalVolume = (double)sqlCommand.Parameters["@totalVolume"].Value;
                            tradeCommand.TradeMessage.Transactions = (int)sqlCommand.Parameters["@transactions"].Value;
                            this.BroadcastCommand(token, tradeCommand, AppType.TradingConsole);
                        }
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", "BroadcastTradeCommand failed:" + e.ToString(), EventLogEntryType.Error);
            }
            return false;
        }

        public bool BroadcastOpenInterestCommand(Token token, iExchange.Common.External.CME.OpenInterestCommand openInterestCommand)
        {
            try
            {
                if (token.AppType == AppType.TransactionServer)
                {
                    using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
                    {
                        connection.Open();
                        SqlCommand sqlCommand = connection.CreateCommand();
                        sqlCommand.CommandText = "Bursa.OpenInterest_Set";
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        SqlCommandBuilder.DeriveParameters(sqlCommand);
                        sqlCommand.Parameters["@instrumentId"].Value = openInterestCommand.InstrumentId;
                        sqlCommand.Parameters["@openInterestQuantity"].Value = openInterestCommand.OpenInterestQuantity;
                        sqlCommand.ExecuteNonQuery();
                        this.BroadcastCommand(token, openInterestCommand, AppType.TradingConsole);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", "BroadcastOpenInterestCommand failed:" + e.ToString(), EventLogEntryType.Error);
            }
            return false;
        }

        public bool ChangeLeverage(Token token, Guid accountId, int leverage, out decimal necessary)
        {
            if (this.transactionAdaptor != null)
            {
                return this.transactionAdaptor.ChangeLeverage(token, accountId, leverage, out necessary);
            }
            else
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                return transactionServer.ChangeLeverage(token, accountId, leverage, out necessary);
            }
        }

        private QuotationServer.Service GetQuotationServer(Guid instrumentID)
        {
            return (QuotationServer.Service)this.quotationServers[0];
        }
        private TransactionServer.Service GetTransactionServer(Guid accountID)
        {
            return (TransactionServer.Service)this.transactionServers[0];
        }

        public void Broadcast(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            this.Broadcast(token, originQs, overridedQs, null);
        }

        private void Broadcast(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs, Guid[] instrumentIds)
        {
            if ((originQs != null && originQs.Length > 0) || (overridedQs != null && overridedQs.Length > 0))
            {
                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                //DealingConsole
                QuotationCommand quotationCommand = new QuotationCommand(commandSequence);
                quotationCommand.OverridedQs = overridedQs;
                quotationCommand.OriginQs = originQs;
                this.BroadcastCommand(token, quotationCommand, AppType.DealingConsole);

                //TradingConsole
                quotationCommand = new QuotationCommand(commandSequence);
                quotationCommand.OverridedQs = overridedQs;
                quotationCommand.OriginQs = null;
                if (originQs != null)
                {
                    ArrayList originQs2 = new ArrayList();
                    foreach (OriginQuotation oq in originQs)
                    {
                        if (oq.HasWatchOnlyQuotePolicies)
                        {
                            originQs2.Add(oq);
                        }
                        else if (oq.Origin == null)
                        {
                            OriginQuotation oq2 = (OriginQuotation)oq.Clone();
                            oq2.Ask = null;
                            oq2.Bid = null;

                            originQs2.Add(oq2);
                        }
                    }

                    if (originQs2.Count > 0)
                    {
                        quotationCommand.OriginQs = (OriginQuotation[])originQs2.ToArray(typeof(OriginQuotation));
                    }
                }
                if (quotationCommand.OriginQs != null || quotationCommand.OverridedQs != null)
                {
                    this.BroadcastCommand(token, quotationCommand, AppType.TradingConsole);
                }

                //RiskMonitor 
                quotationCommand = new QuotationCommand(commandSequence);
                quotationCommand.OverridedQs = overridedQs;
                quotationCommand.OriginQs = null;
                if (originQs != null)
                {
                    ArrayList originQs2 = new ArrayList();
                    foreach (OriginQuotation oq in originQs)
                    {
                        if (!oq.HasWatchOnlyQuotePolicies)
                        {
                            originQs2.Add(oq);
                        }
                    }

                    if (originQs2.Count > 0)
                    {
                        quotationCommand.OriginQs = (OriginQuotation[])originQs2.ToArray(typeof(OriginQuotation));
                    }
                }
                if (quotationCommand.OriginQs != null || quotationCommand.OverridedQs != null)
                {
                    this.BroadcastCommand(token, quotationCommand, AppType.RiskMonitor);
                }

                //Mobile 
                quotationCommand = new QuotationCommand(commandSequence);
                quotationCommand.OverridedQs = overridedQs;
                quotationCommand.OriginQs = originQs;
                if (quotationCommand.OriginQs != null || quotationCommand.OverridedQs != null)
                {
                    this.BroadcastCommand(token, quotationCommand, AppType.Mobile);
                }
            }

            //TransactionServer
            if (instrumentIds != null)
            {
                this.BroadcastQuotationToTransactionServer2(token, originQs, overridedQs, instrumentIds);
            }
            else
            {
                if (_transactionAdapterTester != null) _transactionAdapterTester.SetQuotation(originQs, overridedQs);

                if (this.transactionAdaptor != null)
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    this.transactionAdaptor.SetQuotation(originQs, overridedQs);
                    watch.Stop();
                    if (watch.Elapsed.TotalMilliseconds > 10)
                    {
                        AppDebug.LogEvent("StateServer", string.Format("transactionAdaptor.SetQuotation consume time={0} ms", watch.Elapsed.TotalMilliseconds), EventLogEntryType.Warning);
                    }
                }
                else
                {
                    this.BroadcastQuotationToTransactionServer(token, originQs, overridedQs);
                }
            }
        }

        private void BroadcastQuotationToTransactionServer2(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs, Guid[] instrumentIds)
        {
            //TransactionServer
            foreach (TransactionServer.Service transactionServer in this.transactionServers)
            {
                try
                {
                    transactionServer.SetQuotationOfSettlement(token, originQs, overridedQs, instrumentIds);
                }
                catch (Exception e)
                {
                    AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                }
            }
        }

        private void BroadcastQuotationToTransactionServer(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            QuotationBroadcastHelper.AddQuotation(new QuotationForBroadcast { Token = token, OriginQuotations = originQs, OverridedQuotations = overridedQs });
        }

        //private void BroadcastQuotationToTransactionServer()
        //{
        //    foreach (TransactionServer.Service transactionServer in this.transactionServers)
        //    {
        //        try
        //        {
        //            AsyncCallback cb = new AsyncCallback(this.BroadcastQuotationToTransactionServerCallback);
        //            transactionServer.BeginSetQuotation(token, originQs, overridedQs, cb, transactionServer);
        //        }
        //        catch (Exception e)
        //        {
        //            AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
        //        }
        //    }
        //}

        public void AfterBroadcastQuotationToTransactionServer(Token token, XmlNode xmlHitOrders, AutoFillResult[] autoFillResults, TransactionServer.Service transactionServer)
        {
            try
            {
                if (this._UseManager) token.AppType = AppType.StateServer;
                //Build HitCommand
                if (xmlHitOrders != null)
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);
                    HitCommand hitCommand = new HitCommand(commandSequence);
                    hitCommand.Content = xmlHitOrders;
                    this.BroadcastCommand(token, hitCommand, AppType.DealingConsole);
                }

                //Process the result of auto fill
                if (autoFillResults != null)
                {
                    ArrayList failedExecuteCommands = new ArrayList();
                    ArrayList succeededExecuteCommands = new ArrayList();
                    ArrayList autoFilledXmlTrans = new ArrayList();

                    foreach (AutoFillResult autoFillResult in autoFillResults)
                    {
                        int commandSequence = Interlocked.Increment(ref this.commandSequence);

                        TransactionError error = autoFillResult.ErrorCode;
                        XmlNode xmlAccount = autoFillResult.XmlAccount;
                        XmlNode xmlTran = autoFillResult.XmlTran;
                        Guid tranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);

                        if (error != TransactionError.OK)
                        {
                            if (xmlAccount != null)
                            {
                                //TradingConsole
                                ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                                executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                                executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                                executeCommand.TranID = tranID;
                                executeCommand.ErrorCode = error;

                                XmlDocument xmlDoc = new XmlDocument();
                                XmlElement content = xmlDoc.CreateElement("Content");
                                executeCommand.Content = content;

                                //TradingConsole
                                XmlElement tran2 = xmlDoc.CreateElement("Transaction");
                                content.AppendChild(tran2);

                                tran2.SetAttribute("ID", XmlConvert.ToString(tranID));
                                tran2.SetAttribute("ErrorCode", error.ToString());

                                failedExecuteCommands.Add(executeCommand);
                            }
                        }
                        else
                        {
                            ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                            executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                            executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                            executeCommand.TranID = tranID;

                            XmlDocument xmlDoc = new XmlDocument();
                            XmlElement content = xmlDoc.CreateElement("Content");
                            executeCommand.Content = content;

                            content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                            content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                            succeededExecuteCommands.Add(executeCommand);
                            autoFilledXmlTrans.Add(xmlTran);
                            ThreadPool.QueueUserWorkItem(this.CancelRelatedInvalidOrdersIfExists, tranID);
                        }
                    }

                    if (failedExecuteCommands.Count > 0)
                    {
                        CompositeCommand compositeCommand = new CompositeCommand();
                        compositeCommand.Commands = (Command[])failedExecuteCommands.ToArray(typeof(Command));

                        this.BroadcastCommand(token, compositeCommand, AppType.TradingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.DealingConsole);
                    }

                    if (succeededExecuteCommands.Count > 0)
                    {
                        CompositeCommand compositeCommand = new CompositeCommand();
                        compositeCommand.Commands = (Command[])succeededExecuteCommands.ToArray(typeof(Command));

                        this.BroadcastCommand(token, compositeCommand, AppType.TradingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.DealingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.RiskMonitor);
                        this.BroadcastCommand(token, compositeCommand, AppType.TradingMonitor);

                        foreach (XmlNode xmlTran in autoFilledXmlTrans)
                        {
                            this.OnTranExecuted(xmlTran.Clone());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        private void BroadcastQuotationToTransactionServerCallback(IAsyncResult ar)
        {
            try
            {
                AutoFillResult[] autoFillResults;

                Token token = new Token(Guid.Empty, UserType.System, AppType.TransactionServer);
                TransactionServer.Service transactionServer = (TransactionServer.Service)ar.AsyncState;

                XmlNode xmlHitOrders = transactionServer.EndSetQuotation(ar, out autoFillResults);

                //Build HitCommand
                if (xmlHitOrders != null)
                {
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);
                    HitCommand hitCommand = new HitCommand(commandSequence);
                    hitCommand.Content = xmlHitOrders;
                    this.BroadcastCommand(token, hitCommand, AppType.DealingConsole);
                }

                //Process the result of auto fill
                if (autoFillResults != null)
                {
                    ArrayList failedExecuteCommands = new ArrayList();
                    ArrayList succeededExecuteCommands = new ArrayList();
                    ArrayList autoFilledXmlTrans = new ArrayList();

                    foreach (AutoFillResult autoFillResult in autoFillResults)
                    {
                        int commandSequence = Interlocked.Increment(ref this.commandSequence);

                        TransactionError error = autoFillResult.ErrorCode;
                        XmlNode xmlAccount = autoFillResult.XmlAccount;
                        XmlNode xmlTran = autoFillResult.XmlTran;
                        Guid tranID = XmlConvert.ToGuid(xmlTran.Attributes["ID"].Value);

                        if (error != TransactionError.OK)
                        {
                            if (xmlAccount != null)
                            {
                                //TradingConsole
                                ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                                executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                                executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                                executeCommand.TranID = tranID;
                                executeCommand.ErrorCode = error;

                                XmlDocument xmlDoc = new XmlDocument();
                                XmlElement content = xmlDoc.CreateElement("Content");
                                executeCommand.Content = content;

                                //TradingConsole
                                XmlElement tran2 = xmlDoc.CreateElement("Transaction");
                                content.AppendChild(tran2);

                                tran2.SetAttribute("ID", XmlConvert.ToString(tranID));
                                tran2.SetAttribute("ErrorCode", error.ToString());

                                failedExecuteCommands.Add(executeCommand);
                            }
                        }
                        else
                        {
                            ExecuteCommand executeCommand = new ExecuteCommand(commandSequence);
                            executeCommand.AccountID = XmlConvert.ToGuid(xmlAccount.Attributes["ID"].Value);
                            executeCommand.InstrumentID = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                            executeCommand.TranID = tranID;

                            XmlDocument xmlDoc = new XmlDocument();
                            XmlElement content = xmlDoc.CreateElement("Content");
                            executeCommand.Content = content;

                            content.AppendChild(xmlDoc.ImportNode(xmlAccount, true));
                            content.AppendChild(xmlDoc.ImportNode(xmlTran, true));

                            succeededExecuteCommands.Add(executeCommand);
                            autoFilledXmlTrans.Add(xmlTran);
                            ThreadPool.QueueUserWorkItem(this.CancelRelatedInvalidOrdersIfExists, tranID);
                        }
                    }

                    if (failedExecuteCommands.Count > 0)
                    {
                        CompositeCommand compositeCommand = new CompositeCommand();
                        compositeCommand.Commands = (Command[])failedExecuteCommands.ToArray(typeof(Command));

                        this.BroadcastCommand(token, compositeCommand, AppType.TradingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.DealingConsole);
                    }

                    if (succeededExecuteCommands.Count > 0)
                    {
                        CompositeCommand compositeCommand = new CompositeCommand();
                        compositeCommand.Commands = (Command[])succeededExecuteCommands.ToArray(typeof(Command));

                        this.BroadcastCommand(token, compositeCommand, AppType.TradingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.DealingConsole);
                        this.BroadcastCommand(token, compositeCommand, AppType.RiskMonitor);
                        this.BroadcastCommand(token, compositeCommand, AppType.TradingMonitor);

                        foreach (XmlNode xmlTran in autoFilledXmlTrans)
                        {
                            this.OnTranExecuted(xmlTran.Clone());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        public XmlNode GetAccountStatus(Token token, Guid accountId, Guid instrumentId, OrderType orderType, bool needOutputPrice, out string buyPrice, out string sellPrice)
        {
            if (this.transactionAdaptor != null)
            {
                return this.transactionAdaptor.GetAccountStatus(accountId, instrumentId, orderType, needOutputPrice, out buyPrice, out sellPrice);
            }
            else
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                return transactionServer.GetAccountStatus(token, accountId, instrumentId, orderType, needOutputPrice, out buyPrice, out sellPrice);
            }
        }

        class BroadcastCommandState
        {
            public Token Source;
            public object Target;
            public string TargetUrl;
            public Command Command;

            public BroadcastCommandState(Token source, object target, string targetUrl, Command command)
            {
                this.Source = source;
                this.Target = target;
                this.TargetUrl = targetUrl;
                this.Command = command;
            }

            public override string ToString()
            {
                return string.Format("Source = {0}, Target = {1}, TargetUrl = {2} \n Command = {3}", this.Source, this.Target, this.TargetUrl, this.Command);
            }
        }

        private void BroadcastCommand(Token token, Command command, AppType targetApp)
        {
            try
            {
                this.rwLock.AcquireReaderLock(Timeout.Infinite);

                switch (targetApp)
                {
                    case AppType.Manager:
                        ManagerClient.AddCommand(command);
                        break;
                    case AppType.DealingConsole:
                        if (this._UseManager)
                        {
                            if (token.AppType != AppType.Manager && token.AppType != AppType.QuotationServer)
                            {
                                ManagerClient.AddCommand(command);
                            }
                        }
                        else
                        {
                            foreach (DealingConsole.Service2 dealingConsole in this.dealingConsoles)
                            {
                                BroadcastCommandState broadcastCommandState = new BroadcastCommandState(token, dealingConsole, dealingConsole.Url, command);
                                try
                                {
                                    AsyncCallback cb = new AsyncCallback(this.BroadcastCommandCallback);
                                    dealingConsole.BeginAddCommand(token, command, cb, broadcastCommandState);
                                }
                                catch (Exception e)
                                {
                                    AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n BroadcastCommandState = {1}", e, broadcastCommandState), EventLogEntryType.Error);
                                }
                            }
                        }
                        break;
                    case AppType.TradingConsole:
                        foreach (TradingConsole.Service2 tradingConsole in this.tradingConsoles)
                        {
                            BroadcastCommandState broadcastCommandState = new BroadcastCommandState(token, tradingConsole, tradingConsole.Url, command);
                            try
                            {
                                AsyncCallback cb = new AsyncCallback(this.BroadcastCommandCallback);
                                tradingConsole.BeginAddCommand(token, command, cb, broadcastCommandState);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n Token = {1},TargetApp = {2}\n Command = {3}", e, token, targetApp, command), EventLogEntryType.Error);
                            }
                        }

                        this.slTraderManager.BroadcastCommand(token, command);

                        if (command is MatchInfoCommand)
                        {
                            MatchInfoCommand matchInfoCommand = (MatchInfoCommand)command;
                            if (matchInfoCommand.AccountIds == null) // MatchInfoCommand come from CME
                            {
                                this.matchInfoCommandBuffer.UpdateCache(matchInfoCommand);
                            }
                        }
                        break;
                    case AppType.RiskMonitor:
                        foreach (RiskMonitor.Service2 riskMonitor in this.riskMonitors)
                        {
                            BroadcastCommandState broadcastCommandState = new BroadcastCommandState(token, riskMonitor, riskMonitor.Url, command);
                            try
                            {
                                AsyncCallback cb = new AsyncCallback(this.BroadcastCommandCallback);
                                riskMonitor.BeginAddCommand(token, command, cb, broadcastCommandState);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n Token = {1},TargetApp = {2}\n Command = {3}", e, token, targetApp, command), EventLogEntryType.Error);
                            }
                        }
                        break;
                    case AppType.TradingMonitor:
                        foreach (TradingMonitor.Service2 tradingMonitor in this.tradingMonitors)
                        {
                            BroadcastCommandState broadcastCommandState = new BroadcastCommandState(token, tradingMonitor, tradingMonitor.Url, command);
                            try
                            {
                                AsyncCallback cb = new AsyncCallback(this.BroadcastCommandCallback);
                                tradingMonitor.BeginAddCommand(token, command, cb, broadcastCommandState);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n Token = {1},TargetApp = {2}\n Command = {3}", e, token, targetApp, command), EventLogEntryType.Error);
                            }
                        }
                        break;
                    /*	case AppType.Mobile:
                            foreach(Mobile.Service2 mobile in this.mobiles)
                            {
                                try
                                {
                                    AsyncCallback cb = new AsyncCallback(this.BroadcastCommandCallback);
                                    mobile.BeginAddCommand(token,command,cb,mobile);
                                }
                                catch(Exception e)
                                {
                                    AppDebug.LogEvent("StateServer",e.ToString(),EventLogEntryType.Error);
                                }
                            }
                            break;
                     */
                    case AppType.PhysicalTerminal:
                        DeliveryCommand deliveryCommand = command as DeliveryCommand;
                        if (deliveryCommand != null)
                        {
                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            var deliveryRequest = new
                            {
                                RequestId = deliveryCommand.deliveryNode.Attributes["Id"].Value,
                                AccountId = deliveryCommand.deliveryNode.Attributes["AccountId"].Value,
                                InstrumentId = deliveryCommand.deliveryNode.Attributes["InstrumentId"].Value,
                                RequireQuantity = deliveryCommand.deliveryNode.Attributes["RequireQuantity"].Value
                            };
                            string json = serializer.Serialize(deliveryRequest);
                            byte[] data = Encoding.UTF8.GetBytes(json);

                            string physicalTerminalUrl = ConfigurationManager.AppSettings["PhysicalTerminalUrl"];
                            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(physicalTerminalUrl);
                            request.Method = "POST";
                            request.ContentType = "application/json; charset=utf-8";
                            request.ContentLength = data.Length;
                            request.BeginGetRequestStream(delegate(IAsyncResult asyncResult)
                            {
                                try
                                {
                                    using (Stream stream = request.EndGetRequestStream(asyncResult))
                                    {
                                        stream.Write(data, 0, data.Length);
                                    }
                                }
                                catch (Exception exception)
                                {
                                    AppDebug.LogEvent("StateServer BroadcastCommand EndGetRequestStream", exception.ToString(), EventLogEntryType.Error);
                                }
                            }, null);
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer BroadcastCommand", exception.ToString(), EventLogEntryType.Warning);
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }
        }

        private void BroadcastCommandCallback(IAsyncResult ar)
        {
            BroadcastCommandState broadcastCommandState = ar.AsyncState as BroadcastCommandState;
            if (broadcastCommandState == null)
            {
                AppDebug.LogEvent("StateServer", "IAsyncResult ar is not valid", EventLogEntryType.Error);
                return;
            }

            try
            {
                object target = broadcastCommandState.Target;

                if (target is DealingConsole.Service2)
                {
                    DealingConsole.Service2 dealingConsole = (DealingConsole.Service2)target;
                    dealingConsole.EndAddCommand(ar);
                }
                else if (target is TradingConsole.Service2)
                {
                    TradingConsole.Service2 tradingConsole = (TradingConsole.Service2)target;
                    tradingConsole.EndAddCommand(ar);
                }
                else if (target is ICommandCollectService)
                {
                    //ICommandCollectService commandCollect = (ICommandCollectService)target;
                    //try
                    //{
                    //    commandCollect.EndAddCommand(ar);
                    //}
                    //catch (Exception e)
                    //{
                    //    if (e is FaultException || e is CommunicationObjectFaultedException || e is TimeoutException)
                    //    {
                    //        AppDebug.LogEvent("StateServer", "Try to recover channel to trader sl", EventLogEntryType.Warning);
                    //        this.CreateChannelForTrderSL(broadcastCommandState.TargetUrl);
                    //    }
                    //    else
                    //    {
                    //        throw;
                    //    }
                    //}
                }
                else if (target is RiskMonitor.Service2)
                {
                    RiskMonitor.Service2 riskMonitor = (RiskMonitor.Service2)target;
                    riskMonitor.EndAddCommand(ar);
                }
                else if (target is TradingMonitor.Service2)
                {
                    TradingMonitor.Service2 tradingMonitor = (TradingMonitor.Service2)target;
                    tradingMonitor.EndAddCommand(ar);
                }
                //else if (service is Mobile.Service2)
                //{
                //    Mobile.Service2 mobile = (Mobile.Service2)service;
                //    mobile.EndAddCommand(ar);
                //}
                else
                {
                    AppDebug.LogEvent("StateServer", "service is not valid", EventLogEntryType.Error);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", string.Format("Exception = {0}\n\n BroadcastCommandState = {1}", e, broadcastCommandState), EventLogEntryType.Error);
            }
        }

        private void BroadcastLoginState(Token token, bool isLogin)
        {
            try
            {
                this.rwLock.AcquireReaderLock(Timeout.Infinite);

                switch (token.AppType)
                {
                    case AppType.DealingConsole:
                        foreach (DealingConsole.Service2 dealingConsole in this.dealingConsoles)
                        {
                            try
                            {
                                dealingConsole.UpdateLoginState(token, isLogin);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        break;
                    case AppType.TradingConsole:
                        foreach (TradingConsole.Service2 tradingConsole in this.tradingConsoles)
                        {
                            try
                            {
                                //in fact no tradingConsole on duty now, all trader servers are all in SLTraderManager
                                tradingConsole.BeginUpdateLoginState(token, isLogin, null, null);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        break;
                    case AppType.RiskMonitor:
                        foreach (RiskMonitor.Service2 riskMonitor in this.riskMonitors)
                        {
                            try
                            {
                                riskMonitor.UpdateLoginState(token, isLogin);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        break;
                    case AppType.TradingMonitor:
                        foreach (TradingMonitor.Service2 tradingMonitor in this.tradingMonitors)
                        {
                            try
                            {
                                tradingMonitor.UpdateLoginState(token, isLogin);
                            }
                            catch (Exception e)
                            {
                                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                            }
                        }
                        break;
                    /*	case AppType.Mobile:
                            foreach(Mobile.Service2 mobile in this.mobiles)
                            {
                                try
                                {
                                    IAsyncResult asyncResult =mobile.BeginUpdateLoginState(token,isLogin,null,null);
                                    asyncResult.AsyncWaitHandle.WaitOne();
                                    mobile.EndUpdateLoginState(asyncResult);
                                }
                                catch(Exception e)
                                {
                                    AppDebug.LogEvent("StateServer",e.ToString(),EventLogEntryType.Error);
                                }
                            }
                            break;
                     */
                }
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }
        }

        //Relation

        public bool Update(Token token, XmlNode update)
        {
            return this.Update(token, update, false);
        }

        public bool Update(Token token, XmlNode update, bool notifyRelationToTheSame)
        {
            this.rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                AppDebug.LogEvent("StateServer", token.ToString() + "\r\n" + update.OuterXml.ToString(), EventLogEntryType.Information);

                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                UpdateCommand updateCommand = new UpdateCommand(commandSequence);
                updateCommand.Content = update;
                if (token.AppType == AppType.DealingConsole)
                {
                    if (this.transactionAdaptor != null) this.transactionAdaptor.Update(token, update);
                    if (_transactionAdapterTester != null) _transactionAdapterTester.Update(token, update);

                    this.BroadcastCommand(token, updateCommand, AppType.TradingConsole);

                    //Notify other DealingConsole
                    //if (notifyRelationToTheSame)
                    {
                        this.BroadcastCommand(token, updateCommand, AppType.DealingConsole);
                    }

                    //Notify QuotationServer
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    quotationServer.Update(token, update);

                    //Notify TransactionServer
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    transactionServer.Update(token, update);

                }
                else if (token.AppType == AppType.QuotationServer && token.SessionID != "NotifyOpenPrice")
                {
                    if (this.transactionAdaptor != null) this.transactionAdaptor.Update(token, update);
                    if (_transactionAdapterTester != null) _transactionAdapterTester.Update(token, update);
                    this.BroadcastCommand(token, updateCommand, AppType.DealingConsole);
                    this.BroadcastCommand(token, updateCommand, AppType.TradingConsole);

                    //Notify TransactionServer
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    transactionServer.Update(token, update);
                }
                else if (token.AppType == AppType.TransactionServer)
                {
                    //Notify TradingConsole ,DealingConsole,RiskMonitor
                    this.BroadcastCommand(token, updateCommand, AppType.DealingConsole);
                    this.BroadcastCommand(token, updateCommand, AppType.TradingConsole);
                    this.BroadcastCommand(token, updateCommand, AppType.RiskMonitor);
                }
                else
                {
                    if (this.transactionAdaptor != null) this.transactionAdaptor.Update(token, update);
                    if (_transactionAdapterTester != null) _transactionAdapterTester.Update(token, update);
                    //Notify TradingConsole ,DealingConsole,RiskMonitor
                    this.BroadcastCommand(token, updateCommand, AppType.DealingConsole);
                    this.BroadcastCommand(token, updateCommand, AppType.TradingConsole);
                    this.BroadcastCommand(token, updateCommand, AppType.RiskMonitor);

                    if (token.AppType != AppType.QuotationServer && !this._UseManager)
                    {
                        //Notify QuotationServer
                        QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                        quotationServer.Update(token, update);
                    }

                    if (ShouldUserTransactionServer())
                    {
                        //Notify TransactionServer
                        TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                        transactionServer.Update(token, update);
                    }
                }

                if (token.AppType == AppType.BackOffice)
                {
                    if (update.Name != "NotifyChangeGroup")
                    {

                        //When (Private/Public)DailyQuotation changed, transactionServer will try to do reset which will use the settlement price to calculate interest and tradePLFloat
                        this.CheckDailyQuotationChanged(token, update);

                        if (update.InnerXml.IndexOf("CrossAccountMapping") >= 0)
                        {
                            this.LinkStateServer(this.connectionString);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }

            return true;
        }

        private void CheckDailyQuotationChanged(Token token, XmlNode update)
        {
            AppDebug.LogEvent("StateServer", "Begin checkDailyQuotationChanged", EventLogEntryType.Information);

            #region Add by Sam,Broadcast quotation when (Private/Public)DailyQuotation changed.
            StringBuilder paramXml = new StringBuilder();
            string notifyQuotation = string.Empty;
            List<Guid> instrumentIds = new List<Guid>();

            foreach (XmlNode updateType in update.ChildNodes)
            {
                #region
                foreach (XmlNode updateObject in updateType.ChildNodes)
                {
                    switch (updateObject.Name)
                    {
                        case "PrivateDailyQuotation":
                            {
                                if (updateType.Name == "Add" || updateType.Name == "Modify")
                                {
                                    notifyQuotation = "PrivateDailyQuotation";
                                    paramXml.Append(string.Format(@"<Item TradeDay='{0}' InstrumentId='{1}'></Item>",
                                            updateObject.Attributes["TradeDay"].Value, updateObject.Attributes["InstrumentID"].Value));
                                    instrumentIds.Add(XmlConvert.ToGuid(updateObject.Attributes["InstrumentID"].Value));
                                }
                            }
                            break;
                        case "PublicDailyQuotation":
                            {
                                if (updateType.Name == "Add" || updateType.Name == "Modify")
                                {
                                    notifyQuotation = "PublicDailyQuotation";
                                    paramXml.Append(string.Format(@"<Item TradeDay='{0}' InstrumentOriginCode='{1}'></Item>",
                                            updateObject.Attributes["TradeDay"].Value, updateObject.Attributes["InstrumentOriginCode"].Value));
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                #endregion
            }
            if (!string.IsNullOrEmpty(notifyQuotation))
            {
                DataSet dataSet = new DataSet();
                using (SqlConnection connection = new SqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]))
                {
                    #region get data
                    SqlCommand command = null;
                    if (notifyQuotation == "PrivateDailyQuotation")
                    {
                        command = new SqlCommand("P_GetNotifyPrivateDailyQuotation", connection);
                    }
                    else
                    {
                        command = new SqlCommand("P_GetNotifyPublicDailyQuotation", connection);
                    }
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@xmlNotify", SqlDbType.NText));
                    command.Parameters["@xmlNotify"].Value = string.Format(@"<Notify>{0}</Notify>", paramXml.ToString());

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    bool mustClose = connection.State == ConnectionState.Closed;
                    try
                    {
                        if (mustClose)
                        {
                            connection.Open();
                        }
                        adapter.Fill(dataSet);
                    }
                    catch (Exception exception)
                    {
                        AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Error);
                    }
                    finally
                    {
                        if (mustClose)
                        {
                            connection.Close();
                        }
                    }
                    #endregion
                }
                if (dataSet.Tables.Count == 2 || dataSet.Tables.Count == 3)
                {
                    List<OriginQuotation> origins = new List<OriginQuotation>();
                    List<OverridedQuotation> overrideds = new List<OverridedQuotation>();

                    foreach (DataRow row in dataSet.Tables[0].Rows)
                    {
                        OriginQuotation origin = new OriginQuotation();
                        #region origins
                        origin.Timestamp = (DateTime)row["Timestamp"];
                        origin.InstrumentID = (Guid)row["InstrumentID"];
                        if (row["Ask"] != DBNull.Value)
                        {
                            origin.Ask = (string)row["Ask"];
                        }
                        if (row["Bid"] != DBNull.Value)
                        {
                            origin.Bid = (string)row["Bid"];
                        }
                        if (row["High"] != DBNull.Value)
                        {
                            origin.High = (string)row["High"];
                        }
                        if (row["Low"] != DBNull.Value)
                        {
                            origin.Low = (string)row["Low"];
                        }
                        #endregion
                        origins.Add(origin);
                    }

                    foreach (DataRow row in dataSet.Tables[1].Rows)
                    {
                        OverridedQuotation overrided = new OverridedQuotation();
                        #region overrides
                        overrided.Timestamp = (DateTime)row["Timestamp"];
                        overrided.InstrumentID = (Guid)row["InstrumentID"];
                        overrided.QuotePolicyID = (Guid)row["QuotePolicyID"];
                        if (row["Ask"] != DBNull.Value)
                        {
                            overrided.Ask = (string)row["Ask"];
                        }
                        if (row["Bid"] != DBNull.Value)
                        {
                            overrided.Bid = (string)row["Bid"];
                        }
                        if (row["High"] != DBNull.Value)
                        {
                            overrided.High = (string)row["High"];
                        }
                        if (row["Low"] != DBNull.Value)
                        {
                            overrided.Low = (string)row["Low"];
                        }
                        #endregion
                        overrideds.Add(overrided);
                    }

                    if (dataSet.Tables.Count == 3)
                    {
                        foreach (DataRow row in dataSet.Tables[2].Rows)
                        {
                            instrumentIds.Add((Guid)row["InstrumentId"]);
                        }
                    }

                    if (overrideds.Count > 0 || origins.Count > 0 || instrumentIds.Count > 0)
                    {
                        this.Broadcast(token, origins.ToArray(), overrideds.ToArray(), instrumentIds.ToArray());
                    }
                }
            }
            #endregion

            AppDebug.LogEvent("StateServer", "End checkDailyQuotationChanged", EventLogEntryType.Information);
        }

        private void OnCut(XmlNode transNode)
        {
            try
            {
                foreach (XmlElement tranNode in transNode.ChildNodes)
                {
                    this.OnTranExecuted(tranNode);
                    Guid tranId = new Guid(tranNode.Attributes["ID"].Value);
                    ThreadPool.QueueUserWorkItem(this.CancelRelatedInvalidOrdersIfExists, tranId);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        private void OnTranExecuted(XmlNode xmlTran)
        {
            //AppDebug.LogEvent("StateServer", string.Format("StateServer.OnTranExecuted xmlTran = {0}", xmlTran), EventLogEntryType.Information);

            ThreadPool.QueueUserWorkItem(this.MapToLinkedAccounts, xmlTran);
            ThreadPool.QueueUserWorkItem(this.MapToUpperLevel, xmlTran);
        }

        public void OnTranExecutedForNewVersion(XmlNode xmlTran)
        {
            this.orderMappingBooker.Add(xmlTran);
            ThreadPool.QueueUserWorkItem(this.MapToUpperLevel, xmlTran);
        }


        private void MapToUpperLevel(object args)
        {
            XmlNode xmlTran = (XmlNode)args;

            //added by adam on 2009-02-05
            //send the xmlTran to HeadOffice if work in filiale mode
            string workMode = ConfigurationManager.AppSettings["workMode"];
            if (String.Equals(workMode, "Filiale", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    FilialeTransaction filialeTransaction = new FilialeTransaction();
                    filialeTransaction.Id = Guid.NewGuid();
                    filialeTransaction.XmlTran = xmlTran.OuterXml;

                    FilialeTransactionSwitcher.GetInstance().AddFilialeTransaction(filialeTransaction);
                }
                catch (Exception exception)
                {
                    AppDebug.LogEvent("StateServer", String.Format("AddFilialeTransaction error: {0},\r\n data: {1}", exception, xmlTran.OuterXml), EventLogEntryType.Error);
                }
            }
        }

        private void MapToLinkedAccounts(object args)
        {
            XmlNode xmlTran = (XmlNode)args;
            try
            {
                foreach (XmlNode xmlOrder in xmlTran.ChildNodes)
                {
                    bool isPhysicalOrder = false;
                    PhysicalTradeSide physicalTradeSide = PhysicalTradeSide.None;
                    if (xmlOrder.Attributes["PhysicalTradeSide"] != null)
                    {
                        physicalTradeSide = (PhysicalTradeSide)(XmlConvert.ToInt32(xmlOrder.Attributes["PhysicalTradeSide"].Value));

                    }
                    isPhysicalOrder = physicalTradeSide != PhysicalTradeSide.None;
                    if (isPhysicalOrder) return;

                    Guid orderID = XmlConvert.ToGuid(xmlOrder.Attributes["ID"].Value);
                    this.orderMappingBooker.BookOrderMappedBy(orderID);
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString() + "\n" + xmlTran.OuterXml, EventLogEntryType.Error);
            }
        }

        private TransactionError Book(MappedOrder tran)
        {
            TransactionError error;

            try
            {
                Token token = new Token(Guid.Empty, UserType.System, AppType.RiskMonitor);

                XmlNode xmlAccount, xmlAffectedOrders, xmlTran = tran.ToXmlNode();
                if (tran.IsLocal)
                {
                    error = this.Book(token, ref xmlTran, false, out xmlAccount, out xmlAffectedOrders);
                }
                else
                {
                    if (this.linkedStateServer == null)
                    {
                        AppDebug.LogEvent("StateServer", "Tran is set to link remote server, but the linked server is not set, please check 'iExchange.StateServer.LinkedStateServer.Service' in web.config of StateServer", EventLogEntryType.Error);
                        return TransactionError.NoLinkedServer;
                    }
                    else
                    {
                        Guid instrumentId = XmlConvert.ToGuid(xmlTran.Attributes["InstrumentID"].Value);
                        ((XmlElement)xmlTran).SetAttribute("InstrumentCode", this.instrumentId2MappingCode[instrumentId]);
                        error = this.linkedStateServer.Book(token, ref xmlTran, false, out xmlAccount, out xmlAffectedOrders);
                    }
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString() + "\n" + tran.ToXmlNode().OuterXml, EventLogEntryType.Error);
                error = TransactionError.RuntimeError;
            }

            return error;
        }

        public DataSet GetAccounts(Token token)
        {
            try
            {
                string sql = string.Format("Exec dbo.P_RptGetAccountsByUser '{0}'", token.UserID);
                DataSet account = DataAccess.GetData(sql, this.connectionString);
                if (account.Tables[0].Rows.Count == 0)
                {
                    return null;
                }
                else
                {
                    account.Tables[0].TableName = "Account";
                    return account;
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public bool SetNews(Token token, News[] newsCollection)
        {
            bool isSucceed = false;
            try
            {
                if (newsCollection.Length > 0)
                {
                    if (this.transactionAdaptor != null)
                    {
                        this.transactionAdaptor.SetNews(newsCollection);
                    }
                    int commandSequence = Interlocked.Increment(ref this.commandSequence);
                    NewsCommand newsCommand = new NewsCommand(commandSequence, newsCollection);
                    this.BroadcastCommand(token, newsCommand, AppType.TradingConsole);
                    isSucceed = true;
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer", exception.ToString(), EventLogEntryType.Error);
            }
            return isSucceed;
        }

        private Guid GetMappedOrderId(Guid orderId, Guid linkedAccountId, out bool? isOpposite)
        {
            isOpposite = null;
            using (SqlConnection sqlConnection = new SqlConnection(this.connectionString))
            {
                try
                {
                    SqlCommand command = sqlConnection.CreateCommand();
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.CommandText = "dbo.OrderMappings_Get";

                    command.Parameters.Add("@orderID", orderId);
                    command.Parameters.Add("@linkedAccountID", linkedAccountId);
                    command.Parameters.Add("@linkedOrderID", SqlDbType.UniqueIdentifier);
                    command.Parameters.Add("@isOpposite", SqlDbType.Bit);
                    command.Parameters["@linkedOrderID"].Direction = ParameterDirection.Output;
                    command.Parameters["@isOpposite"].Direction = ParameterDirection.Output;

                    sqlConnection.Open();
                    command.ExecuteNonQuery();

                    if (command.Parameters["@isOpposite"].Value != DBNull.Value)
                    {
                        isOpposite = (bool)command.Parameters["@isOpposite"].Value;
                    }

                    if (command.Parameters["@linkedOrderID"].Value == DBNull.Value)
                    {
                        return Guid.Empty;
                    }
                    else
                    {
                        return (Guid)command.Parameters["@linkedOrderID"].Value;
                    }
                }
                catch (Exception e)
                {
                    AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                }

                return Guid.Empty;
            }
        }

        private void SaveOrderMappingFailed(Guid tranId, Guid linkedAccountID, OrderMappingFailedReason reason)
        {
            try
            {
                string sql = string.Format("Exec dbo.OrderMappingFailed_Add '{0}','{1}','{2}'", tranId, linkedAccountID, (byte)reason);
                DataAccess.UpdateDB(sql, this.connectionString);
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
        }

        private bool SaveOrderMapping(Hashtable linkedOrders, bool needSaveLinkedOrderId, Guid linkedAccountID, bool isOpposite)
        {
            try
            {
                System.Text.StringBuilder sql = new StringBuilder();

                foreach (DictionaryEntry de in linkedOrders)
                {
                    Guid orderID = (Guid)de.Key;
                    Guid linkedOrderID = (Guid)de.Value;

                    if (needSaveLinkedOrderId)
                    {
                        sql.AppendFormat("Exec dbo.OrderMappings_Add '{0}','{1}','{2}','{3}'\n", orderID, linkedAccountID, linkedOrderID, isOpposite ? "1" : "0");
                    }
                    else
                    {
                        sql.AppendFormat("Exec dbo.OrderMappings_Add '{0}','{1}'\n", orderID, linkedAccountID);
                    }
                }

                DataAccess.UpdateDB(sql.ToString(), this.connectionString);
                return true;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        //Added by Michael on 2005-02-23
        //different region datetime will error at the different PC(Client & Server PC)
        public DataSet GetOrders(Token token, XmlNode xmlAccounts, string tradeDay)
        {
            try
            {
                SqlCommand command = new SqlCommand("dbo.P_GetOrders");
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.Add("@userID", SqlDbType.UniqueIdentifier);
                command.Parameters.Add("@xmlAccounts", SqlDbType.NText);
                command.Parameters.Add("@tradeDay", SqlDbType.NVarChar, 10);
                //command
                string xmlAccounts2 = DataAccess.ConvertToSqlXml(xmlAccounts.OuterXml);
                command.Parameters["@userID"].Value = token.UserID;
                command.Parameters["@xmlAccounts"].Value = xmlAccounts2;
                command.Parameters["@tradeDay"].Value = tradeDay;
                command.Connection = new SqlConnection(this.connectionString);

                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataAdapter.Fill(dataSet);

                return dataSet;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetOrders", e.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        private bool FixChart(Token token, Guid instrumentId, DateTime minTimestamp)
        {
            XmlDocument xmlFixChart = new XmlDocument();
            XmlElement fixChartData = xmlFixChart.CreateElement("FixChartData");
            xmlFixChart.AppendChild(fixChartData);
            fixChartData.SetAttribute("InstrumentId", XmlConvert.ToString(instrumentId));
            fixChartData.SetAttribute("MinTimestamp", XmlConvert.ToString(minTimestamp, DateTimeFormat.Xml2));

            this.rwLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                AppDebug.LogEvent("StateServer", token.ToString() + "\r\n InstrumentID=" + instrumentId.ToString() + " MinTimestamp=" + minTimestamp.ToString(DateTimeFormat.Xml2), EventLogEntryType.Information);

                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                FixChartCommand2 fixChartCommand = new FixChartCommand2(commandSequence);
                fixChartCommand.Content = fixChartData;
                if (token.AppType == AppType.DealingConsole)
                {
                    this.BroadcastCommand(token, fixChartCommand, AppType.TradingConsole);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }

            return true;
        }

        private bool FixChart(Token token, XmlNode fixChartData)
        {
            this.rwLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                AppDebug.LogEvent("StateServer", token.ToString() + "\r\n" + fixChartData.OuterXml.ToString(), EventLogEntryType.Information);

                int commandSequence = Interlocked.Increment(ref this.commandSequence);

                FixChartCommand fixChartCommand = new FixChartCommand(commandSequence);
                fixChartCommand.Content = fixChartData;
                if (token.AppType == AppType.DealingConsole)
                {
                    this.BroadcastCommand(token, fixChartCommand, AppType.TradingConsole);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                this.rwLock.ReleaseReaderLock();
            }

            return true;
        }

        public void UpdateHighLow(Token token, string ip, Guid instrumentId, bool isOriginHiLo, string newInput, bool isUpdateHigh, out int batchProcessId, out string instrumentCode, out bool highBid, out bool lowBid, out DateTime updateTime, out int returnValue, out string errorMessage)
        {
            //--0: Succeed
            //---1: FailedOnDBQuotation
            //---2: InvalidNewInput
            //---3: NotEffectedRecord
            //---4: FailedDeleteChartData
            //---5: FailedOnDBV3
            //-10: FailedCallOnQuotationServer
            //-11: FailedCallOnStateServer
            returnValue = -11;
            batchProcessId = -1;
            instrumentCode = string.Empty;
            highBid = true;
            lowBid = true;
            updateTime = DateTime.MinValue;
            errorMessage = "FailedCallOnStateServer";
            Common.OverridedQuotation[] overridedQs = null;
            string message = string.Format("Token={0},IP={1},InstrumentId={2},IsOriginHiLo={3},NewInput={4},IsUpdateHigh={5}", token.ToString(), ip, instrumentId, isOriginHiLo, newInput, isUpdateHigh);
            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    AppDebug.LogEvent("StateServer.UpdateHighLow.Begin", message, EventLogEntryType.Information);
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        DateTime minTimestamp = DateTime.MinValue;
                        quotationServer.UpdateHighLow(token, ip, instrumentId, isOriginHiLo, newInput, isUpdateHigh, out batchProcessId, out instrumentCode, out highBid, out lowBid, out updateTime, out minTimestamp, out overridedQs, out returnValue, out errorMessage);
                        if (returnValue == 0)
                        {
                            if (overridedQs != null)
                            {
                                this.rwLock.AcquireReaderLock(Timeout.Infinite);
                                try
                                {
                                    AppDebug.LogEvent("StateServer.UpdateHighLow.Broadcast.Begin", message, EventLogEntryType.Information);
                                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), null, overridedQs);
                                    AppDebug.LogEvent("StateServer.UpdateHighLow.Broadcast.End", message, EventLogEntryType.Information);
                                }
                                catch (Exception e)
                                {
                                    AppDebug.LogEvent("StateServer.UpdateHighLow.Broadcast(Exception)", message + "\r\n" + e.ToString(), EventLogEntryType.Error);
                                }
                                finally
                                {
                                    this.rwLock.ReleaseReaderLock();
                                }
                            }
                            if (!isOriginHiLo)
                            {
                                AppDebug.LogEvent("StateServer.UpdateHighLow.FixChart.Begin", message, EventLogEntryType.Information);
                                this.FixChart(token, instrumentId, minTimestamp);
                                AppDebug.LogEvent("StateServer.UpdateHighLow.FixChart.End", message, EventLogEntryType.Information);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer.UpdateHighLow(Exception)", message + "\r\n" + e.ToString(), EventLogEntryType.Error);
                    }
                    finally
                    {
                        AppDebug.LogEvent("StateServer.UpdateHighLow.End", message, EventLogEntryType.Information);
                    }
                    break;
            }
        }

        public void RestoreHighLow(Token token, string ip, int batchProcessId, out Guid instrumentId, out string instrumentCode, out string newInput, out bool isUpdateHigh, out bool highBid, out bool lowBid, out int returnValue, out string errorMessage)
        {
            //0: Succeed
            //-1: FailedOnDBQuotation
            //-2: ExitsNewestBatchUpdate
            //-3: NotEffectedRecord
            //-4: FailedDeleteChartData
            //-5: FailedOnDBV3
            //-10: FailedCallOnQuotationServer
            //-11: FailedCallOnStateServer
            returnValue = -11;
            instrumentId = Guid.Empty;
            instrumentCode = string.Empty;
            newInput = string.Empty;
            isUpdateHigh = true;
            highBid = true;
            lowBid = true;
            errorMessage = "FailedCallOnStateServer";
            Common.OverridedQuotation[] overridedQs = null;
            string message = string.Format("Token={0},IP={1},BatchProcessId={2}", token.ToString(), ip, batchProcessId);
            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    AppDebug.LogEvent("StateServer.RestoreHighLow.Begin", message, EventLogEntryType.Information);
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        DateTime minTimestamp = DateTime.MinValue;
                        quotationServer.RestoreHighLow(token, ip, batchProcessId, out instrumentId, out instrumentCode, out newInput, out isUpdateHigh, out highBid, out lowBid, out minTimestamp, out overridedQs, out returnValue, out errorMessage);
                        if (returnValue == 0)
                        {
                            if (overridedQs != null)
                            {
                                this.rwLock.AcquireReaderLock(Timeout.Infinite);
                                try
                                {
                                    AppDebug.LogEvent("StateServer.RestoreHighLow.Broadcast.Begin", message, EventLogEntryType.Information);
                                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), null, overridedQs);
                                    AppDebug.LogEvent("StateServer.RestoreHighLow.Broadcast.End", message, EventLogEntryType.Information);
                                }
                                catch (Exception e)
                                {
                                    AppDebug.LogEvent("StateServer.RestoreHighLow.Broadcast(Exception)", message + "\r\n" + e.ToString(), EventLogEntryType.Error);
                                }
                                finally
                                {
                                    this.rwLock.ReleaseReaderLock();
                                }
                            }
                            AppDebug.LogEvent("StateServer.RestoreHighLow.FixChart.Begin", message, EventLogEntryType.Information);
                            this.FixChart(token, instrumentId, minTimestamp);
                            AppDebug.LogEvent("StateServer.RestoreHighLow.FixChart.End", message, EventLogEntryType.Information);
                        }
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer.RestoreHighLow(Exception)", message + "\r\n" + e.ToString(), EventLogEntryType.Error);
                    }
                    finally
                    {
                        AppDebug.LogEvent("StateServer.RestoreHighLow.End", message, EventLogEntryType.Information);
                    }
                    break;
            }
        }

        public bool FixOverridedQuotationHistory(Token token, string quotation, bool needApplyAutoAdjustPoints)
        {
            bool isSucceed = false;
            bool needBroadcastQuotation = false;
            OriginQuotation[] originQs = null;
            OverridedQuotation[] overridedQs = null;
            XmlNode fixChartDatas = null;

            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.FixOverridedQuotationHistory(token, quotation, needApplyAutoAdjustPoints, out originQs, out overridedQs, out needBroadcastQuotation, out fixChartDatas);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if ((originQs != null || overridedQs != null) && needBroadcastQuotation)
                {
                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), null, overridedQs);
                }

                if (fixChartDatas != null)
                {
                    this.FixChart(token, fixChartDatas);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        public bool SetHistoryQuotation(Token token, DateTime tradeDay, string quotation, bool needApplyAutoAdjustPoints)
        {
            bool isSucceed = false;
            bool needBroadcast = false;
            OriginQuotation[] originQs = null;
            OverridedQuotation[] overridedQs = null;

            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.SetHistoryQuotation(token, tradeDay, quotation, needApplyAutoAdjustPoints, out originQs, out overridedQs, out needBroadcast);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if ((originQs != null || overridedQs != null) && needBroadcast)
                {
                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), null, overridedQs);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        public bool UpdateOverridedQuotationHighLow(Token token, Guid instrumentID, string quotation)
        {
            bool isSucceed = false;
            OverridedQuotation[] overridedQs = null;

            switch (token.AppType)
            {
                case AppType.DealingConsole:
                    QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
                    try
                    {
                        isSucceed = quotationServer.UpdateOverridedQuotationHighLow(token, instrumentID, quotation, out overridedQs);
                    }
                    catch (Exception e)
                    {
                        AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                    }
                    break;
            }

            try
            {
                if (overridedQs != null)
                {
                    this.Broadcast(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), null, overridedQs);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
            }

            return isSucceed;
        }

        internal bool SetBestLimit(Token token, BestLimit[] bestLimits)
        {
            QuotationServer.Service quotationServer = this.GetQuotationServer(Guid.Empty);
            DateTime timeStamp;
            if (quotationServer.SetBestLimit(token, bestLimits, out timeStamp))
            {
                foreach (BestLimit bestLimit in bestLimits)
                {
                    bestLimit.Timestamp = timeStamp;
                }
                BestLimitsCommand bestLimitsCommand = new BestLimitsCommand();
                bestLimitsCommand.BestLimits = bestLimits;

                this.BroadcastCommand(token, bestLimitsCommand, AppType.TradingConsole);
                return true;
            }
            else
            {
                return false;
            }
        }

        public MatchInfoCommand[] GetMatchInfoCommands(Guid[] instrumentIds)
        {
            return this.matchInfoCommandBuffer.GetCommands(instrumentIds).ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="instrumentCode"></param>
        /// <param name="priceEnable"></param>
        /// <param name="sourceName"></param>
        /// <returns>0 Success other fail:1 sourceName!=activeSource ,2  inner error</returns>
        internal int Update(Common.Token token, string instrumentCode, bool priceEnable, string sourceName, out string errorMsg)
        {
            //update DB return Instrument ID
            //create update XML 
            //this.Update(token, updateXml);
            errorMsg = string.Empty;
            string sql = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(instrumentCode))
                {
                    sql = string.Format("EXEC [dbo].[Update_AllInstrumentPriceEnable] {0},'{1}'", priceEnable ? 1 : 0, sourceName);

                }
                else
                {
                    sql = string.Format("EXEC dbo.Instrument_UpdateInstrument '{0}',{1},'{2}'", instrumentCode, priceEnable ? 1 : 0, sourceName);
                }
                DataSet ds = DataAccess.GetData(sql, this.connectionString, TimeSpan.FromSeconds(15));
                if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0) return 0;
                if (ds != null && ds.Tables.Count != 0 && ds.Tables[0].Rows.Count != 0)
                {
                    int result = 0;
                    if (int.TryParse(ds.Tables[0].Rows[0][0].ToString(), out result))
                    {
                        if (result == -1)
                        {
                            errorMsg = "The source is not equal to active source!";
                            return 2;
                        }
                    }
                }
                AppDebug.LogEvent("StateSever", ds.Tables[0].Rows.Count.ToString(), EventLogEntryType.Information);
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement update = xmlDoc.CreateElement("Update");
                XmlElement newChild = xmlDoc.CreateElement("Modify");
                XmlElement newInstrumens = xmlDoc.CreateElement("Instruments");
                newChild.AppendChild(newInstrumens);
                update.AppendChild(newChild);
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    newInstrumens.AppendChild(this.GetUpdateNode(xmlDoc, (Guid)dr["ID"], priceEnable));
                }
                AppDebug.LogEvent("StateSever", update.OuterXml, EventLogEntryType.Information);
                this.Update(token, update);
                AppDebug.LogEvent("StateSever.ChangePriceEnable", "Success", EventLogEntryType.Information);
                return 0;
            }
            catch (Exception e)
            {
                string errorInfo = string.Format("{0}-{1}-{2}", instrumentCode, priceEnable, sourceName, e.ToString());
                AppDebug.LogEvent("StateSever.ChangePriceEnable", errorInfo, EventLogEntryType.Error);
                errorMsg = "it has a SqlException";
                return 1;
            }
        }





        private XmlNode GetUpdateNode(XmlDocument xmlDoc, Guid id, bool priceEnable)
        {
            XmlElement element = xmlDoc.CreateElement("Instrument");
            element.SetAttribute("ID", XmlConvert.ToString(id));
            element.SetAttribute("IsPriceEnabled", XmlConvert.ToString(priceEnable));
            return element;
        }

        private string GetUpdateSql(Guid id, bool priceEnable)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("EXEC dbo.QuotationServer_Instrument_Update ");
            builder.Append("'").Append(id.ToString()).Append("',");
            builder.Append(priceEnable ? 1 : 0);
            builder.Append("\n");
            return builder.ToString();
        }

        internal bool NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.NotifyPasswordChanged(customerId, loginName, newPassword);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.NotifyPasswordChanged(customerId, loginName, newPassword);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.NotifyPasswordChanged", exception.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        internal bool NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.NotifyTelephonePinReset(customerId, accountId, verificationCode);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.NotifyTelephonePinReset(customerId, accountId, verificationCode);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.NotifyTelephonePinReset", exception.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public TransactionError InstalmentPayoff(Token token, Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, string instalmentXml, string terminateXml)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.InstalmentPayoff(submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalmentXml, terminateXml);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.InstalmentPayoff(token, submitorId, accountId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalmentXml, terminateXml);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.InstalmentPayoff", exception.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError InstalmentUndoPayoff(Token token, Guid submitorId, Guid accountId, string undoInstalmentXml)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    //return this.transactionAdaptor.InstalmentPayoff(submitorId, accountId, undoInstalmentXml);
                    throw new NotImplementedException();
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.InstalmentUndoPayoff(token, submitorId, accountId, undoInstalmentXml);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.InstalmentUndoPayoff", exception.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        internal TransactionError ApplyDelivery(Common.Token token, ref XmlNode deliveryRequire, out string code, out string balance, out string usableMargin)
        {
            code = balance = usableMargin = null;
            TransactionError result = TransactionError.OK;
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.ApplyDelivery(deliveryRequire, out code, out balance, out usableMargin);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    result = transactionServer.ApplyDelivery(token, ref deliveryRequire, out code, out balance, out usableMargin);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.ApplyDelivery", exception.ToString(), EventLogEntryType.Error);
                result = TransactionError.RuntimeError;
            }

            if (result == TransactionError.OK)
            {
                DeliveryCommand command = new DeliveryCommand(deliveryRequire);
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.PhysicalTerminal);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);
            }
            return result;
        }

        internal bool CancelDelivery(Common.Token token, Guid deliveryRequestId, string title, string notifyMessage)
        {
            bool result = false;
            Guid accountId = Guid.Empty;
            int status = 100;
            try
            {
                byte requestStatus = (byte)DataAccessHelper.ExecuteScalar("SELECT [STATUS] FROM Physical.DeliveryRequest WHERE Id=@Id", CommandType.Text, new SqlParameter("Id", deliveryRequestId));
                if (requestStatus == (byte)DeliveryRequestStatus.Accepted)
                {
                    if (this.transactionAdaptor != null)
                    {
                        result = this.transactionAdaptor.CancelDelivery(token.UserID, deliveryRequestId, title, notifyMessage, out accountId, out status);
                    }
                    else
                    {
                        TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                        result = transactionServer.CancelDelivery(token, deliveryRequestId, out accountId, out status);
                    }
                }
                else if (requestStatus == (byte)DeliveryRequestStatus.Approved || requestStatus == (byte)DeliveryRequestStatus.Stocked)
                {
                    //Guid orderId = (Guid)DataAccessHelper.ExecuteScalar("SELECT ID FROM V_OrderAllExec o WHERE o.PhysicalRequestId=@Id AND o.IsOpen=0", CommandType.Text, new SqlParameter("Id", deliveryRequestId));
                    Guid orderId = Guid.Empty;
                    DataAccessHelper.ExecuteReader("SELECT ID, AccountID FROM V_OrderAllExec o WHERE o.PhysicalRequestId=@Id AND o.IsOpen=0", CommandType.Text, reader =>
                    {
                        reader.Read();
                        orderId = reader.GetGuid(0);
                        accountId = reader.GetGuid(1);
                    }, new SqlParameter("Id", deliveryRequestId));
                    TransactionError transactionError;
                    if (this.transactionAdaptor != null)
                    {
                        transactionError = this.DeleteByCancelDeliveryForNewVersion(token, false, false, accountId, orderId, deliveryRequestId);
                    }
                    else
                    {
                        XmlNode xmlTran, xmlAccount;
                        transactionError = this.Delete(token, orderId, false, false, out xmlTran, out xmlAccount);
                    }
                    result = transactionError == TransactionError.OK;
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.CancleDelivery", exception.ToString(), EventLogEntryType.Error);
                result = false;
            }

            if (result)
            {
                CancelDeliveryCommand command = new CancelDeliveryCommand(accountId, deliveryRequestId, status);
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);

                string expireTime = XmlConvert.ToString(DateTime.Now.AddDays(3), DateTimeFormat.Xml);
                ChatCommand chatCommand = ChatHelper.SaveMessage(token.UserID, accountId, title, notifyMessage, expireTime, this.connectionString);
                if (chatCommand != null)
                {
                    this.BroadcastCommand(token, chatCommand, AppType.TradingConsole);
                }
            }
            return result;
        }

        internal bool NotifyDeliveryApproved(Common.Token token, Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime, string title, string notifyMessage)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    this.transactionAdaptor.NotifyDeliveryApproved(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime, title, notifyMessage);
                }

                NotifyDeliveryCommand command = new NotifyDeliveryCommand(accountId, deliveryRequestId, approvedId, approvedTime, deliveryTime);
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);

                string expireTime = XmlConvert.ToString(DateTime.Now.AddDays(3), DateTimeFormat.Xml);
                ChatCommand chatCommand = ChatHelper.SaveMessage(token.UserID, accountId, title, notifyMessage, expireTime, this.connectionString);
                if (chatCommand != null)
                {
                    this.BroadcastCommand(token, chatCommand, AppType.TradingConsole);
                }
                return true;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.NotifyDeliveryApproved", exception.ToString(), EventLogEntryType.Error);
            }
            return false;
        }

        internal bool NotifyDelivery(Common.Token token, Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage)
        {
            bool result = true;
            Guid accountId = Guid.Empty;
            try
            {
                if (this.transactionAdaptor != null)
                {
                    this.transactionAdaptor.NotifyDelivery(deliveryRequestId, availableDeliveryTime, title, notifyMessage, out accountId);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    result = transactionServer.NotifyDelivery(token, deliveryRequestId, availableDeliveryTime, title, notifyMessage, out accountId);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.NotifyDelivery", exception.ToString(), EventLogEntryType.Error);
                result = false;
            }
            if (result)
            {
                NotifyDeliveryCommand command = new NotifyDeliveryCommand(accountId, deliveryRequestId, availableDeliveryTime);
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);
            }
            return result;
        }

        internal bool NotifyDelivered(Token token, Guid deliveryRequestId, Guid accountId)
        {
            bool result = true;
            try
            {
                NotifyDeliveryCommand command = new NotifyDeliveryCommand();
                command.deliveryRequestId = deliveryRequestId;
                command.deliveryRequestStatus = DeliveryRequestStatus.OrderCreated;
                command.accountId = accountId;
                if (this.transactionAdaptor != null)
                {
                    this.transactionAdaptor.NotifyDeliveried(accountId, deliveryRequestId);
                }
                this.BroadcastCommand(token, command, AppType.TradingConsole);
                this.BroadcastCommand(token, command, AppType.RiskMonitor);
                this.BroadcastCommand(token, command, AppType.TradingMonitor);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServerNotifyDelivered", ex.ToString(), EventLogEntryType.Error);
                result = false;
            }
            return result;
        }

        internal bool NotifyScrapDeposit(Common.Token token, XmlNode scrapDeposit, string title, string notifyMessage)
        {
            try
            {
                NotifyScrapDepositCommand command = new NotifyScrapDepositCommand();
                command.scrapDepositId = XmlConvert.ToGuid(scrapDeposit.Attributes["Id"].Value);
                command.scrapDepositCode = scrapDeposit.Attributes["Code"].Value;
                command.accountId = XmlConvert.ToGuid(scrapDeposit.Attributes["AccountId"].Value);
                command.scrapInstrumentId = XmlConvert.ToGuid(scrapDeposit.Attributes["ScrapInstrumentId"].Value);
                command.scrapInstrumentCode = scrapDeposit.Attributes["ScrapInstrumentCode"].Value;
                command.quantityDecimalDigits = XmlConvert.ToInt32(scrapDeposit.Attributes["QuantityDecimalDigits"].Value);
                command.unitCode = scrapDeposit.Attributes["UnitCode"].Value;
                command.tradeInstrumentId = XmlConvert.ToGuid(scrapDeposit.Attributes["TradeInstrumentId"].Value);
                command.submitTime = XmlConvert.ToDateTime(scrapDeposit.Attributes["SubmitTime"].Value, DateTimeFormat.Xml);
                command.rawQuantity = XmlConvert.ToDecimal(scrapDeposit.Attributes["RawQuantity"].Value);
                command.status = XmlConvert.ToByte(scrapDeposit.Attributes["Status"].Value);
                if (command.status == 1)
                {
                    command.adjustedQuantity = XmlConvert.ToDecimal(scrapDeposit.Attributes["AdjustedQuantity"].Value);
                    command.finalQuantity = XmlConvert.ToDecimal(scrapDeposit.Attributes["FinalQuantity"].Value);
                    command.validatorId = scrapDeposit.Attributes["ValidatorId"].Value;
                }
                command.acceptTime = XmlConvert.ToDateTime(scrapDeposit.Attributes["AcceptTime"].Value, DateTimeFormat.Xml);

                this.BroadcastCommand(token, command, AppType.TradingConsole);

                string expireTime = XmlConvert.ToString(DateTime.Now.AddMonths(1), DateTimeFormat.Xml);
                ChatCommand chatCommand = ChatHelper.SaveMessage(token.UserID, command.accountId, title, notifyMessage, expireTime, this.connectionString);
                if (chatCommand != null)
                {
                    this.BroadcastCommand(token, chatCommand, AppType.TradingConsole);
                }

                return true;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer.NotifyScrapDeposit", ex.ToString(), EventLogEntryType.Error);
            }
            return false;
        }

        internal bool NotifyScrapDepositCanceled(Common.Token token, XmlNode scrapDepositCancel)
        {
            try
            {
                NotifyScrapDepositCanceledCommand command = new NotifyScrapDepositCanceledCommand();
                command.scrapDepositId = XmlConvert.ToGuid(scrapDepositCancel.Attributes["Id"].Value);
                command.accountId = XmlConvert.ToGuid(scrapDepositCancel.Attributes["AccountId"].Value);
                command.cancelTime = XmlConvert.ToDateTime(scrapDepositCancel.Attributes["CancelTime"].Value, DateTimeFormat.Xml);
                command.cancelReason = (CancelReason)XmlConvert.ToByte(scrapDepositCancel.Attributes["CancelReason"].Value);

                this.BroadcastCommand(token, command, AppType.TradingConsole);
                return true;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer.NotifyScrapDepositCanceled", ex.ToString(), EventLogEntryType.Error);
            }
            return false;
        }

        internal bool SetPriceAlerts(Common.Token token, XmlNode priceAlertsNode)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.SetPriceAlerts(token.UserID, priceAlertsNode);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.SetPriceAlerts(token, priceAlertsNode);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.SetPriceAlerts", exception.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        internal DataSet RedoLimitOrder(Common.Token token, XmlNode orderIds, out TransactionError returnValue)
        {
            returnValue = TransactionError.RuntimeError;
            try
            {
                TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                DataSet dataSet = transactionServer.RedoLimitOrder(token, orderIds, out returnValue);
                if (dataSet != null)
                {
                    int count = 0;
                    DataTable affectedInstruments = dataSet.Tables["Instrument"];
                    count = affectedInstruments.Rows.Count;
                    if (count == 0) return dataSet;
                    Guid[] instrumentIds = new Guid[count];
                    int index = 0;
                    foreach (DataRow row in affectedInstruments.Rows)
                    {
                        instrumentIds[index++] = (Guid)row["ID"];
                    }

                    DataTable affectedAccounts = dataSet.Tables["Account"];
                    Guid[] accountIds = new Guid[affectedAccounts.Rows.Count];
                    index = 0;
                    foreach (DataRow row in affectedAccounts.Rows)
                    {
                        accountIds[index++] = (Guid)row["ID"];
                    }

                    KickoutCommand command = new KickoutCommand();
                    command.accountIDs = accountIds;
                    command.instrumentIDs = instrumentIds;

                    this.BroadcastCommand(token, command, AppType.RiskMonitor);
                    this.BroadcastCommand(token, command, AppType.DealingConsole);
                    this.BroadcastCommand(token, command, AppType.TradingMonitor);

                    DataTable affectedUsers = dataSet.Tables["User"];
                    foreach (DataRow row in affectedUsers.Rows)
                    {
                        Guid userId = (Guid)row["ID"];
                        this.KickoutPredecessor(userId);
                    }
                }

                return dataSet;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.RedoLimitOrder", exception.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        public bool ChangeSystemStatus(Token token, SystemStatus newStatus)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.ChangeSystemStatus(token, newStatus);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.ChangeSystemStatus(token, newStatus);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.ChangeSystemStatus", exception.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public TransactionError ApplyTransfer(Token token, Guid sourceAccountID, Guid sourceCurrencyID,
            decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
            decimal rate, DateTime expireDate)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.ApplyTransfer(token.UserID, sourceAccountID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.ApplyTransfer(token, sourceAccountID, sourceCurrencyID,
                        sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.ApplyTransfer", exception.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError AcceptTransfer(Token token, Guid transferID)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.AcceptTransfer(token.UserID, transferID);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.AcceptTransfer(token, transferID);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.AcceptTransfer", exception.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        public TransactionError DeclineTransfer(Token token, Guid transferID)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.DeclineTransfer(token.UserID, transferID);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);
                    return transactionServer.DeclineTransfer(token, transferID);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.DeclineTransfer", exception.ToString(), EventLogEntryType.Error);
                return TransactionError.RuntimeError;
            }
        }

        internal Guid[] Rehit(Common.Token token, Guid[] orderIDs, string[] hitPrices, Guid[] accountIDs = null)
        {
            try
            {
                if (this.transactionAdaptor != null)
                {
                    return this.transactionAdaptor.Rehit(orderIDs, accountIDs);
                }
                else
                {
                    TransactionServer.Service transactionServer = this.GetTransactionServer(Guid.Empty);

                    XmlNode xmlHitOrders = null;
                    AutoFillResult[] autoFillResults = null;
                    Guid[] result = transactionServer.Rehit(token, orderIDs, hitPrices, out xmlHitOrders, out autoFillResults);

                    this.AfterBroadcastQuotationToTransactionServer(token, xmlHitOrders, autoFillResults, transactionServer);

                    return result;
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.Rehit", exception.ToString(), EventLogEntryType.Error);
                return null;
            }
        }

        private void InitializeTransactionServerServiceChannel()
        {
            try
            {
                string serviceUrl = ConfigurationManager.AppSettings["TransactionServiceUrl"];
                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    this._TransactionServerServiceProxy =
                        Protocal.ChannelFactory.CreateHttpChannel<Protocal.ITransactionServerService>(serviceUrl);

                    AppDebug.LogEvent("StateServer.InitializeTransactionServerService", "channel created :" + serviceUrl, EventLogEntryType.Information);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.InitializeTransactionServerService", exception.ToString(), EventLogEntryType.Error);
            }
        }

        public void SetDailyClosePrice(Token token, Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            try
            {
                AppDebug.LogEvent("StateServer.SetDailyClosePrice",
                    string.Format("received request: close price for insturment {0} tradeday {1}.", instrumentId, tradeDay), EventLogEntryType.Information);

                if (this._TransactionServerServiceProxy != null)
                {
                    this._TransactionServerServiceProxy.SetDailyClosePrice(instrumentId, tradeDay, closeQuotations);
                    AppDebug.LogEvent("StateServer.SetDailyClosePrice", "completed request.", EventLogEntryType.Information);
                }
                else
                {
                    AppDebug.LogEvent("StateServer.SetDailyClosePrice", "channel not created to TransactionServerService.", EventLogEntryType.Error);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.SetDailyClosePrice", exception.ToString(), EventLogEntryType.Error);
            }
        }

        public void SetDailyClosePrices(Token token, List<InstrumentDailyClosePriceInfo> closeQuotations)
        {
            try
            {
                //AppDebug.LogEvent("StateServer.SetDailyClosePrices",
                //    string.Format("received request: close price for insturment {0} tradeday {1}.", instrumentId, tradeDay), EventLogEntryType.Information);

                if (this._TransactionServerServiceProxy != null)
                {
                    this._TransactionServerServiceProxy.SetDailyClosePrices(closeQuotations);
                    AppDebug.LogEvent("StateServer.SetDailyClosePrices", string.Format("success. instrument count={0}.", closeQuotations.Count), EventLogEntryType.Information);
                }
                else
                {
                    AppDebug.LogEvent("StateServer.SetDailyClosePrices", "channel not created to TransactionServerService.", EventLogEntryType.Error);
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.SetDailyClosePrices", exception.ToString(), EventLogEntryType.Error);
            }
        }

        public Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand()
        {
            try
            {
                if (!string.IsNullOrEmpty(_GateWayServiceUrl))
                {
                    if (_GatewayServiceChanel == null)
                        _GatewayServiceChanel = Protocal.ChannelFactory.CreateHttpChannel<Protocal.IGatewayService>(_GateWayServiceUrl);

                    return _GatewayServiceChanel.GetTradingInstrumentStatusCommand();
                }
            }
            catch (CommunicationException)
            {
                _GatewayServiceChanel = Protocal.ChannelFactory.CreateHttpChannel<Protocal.IGatewayService>(_GateWayServiceUrl);
                return _GatewayServiceChanel.GetTradingInstrumentStatusCommand();
            }
            return null;
        }

        public void BrodcastMinuteChartDataCommand(Token token, MinuteChartData[] minuteChartDatas)
        {
            try
            {
                MinuteChartDataCommand command = new MinuteChartDataCommand() { MinuteChartDatas = minuteChartDatas };
                this.BroadcastCommands(token, new Command[] { command });
                AppDebug.LogEvent("StateServer.BrodcastMinuteChartDataCommand", string.Format("success:\r\n{0}", command.ToString()), EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer.BrodcastMinuteChartDataCommand",
                    string.Format("BrodcastMinuteChartDataCommand:Error\r\n{0}", ex.ToString()), EventLogEntryType.Error);
            }
        }

        public MinuteChartData[] GetMinuteChartData(Guid? instrumentId, Guid? quotePolicyId)
        {
            try
            {
                if (this._UseManager)
                {
                    return ManagerClient.GetMinuteChartData(instrumentId, quotePolicyId);
                }
                else
                {
                    throw new NotImplementedException("GetMinuteChartData Faild: QuotationServer not implemented this method.");
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.GetMinuteChartData",
                    string.Format("GetMinuteChartData:Error\r\n{0}", e.ToString()), EventLogEntryType.Error);
                return null;
            }
        }

        public void UpdateLoginInfo(iExchange.Common.TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            AppDebug.LogEvent("StateServer", string.Format("RECV UpdateLoginInfo: traderServerType = {0}, onlineXml = {1}, expireTime = {2}", appType, onlineXml, expireTime), EventLogEntryType.Information);

            try
            {
                if (this._UseManager)
                {
                    ManagerClient.UpdateLoginInfo(appType, onlineXml, expireTime);
                }
                else
                {
                    AppDebug.LogEvent("StateServer.UpdateLoginInfo", "Faild, WebConfig.UseManager = FALSE.", EventLogEntryType.Warning);
                }
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer.UpdateLoginInfo",
                    string.Format("UpdateLoginInfo:Error\r\n{0}", e.ToString()), EventLogEntryType.Error);
            }
        }



        public bool UpdateCustomeDisallowLogin(Token token, XmlNode customerPolicyNodes)
        {
            try
            {
                //Save to DB
                string customerPolicyXml = DataAccess.ConvertToSqlXml(customerPolicyNodes.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateCustomerDisallowLogin '{0}','{1}'", token.UserID, customerPolicyXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(customerPolicyNodes, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }

        public bool UpdateEmployeeDisallowLogin(Token token, XmlNode employeePolicyNodes)
        {
            try
            {
                //Save to DB
                string employeePolicyXml = DataAccess.ConvertToSqlXml(employeePolicyNodes.OuterXml);
                string sql = string.Format("Exec dbo.P_UpdateEmployeeDisallowLogin '{0}','{1}'", token.UserID, employeePolicyXml);
                bool isSucceed = DataAccess.UpdateDB(sql, this.connectionString);

                if (isSucceed)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlElement update = xmlDoc.CreateElement("Update");
                    XmlElement modify = xmlDoc.CreateElement("Modify");
                    modify.AppendChild(xmlDoc.ImportNode(employeePolicyNodes, true));
                    update.AppendChild(modify);

                    this.Update(token, update);
                }
                return isSucceed;
            }
            catch (Exception e)
            {
                AppDebug.LogEvent("StateServer", e.ToString(), EventLogEntryType.Error);
                return false;
            }
        }
    }

    public static class ChatHelper
    {
        public static ChatCommand SaveMessage(Guid userId, Guid accountId, string title, string content, string expireTime, string connectionString)
        {
            XmlElement message = ChatHelper.SaveMessageToDB(userId, accountId, title, content, expireTime, connectionString);
            if (message != null)
            {
                ChatCommand chatCommand = new ChatCommand();
                chatCommand.AccountId = accountId;

                XmlDocument xmlDoc = new XmlDocument();
                XmlElement chatNode = xmlDoc.CreateElement("Chat");
                xmlDoc.AppendChild(chatNode);

                chatNode.SetAttribute("ID", message.Attributes["ID"].Value);
                chatNode.SetAttribute("Title", message.Attributes["Title"].Value);
                chatNode.SetAttribute("Content", message.Attributes["Content"].Value);
                chatNode.SetAttribute("PublishTime", XmlConvert.ToString(DateTime.Now, DateTimeFormat.Xml));
                chatCommand.Content = chatNode;

                return chatCommand;
            }
            else
            {
                return null;
            }
        }

        private static XmlElement SaveMessageToDB(Guid userId, Guid accountId, string title, string content, string expireTime, string connectionString)
        {
            Guid messageId = Guid.NewGuid();

            XmlDocument doc = new XmlDocument();
            XmlElement elmentChat = doc.CreateElement("Chat");
            elmentChat.SetAttribute("ID", XmlConvert.ToString(messageId));
            elmentChat.SetAttribute("Title", title);
            elmentChat.SetAttribute("Content", content);
            elmentChat.SetAttribute("ExpireTime", expireTime);//.ToString());
            elmentChat.SetAttribute("Publisher", XmlConvert.ToString(userId));
            doc.AppendChild(elmentChat);

            XmlElement elmentRecipients = doc.CreateElement("Recipients");
            elmentChat.AppendChild(elmentRecipients);

            XmlElement elmentCustomers = doc.CreateElement("Accounts");
            XmlElement elmentCustomer = doc.CreateElement("Account"); ;
            elmentCustomer.SetAttribute("ID", XmlConvert.ToString(accountId));
            elmentCustomers.AppendChild(elmentCustomer);
            elmentRecipients.AppendChild(elmentCustomers);

            int result = 0;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    SqlCommand sqlCommand = new SqlCommand("dbo.P_SendMessage", sqlConnection);
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    SqlParameter sqlParameter = new SqlParameter("@xmlChat", SqlDbType.NText);
                    sqlParameter.Value = doc.DocumentElement.OuterXml;
                    sqlCommand.Parameters.Add(sqlParameter);
                    sqlCommand.Parameters.Add(new SqlParameter("@RETURN_VALUE", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, "RETURN_VALUE", DataRowVersion.Default, null));

                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                    DataSet dataSet = new DataSet();
                    sqlDataAdapter.Fill(dataSet);
                    result = (int)(sqlCommand.Parameters["@RETURN_VALUE"].Value);

                    if (result == 0)
                    {
                        XmlDocument doc2 = new XmlDocument();
                        XmlElement elmentChat2 = doc2.CreateElement("Chat");
                        elmentChat2.SetAttribute("ID", XmlConvert.ToString(messageId));
                        elmentChat2.SetAttribute("Title", title);
                        elmentChat2.SetAttribute("Content", content);
                        elmentChat2.SetAttribute("ExpireTime", expireTime);
                        elmentChat2.SetAttribute("Publisher", XmlConvert.ToString(userId));
                        doc2.AppendChild(elmentChat2);

                        XmlElement elmentRecipients2 = doc2.CreateElement("Recipients");
                        elmentChat2.AppendChild(elmentRecipients2);

                        XmlElement elmentCustomers2 = null;
                        DataTable dataTable = dataSet.Tables[0];
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            Guid recipientsID = (System.Guid)(dataRow["RecipientsID"]);
                            if (elmentCustomers2 == null)
                                elmentCustomers2 = doc2.CreateElement("Customers");
                            XmlElement elmentCustomer2 = doc2.CreateElement("Customer"); ;
                            elmentCustomer2.SetAttribute("ID", XmlConvert.ToString(recipientsID));
                            elmentCustomers2.AppendChild(elmentCustomer2);
                        }
                        if (elmentCustomers2 != null)
                            elmentRecipients2.AppendChild(elmentCustomers2);

                        return doc2.DocumentElement;
                    }
                }
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("StateServer.SaveMessage(dbo.P_SendMessage)", string.Format("DB Return Result:{0} Exception:{1} \n", result, exception.ToString()), EventLogEntryType.Error);
            }
            finally
            {
            }

            return null;
            //return doc.DocumentElement;
        }
    }

    [ServiceContract]
    public interface ICommandCollectService
    {
        //[OperationContract(AsyncPattern = true)]
        //[XmlSerializerFormat]
        //IAsyncResult BeginAddCommand(Token token, Command command, AsyncCallback cb, object state);
        //void EndAddCommand(IAsyncResult result);

        [OperationContract(AsyncPattern = false)]
        [XmlSerializerFormat]
        void AddCommand(Token token, Command command);

        [OperationContract(AsyncPattern = false)]
        [XmlSerializerFormat]
        void KickoutPredecessor(Guid userId);

    }
}
