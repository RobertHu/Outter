using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using Core.TransactionServer.Agent.Util.TypeExtension;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Protocal.Physical;
using Core.TransactionServer.Agent.Physical.InstalmentBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Protocal.TypeExtensions;
using System.Xml.Linq;
using System.Xml;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Protocal.CommonSetting;
using System.Threading;
using System.Collections.Concurrent;

namespace Core.TransactionServer.Agent
{
    public sealed class TradingSetting
    {
        private sealed class OrderInstalmentRepository
        {
            private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderInstalmentRepository));

            private Dictionary<Guid, List<OrderInstalmentData>> _orderInstalmentDict = new Dictionary<Guid, List<OrderInstalmentData>>(100);
            private object _mutex = new object();

            internal List<OrderInstalmentData> GetInstalments(Guid orderId)
            {
                lock (_mutex)
                {
                    List<OrderInstalmentData> result;
                    if (!_orderInstalmentDict.TryGetValue(orderId, out result))
                    {
                        Logger.WarnFormat("GetInstalments failed orderId={0}", orderId);
                    }
                    return result;
                }
            }

            internal void AddOrderInstalment(OrderInstalmentData instalment)
            {
                lock (_mutex)
                {
                    List<OrderInstalmentData> instalments;
                    if (!_orderInstalmentDict.TryGetValue(instalment.OrderId, out instalments))
                    {
                        instalments = new List<OrderInstalmentData>();
                        _orderInstalmentDict.Add(instalment.OrderId, instalments);
                    }
                    instalments.Add(instalment);
                }
            }


            internal void DeleteOrderInstalment(Guid orderId, int sequence)
            {
                lock (_mutex)
                {
                    List<OrderInstalmentData> instalments;
                    var result = this.GetOrderInstalment(orderId, sequence, out instalments);
                    if (result != null)
                    {
                        instalments.Remove(result);
                    }
                }
            }

            internal void UpdateOrderInstalment(Guid orderId, int sequence, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime)
            {
                lock (_mutex)
                {
                    List<OrderInstalmentData> instalments;
                    var result = this.GetOrderInstalment(orderId, sequence, out instalments);
                    if (result != null)
                    {
                        result.Interest = interest;
                        result.Principal = principal;
                        result.DebitInterest = debitInterest;
                        result.PaidDateTime = paidDateTime;
                        result.UpdateTime = updateTime;
                    }
                }
            }

            internal void UpdateOrderInstalment(Guid orderId, int sequence, decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
            {
                lock (_mutex)
                {
                    List<OrderInstalmentData> instalments;
                    var result = this.GetOrderInstalment(orderId, sequence, out instalments);
                    if (result != null)
                    {
                        result.InterestRate = interestRate;
                        result.Interest = interest;
                        result.Principal = principal;
                        result.DebitInterest = debitInterest;
                        result.PaidDateTime = paidDateTime;
                        result.UpdateTime = updateTime;
                        result.UpdatePersonId = updatePersonId;
                        result.LotBalance = lotBalance;
                    }
                }
            }

            private OrderInstalmentData GetOrderInstalment(Guid orderId, int sequence, out List<OrderInstalmentData> instalments)
            {
                OrderInstalmentData result = null;
                if (_orderInstalmentDict.TryGetValue(orderId, out instalments))
                {
                    foreach (var eachInstalment in instalments)
                    {
                        if (eachInstalment.Sequence == sequence)
                        {
                            result = eachInstalment;
                            break;
                        }
                    }
                }
                return result;
            }

        }
        private sealed class AccountRepository
        {
            private ConcurrentDictionary<Guid, Account> _accounts;
            private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountRepository));

            internal AccountRepository()
            {
                _accounts = new ConcurrentDictionary<Guid, Account>();
            }


            internal Account this[Guid id]
            {
                get
                {
                    if (!_accounts.ContainsKey(id))
                    {
                        throw new NullReferenceException(string.Format("account with Id = {0} not found", id));
                    }
                    return _accounts[id];
                }
                set
                {
                    _accounts.TryAdd(id, value);
                }
            }

            internal void Add(Guid id, Account account)
            {
                _accounts.TryAdd(id, account);
            }

            internal void AddAccount(Account account)
            {
                if (!_accounts.ContainsKey(account.Id))
                {
                    _accounts.TryAdd(account.Id, account);
                }
                else
                {
                    Logger.ErrorFormat("Add Account , accountId= {0} already exist", account.Id);
                }
            }

            internal void RemoveAccount(Guid accountId)
            {
                if (_accounts.ContainsKey(accountId))
                {
                    Account account;
                    _accounts.TryRemove(accountId, out account);
                }
                else
                {
                    Logger.ErrorFormat("Remove Account , accountId= {0} not exist", accountId);
                }
            }

            internal Account GetAccount(Guid accountId)
            {
                Account result = null;
                _accounts.TryGetValue(accountId, out result);
                return result;
            }

            internal void DoReset(DateTime tradeDay)
            {
                Parallel.ForEach(_accounts.Values, account => account.DoInstrumentReset(tradeDay));
                Parallel.ForEach(_accounts.Values, account => account.DoSystemReset(tradeDay));
            }

            internal string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
            {
                XElement result = new XElement("Accounts");
                foreach (var eachAccount in _accounts.Values)
                {
                    var accountNode = eachAccount.GetProfitWithinXMl(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                    if (accountNode != null)
                    {
                        result.Add(accountNode);
                    }
                }
                return result.ToString();
            }


            internal void DoParallelForAccounts(Action<Account> action)
            {
                Parallel.ForEach(_accounts.Values, action);
            }

            internal void DoWorkForAccounts(Action<Account> action)
            {
                foreach (var eachAccount in _accounts.Values)
                {
                    action(eachAccount);
                }
            }



            internal void CheckAccountsRisk(IEnumerable<Protocal.InstrumentStatusInfo> instruments)
            {
                foreach (var eachAccount in _accounts.Values)
                {
                    foreach (var eachInstrumentInfo in instruments)
                    {
                        if (eachAccount.ExistInstrument(eachInstrumentInfo.Id))
                        {
                            eachAccount.CheckRisk();
                            break;
                        }
                    }
                }
            }

            internal void CheckAllPlacingAndPlacedTransactions()
            {
                foreach (var eachAccount in _accounts.Values)
                {
                    foreach (var eachTran in eachAccount.Transactions)
                    {
                        if (eachTran.ExistsPlacingOrPlacedOrder())
                        {
                            TransactionExpireChecker.Default.Add(eachTran);
                        }
                    }
                }
            }


            internal void CreateInstrumentsForAccount()
            {
                foreach (var eachAccount in _accounts.Values)
                {
                    foreach (var eachTran in eachAccount.Transactions)
                    {
                        if (eachTran.OrderCount > 0)
                        {
                            eachAccount.GetOrCreateInstrument(eachTran.InstrumentId);
                        }
                    }
                }
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradingSetting));
        private const int CAPACITY = 57;
        public static readonly TradingSetting Default = new TradingSetting();

        private Dictionary<Guid, TradingInstrument> _instrumentDict;
        private OrderAndTransactionParser _orderAndTransactionParser;
        private ResetParser _resetParser;
        private OrderInstalmentRepository _orderInstalmentRepository;
        private AccountRepository _accountRepository;
        private object _mutex = new object();

        static TradingSetting() { }

        private TradingSetting()
        {
            _instrumentDict = new Dictionary<Guid, TradingInstrument>(50);
            _orderAndTransactionParser = new OrderAndTransactionParser(this);
            _resetParser = new ResetParser(this);
            _orderInstalmentRepository = new OrderInstalmentRepository();
            _accountRepository = new AccountRepository();
        }

        internal void AddAccount(Account account)
        {
            _accountRepository.AddAccount(account);
        }

        internal void RemoveAccount(Guid accountId)
        {
            _accountRepository.RemoveAccount(accountId);
        }

        internal Account GetAccount(Guid accountId)
        {
            return _accountRepository.GetAccount(accountId);
        }

        internal string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            return _accountRepository.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
        }


        internal TradingInstrument GetInstrument(Guid id)
        {
            lock (_mutex)
            {
                TradingInstrument instrument;
                _instrumentDict.TryGetValue(id, out instrument);
                return instrument;
            }
        }

        internal bool ExistsInstrument(Guid id)
        {
            lock (_mutex)
            {
                return _instrumentDict.ContainsKey(id);
            }
        }

        internal List<OrderInstalmentData> GetInstalments(Guid orderId)
        {
            return _orderInstalmentRepository.GetInstalments(orderId);
        }

        internal void AddOrderInstalment(OrderInstalmentData instalment)
        {
            _orderInstalmentRepository.AddOrderInstalment(instalment);
        }

        internal void DeleteOrderInstalment(Guid orderId, int sequence)
        {
            _orderInstalmentRepository.DeleteOrderInstalment(orderId, sequence);
        }

        internal void UpdateOrderInstalment(Guid orderId, int sequence, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime)
        {
            _orderInstalmentRepository.UpdateOrderInstalment(orderId, sequence, interest, principal, debitInterest, paidDateTime, updateTime);
        }
        internal void UpdateOrderInstalment(Guid orderId, int sequence, decimal interestRate, decimal interest, decimal principal, decimal debitInterest, DateTime paidDateTime, DateTime updateTime, Guid updatePersonId, decimal lotBalance)
        {
            _orderInstalmentRepository.UpdateOrderInstalment(orderId, sequence, interestRate, interest, principal, debitInterest, paidDateTime, updateTime, updatePersonId, lotBalance);
        }


        internal void DoReset(DateTime tradeDay)
        {
            _accountRepository.DoReset(tradeDay);
        }


        internal void DoParallelForAccounts(Action<Account> action)
        {
            _accountRepository.DoParallelForAccounts(action);
        }

        internal void DoWorkForAccounts(Action<Account> action)
        {
            _accountRepository.DoWorkForAccounts(action);
        }

        internal void CheckAccountsRisk(IEnumerable<Protocal.InstrumentStatusInfo> instruments)
        {
            _accountRepository.CheckAccountsRisk(instruments);
        }

        internal void InitializeUnclearDeposit(IDataReader dr)
        {
            UnclearDeposit deposit = new UnclearDeposit(new DBReader(dr));
            _accountRepository[deposit.AccountId].UnclearDepositManager.Add(deposit);
        }

        internal void InitializeAccountEx(IDataReader dr)
        {
            Account account = new Account(new DBReader(dr));
            _accountRepository.Add(account.Setting().Id, account);
        }

        internal void InitializeAccountBalance(IDataReader dr)
        {
            SubFund.Create(new DBReader(dr));
        }


        internal void InitializeDeliveryRequest(IDataReader dr, Dictionary<Guid, DeliveryRequest> deliveryRequests)
        {
            Guid accountId = (Guid)dr["AccountId"];
            Account account = _accountRepository[accountId];
            DeliveryRequest deliveryRequest = new DeliveryRequest(account, new DBReader(dr));
            deliveryRequests.Add(deliveryRequest.Id, deliveryRequest);
            DeliveryRequestManager.Default.Add(deliveryRequest);
        }


        internal void InitializeDeliveryRequestOrderRelation(IDataReader dr, Dictionary<Guid, DeliveryRequest> deliveryRequests)
        {
            Guid deliveryRequestId = (Guid)dr["DeliveryRequestId"];
            DeliveryRequest deliveryRequest = deliveryRequests[deliveryRequestId];
            DeliveryRequestOrderRelation deliveryRequestOrderRelation = new DeliveryRequestOrderRelation(deliveryRequest, new DBReader(dr));
            deliveryRequestOrderRelation.LockDeliveryLot();
        }

        internal void InitializeOrderInstalment(IDataReader dr, Dictionary<Guid, Order> orders)
        {
            _orderAndTransactionParser.InitializeOrderInstalment(dr, orders);
        }


        internal void InitializeTransaction(IDataReader dr, Dictionary<Guid, Transaction> trans)
        {
            _orderAndTransactionParser.InitializeTransaction(dr, trans);
        }

        internal void InitializeOrder(IDataReader dr, Dictionary<Guid, Transaction> trans, Dictionary<Guid, Order> orders)
        {
            _orderAndTransactionParser.InitializeOrder(dr, trans, orders);
        }

        internal void InitializeOrderRelation(IDataReader dr, Dictionary<Guid, Order> orders)
        {
            _orderAndTransactionParser.InitializeOrderRelation(dr, orders);
        }

        internal void InitializeBill(IDataReader dr)
        {
            BillParser.Default.Initialize(dr);
        }

        internal void InitializeInstrumentResetStatus(IDataReader dr)
        {
            _resetParser.InitializeInstrumentResetStatus(new DBReader(dr));
        }

        internal void InitializeAccountResetStatus(IDataReader dr)
        {
            _resetParser.InitializeAccountResetStatus(new DBReader(dr));
        }


        internal void InitializeOrderPLNotValued(IDBRow dr, Dictionary<Guid, Order> orderDict)
        {
            _orderAndTransactionParser.InitializeOrderPLNotValued(dr, orderDict);
        }

        internal void ParseDBRecords(DataSet dataSet)
        {
            SettingInitializer.Initialize(dataSet, "OrderDayHistory", dr =>
              {
                  DB.DBMapping.OrderDayHistory model = new DB.DBMapping.OrderDayHistory(new DBRow(dr));
                  ResetManager.Default.AddOrderDayHistory(model);
              });

            Dictionary<Guid, Order> orderDict;
            var trans = _orderAndTransactionParser.Parse(dataSet, out orderDict);
            Stopwatch watch = Stopwatch.StartNew();
            BillParser.Default.Parse(dataSet);
            watch.Stop();
            Logger.InfoFormat("parse bill cost time = {0}ms", watch.ElapsedMilliseconds);

            foreach (var eachTran in trans.Values)
            {
                if (eachTran.OrderCount > 0)
                {
                    Guid instrumentId = eachTran.InstrumentId;
                    eachTran.Owner.GetOrCreateInstrument(instrumentId);
                    if (!_instrumentDict.ContainsKey(instrumentId))
                    {
                        _instrumentDict.Add(instrumentId, new TradingInstrument(Setting.Default.GetInstrument(instrumentId)));
                    }
                }
            }

            if (orderDict != null)
            {
                foreach (var eachOrder in orderDict.Values)
                {
                    eachOrder.CalculateInit();
                }
            }
        }


        internal void BuildTradingInstruments()
        {
            foreach (var eachInstrument in Settings.Setting.Default.Instruments.Values)
            {
                if (!_instrumentDict.ContainsKey(eachInstrument.Id))
                {
                    _instrumentDict.Add(eachInstrument.Id, new TradingInstrument(eachInstrument));
                }
            }
        }

        internal void OnInstrumentUpdated(Settings.Instrument instrument, Settings.InstrumentUpdateType updateType)
        {
            lock (_mutex)
            {
                Logger.InfoFormat("TradingSetting update instrument, updateType = {0}, instrument = {1}", updateType, instrument);
                if (updateType == Settings.InstrumentUpdateType.Add)
                {
                    if (!_instrumentDict.ContainsKey(instrument.Id))
                    {
                        _instrumentDict.Add(instrument.Id, new TradingInstrument(instrument));
                    }
                }
                else if (updateType == Settings.InstrumentUpdateType.Delete)
                {
                    if (_instrumentDict.ContainsKey(instrument.Id))
                    {
                        _instrumentDict.Remove(instrument.Id);
                    }
                }
            }
        }


        internal void CheckAllPlacingAndPlacedTransactions()
        {
            _accountRepository.CheckAllPlacingAndPlacedTransactions();
        }


        internal void CreateInstrumentsForAccount()
        {
            _accountRepository.CreateInstrumentsForAccount();
        }

        internal void Update(AppType appType, XElement updateNode)
        {
            try
            {
                if (this.InnerUpdate(appType, updateNode))
                {
                    Task.Factory.StartNew(() => this.DoParallelForAccounts(m => m.CheckRisk()));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private bool InnerUpdate(AppType appType, XElement updateNode)
        {
            bool shouldCheckRisk = false;
            foreach (var eachMethod in updateNode.Elements())
            {
                foreach (var eachRow in eachMethod.Elements())
                {
                    shouldCheckRisk |= this.UpdateIndividualNode(appType, eachMethod.Name.ToString(), eachRow);
                }
            }
            return shouldCheckRisk;
        }


        private bool UpdateIndividualNode(AppType appType, string methodName, XElement node)
        {
            bool shouldCheckRisk = false;
            string entityName = node.Name.ToString();
            switch (entityName)
            {
                case "Deposit":
                    if (methodName == "Modify")
                    {
                        if (node.HasAttribute("IsClear"))
                        {
                            bool isClear = node.AttrToBoolean("IsClear");
                            if (isClear)
                            {
                                Guid accountId = node.AttrToGuid("AccountID");
                                var account = this.GetAccount(accountId);
                                Guid depositId = node.AttrToGuid("ID");
                                account.RemoveUnclearDeposit(depositId);
                            }
                        }
                    }
                    break;
                case "AccountBalance":
                    this.ProcessAccountBalance(methodName, node, false);
                    break;
                case "CashSettlement":
                    this.ProcessAccountBalance(methodName, node, true);
                    break;
                case "VolumeNecessary":
                case "VolumeNecessaryDetail":
                    shouldCheckRisk = true;
                    break;
                default:
                    if (!(appType == AppType.QuotationServer || appType == AppType.DealingConsole || appType == AppType.Manager)
                                 && methodName == "Modify")
                    {
                        shouldCheckRisk = true;
                    }
                    break;
            }
            return shouldCheckRisk;

        }



        private void ProcessAccountBalance(string methodName, XElement row, bool isCashSettlement)
        {
            Guid accountID = row.AttrToGuid("AccountID");
            Guid currencyID = row.AttrToGuid("CurrencyID");
            var account = this.GetAccount(accountID);
            if (methodName == "Modify" || methodName == "Add")
            {
                decimal balance = row.AttrToDecimal("Balance");
                bool isDeposit = row.HasAttribute("DepositID");

                if (isDeposit && row.HasAttribute("IsClear"))
                {
                    bool isClear = row.AttrToBoolean("IsClear");
                    if (!isClear)
                    {
                        account.AddUnclearDeposit(new UnclearDeposit(row));
                    }
                }
                Guid? settledOrderId = null;
                if (isCashSettlement)
                {
                    settledOrderId = row.AttrToGuid("OrderID");
                }
                this.ProcessAccountBalance(accountID, currencyID, balance, isDeposit, settledOrderId);
            }
            account.SaveAndBroadcastChanges();
            account.CheckRisk();
        }

        private void ProcessAccountBalance(Guid accountID, Guid currencyID, decimal balance,
            bool isDeposit, Guid? settledOrderId = null)
        {
            var account = this.GetAccount(accountID);
            if (account == null)
            {
                throw new NullReferenceException(string.Format("AccountId = {0}, account not exists", accountID));
            }
            account.AddDeposit(currencyID, balance, isDeposit);
            if (settledOrderId != null)
            {
                Order settleOrder = account.GetOrder(settledOrderId.Value);
                settleOrder.LotBalance = 0m;
            }
        }

    }

    internal sealed class OrderAndTransactionParser
    {
        private const int CAPACITY = 57;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderAndTransactionParser));
        private TradingSetting _tradingSetting;

        internal OrderAndTransactionParser(TradingSetting tradingSetting)
        {
            _tradingSetting = tradingSetting;
        }


        internal void InitializeOrderInstalment(IDataReader dr, Dictionary<Guid, Order> orders)
        {
            try
            {
                var model = this.CreateInstalment(new DBReader(dr));
                _tradingSetting.AddOrderInstalment(model);
                if (orders.ContainsKey(model.OrderId))
                {
                    var physicalOrder = (Physical.PhysicalOrder)orders[model.OrderId];
                    physicalOrder.AddInstalmentDetail(model);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        internal void InitializeTransaction(IDataReader dr, Dictionary<Guid, Transaction> trans)
        {
            try
            {
                Guid accountId = (Guid)dr["AccountID"];
                Account account = _tradingSetting.GetAccount(accountId);
                if (account == null)
                {
                    Logger.WarnFormat("account = {0} not exist but exist transaction", accountId);
                    return;
                }
                var orderType = (OrderType)(int)dr["OrderTypeID"];
                var instrumentCategory = (InstrumentCategory)dr["InstrumentCategory"];
                TransactionPhase phase = (TransactionPhase)(byte)dr["Phase"];
                if (phase == TransactionPhase.Canceled) return;
                var factory = TransactionFacade.CreateAddTranCommandFactory(orderType, instrumentCategory);
                var command = factory.Create(account, new DBReader(dr), OperationType.None);
                command.Execute();
                var tran = command.Result;
                trans.Add(tran.Id, tran);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        internal void InitializeOrder(IDataReader dr, Dictionary<Guid, Transaction> trans, Dictionary<Guid, Order> orders)
        {
            try
            {
                Guid transactionID = (Guid)dr["TransactionID"];
                if (!trans.ContainsKey(transactionID)) return;
                var tran = trans[transactionID];
                OrderPhase phase = (OrderPhase)(byte)dr["Phase"];
                if (phase == OrderPhase.Canceled) return;
                this.ParseOrder(new DBReader(dr), tran, orders);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        internal void InitializeOrderRelation(IDataReader dr, Dictionary<Guid, Order> orders)
        {
            try
            {
                var closeOrderId = (Guid)dr["CloseOrderID"];
                if (!orders.ContainsKey(closeOrderId)) return;
                Order closeOrder = orders[closeOrderId];
                if (closeOrder != null)
                {
                    var factory = OrderRelationFacade.Default.GetAddOrderRelationFactory(closeOrder);
                    var command = factory.Create(closeOrder, new DBReader(dr));
                    command.Execute();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        internal Dictionary<Guid, Transaction> Parse(DataSet ds, out Dictionary<Guid, Order> orderDict)
        {
            var trans = new Dictionary<Guid, Transaction>(CAPACITY);
            SettingInitializer.InitializeWithException(ds, "Transaction", dr =>
                {
                    Guid accountId = (Guid)dr["AccountID"];
                    Account account = _tradingSetting.GetAccount(accountId);
                    if (account == null)
                    {
                        Logger.WarnFormat("account = {0} not exist but exist transaction", accountId);
                        return;
                    }
                    var orderType = (OrderType)(int)dr["OrderTypeID"];
                    var instrumentCategory = (InstrumentCategory)dr["InstrumentCategory"];
                    TransactionPhase phase = (TransactionPhase)(byte)dr["Phase"];
                    if (phase == TransactionPhase.Canceled) return;
                    var factory = TransactionFacade.CreateAddTranCommandFactory(orderType, instrumentCategory);
                    var command = factory.Create(account, new DBRow(dr), OperationType.AsNewRecord);
                    command.Execute();
                    var tran = command.Result;
                    trans.Add(tran.Id, tran);
                });

            var orders = this.ParseOrders(ds, trans);
            orderDict = orders;

            SettingInitializer.InitializeWithException(ds, "OrderRelation", dr =>
                {
                    var closeOrderId = (Guid)dr["CloseOrderID"];
                    if (!orders.ContainsKey(closeOrderId)) return;
                    Order closeOrder = orders[closeOrderId];
                    if (closeOrder != null)
                    {
                        var factory = OrderRelationFacade.Default.GetAddOrderRelationFactory(closeOrder);
                        var command = factory.Create(closeOrder, new DBRow(dr));
                        command.Execute();
                    }
                });
            return trans;
        }

        private Dictionary<Guid, Order> ParseOrders(DataSet ds, Dictionary<Guid, Transaction> trans)
        {
            var orders = new Dictionary<Guid, Order>(CAPACITY);
            SettingInitializer.InitializeWithException(ds, "Order", dr =>
            {
                Guid transactionID = (Guid)dr["TransactionID"];
                if (!trans.ContainsKey(transactionID)) return;
                var tran = trans[transactionID];
                OrderPhase phase = (OrderPhase)(byte)dr["Phase"];
                if (phase == OrderPhase.Canceled) return;
                this.ParseOrder(new DBRow(dr), tran, orders);
            });
            return orders;
        }

        private void ParseOrder(IDBRow dr, Transaction tran, Dictionary<Guid, Order> orders)
        {
            var addOrderCommandFactory = OrderFacade.Default.GetAddOrderCommandFactory(tran);
            var command = addOrderCommandFactory.CreateByDataRow(tran, dr);
            command.Execute();
            var order = command.Result;
            orders.Add(order.Id, order);
            if (tran.OrderType == OrderType.Limit && tran.CanExecute && order.BestPrice != null && order.CalculateLimitMarketHitStatus(false) == OrderHitStatus.Hit)
            {
                tran.Owner.AddPendingConfirmLimitOrder(order);
            }
        }

        private Protocal.Physical.OrderInstalmentData CreateInstalment(IDBRow dr)
        {
            var result = new Protocal.Physical.OrderInstalmentData();
            result.OrderId = dr.GetColumn<Guid>("OrderId");
            result.Sequence = dr.GetColumn<int>("Sequence");
            result.InterestRate = dr.GetColumn<decimal>("InterestRate");
            result.Principal = dr.GetColumn<decimal>("Principal");
            result.Interest = dr.GetColumn<decimal>("Interest");
            result.DebitInterest = dr.GetColumn<decimal>("DebitInterest");
            result.PaymentDateTimeOnPlan = dr.GetColumn<DateTime?>("PaymentDateTimeOnPlan");
            result.PaidDateTime = dr.GetColumn<DateTime?>("PaidDateTime");
            result.UpdatePersonId = dr.GetColumn<Guid?>("UpdatePersonId");
            result.UpdateTime = dr.GetColumn<DateTime?>("UpdateTime");
            result.LotBalance = dr.GetColumn<decimal?>("LotBalance");
            return result;
        }


        internal void InitializeOrderPLNotValued(IDBRow dr, Dictionary<Guid, Order> orderDict)
        {
            Guid orderId = (Guid)dr["OrderID"];
            Order order;
            if (!orderDict.TryGetValue(orderId, out order))
            {
                Logger.WarnFormat("Initialize orderPLNotValued orderId= {0}, can't find related order", orderId);
                return;
            }
            order.NotValuedDayInterestAndStorage.Add((decimal)dr["DayInterestPLNotValued"], (decimal)dr["DayStoragePLNotValued"]);
        }


    }

    internal sealed class ResetParser
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ResetParser));
        private TradingSetting _tradingSetting;

        internal ResetParser(TradingSetting tradingSetting)
        {
            _tradingSetting = tradingSetting;
        }

        internal void Parse(DataSet ds)
        {
            this.ParseInstrumentLastResetDay(ds);
            this.ParseAccountLastResetDay(ds);
        }

        private void ParseInstrumentLastResetDay(DataSet ds)
        {
            foreach (DataRow eachDataRow in ds.Tables["InstrumentResetStatus"].Rows)
            {
                this.InitializeInstrumentResetStatus(new DBRow(eachDataRow));
            }
        }

        internal void InitializeInstrumentResetStatus(IDBRow dr)
        {
            if (dr["LastResetDay"] == DBNull.Value) return;
            Guid accountId = (Guid)dr["AccountID"];
            Guid instrumentId = (Guid)dr["InstrumentID"];
            DateTime lastResetDay = (DateTime)dr["LastResetDay"];
            var account = _tradingSetting.GetAccount(accountId);
            if (account == null)
            {
                Logger.ErrorFormat("ParseInstrumentLastResetDay can't find account = {0}, instrumentId= {1}, lastResetDay = {2}", accountId, instrumentId, lastResetDay);
                return;
            }
            var instrument = account.GetOrCreateInstrument(instrumentId);
            instrument.LastResetDay = lastResetDay;
        }

        internal void InitializeAccountResetStatus(IDBRow dr)
        {
            if (dr["LastResetDay"] != DBNull.Value)
            {
                Guid accountId = (Guid)dr["AccountID"];
                DateTime lastResetDay = (DateTime)dr["LastResetDay"];
                var account = _tradingSetting.GetAccount(accountId);
                if (account == null)
                {
                    Logger.WarnFormat("ParseAccountLastResetDay can't find account = {0}, lastResetDay = {1}", accountId, lastResetDay);
                    return;
                }
                account.LastResetDay = lastResetDay;
            }
        }


        private void ParseAccountLastResetDay(DataSet ds)
        {
            foreach (DataRow eachDataRow in ds.Tables["AccountResetStatus"].Rows)
            {
                this.InitializeInstrumentResetStatus(new DBRow(eachDataRow));
            }
        }

    }
}
