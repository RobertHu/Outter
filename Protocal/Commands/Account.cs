using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Collections.Concurrent;

namespace Protocal.Commands
{
    public class Account : XmlFillable<Account>
    {
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(Account));

        protected object _mutex = new object();
        protected decimal[] _necessaries = new decimal[4];
        protected Fund _fund;
        protected Dictionary<Guid, Fund> _subFunds = new Dictionary<Guid, Fund>();
        private Dictionary<Guid, Transaction> _transactions = new Dictionary<Guid, Transaction>();

        private List<DeliveryRequest> _changedRequests = new List<DeliveryRequest>();

        private long refrenceCount = 0;

        public Account(Guid id)
        {
            this.Id = id;
            this.DeliveryRequests = new Dictionary<Guid, DeliveryRequest>();
        }

        public Dictionary<Guid, DeliveryRequest> DeliveryRequests { get; private set; }

        public Guid Id { get; private set; }
        public decimal Equity { get; set; }
        public decimal TotalDeposit { get; private set; }
        public decimal MinUpkeepEquity { get; private set; }
        public AlertLevel AlertLevel { get; private set; }
        public DateTime AlertTime { get; private set; }
        public bool IsMultiCurrency { get; private set; }
        public AccountType Type { get; private set; }
        public Fund Fund { get { return _fund; } }

        public string CustomerName { get; private set; }

        public string CurrencyCode { get; private set; }

        public string Code { get; private set; }

        public decimal CreditAmount { get; private set; }

        public int? Leverage { get; set; }

        public decimal RateMarginD { get; private set; }
        public decimal RateMarginO { get; private set; }
        public decimal RateMarginLockD { get; private set; }
        public decimal RateMarginLockO { get; private set; }

        public decimal EstimateCloseCommission { get; private set; }
        public decimal EstimateCloseLevy { get; private set; }


        public decimal Necessary
        {
            get { return _necessaries[0]; }
        }


        public IEnumerable<Fund> SubFunds
        {
            get
            {
                return this._subFunds.Values;
            }
        }

        public IEnumerable<Transaction> Transactions
        {
            get
            {
                lock (_mutex)
                {
                    return _transactions.Values;
                }
            }
        }

        public int TransactionCount
        {
            get
            {
                lock (_mutex)
                {
                    return _transactions.Count;
                }
            }
        }

        public List<DeliveryRequest> ChangedRequests
        {
            get
            {
                lock (_mutex)
                {
                    return _changedRequests;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("Id={0}, customerName={1}, code={2},  equity={3}, nessary={4}, alertLevel= {5}, alertTime={6}",
                this.Id, this.CustomerName, this.Code, this.Equity, this.Necessary, this.AlertLevel.ToString(), this.AlertTime);
        }


        public Transaction GetTran(Guid tranId)
        {
            lock (_mutex)
            {
                Transaction result;
                _transactions.TryGetValue(tranId, out result);
                return result;
            }
        }

        public List<Transaction> GetTrans(Guid[] instrumentIds)
        {
            lock (_mutex)
            {
                List<Transaction> result = new List<Transaction>();
                foreach (var eachTran in this.Transactions)
                {
                    if (instrumentIds.Contains(eachTran.InstrumentId))
                    {
                        result.Add(eachTran);
                    }

                }
                return result;
            }
        }

        public Order GetOrder(Guid orderId)
        {
            lock (_mutex)
            {
                foreach (var eachTran in this.Transactions)
                {
                    foreach (var eachOrder in eachTran.Orders)
                    {
                        if (eachOrder.Id == orderId) return eachOrder;
                    }
                }
                return null;
            }
        }


        public Fund GetFund(Guid currencyId)
        {
            lock (_mutex)
            {
                Fund result;
                _subFunds.TryGetValue(currencyId, out result);
                return result;
            }
        }

        public long IncrementReference()
        {
            lock (this._mutex)
            {
                refrenceCount++;
                return refrenceCount;
            }
        }

        public long DecrementReference()
        {
            lock (this._mutex)
            {
                refrenceCount--;

                return refrenceCount;
            }
        }

        public void Initialize(XElement accountElement)
        {
            lock (this._mutex)
            {
                this._fund = this.CreateFund(this, accountElement);
                this._subFunds.Clear();
                this._transactions.Clear();
                this.DeliveryRequests.Clear();
                this.UpdateForInitialize(accountElement);
            }
        }

        private void UpdateForInitialize(XElement accountNode)
        {
            bool isMainFundChanged;
            this.UpdateAccountPropertiesAndMainFund(accountNode, out isMainFundChanged);
            XElement fundsElement = accountNode.Element("Funds");
            if (fundsElement != null)
            {
                this.ParseSubFunds(fundsElement);
            }
            XElement transNode = accountNode.Element("Transactions");
            if (transNode != null)
            {
                this.ParseTransactions(transNode);
            }

            XElement deliveryRequestsNode = accountNode.Element("DeliveryRequests");
            if (deliveryRequestsNode != null && deliveryRequestsNode.Elements("DeliveryRequest").Count() > 0)
            {
                this.ParseDeliveryRequests(deliveryRequestsNode);
            }

        }

        private void ParseDeliveryRequests(XElement requestsNode)
        {
            foreach (XElement eachRequest in requestsNode.Elements("DeliveryRequest"))
            {
                var id = eachRequest.AttrToGuid("Id");
                DeliveryRequest request;
                if (!this.DeliveryRequests.TryGetValue(id, out request))
                {
                    request = new DeliveryRequest(this, eachRequest);
                    this.DeliveryRequests.Add(id, request);
                    _changedRequests.Add(request);
                }
                else
                {
                    request.Update(eachRequest);
                }
            }
        }

        protected virtual OrderPhaseChange CreateOrderPhaseChange(OrderChangeType type)
        {
            return new OrderPhaseChange(type, null);
        }


        private void ParseTransactions(XElement transNode)
        {
            foreach (XElement eachTranNode in transNode.Elements("Transaction"))
            {
                Transaction tran = this.CreateTran(this);
                tran.Initialize(eachTranNode);
                _transactions.Add(tran.Id, tran);
            }
        }

        protected virtual Transaction CreateTran(Account account)
        {
            return new Transaction(account);
        }

        private Fund CreateFund(Account owner, XElement fundNode)
        {
            Guid subCurrencyId = Guid.Parse(fundNode.Attribute("CurrencyID").Value);
            string currencyCode = fundNode.Attribute("CurrencyCode").Value;
            return this.DoCreateFund(owner, subCurrencyId, currencyCode);
        }

        protected virtual Fund DoCreateFund(Account owner, Guid subCurrencyId, string currencyCode)
        {
            return new Fund(owner, subCurrencyId, currencyCode);
        }


        private void ParseSubFunds(XElement fundsNode)
        {
            foreach (XElement eachFundNode in fundsNode.Elements("Fund"))
            {
                Fund subFund = this.CreateFund(this, eachFundNode);
                this._subFunds.Add(subFund.CurrencyId, subFund);
                subFund.Initialize(eachFundNode);
            }
        }

        public List<OrderPhaseChange> Update(XElement accountElement, out ChangedFund changedFund)
        {
            lock (_mutex)
            {
                bool isMainFundChanged;
                this.UpdateAccountPropertiesAndMainFund(accountElement, out isMainFundChanged);
                _changedRequests.Clear();
                XElement fundsElement = accountElement.Element("Funds");
                Fund changedSubFund = null;
                if (fundsElement != null && fundsElement.Elements("Fund").Count() > 0)
                {
                    this.UpdateSubFunds(fundsElement, out changedSubFund);
                }
                changedFund = new ChangedFund(isMainFundChanged, changedSubFund);
                XElement deliveryRequestsNode = accountElement.Element("DeliveryRequests");
                if (deliveryRequestsNode != null && deliveryRequestsNode.Elements("DeliveryRequest").Count() > 0)
                {
                    this.ParseDeliveryRequests(deliveryRequestsNode);
                }
                XElement transNode = accountElement.Element("Transactions");
                if (transNode != null && transNode.Elements("Transaction").Count() > 0)
                {
                    return this.UpdateTransactions(transNode);
                }
                return new List<OrderPhaseChange>();
            }
        }

        private List<OrderPhaseChange> UpdateTransactions(XElement transNode)
        {
            List<OrderPhaseChange> changes = new List<OrderPhaseChange>();
            foreach (XElement eachTranElement in transNode.Elements("Transaction"))
            {
                var orderChanges = this.UpdateTran(eachTranElement);
                if (orderChanges != null)
                {
                    changes.AddRange(orderChanges);
                }
            }
            return changes;
        }

        private List<OrderPhaseChange> UpdateTran(XElement tranElement)
        {
            Transaction tran = null;
            Guid tranId = Guid.Parse(tranElement.Attribute("ID").Value);
            if (!_transactions.TryGetValue(tranId, out tran))
            {
                tran = this.CreateTran(this);
                _transactions.Add(tranId, tran);
            }
            return tran.Update(tranElement);
        }


        private void UpdateAccountPropertiesAndMainFund(XElement accountNode, out bool isMainFundChanged)
        {
            isMainFundChanged = false;
            this.UpdateAccountProperties(accountNode);
            try
            {
                decimal oldBalance = _fund.Balance;
                decimal oldDeposit = _fund.TotalDeposit;
                this._fund.Update(accountNode);
                if (_fund.Balance != oldBalance && oldDeposit == _fund.TotalDeposit)
                {
                    Logger.InfoFormat("fund balance changed oldBalance= {0} , balance = {1}", oldBalance, _fund.Balance);
                    isMainFundChanged = true;
                }
            }
            catch (System.NullReferenceException ex)
            {
                Logger.Error(string.Format("fund is {0}", _fund == null ? "null" : "not null"), ex);
                throw;
            }
        }

        private void UpdateAccountProperties(XElement accountNode)
        {
            this.InitializeProperties(accountNode);
        }

        private void UpdateSubFunds(XElement fundsNode, out Fund changedFund)
        {
            changedFund = null;
            foreach (XElement eachFundElement in fundsNode.Elements("Fund"))
            {
                Guid subCurrencyId = Guid.Parse(eachFundElement.Attribute("CurrencyID").Value);
                Fund subFund = null;
                if (!this._subFunds.TryGetValue(subCurrencyId, out subFund))
                {
                    subFund = this.CreateFund(this, eachFundElement);
                    this._subFunds.Add(subFund.CurrencyId, subFund);
                }
                decimal oldBalance = subFund.Balance;
                subFund.Update(eachFundElement);
                if (oldBalance != subFund.Balance)
                {
                    changedFund = subFund;
                }
            }
        }

        protected override void InnerInitializeProperties(System.Xml.Linq.XElement element)
        {
            this.FillProperty(m => m.TotalDeposit);
            this.FillProperty(m => m.Equity);
            this.FillProperty(m => m.AlertLevel);
            this.FillProperty(m => m.AlertTime);
            this.FillProperty(m => m.IsMultiCurrency);
            this.FillProperty(m => m.Code);
            this.FillProperty(m => m.CustomerName);
            this.FillProperty(m => m.CurrencyCode);
            this.FillProperty(m => m.Type);
            this.FillProperty(m => m.CreditAmount);
            this.FillProperty(m => m.Leverage);
            this.FillProperty(m => m.RateMarginD);
            this.FillProperty(m => m.RateMarginO);
            this.FillProperty(m => m.RateMarginLockD);
            this.FillProperty(m => m.RateMarginLockO);
            this.FillProperty(m => m.EstimateCloseCommission);
            this.FillProperty(m => m.EstimateCloseLevy);

            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.Name == "Necessary")
                {
                    this._necessaries[0] = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "MinEquityAvoidRiskLevel1")
                {
                    this._necessaries[1] = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "MinEquityAvoidRiskLevel2")
                {
                    this._necessaries[2] = decimal.Parse(attribute.Value);
                }
                else if (attribute.Name == "MinEquityAvoidRiskLevel3")
                {
                    this._necessaries[3] = decimal.Parse(attribute.Value);
                }
            }
        }
    }

    public class Customer
    {
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(Customer));

        public Guid Id { get; private set; }
        private List<Account> accounts = new List<Account>();
        private static ConcurrentDictionary<Guid, Customer> customers = new ConcurrentDictionary<Guid, Customer>();

        public Customer(Guid id)
        {
            this.Id = id;
        }

        public static Customer GetOrAdd(Guid userId)
        {
            return customers.GetOrAdd(userId, (id) => { return new Customer(id); });
        }

        public static bool TryRemove(Guid userId, out Customer value)
        {
            return customers.TryRemove(userId, out value);
        }

        public void Add(Account account)
        {
            this.accounts.Add(account);
        }

        public void Clear()
        {
            foreach (Account account in accounts)
            {
                AccountRepository.Default.Remove(account.Id);
            }

            this.accounts.Clear();
        }
    }

    public sealed class ChangedFund
    {
        private Fund _fund;
        private bool _isMainFundChanged;

        public ChangedFund(bool isMainFundChanged, Fund subFund)
        {
            this._isMainFundChanged = isMainFundChanged;
            this._fund = subFund;
        }

        public Fund Result
        {
            get
            {
                if (this._isMainFundChanged && this._fund != null)
                {
                    return this._fund;
                }
                return null;
            }
        }
    }

}
