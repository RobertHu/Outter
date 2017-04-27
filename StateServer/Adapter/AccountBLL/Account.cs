using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using iExchange.Common;
using System.Xml.Linq;
using System.Diagnostics;
using Protocal.TypeExtensions;
using System.Xml.Xsl;
using log4net;
using iExchange.StateServer.Adapter.Util;
using iExchange.StateServer.Adapter.FaxEmailServices;
using iExchange.StateServer.Adapter.Settings;

namespace iExchange.StateServer.Adapter
{
    internal class Account : Protocal.Commands.Account
    {
        private AccountCommandBuilder _commandBuilder;
        private List<AccountBLL.AccountInstrument> _instruments = new List<AccountBLL.AccountInstrument>();

        internal decimal Balance
        {
            get { return _fund.Balance; }
        }

        internal Account(Guid id)
            : base(id)
        {
            _commandBuilder = new AccountCommandBuilder(this);
        }


        public override string ToString()
        {
            return string.Format("Id={0}, customerName={1}, code={2},  equity={3}, nessary={4}, alertLevel= {5}, alertTime={6}",
                this.Id, this.CustomerName, this.Code, this.Equity, this.Necessary, this.AlertLevel.ToString(), this.AlertTime);
        }

        protected override Protocal.Commands.Transaction CreateTran(Protocal.Commands.Account account)
        {
            return new Transaction((Account)account);
        }

        protected override Protocal.Commands.OrderPhaseChange CreateOrderPhaseChange(Protocal.Commands.OrderChangeType type)
        {
            return new OrderChange(null, type);
        }

        protected override Protocal.Commands.Fund DoCreateFund(Protocal.Commands.Account owner, Guid subCurrencyId, string currencyCode)
        {
            return new Fund((Account)owner, subCurrencyId, currencyCode);
        }


        protected override void InnerInitializeProperties(XElement element)
        {
            base.InnerInitializeProperties(element);
        }


        internal AccountBLL.AccountInstrument GetInstrument(Guid instrumentId)
        {
            lock (_mutex)
            {
                return _instruments.Find(m => m.InstrumentId == instrumentId);
            }
        }

        public void InitializeFloatingStatus(Protocal.AccountFloatingStatus status)
        {
            lock (_mutex)
            {
                this.UpdateSumFundStatus(status);
                this.UpdateFundFloatingStatus(status.FundStatus);
                this.UpdateFloating(status.OrderStatus);
            }
        }

        private void UpdateSumFundStatus(Protocal.AccountFloatingStatus status)
        {
            this.Equity = status.Equity;
            this.Fund.TradePLFloat = status.FloatingPL;
            this.Fund.Necessary = status.Necessary;
        }

        private void UpdateFundFloatingStatus(List<Protocal.FundStatus> funds)
        {
            foreach (var eachFund in funds)
            {
                var fund = this.GetFund(eachFund.CurrencyId);
                if (fund != null)
                {
                    fund.TradePLFloat = eachFund.FloatingPL;
                    fund.Equity = eachFund.Equity;
                    fund.Necessary = eachFund.Necessary;
                }
            }
        }


        private void UpdateFloating(List<Protocal.OrderFloatingStatus> orderStatus)
        {
            Dictionary<Guid, decimal> currencyPerTradePLFloat = new Dictionary<Guid, decimal>(4);
            foreach (var eachOrderStatus in orderStatus)
            {
                var order = this.GetOrder(eachOrderStatus.ID);
                order.TradePLFloat = eachOrderStatus.FloatingPL;
                order.LivePrice = eachOrderStatus.LivePrice;
                Guid currencyId = ((Transaction)order.Owner).CurrencyId;
                decimal oldPL;
                if (!currencyPerTradePLFloat.TryGetValue(currencyId, out oldPL))
                {
                    currencyPerTradePLFloat.Add(currencyId, eachOrderStatus.FloatingPL);
                }
                else
                {
                    currencyPerTradePLFloat[currencyId] = eachOrderStatus.FloatingPL + oldPL;
                }
            }
        }


        private void ClearFloating()
        {
            foreach (var eachTran in this.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    eachOrder.TradePLFloat = 0m;
                }
            }

            foreach (var eachFund in this.SubFunds)
            {
                eachFund.TradePLFloat = 0m;
            }

        }


        internal List<Command> UpdateAndCreateCommand(XElement accountNode, out List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            lock (this._mutex)
            {
                return _commandBuilder.UpdateAndCreateCommand(accountNode, out orderChanges);
            }
        }

        internal XElement InitializeAndGetXml(XElement accountElement)
        {
            lock (this._mutex)
            {
                base.Initialize(accountElement);
                return this.GetInitDataXml();
            }
        }
        private XElement GetInitDataXml()
        {
            XElement result = new XElement("Account");
            this.FillXmlAttrs(result);
            foreach (Fund eachFund in _subFunds.Values)
            {
                var currencyNode = new XElement("Currency");
                eachFund.FillXmlAttrs(currencyNode);
                result.Add(currencyNode);
            }

            XElement ordersNode = new XElement("Orders");
            foreach (var eachTran in this.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.Phase != null && eachOrder.Phase == OrderPhase.Executed && (eachOrder.LotBalance > 0 || eachOrder.DeliveryLockLot > 0))
                    {
                        XElement orderNode = ((Order)eachOrder).ToXml(true, false);
                        ordersNode.Add(orderNode);
                    }
                }
            }
            result.Add(ordersNode);
            return result;
        }

        private void FillXmlAttrs(XElement accountNode)
        {
            accountNode.SetAttributeValue("ID", XmlConvert.ToString(this.Id));
            accountNode.SetAttributeValue("Balance", XmlConvert.ToString(this._fund.Balance));
            accountNode.SetAttributeValue("Necessary", XmlConvert.ToString(this._fund.Necessary));
            accountNode.SetAttributeValue("Necessary0", XmlConvert.ToString(this._necessaries[0]));
            accountNode.SetAttributeValue("Necessary1", XmlConvert.ToString(this._necessaries[1]));
            accountNode.SetAttributeValue("Necessary2", XmlConvert.ToString(this._necessaries[2]));
            accountNode.SetAttributeValue("Necessary3", XmlConvert.ToString(this._necessaries[3]));
            accountNode.SetAttributeValue("Equity", XmlConvert.ToString(this.Equity));
            accountNode.SetAttributeValue("MinUpkeepEquity", XmlConvert.ToString(this.MinUpkeepEquity));
            accountNode.SetAttributeValue("InterestPLNotValued", XmlConvert.ToString(this._fund.InterestPLNotValued));
            accountNode.SetAttributeValue("StoragePLNotValued", XmlConvert.ToString(this._fund.StoragePLNotValued));
            accountNode.SetAttributeValue("TradePLNotValued", XmlConvert.ToString(this._fund.TradePLNotValued));
            accountNode.SetAttributeValue("InterestPLFloat", XmlConvert.ToString(this._fund.InterestPLFloat));
            accountNode.SetAttributeValue("StoragePLFloat", XmlConvert.ToString(this._fund.StoragePLFloat));
            accountNode.SetAttributeValue("TradePLFloat", XmlConvert.ToString(this._fund.TradePLFloat));
            accountNode.SetAttributeValue("ValueAsMargin", XmlConvert.ToString(this._fund.ValueAsMargin));
            accountNode.SetAttributeValue("FrozenFund", XmlConvert.ToString(this._fund.FrozenFund));
            accountNode.SetAttributeValue("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this._fund.PartialPaymentPhysicalNecessary));
            accountNode.SetAttributeValue("TotalPaidAmount", XmlConvert.ToString(this._fund.TotalPaidAmount));
            accountNode.SetAttributeValue("AlertLevel", XmlConvert.ToString((int)this.AlertLevel));
            accountNode.SetAttributeValue("EstimateCloseCommission", XmlConvert.ToString(this.EstimateCloseCommission));
            accountNode.SetAttributeValue("EstimateCloseLevy", XmlConvert.ToString(this.EstimateCloseLevy));


            if (this.AlertTime != DateTime.MinValue)
            {
                accountNode.SetAttributeValue("AlertTime", XmlConvert.ToString(this.AlertTime, DateTimeFormat.Xml));
            }
        }


        internal XmlElement ToXmlNode(Guid currencyId)
        {
            lock (_mutex)
            {
                XsltArgumentList xsltArgList = new XsltArgumentList();
                xsltArgList.AddParam("currencyID", "", currencyId);
                return (XmlElement)XmlTransform.Transform(this.ToXmlNode(), Util.PathHelper.GetAccountCurrencyStylesheetPath(), xsltArgList);
            }
        }


        internal XmlNode ToXmlNode()
        {
            lock (_mutex)
            {
                return this.ToXml().ToXmlNode();
            }
        }


        public XmlElement ToXmlNodeTest()
        {
            lock (_mutex)
            {
                XmlDocument xmlDoc = new XmlDocument();

                XmlElement accountNode = xmlDoc.CreateElement("Account");
                xmlDoc.AppendChild(accountNode);

                accountNode.SetAttribute("ID", XmlConvert.ToString(this.Id));
                accountNode.SetAttribute("Balance", XmlConvert.ToString(this.Balance));
                accountNode.SetAttribute("Equity", XmlConvert.ToString(this.Equity));
                accountNode.SetAttribute("MinUpkeepEquity", XmlConvert.ToString(this.MinUpkeepEquity));
                accountNode.SetAttribute("Credit", XmlConvert.ToString(this.Fund.RiskCredit));
                accountNode.SetAttribute("CreditAmount", XmlConvert.ToString(this.CreditAmount));
                accountNode.SetAttribute("Necessary", XmlConvert.ToString(this.Necessary));
                accountNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(this.Fund.InterestPLNotValued));
                accountNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(this.Fund.StoragePLNotValued));
                accountNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(this.Fund.TradePLNotValued));
                accountNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(this.Fund.InterestPLFloat));
                accountNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(this.Fund.ValueAsMargin));
                accountNode.SetAttribute("FrozenFund", XmlConvert.ToString(this.Fund.FrozenFund));
                accountNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(this.Fund.StoragePLFloat));
                accountNode.SetAttribute("TradePLFloat", XmlConvert.ToString(this.Fund.TradePLFloat));
                accountNode.SetAttribute("AlertLevel", XmlConvert.ToString((int)this.AlertLevel));
                accountNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this.Fund.PartialPaymentPhysicalNecessary));
                accountNode.SetAttribute("AlertLevel1Necessary", XmlConvert.ToString(this._necessaries[1]));
                accountNode.SetAttribute("AlertLevel2Necessary", XmlConvert.ToString(this._necessaries[2]));
                accountNode.SetAttribute("AlertLevel3Necessary", XmlConvert.ToString(this._necessaries[3]));

                foreach (var eachFund in _subFunds.Values)
                {
                    XmlElement currencyNode = xmlDoc.CreateElement("Currency");
                    accountNode.AppendChild(currencyNode);
                    currencyNode.SetAttribute("ID", XmlConvert.ToString(eachFund.CurrencyId));
                    currencyNode.SetAttribute("Balance", XmlConvert.ToString(eachFund.Balance));
                    currencyNode.SetAttribute("Necessary", XmlConvert.ToString(eachFund.Necessary));
                    currencyNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(eachFund.InterestPLNotValued));
                    currencyNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(eachFund.StoragePLNotValued));
                    currencyNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(eachFund.TradePLNotValued));
                    currencyNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(eachFund.InterestPLFloat));
                    currencyNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(eachFund.StoragePLFloat));
                    currencyNode.SetAttribute("TradePLFloat", XmlConvert.ToString(eachFund.TradePLFloat));
                    currencyNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(eachFund.ValueAsMargin));
                    currencyNode.SetAttribute("FrozenFund", XmlConvert.ToString(eachFund.FrozenFund));
                    currencyNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(eachFund.PartialPaymentPhysicalNecessary));
                }

                return accountNode;
            }
        }


        internal XElement ToXml()
        {
            lock (_mutex)
            {
                return this.ToXml(new XmlParameter(false, false, false));
            }
        }

        internal XElement ToXml(XmlParameter parameter)
        {
            lock (_mutex)
            {
                XElement result = new XElement("Account");
                this.FillXmlAttrs(result);

                foreach (Fund eachFund in this._subFunds.Values)
                {
                    result.Add(eachFund.ToXml(parameter));
                }

                return result;
            }
        }

        internal XmlNode GetAcountInfo(Guid instrumentID)
        {
            lock (_mutex)
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement account = xmlDoc.CreateElement("Account");
                account.SetAttribute("ID", XmlConvert.ToString(this.Id));
                account.SetAttribute("Balance", XmlConvert.ToString(_fund.Balance));
                account.SetAttribute("Equity", XmlConvert.ToString(this.Equity));
                account.SetAttribute("MinUpkeepEquity", XmlConvert.ToString(this.MinUpkeepEquity));
                account.SetAttribute("Necessary", XmlConvert.ToString(this.Necessary));

                var accountInstrument = this.GetInstrument(instrumentID);
                if (accountInstrument != null)
                {
                    XmlElement instrument = xmlDoc.CreateElement("Instrument");
                    account.AppendChild(instrument);
                    instrument.SetAttribute("ID", XmlConvert.ToString(instrumentID));
                    instrument.SetAttribute("BuyLotBalanceSum", XmlConvert.ToString(accountInstrument.BuyLotBalanceSum));
                    instrument.SetAttribute("SellLotBalanceSum", XmlConvert.ToString(accountInstrument.SellLotBalanceSum));
                }

                return account;
            }
        }

        internal XmlNode GetMemoryBalanceNecessaryEquity(XmlDocument xmlDoc)
        {
            lock (_mutex)
            {
                XmlElement accountNode = xmlDoc.CreateElement("Account");
                accountNode.SetAttribute("ID", XmlConvert.ToString(this.Id));
                if (_fund == null) return accountNode;
                accountNode.SetAttribute("Balance", XmlConvert.ToString(_fund.Balance));
                accountNode.SetAttribute("Necessary", XmlConvert.ToString(this.Necessary));

                accountNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(_fund.InterestPLNotValued));
                accountNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(_fund.StoragePLNotValued));
                accountNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(_fund.TradePLNotValued));
                accountNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(_fund.InterestPLFloat));
                accountNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(_fund.StoragePLFloat));
                accountNode.SetAttribute("TradePLFloat", XmlConvert.ToString(_fund.TradePLFloat));
                accountNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(_fund.ValueAsMargin));
                accountNode.SetAttribute("FrozenFund", XmlConvert.ToString(_fund.FrozenFund));
                accountNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(_fund.PartialPaymentPhysicalNecessary));

                foreach (Fund eachFund in this.SubFunds)
                {
                    accountNode.AppendChild(eachFund.GetMemoryBalanceNecessaryEquity(xmlDoc));
                }
                return accountNode;
            }
        }

        internal XmlNode GetOpenInterestSummaryOrderList(Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            lock (_mutex)
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement transactionsNode = xmlDoc.CreateElement("Transactions");
                var trans = this.GetTrans(instrumentIDs);
                if (trans.Count > 0)
                {
                    foreach (Transaction tran in trans)
                    {
                        if (tran.OrderType == OrderType.BinaryOption) continue;

                        XmlElement tranXmlElement = tran.GetOpenInterestSummaryOrderList(blotterCodeSelecteds);
                        if (tranXmlElement != null)
                        {
                            transactionsNode.AppendChild(xmlDoc.ImportNode(tranXmlElement, true));
                        }
                    }
                }
                return transactionsNode;
            }
        }

        public XmlNode GetAccountStatus(XmlDocument xmlDoc)
        {
            lock (_mutex)
            {
                XmlElement accountNode = xmlDoc.CreateElement("Account");
                accountNode.SetAttribute("ID", XmlConvert.ToString(this.Id));
                accountNode.SetAttribute("Balance", XmlConvert.ToString(this.Balance));
                accountNode.SetAttribute("Necessary", XmlConvert.ToString(this.Necessary));
                accountNode.SetAttribute("Equity", XmlConvert.ToString(this.Equity));
                accountNode.SetAttribute("MinUpkeepEquity", XmlConvert.ToString(this.MinUpkeepEquity));
                accountNode.SetAttribute("TradePLFloat", XmlConvert.ToString(this.Fund.TradePLFloat));
                accountNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(this.Fund.ValueAsMargin));
                accountNode.SetAttribute("FrozenFund", XmlConvert.ToString(this.Fund.FrozenFund));
                accountNode.SetAttribute("TotalPaidAmount", XmlConvert.ToString(this.Fund.TotalPaidAmount));
                accountNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this.Fund.PartialPaymentPhysicalNecessary));
                return accountNode;
            }
        }

    }

    internal sealed class AccountCommandBuilder
    {
        private sealed class LastValues
        {
            public AlertLevel AlertLevel { get; set; }
            public int? Leverage { get; set; }
            public decimal Neccessary { get; set; }
        }


        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountCommandBuilder));
        private Account _account;

        internal AccountCommandBuilder(Account account)
        {
            _account = account;
        }

        internal List<Command> UpdateAndCreateCommand(XElement accountNode, out List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            List<Command> result = new List<Command>();
            Protocal.Commands.ChangedFund changedFund;
            LastValues lastValues = this.CreateLastValues(_account);
            orderChanges = _account.Update(accountNode, out changedFund);
            this.ProcessRisk(lastValues.AlertLevel, result, orderChanges);
            this.CreateChangeLeverageCommand(lastValues.Leverage, result);
            this.ProcessOrderChanges(orderChanges, result, accountNode, changedFund, lastValues);
            return result;
        }

        private LastValues CreateLastValues(Account account)
        {
            LastValues result = new LastValues { };
            result.AlertLevel = account.AlertLevel;
            result.Leverage = account.Leverage;
            result.Neccessary = account.Necessary;
            return result;
        }


        private void ProcessOrderChanges(IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges, List<Command> commands, XElement accountNode, Protocal.Commands.ChangedFund changedFund, LastValues lastValues)
        {
            Logger.InfoFormat("order changes count = {0}", orderChanges.Count());
            if (orderChanges != null && orderChanges.Count() == 1 && orderChanges.Single().ChangeType == Protocal.Commands.OrderChangeType.Deleted)
            {
                this.ProcessForOrderDeleted(accountNode, (OrderChange)orderChanges.Single());
            }
            commands.AddRange(this.CreateCommands(accountNode, orderChanges, changedFund, lastValues));
        }

        private void ProcessRisk(AlertLevel oldLevel, List<Command> commands, List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            if (_account.AlertLevel != oldLevel)
            {
                Logger.InfoFormat("risk rised oldRiskLevel = {0}, currentRiskLevel = {1}", oldLevel, _account.AlertLevel);
                var command = this.ProcessWhenRiskLevelChanged(oldLevel, orderChanges);
                if (command != null)
                {
                    commands.Add(command);
                }
            }
        }


        private void CreateChangeLeverageCommand(int? oldLeverage, List<Command> commands)
        {
            if (oldLeverage == _account.Leverage || _account.Leverage == null) return;

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode updateNode = xmlDoc.CreateElement("Update");
            xmlDoc.AppendChild(updateNode);
            XmlNode modifyNode = xmlDoc.CreateElement("Modify");
            updateNode.AppendChild(modifyNode);
            XmlElement accountNode = xmlDoc.CreateElement("Account");
            modifyNode.AppendChild(accountNode);
            accountNode.SetAttribute("ID", _account.Id.ToString());
            accountNode.SetAttribute("Leverage", _account.Leverage.ToString());
            accountNode.SetAttribute("Necessary", _account.Necessary.ToString());
            accountNode.SetAttribute("RateMarginD", _account.RateMarginD.ToString("F4"));
            accountNode.SetAttribute("RateMarginO", _account.RateMarginO.ToString("F4"));
            accountNode.SetAttribute("RateMarginLockD", _account.RateMarginLockD.ToString("F4"));
            accountNode.SetAttribute("RateMarginLockO", _account.RateMarginLockO.ToString("F4"));

            if (_account.IsMultiCurrency)
            {
                XmlElement accountCurrenciesNode = xmlDoc.CreateElement("AccountCurrencies");
                accountNode.AppendChild(accountCurrenciesNode);

                foreach (var accountCurreny in _account.SubFunds)
                {
                    XmlElement accountCurrencyNode = xmlDoc.CreateElement("AccountCurrency");
                    accountCurrencyNode.SetAttribute("CurrencyId", accountCurreny.CurrencyId.ToString());
                    accountCurrencyNode.SetAttribute("Necessary", accountCurreny.Necessary.ToString());
                    accountCurrenciesNode.AppendChild(accountCurrencyNode);
                }
            }
            UpdateCommand updateCommand = new UpdateCommand();
            updateCommand.Content = updateNode;
            commands.Add(updateCommand);
        }


        private Command ProcessWhenRiskLevelChanged(AlertLevel alertLevel, List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            AlertLevel liveAlertLevel = _account.AlertLevel;
            XmlNode alertDb = null;
            XmlNode alertRisk = null;

            if (alertLevel < AlertLevel.Cut)
            {
                if (liveAlertLevel > alertLevel)
                {
                    alertDb = this.GetAlertDb();
                }
                //Alert Risk
                if (liveAlertLevel > alertLevel && alertLevel == AlertLevel.Normal)
                {
                    alertRisk = this.GetAlertRisk();
                }
                if (alertDb != null)
                {
                    alertRisk = this.MargeAlertRisk(alertDb, alertRisk);
                }
            }

            if (this.ShouldAutoResetAlertLevel(alertLevel))
            {
                alertDb = this.GetAlertDb();
                alertRisk = this.GetAlertRisk();
                alertRisk = this.MargeAlertRisk(alertDb, alertRisk);
                if (liveAlertLevel == AlertLevel.Normal)
                {
                    alertDb = null;
                }
                Logger.InfoFormat("accountId = {0}, ResetDBAlertLevel", _account.Id);
                DBHelper.ResetDBAlertLevel(Guid.Empty, _account.Id);
            }
            Logger.InfoFormat("accountId = {0} AutoResetAlertLevel = {1}, oldaLertLevel = {2}, liveAlertLevel = {3}, alertDb = {4}", _account.Id, this.ShouldAutoResetAlertLevel(alertLevel), alertLevel, liveAlertLevel, alertDb == null ? string.Empty : alertDb.OuterXml);

            if (alertDb != null)
            {
                this.NotifyFaxEmailEngine(alertLevel, orderChanges);
                Logger.WarnFormat("accountId = {0},dbo.P_UpdateAlertHistory @xmlAlert= {1}", _account.Id, alertDb.OuterXml);
                DBHelper.UpdateDBAlertHistory(_account.Id, alertDb);
            }

            if (alertRisk != null)
            {
                AlertCommand alertCommand = new AlertCommand();
                alertCommand.AccountID = _account.Id;
                alertCommand.Content = alertRisk;
                Logger.InfoFormat("alert command {0}", alertRisk.OuterXml);
                return alertCommand;
            }
            return null;
        }

        private void NotifyFaxEmailEngine(AlertLevel lastAlertLevel, List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            int alertLevel = this.ShouldAutoResetAlertLevel(lastAlertLevel) ? (int)AlertLevel.Normal : (int)_account.AlertLevel;
            decimal equity = _account.Equity;
            decimal necessary = _account.Necessary;
            string[] orderCodes = this.GetOrderCodes(orderChanges);
            AccountRisk accountRisk = new AccountRisk(_account.Id, _account.CustomerName, _account.Code, _account.CurrencyCode, TradeDayManager.Default.GetTradeDay(), alertLevel, (double)equity, (double)necessary, orderCodes);
            FaxEmailEngine.Default.NotifyAccountRisk(accountRisk);
        }

        private bool ShouldAutoResetAlertLevel(AlertLevel alertLevel)
        {
            return SettingManager.Default.SystemParameter.EnableAutoResetAlertLevel && _account.AlertLevel < alertLevel;
        }



        private string[] GetOrderCodes(List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            if (orderChanges == null) return null;
            List<string> orderCodes = new List<string>();
            foreach (var eachOrder in orderChanges)
            {
                orderCodes.Add(eachOrder.Source.Code);
            }
            return orderCodes.ToArray();
        }


        private XmlNode MargeAlertRisk(XmlNode alertDb, XmlNode alertRisk)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement alertNode = xmlDoc.CreateElement("Alert");
            xmlDoc.AppendChild(alertNode);

            XmlElement alertAccountsNode = xmlDoc.CreateElement("AlertAccounts");
            alertNode.AppendChild(alertAccountsNode);
            alertAccountsNode.AppendChild((XmlElement)xmlDoc.ImportNode(alertDb, true));

            if (alertRisk != null)
            {
                foreach (XmlNode accountNode in alertRisk.ChildNodes)
                {
                    alertNode.AppendChild((XmlElement)xmlDoc.ImportNode(accountNode, true));
                }
            }

            return alertNode;
        }

        private XmlNode GetAlertRisk()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement alertNode = xmlDoc.CreateElement("Alert");
            xmlDoc.AppendChild(alertNode);

            XmlElement accountNode = (XmlElement)xmlDoc.ImportNode(_account.ToXmlNode(), true);
            alertNode.AppendChild(accountNode);

            accountNode.SetAttribute("AlertTime", XmlConvert.ToString(_account.AlertTime, DateTimeFormat.Xml));
            XmlElement transNode = xmlDoc.CreateElement("Transactions");

            foreach (Transaction tran in _account.Transactions)
            {
                transNode.AppendChild(xmlDoc.ImportNode(tran.ToXml().ToXmlNode(), true));
            }
            accountNode.AppendChild(transNode);

            string url = PathHelper.GetAlertRiskStylesheetPath();
            return XmlTransform.Transform(alertNode, url, null);
        }

        private XmlNode GetAlertDb()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement accountNode = xmlDoc.CreateElement("Account");
            xmlDoc.AppendChild(accountNode);

            accountNode.SetAttribute("AlertTime", _account.AlertTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            accountNode.SetAttribute("ID", XmlConvert.ToString(_account.Id));
            accountNode.SetAttribute("Balance", XmlConvert.ToString(_account.Balance));
            accountNode.SetAttribute("Necessary", XmlConvert.ToString(_account.Necessary));
            accountNode.SetAttribute("Equity", XmlConvert.ToString(_account.Equity));
            accountNode.SetAttribute("AlertLevel", XmlConvert.ToString((int)_account.AlertLevel));

            foreach (Transaction tran in _account.Transactions)
            {
                foreach (Order order in tran.Orders)
                {
                    if (order.Phase == OrderPhase.Executed && order.LotBalance > 0)
                    {
                        XmlElement orderNode = xmlDoc.CreateElement("Order");
                        accountNode.AppendChild(orderNode);

                        orderNode.SetAttribute("ID", XmlConvert.ToString(order.Id));
                        orderNode.SetAttribute("LivePrice", (string)order.LivePrice);
                        orderNode.SetAttribute("LotBalance", XmlConvert.ToString(order.LotBalance));
                        orderNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(order.InterestPLFloat));
                        orderNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(order.StoragePLFloat));
                        orderNode.SetAttribute("TradePLFloat", XmlConvert.ToString(order.TradePLFloat));
                    }
                }
            }

            return accountNode;
        }


        private XElement CreateRiskAccountNode()
        {
            XElement result = new XElement("Account");
            result.SetAttributeValue("AlertTime", _account.AlertTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            result.SetAttributeValue("ID", XmlConvert.ToString(_account.Id));
            result.SetAttributeValue("Balance", XmlConvert.ToString(_account.Balance));
            result.SetAttributeValue("Necessary", XmlConvert.ToString(_account.Necessary));
            result.SetAttributeValue("Equity", XmlConvert.ToString(_account.Equity));
            result.SetAttributeValue("AlertLevel", XmlConvert.ToString((int)_account.AlertLevel));
            return result;
        }

        private XElement CreateOrderNode(Order order)
        {
            XElement result = new XElement("Order");
            result.SetAttributeValue("ID", XmlConvert.ToString(order.Id));
            result.SetAttributeValue("LivePrice", (string)order.LivePrice);
            result.SetAttributeValue("LotBalance", XmlConvert.ToString(order.LotBalance));
            result.SetAttributeValue("InterestPLFloat", XmlConvert.ToString(order.InterestPLFloat));
            result.SetAttributeValue("StoragePLFloat", XmlConvert.ToString(order.StoragePLFloat));
            result.SetAttributeValue("TradePLFloat", XmlConvert.ToString(order.TradePLFloat));
            return result;
        }


        private List<Command> CreateCommands(XElement accountNode, IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges, Protocal.Commands.ChangedFund changedFund, LastValues lastValues)
        {
            List<Command> result = new List<Command>();
            if (_account.AlertLevel > lastValues.AlertLevel)
            {
                result.Add(_account.CreateAlertLevelRisedCommand());
            }
            var orderCommands = this.CreateOrderChangedCommand(orderChanges);
            if (orderCommands.Count > 0)
            {
                result.AddRange(orderCommands);
            }
            if (lastValues.Neccessary != _account.Fund.Necessary)
            {
                result.Add(_account.CreateAccountUpdateCommand());
            }
            else if (changedFund.Result != null)
            {
                result.Add(_account.CreateAccountBalanceCommand((Fund)changedFund.Result));
            }

            if (_account.ChangedRequests != null)
            {
                Logger.InfoFormat("create commands changedDeliveryRequest count = {0}", _account.ChangedRequests.Count);
                foreach (var eachRequest in _account.ChangedRequests)
                {
                    result.Add(new DeliveryCommand(eachRequest.ToXmlNode()));
                }
            }

            return result;
        }


        private void ProcessForOrderDeleted(XElement accountXml, OrderChange orderChange)
        {
            Logger.InfoFormat("ProcessForOrderDeleted content = {0}", accountXml);
            foreach (XElement eachTran in accountXml.Element("Transactions").Elements("Transaction"))
            {
                Guid tranId = eachTran.AttrToGuid("ID");
                if (orderChange.TranId != tranId)
                {
                    orderChange.AffectedTrans.Add((Transaction)orderChange.Account.GetTran(tranId));
                }
            }
        }


        private List<Command> CreateApplyDeliveryCommand(XElement accountNode, Account account)
        {
            List<Command> result = new List<Command>();
            var deliveryRequestsNode = accountNode.Element("DeliveryRequests");
            if (deliveryRequestsNode != null)
            {
                foreach (XElement eachRequestNode in deliveryRequestsNode.Elements("DeliveryRequest"))
                {
                    Guid id = eachRequestNode.AttrToGuid("Id");
                    var request = account.DeliveryRequests[id];
                    result.Add(new DeliveryCommand(this.CreateApplyDeliveryNode(request)));
                }
            }
            return result;
        }


        private XmlNode CreateApplyDeliveryNode(Protocal.Commands.DeliveryRequest deliveryRequest)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode xmlApplyDelivery = doc.CreateElement("ApplyDelivery");
            ((XmlElement)xmlApplyDelivery).SetAttribute("Id", XmlConvert.ToString(deliveryRequest.Id));
            ((XmlElement)xmlApplyDelivery).SetAttribute("Code", deliveryRequest.Code);
            ((XmlElement)xmlApplyDelivery).SetAttribute("SubmitTime", deliveryRequest.SubmitTime.ToString("yyyy-MM-dd HH:mm:ss"));
            ((XmlElement)xmlApplyDelivery).SetAttribute("AccountId", XmlConvert.ToString(deliveryRequest.AccountId));
            ((XmlElement)xmlApplyDelivery).SetAttribute("InstrumentId", XmlConvert.ToString(deliveryRequest.InstrumentId));
            ((XmlElement)xmlApplyDelivery).SetAttribute("RequireQuantity", XmlConvert.ToString(deliveryRequest.RequireQuantity));
            ((XmlElement)xmlApplyDelivery).SetAttribute("RequireLot", XmlConvert.ToString(deliveryRequest.RequireLot));
            ((XmlElement)xmlApplyDelivery).SetAttribute("DeliveryTime", deliveryRequest.DeliveryTime.ToString("yyyy-MM-dd"));  // 客户要求提货日期
            ((XmlElement)xmlApplyDelivery).SetAttribute("Charge", XmlConvert.ToString(deliveryRequest.Charge));
            ((XmlElement)xmlApplyDelivery).SetAttribute("Status", XmlConvert.ToString((int)deliveryRequest.Status));
            ((XmlElement)xmlApplyDelivery).SetAttribute("Ask", deliveryRequest.Ask);
            ((XmlElement)xmlApplyDelivery).SetAttribute("Bid", deliveryRequest.Bid);
            ((XmlElement)xmlApplyDelivery).SetAttribute("ChargeCurrencyId", XmlConvert.ToString(deliveryRequest.ChargeCurrencyId));
            if (deliveryRequest.DeliveryAddressId != Guid.Empty)
            {
                ((XmlElement)xmlApplyDelivery).SetAttribute("DeliveryAddressId", XmlConvert.ToString(deliveryRequest.DeliveryAddressId));
            }
            foreach (Protocal.Commands.DeliveryRequestOrderRelation relation in deliveryRequest.Relations)
            {
                XmlElement xmlDeliveryRequestOrderRelation = doc.CreateElement("DeliveryRequestOrderRelation");
                xmlApplyDelivery.AppendChild(xmlDeliveryRequestOrderRelation);
                xmlDeliveryRequestOrderRelation.SetAttribute("OpenOrderId", XmlConvert.ToString(relation.OpenOrderId));
                xmlDeliveryRequestOrderRelation.SetAttribute("DeliveryQuantity", XmlConvert.ToString(relation.DeliveryQuantity));
                xmlDeliveryRequestOrderRelation.SetAttribute("DeliveryLot", XmlConvert.ToString(relation.DeliveryLot));
            }
            if (deliveryRequest.Specifications != null)
            {
                XmlElement xmlDeliverySpecifications = doc.CreateElement("DeliveryRequestSpecifications");
                foreach (var specification in deliveryRequest.Specifications)
                {
                    if (specification.Quantity > 0)
                    {
                        XmlElement xmlDeliverySpecification = doc.CreateElement("DeliveryRequestSpecification");
                        xmlDeliverySpecification.SetAttribute("Size", XmlConvert.ToString(specification.Size));
                        xmlDeliverySpecification.SetAttribute("Quantity", XmlConvert.ToString(specification.Quantity));
                        xmlDeliverySpecifications.AppendChild(xmlDeliverySpecification);
                    }
                }
                xmlApplyDelivery.AppendChild(xmlDeliverySpecifications);
            }
            return xmlApplyDelivery;
        }


        private List<Command> CreateOrderChangedCommand(IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            List<Command> result = new List<Command>();
            foreach (OrderChange orderChange in orderChanges)
            {
                result.AddRange(orderChange.ToCommand());
            }
            return result;
        }

    }

    internal static class CommandHelper
    {
        internal static bool SameAsExecuteCommand(this Command command)
        {
            return command is ExecuteCommand || command is Execute2Command || command is CutCommand;
        }
    }

    internal sealed class OrderChange : Protocal.Commands.OrderPhaseChange
    {
        internal OrderChange(Order source, Protocal.Commands.OrderChangeType changeType, PropertyChangeType changeProperties = PropertyChangeType.None)
            : base(changeType, source)
        {
            this.PropertyType = changeProperties;
            this.AffectedTrans = new List<Transaction>();
        }
        internal PropertyChangeType PropertyType { get; private set; }

        internal Account Account { get { return (Account)this.Source.Owner.Owner; } }

        new public Transaction Tran { get { return (Transaction)base.Tran; } }

        internal Guid AccountId { get { return this.Account.Id; } }

        internal Guid InstrumentId { get { return this.Source.Owner.InstrumentId; } }

        internal Guid TranId { get { return this.Tran.Id; } }

        internal List<Transaction> AffectedTrans { get; private set; }

    }

    internal struct XmlParameter
    {
        internal XmlParameter(bool isForGetInitData, bool isForReport, bool includeTrans)
            : this()
        {
            this.IsForGetInitData = isForGetInitData;
            this.IsForReport = isForReport;
            this.IncludeTrans = includeTrans;
        }

        internal bool IsForGetInitData { get; private set; }
        internal bool IsForReport { get; private set; }
        internal bool IncludeTrans { get; private set; }
    }
}