using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml;
using System.Diagnostics;
using log4net;
using System.Threading;
using System.Configuration;
using System.Data;
using iExchange.StateServer.Adapter.FaxEmailServices;
using System.Xml.Linq;

namespace iExchange.StateServer.Adapter.OuterService
{
    internal sealed class OuterTransactionService
    {
        private sealed class OpenInterestSummary
        {
            private string id;
            private int? numeratorUnit = null;
            private int? denominator = null;
            private decimal buyLot = decimal.Zero;
            private decimal sellLot = decimal.Zero;
            private decimal buySumEL = decimal.Zero;
            private decimal sellSumEL = decimal.Zero;
            private decimal buyContractSize = decimal.Zero;
            private decimal sellContractSize = decimal.Zero;
            private decimal netLot = decimal.Zero;
            private decimal netContractSize = decimal.Zero;

            private OpenInterestSummary()
            { }

            public OpenInterestSummary(string id)
            {
                this.id = id;
            }

            public string Id
            {
                get { return this.id; }
            }

            public int? NumeratorUnit
            {
                get { return this.numeratorUnit; }
                set { this.numeratorUnit = value; }
            }

            public int? Denominator
            {
                get { return this.denominator; }
                set { this.denominator = value; }
            }

            public decimal BuyLot
            {
                get { return this.buyLot; }
                set { this.buyLot = value; }
            }
            public decimal SellLot
            {
                get { return this.sellLot; }
                set { this.sellLot = value; }
            }
            public decimal BuySumEL
            {
                get { return this.buySumEL; }
                set { this.buySumEL = value; }
            }
            public decimal SellSumEL
            {
                get { return this.sellSumEL; }
                set { this.sellSumEL = value; }
            }
            public decimal BuyContractSize
            {
                get { return this.buyContractSize; }
                set { this.buyContractSize = value; }
            }
            public decimal SellContractSize
            {
                get { return this.sellContractSize; }
                set { this.sellContractSize = value; }
            }
            private decimal AvgBuyPrice
            {
                get { return (this.buyLot != decimal.Zero) ? this.ConvertPriceValue(this.buySumEL / this.buyLot) : decimal.Zero; }
            }
            private decimal AvgSellPrice
            {
                get { return (this.sellLot != decimal.Zero) ? this.ConvertPriceValue(sellSumEL / sellLot) : decimal.Zero; }
            }
            public decimal NetLot
            {
                //get { return this.BuyLot - this.SellLot; }
                get { return this.netLot; }
                set { this.netLot = value; }
            }
            public decimal NetContractSize
            {
                get { return this.netContractSize; }
                set { this.netContractSize = value; }
            }
            private decimal AvgNetPrice
            {
                get
                {
                    return (this.NetLot != decimal.Zero) ? this.ConvertPriceValue((this.AvgBuyPrice * this.buyLot - this.AvgSellPrice * this.sellLot) / this.NetLot) : decimal.Zero;
                }
            }

            private decimal ConvertPriceValue(decimal value)
            {
                if (value == decimal.Zero) return value;
                if (value < decimal.Zero)
                {
                    return decimal.Zero - (decimal)Price.CreateInstance(Decimal.ToDouble(Math.Abs(value)), this.numeratorUnit.Value, this.denominator.Value);
                }
                else
                {
                    return (decimal)Price.CreateInstance(Decimal.ToDouble(value), this.numeratorUnit.Value, this.denominator.Value);
                }
            }

            private AccountType accountType = AccountType.Common;
            public AccountType AccountType
            {
                get { return this.accountType; }
                set { this.accountType = value; }
            }

            public XmlElement ToXmlNode(string elementName, string code)
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlElement node = xmlDocument.CreateElement(elementName);
                xmlDocument.AppendChild(node);

                node.SetAttribute("ID", this.Id);//XmlConvert.ToString(this.Id));
                if (elementName == "Instrument")
                {
                    node.SetAttribute("Code", code);
                }
                if (elementName == "Account")
                {
                    node.SetAttribute("Type", XmlConvert.ToString((int)this.AccountType));
                }
                if (elementName == "Account" || elementName == "Instrument")
                {
                    node.SetAttribute("MinNumeratorUnit", XmlConvert.ToString(this.NumeratorUnit.Value));
                    node.SetAttribute("MaxDenominator", XmlConvert.ToString(this.Denominator.Value));
                }
                node.SetAttribute("BuyLot", XmlConvert.ToString(this.BuyLot));
                node.SetAttribute("AvgBuyPrice", XmlConvert.ToString(this.AvgBuyPrice));
                node.SetAttribute("BuyContractSize", XmlConvert.ToString(this.BuyContractSize));
                node.SetAttribute("SellLot", XmlConvert.ToString(this.SellLot));
                node.SetAttribute("AvgSellPrice", XmlConvert.ToString(this.AvgSellPrice));
                node.SetAttribute("SellContractSize", XmlConvert.ToString(this.SellContractSize));
                node.SetAttribute("NetLot", XmlConvert.ToString(this.NetLot));
                node.SetAttribute("AvgNetPrice", XmlConvert.ToString(this.AvgNetPrice));
                node.SetAttribute("NetContractSize", XmlConvert.ToString(this.NetContractSize));

                return node;
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(OuterTransactionService));
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private SystemControllerProxy _proxy;
        private GroupNetPositionManager _groupNetPositionManager;
        private AccountManager _accountManager;

        internal OuterTransactionService(string connectionString, SystemControllerProxy proxy)
        {
            this.ConnectionString = connectionString;
            _proxy = proxy;
            _groupNetPositionManager = new GroupNetPositionManager(connectionString, proxy);
            _accountManager = new AccountManager(proxy);
            FaxEmailEngine.Default.Initialize(connectionString);
        }

        internal string ConnectionString { get; private set; }

        internal Protocal.CommonSetting.SystemParameter SystemParameter
        {
            get { return Settings.SettingManager.Default.SystemParameter; }
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, Guid[] instrumentIDs, bool includeTransactions, bool onlyCutOrder)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.InfoFormat("GetAccounts appType = {0},  includeTransactions = {1}, onlyCutOrder = {2}", token.AppType, includeTransactions, onlyCutOrder);
                XmlNode result = _accountManager.GetAccounts(token, accountIDs, instrumentIDs, includeTransactions, onlyCutOrder);
                if (this.ShouldDisplayLogForGetAcounts() && result != null)
                {
                    Logger.InfoFormat("GetAccounts content = {0}", result.OuterXml);
                }
                return result;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        private bool ShouldDisplayLogForGetAcounts()
        {
            try
            {
                string logFlag = ConfigurationManager.AppSettings["IsDisplayGetAccounts"];
                if (string.IsNullOrEmpty(logFlag)) return false;
                return bool.Parse(logFlag.Trim());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }


        public XmlNode GetGroupNetPositionForManager(Token token, string permissionName, Guid[] accountGroupIDs, Guid[] instrumentGroupIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.Info("GetGroupNetPositionForManager");
                return _groupNetPositionManager.GetGroupNetPositionForManager(token, permissionName, accountGroupIDs, instrumentGroupIDs, showActualQuantity, blotterCodeSelecteds);
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }


        public XmlNode GetAccountStatus(Guid accountId, Guid instrumentId, OrderType orderType, bool needOutputPrice, out string buyPrice, out string sellPrice)
        {
            Logger.InfoFormat("GetAccountStatus, accountId = {0}, instrumentId = {1}, orderType = {2}, needOutputPrice = {3}", accountId, instrumentId, orderType, needOutputPrice);
            _readWriteLock.EnterWriteLock();
            buyPrice = string.Empty;
            sellPrice = string.Empty;
            try
            {
                Account account = null;
                if (!AccountRepository.Default.Contains(accountId))
                {
                    var initData = _proxy.GetInitializeData(new List<Guid> { accountId });
                    if (!string.IsNullOrEmpty(initData))
                    {
                        account = (Account)AccountRepository.Default.GetOrAdd(accountId);
                        account.Initialize(XElement.Parse(initData).Element("Account"));
                    }
                }
                else
                {
                    account = (Account)AccountRepository.Default.Get(accountId);
                }
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement transactionServerNode = xmlDoc.CreateElement("TransactionServer");
                xmlDoc.AppendChild(transactionServerNode);
                if (account != null)
                {
                    transactionServerNode.AppendChild(account.GetAccountStatus(xmlDoc));
                }
                if (needOutputPrice)
                {
                    _proxy.GetAccountInstrumentPrice(accountId, instrumentId, out buyPrice, out sellPrice);
                    buyPrice = buyPrice ?? string.Empty;
                    sellPrice = sellPrice ?? string.Empty;
                }
                return transactionServerNode;
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("GetAccountStatus accountId ={0}", accountId), ex);
                return null;
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }



        internal bool NotifyPasswordChanged(Guid customerId, string loginName, string newPassword)
        {
            try
            {
                if (this.SystemParameter.EmailNotifyChangePassword)
                {
                    FaxEmailEngine.Default.NotifyPasswordChanged(customerId, loginName, newPassword);
                }
                return true;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("TransactionServer.NotifyPasswordChanged", exception.ToString(), EventLogEntryType.Warning);
                return false;
            }
        }

        internal bool NotifyTelephonePinReset(Guid customerId, Guid accountId, string verificationCode)
        {
            try
            {
                if (this.SystemParameter.EnableResetTelephonePin)
                {
                    FaxEmailEngine.Default.NotifyTelephonePinReset(customerId, accountId, verificationCode);
                }
                return true;
            }
            catch (Exception exception)
            {
                AppDebug.LogEvent("TransactionServer.NotifyTelephonePinReset", exception.ToString(), EventLogEntryType.Warning);
                return false;
            }
        }


        internal Guid[] VerifyTransaction(Token token, Guid[] transactionIDs, out XmlNode[] xmlTrans, out XmlNode[] xmlAccounts)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                return TransactionVerifier.Default.VerifyTransaction(token, transactionIDs, out xmlTrans, out xmlAccounts);
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        internal XmlNode GetAcountInfo(Token token, Guid tranID)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.InfoFormat("GetAcountInfo  tranId = {0}", tranID);
                Account account = (Account)AccountRepository.Default.GetByTranId(tranID);
                if (account == null)
                {
                    Logger.WarnFormat("GetAcountInfo  tranId = {0} can't find account", tranID);
                    return null;
                }
                Transaction tran = (Transaction)account.GetTran(tranID);
                if (tran == null)
                {
                    Logger.WarnFormat("GetAcountInfo  tranId = {0}, accountId = {1}, can't find tran", tranID, account.Id);
                    return null;
                }
                return account.GetAcountInfo(tran.InstrumentId);
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        public XmlNode GetOpenInterestSummaryOrderList(Guid accountID, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.Info("GetOpenInterestSummaryOrderList");
                if (!AccountRepository.Default.Contains(accountID)) return null;
                Account accountEx = (Account)AccountRepository.Default.Get(accountID);
                return accountEx.GetOpenInterestSummaryOrderList(instrumentIDs, blotterCodeSelecteds);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }


        public XmlNode GetOpenInterestInstrumentSummary(Token token, bool isGroupByOriginCode, string[] blotterCodeSelecteds)
        {
            Guid[] accountIDs = null;
            this.GetAccountIDs(token, ref accountIDs);
            if (accountIDs == null) return null;
            Array.Sort(accountIDs);
            var openInterestInstrumentInfos = this.GetOpenInterestInstrumentInfoFromDB(token);

            _readWriteLock.EnterReadLock();
            try
            {
                Logger.Info("GetOpenInterestInstrumentSummary");
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement instrumentsNode = xmlDoc.CreateElement("Instruments");
                if (!isGroupByOriginCode)
                {
                    foreach (OpenInterestInstrumentInfo openInterestInstrumentInfo in openInterestInstrumentInfos.Values)
                    {
                        Guid[] instrumentIDs = new Guid[1];
                        instrumentIDs.SetValue(openInterestInstrumentInfo.InstrumentId, 0);
                        var openInterestSummarys = this.GetOpenInterestSummarysByInstrument(accountIDs, instrumentIDs, blotterCodeSelecteds);
                        if (openInterestSummarys.Count > 0)
                        {
                            OpenInterestSummary openInterestInstrumentSummary = new OpenInterestSummary(openInterestInstrumentInfo.InstrumentId.ToString());
                            foreach (OpenInterestSummary accountOpenInterestSummary in openInterestSummarys.Values)
                            {
                                if (openInterestInstrumentSummary.NumeratorUnit == null || accountOpenInterestSummary.NumeratorUnit > accountOpenInterestSummary.NumeratorUnit)
                                {
                                    openInterestInstrumentSummary.NumeratorUnit = accountOpenInterestSummary.NumeratorUnit;
                                }
                                if (openInterestInstrumentSummary.Denominator == null || accountOpenInterestSummary.Denominator < accountOpenInterestSummary.Denominator)
                                {
                                    openInterestInstrumentSummary.Denominator = accountOpenInterestSummary.Denominator;
                                }
                                openInterestInstrumentSummary.BuyLot += accountOpenInterestSummary.BuyLot;
                                openInterestInstrumentSummary.SellLot += accountOpenInterestSummary.SellLot;
                                openInterestInstrumentSummary.BuySumEL += accountOpenInterestSummary.BuySumEL;
                                openInterestInstrumentSummary.SellSumEL += accountOpenInterestSummary.SellSumEL;
                                openInterestInstrumentSummary.BuyContractSize += accountOpenInterestSummary.BuyContractSize;
                                openInterestInstrumentSummary.SellContractSize += accountOpenInterestSummary.SellContractSize;
                                openInterestInstrumentSummary.NetLot = accountOpenInterestSummary.NetLot;
                                openInterestInstrumentSummary.NetContractSize = accountOpenInterestSummary.NetContractSize;
                            }
                            XmlNode instrumentNode = xmlDoc.ImportNode(openInterestInstrumentSummary.ToXmlNode("Instrument", openInterestInstrumentInfo.Code), true);
                            instrumentsNode.AppendChild(instrumentNode);
                        }
                    }
                }
                else
                {
                    Dictionary<string, List<Guid>> originCodeInstruments = new Dictionary<string, List<Guid>>();
                    foreach (OpenInterestInstrumentInfo openInterestInstrumentInfo in openInterestInstrumentInfos.Values)
                    {
                        List<Guid> originCodeOpenInterestInstrumentInfos;
                        if (!originCodeInstruments.ContainsKey(openInterestInstrumentInfo.OriginCode))
                        {
                            originCodeOpenInterestInstrumentInfos = new List<Guid>();
                            originCodeInstruments.Add(openInterestInstrumentInfo.OriginCode, originCodeOpenInterestInstrumentInfos);
                        }
                        else
                        {
                            originCodeOpenInterestInstrumentInfos = originCodeInstruments[openInterestInstrumentInfo.OriginCode];
                        }
                        originCodeOpenInterestInstrumentInfos.Add(openInterestInstrumentInfo.InstrumentId);
                    }
                    foreach (string originCode in originCodeInstruments.Keys)
                    {
                        List<Guid> originCodeOpenInterestInstrumentInfos = originCodeInstruments[originCode];
                        var openInterestSummarys = this.GetOpenInterestSummarysByInstrument(accountIDs, originCodeOpenInterestInstrumentInfos.ToArray(), blotterCodeSelecteds);
                        if (openInterestSummarys.Count > 0)
                        {
                            OpenInterestSummary openInterestInstrumentSummary = new OpenInterestSummary(originCode);
                            foreach (OpenInterestSummary accountOpenInterestSummary in openInterestSummarys.Values)
                            {
                                if (openInterestInstrumentSummary.NumeratorUnit == null || accountOpenInterestSummary.NumeratorUnit > accountOpenInterestSummary.NumeratorUnit)
                                {
                                    openInterestInstrumentSummary.NumeratorUnit = accountOpenInterestSummary.NumeratorUnit;
                                }
                                if (openInterestInstrumentSummary.Denominator == null || accountOpenInterestSummary.Denominator < accountOpenInterestSummary.Denominator)
                                {
                                    openInterestInstrumentSummary.Denominator = accountOpenInterestSummary.Denominator;
                                }
                                openInterestInstrumentSummary.BuyLot += accountOpenInterestSummary.BuyLot;
                                openInterestInstrumentSummary.SellLot += accountOpenInterestSummary.SellLot;
                                openInterestInstrumentSummary.BuySumEL += accountOpenInterestSummary.BuySumEL;
                                openInterestInstrumentSummary.SellSumEL += accountOpenInterestSummary.SellSumEL;
                                openInterestInstrumentSummary.BuyContractSize += accountOpenInterestSummary.BuyContractSize;
                                openInterestInstrumentSummary.SellContractSize += accountOpenInterestSummary.SellContractSize;
                                openInterestInstrumentSummary.NetLot += accountOpenInterestSummary.NetLot;
                                openInterestInstrumentSummary.NetContractSize += accountOpenInterestSummary.NetContractSize;
                            }
                            XmlNode instrumentNode = xmlDoc.ImportNode(openInterestInstrumentSummary.ToXmlNode("Instrument", originCode), true);
                            instrumentsNode.AppendChild(instrumentNode);
                        }
                    }
                }

                if (!instrumentsNode.HasChildNodes) instrumentsNode = null;

                return instrumentsNode;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        private Dictionary<Guid, OpenInterestInstrumentInfo> GetOpenInterestInstrumentInfoFromDB(Token token)
        {
            Dictionary<Guid, OpenInterestInstrumentInfo> openInterestInstrumentInfos = new Dictionary<Guid, OpenInterestInstrumentInfo>();
            string tableName = string.Empty;
            if (token.AppType == AppType.TradingMonitor)
            {
                tableName = "[dbo].[TradingMonitorInstrument]";
            }
            else
            {
                tableName = "[dbo].[DealingConsoleInstrument]";
            }
            string sql = string.Format(@"SELECT dci.InstrumentId,i.Code,i.OriginCode,i.NumeratorUnit,i.Denominator
                        FROM {0} dci
        	                INNER JOIN dbo.Instrument i
        	                ON i.ID = dci.InstrumentID
                        WHERE dci.UserID='{1}'
                        ORDER BY i.OriginCode,dci.Sequence", tableName, token.UserID);
            DataSet dataSet = DataAccess.GetData(sql, this.ConnectionString);
            if (dataSet.Tables.Count <= 0 || dataSet.Tables[0].Rows.Count <= 0) return openInterestInstrumentInfos;
            foreach (DataRow dataRow in dataSet.Tables[0].Rows)
            {
                OpenInterestInstrumentInfo openInterestInstrumentInfo = new OpenInterestInstrumentInfo(dataRow);
                openInterestInstrumentInfos.Add(openInterestInstrumentInfo.InstrumentId, openInterestInstrumentInfo);
            }
            return openInterestInstrumentInfos;
        }


        internal XmlNode GetOpenInterestSummary(Token token, Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.Info("GetOpenInterestSummary");
                this.GetAccountIDs(token, ref accountIDs);
                if (accountIDs == null) return null;
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement accountsNode = xmlDoc.CreateElement("Accounts");
                var openInterestSummarys = this.GetOpenInterestSummarysByInstrument(accountIDs, instrumentIDs, blotterCodeSelecteds);
                foreach (OpenInterestSummary openInterestSummary in openInterestSummarys.Values)
                {
                    XmlNode accountNode = xmlDoc.ImportNode(openInterestSummary.ToXmlNode("Account", null), true);
                    accountsNode.AppendChild(accountNode);
                }
                if (!accountsNode.HasChildNodes) accountsNode = null;

                return accountsNode;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        private Dictionary<string, OpenInterestSummary> GetOpenInterestSummarysByInstrument(Guid[] accountIDs, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            Dictionary<string, OpenInterestSummary> openInterestSummarys = new Dictionary<string, OpenInterestSummary>();
            foreach (Guid eachAccountId in accountIDs)
            {
                if (!AccountRepository.Default.Contains(eachAccountId)) continue;
                Account account = (Account)AccountRepository.Default.Get(eachAccountId);
                var trans = account.GetTrans(instrumentIDs);
                if (trans.Count > 0)
                {
                    bool hasOpenOrder = false;
                    OpenInterestSummary accountOpenInterestSummary = new OpenInterestSummary(eachAccountId.ToString());
                    foreach (Transaction tran in trans)
                    {
                        if (tran.OrderType == OrderType.BinaryOption) continue;
                        bool isExistsOpenOrder;
                        decimal buyLot, sellLot, buySumEL, sellSumEL, buyContractSize, sellContractSize;
                        tran.OpenInterestSummary(blotterCodeSelecteds, out isExistsOpenOrder, out buyLot, out sellLot, out buySumEL, out sellSumEL, out buyContractSize, out sellContractSize);
                        if (!isExistsOpenOrder) continue;

                        hasOpenOrder = true;
                        Instrument instrument = tran.Instrument;
                        if (accountOpenInterestSummary.NumeratorUnit == null || accountOpenInterestSummary.NumeratorUnit > instrument.NumeratorUnit)
                        {
                            accountOpenInterestSummary.NumeratorUnit = instrument.NumeratorUnit;
                        }
                        if (accountOpenInterestSummary.Denominator == null || accountOpenInterestSummary.Denominator < instrument.Denominator)
                        {
                            accountOpenInterestSummary.Denominator = instrument.Denominator;
                        }
                        accountOpenInterestSummary.BuyLot += buyLot;
                        accountOpenInterestSummary.SellLot += sellLot;
                        accountOpenInterestSummary.BuySumEL += buySumEL;
                        accountOpenInterestSummary.SellSumEL += sellSumEL;
                        accountOpenInterestSummary.BuyContractSize += buyContractSize;
                        accountOpenInterestSummary.SellContractSize += sellContractSize;
                        accountOpenInterestSummary.AccountType = account.Type;
                        if (account.Type == AccountType.Company)
                        {
                            accountOpenInterestSummary.NetLot = accountOpenInterestSummary.SellLot - accountOpenInterestSummary.BuyLot;
                            accountOpenInterestSummary.NetContractSize = accountOpenInterestSummary.SellContractSize - accountOpenInterestSummary.BuyContractSize;
                        }
                        else
                        {
                            accountOpenInterestSummary.NetLot = accountOpenInterestSummary.BuyLot - accountOpenInterestSummary.SellLot;
                            accountOpenInterestSummary.NetContractSize = accountOpenInterestSummary.BuyContractSize - accountOpenInterestSummary.SellContractSize;
                        }
                    }
                    if (hasOpenOrder)
                    {
                        openInterestSummarys.Add(accountOpenInterestSummary.Id, accountOpenInterestSummary);
                    }
                }
            }
            return openInterestSummarys;
        }

        private void GetAccountIDs(Token token, ref Guid[] accountIDs)
        {
            HashSet<Guid> dbAccoutIds = this.GetAccountIdsFromDB(token);
            if (dbAccoutIds == null || dbAccoutIds.Count == 0) return;
            if (accountIDs == null)
            {
                accountIDs = dbAccoutIds.ToArray();
            }
            else
            {
                HashSet<Guid> sourceAccountIds = new HashSet<Guid>(accountIDs);
                accountIDs = sourceAccountIds.Intersect(dbAccoutIds).ToArray();
            }
        }

        private HashSet<Guid> GetAccountIdsFromDB(Token token)
        {
            string permissionName = (token.AppType == AppType.DealingConsole ? "Access1" : "Access4"); //other.....
            string sql = string.Empty;
            if (token.AppType == AppType.DealingConsole)
            {
                sql = string.Format("SELECT AccountID FROM [dbo].[FT_GetDealerGroupAccount]('{0}')", token.UserID);
            }
            else
            {
                sql = string.Format(@"SELECT AccountID FROM [dbo].[FT_GetAccountsByUser]('{0}','{1}',0)", token.UserID, permissionName);
            }
            DataSet dataSet = DataAccess.GetData(sql, this.ConnectionString);
            if (dataSet.Tables.Count <= 0 || dataSet.Tables[0].Rows.Count <= 0) return null;
            HashSet<Guid> result = new HashSet<Guid>();
            foreach (DataRow dataRow in dataSet.Tables[0].Rows)
            {
                result.Add((Guid)dataRow["AccountID"]);
            }
            return result;
        }


        internal XmlNode GetMemoryBalanceNecessaryEquityExcludeAlerted(Token token)
        {
            List<Guid> accountIds = this.GetRiskMonitorAccountIdsExcludeAlerted(token);
            if (accountIds.Count <= 0) return null;

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement transactionServerNode = xmlDoc.CreateElement("TransactionServer");
            xmlDoc.AppendChild(transactionServerNode);

            foreach (Account eachAccount in AccountRepository.Default.Accounts)
            {
                if (accountIds.Count == 0 || accountIds.Contains(eachAccount.Id))
                {
                    transactionServerNode.AppendChild(eachAccount.GetMemoryBalanceNecessaryEquity(xmlDoc));
                }
            }

            return transactionServerNode;
        }

        private List<Guid> GetRiskMonitorAccountIdsExcludeAlerted(Token token)
        {
            List<Guid> accountIds = new List<Guid>();
            try
            {
                string sql = string.Format("EXEC dbo.P_GetRiskMonitorAccountIdsExcludeAlerted '{0}'", token.UserID);
                DataSet dataSet = DataAccess.GetData(sql, this.ConnectionString);
                if (dataSet == null || dataSet.Tables.Count <= 0)
                {
                    return accountIds;
                }
                DataTable table = dataSet.Tables[0];
                DataRowCollection rows = table.Rows;
                foreach (DataRow row in rows)
                {
                    accountIds.Add((Guid)row["AccountID"]);
                }
                return accountIds;
            }
            catch (System.Exception exception)
            {
                Logger.Error(exception);
            }
            return accountIds;
        }

        private class OpenInterestInstrumentInfo
        {
            private Guid instrumentId;
            private string code;
            private string originCode;
            private int numeratorUnit;
            private int denominator;

            public Guid InstrumentId
            {
                get { return this.instrumentId; }
            }

            public string Code
            {
                get { return this.code; }
            }

            public string OriginCode
            {
                get { return this.originCode; }
            }
            public int NumeratorUnit
            {
                get { return this.numeratorUnit; }
            }
            public int Denominator
            {
                get { return this.denominator; }
            }

            private OpenInterestInstrumentInfo() { }

            public OpenInterestInstrumentInfo(DataRow dataRow)
            {
                this.instrumentId = (Guid)dataRow["InstrumentId"];
                this.code = (string)dataRow["Code"];
                this.originCode = (string)dataRow["OriginCode"];
                this.numeratorUnit = (int)dataRow["NumeratorUnit"];
                this.denominator = (int)dataRow["Denominator"];
            }
        }

    }


}