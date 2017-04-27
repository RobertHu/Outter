using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Diagnostics;
using System.Threading;

namespace Protocal.Commands
{
    public delegate void OrderChangedHandle(Order order);

    public sealed class TradingCommandManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TradingCommandManager));

        public static readonly TradingCommandManager Default = new TradingCommandManager();

        private Queue<TradingCommand> _commands = new Queue<TradingCommand>(100);
        private object _mutex = new object();
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private Dictionary<OrderChangeType, Action<OrderPhaseChange>> _orderChangeActionDict;

        private TradingCommandManager()
        {
            _orderChangeActionDict = new Dictionary<OrderChangeType, Action<OrderPhaseChange>>()
            {
                {OrderChangeType.Placing, change => this.RaiseEventCommon(this.OrderPlacing, change.Source)},
                {OrderChangeType.Placed, change => this.RaiseEventCommon(this.OrderPlaced, change.Source)},
                {OrderChangeType.Canceled, change => this.RaiseEventCommon(this.OrderCanceled, change.Source)},
                {OrderChangeType.Executed, change => this.RaiseEventCommon(this.OrderExecuted, change.Source)},
            };

            new Thread(this.Process)
            {
                IsBackground = true
            }.Start();
        }

        static TradingCommandManager() { }

        public event OrderChangedHandle OrderPlacing;
        public event OrderChangedHandle OrderPlaced;
        public event OrderChangedHandle OrderCanceled;
        public event OrderChangedHandle OrderExecuted;

        internal void Add(TradingCommand command)
        {
            lock (_mutex)
            {
                _commands.Enqueue(command);
                _resetEvent.Set();
            }
        }

        private void Process()
        {
            while (true)
            {
                _resetEvent.WaitOne();
                while (true)
                {
                    int commandCount;
                    lock (_mutex)
                    {
                        commandCount = _commands.Count;
                    }
                    if (commandCount == 0) break;
                    TradingCommand command;
                    lock (_mutex)
                    {
                        command = _commands.Dequeue();
                    }
                    try
                    {
                        this.DoProccess(command);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }
        }


        internal List<OrderPhaseChange> ProcessTest(string content)
        {
            XElement accountXElement = XElement.Parse(content);
            Guid accountId = accountXElement.AttrToGuid("ID");
            var account = AccountRepository.Default.Get(accountId);
            List<OrderPhaseChange> result = null;
            ChangedFund changedFund;
            if (account != null)
            {
                result = account.Update(accountXElement, out changedFund);
            }
            return result;
        }


        private void RaiseEventCommon(Delegate handler, Order order)
        {
            if (handler != null)
            {
                handler.DynamicInvoke(order);
            }
        }

        public void Clear(Guid userId)
        {
            Customer customer = null;
            if (Customer.TryRemove(userId, out customer))
            {
                customer.Clear();
            }
        }

        public void FillAccountInitData(XElement initData, Guid? userId = null)
        {
            try
            {
                this.InitailizeAccounts(initData, userId);
            }
            catch (Exception ex)
            {
                Logger.Error(initData.ToString(), ex);
            }
        }

        private void InitailizeAccounts(XElement initData, Guid? userId)
        {
            foreach (XElement eachAccountElement in initData.Elements("Account"))
            {
                this.InitializeAccount(eachAccountElement, userId);
            }
        }

        private void InitializeAccount(XElement accountElement, Guid? userId)
        {
            Guid accountId = Guid.Parse(accountElement.Attribute("ID").Value);
            var account = AccountRepository.Default.GetOrAdd(accountId);
            account.Initialize(accountElement);
            if (userId != null)
            {
                Customer customer = Customer.GetOrAdd(userId.Value);
                customer.Add(account);
            }
            TransactionMapping.Default.Initialize(account);
        }

        private void DoProccess(TradingCommand command)
        {
            if (string.IsNullOrEmpty(command.Content)) return;
            XElement accountXElement = XElement.Parse(command.Content);
            Guid accountId = accountXElement.AttrToGuid("ID");
            var account = AccountRepository.Default.Get(accountId);
            ChangedFund changedFund;
            if (account != null)
            {
                List<OrderPhaseChange> orderChangs = account.Update(accountXElement, out changedFund);
                this.NotifyOrderChanged(orderChangs);
            }
        }

        private void NotifyOrderChanged(List<OrderPhaseChange> changes)
        {
            if (changes == null || changes.Count == 0) return;
            foreach (var eachOrderChange in changes)
            {
                Action<OrderPhaseChange> changeAction;
                if (_orderChangeActionDict.TryGetValue(eachOrderChange.ChangeType, out changeAction))
                {
                    changeAction(eachOrderChange);
                }
            }
        }

    }
}
