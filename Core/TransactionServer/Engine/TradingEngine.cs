using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Quotations;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using log4net;

namespace Core.TransactionServer.Engine
{
    public abstract class TradingEngine
    {
        public abstract void Place(Transaction tran);

        public abstract void AcceptPlace(Transaction tran);

        public abstract TransactionError Cancel(Transaction tran, CancelReason cancelType);

        public event EventHandler<PlaceEventArgs> Placed;

        public event EventHandler<CancelEventArgs> Canceled;

        public event EventHandler<OrderExecuteEventArgs> OrderExecuted;

        public event EventHandler<InstrumentStatusChangedEventArgs> InstrumentStatusChanged;

        protected void OnExecuted(OrderExecuteEventArgs e)
        {
            this.RaiseEventCommon(this.OrderExecuted, e);
        }

        protected void OnCanceled(CancelEventArgs e)
        {
            this.RaiseEventCommon(this.Canceled, e);
        }

        protected void OnInstrumentStatusChanged(InstrumentStatusChangedEventArgs e)
        {
            this.RaiseEventCommon(this.InstrumentStatusChanged, e);
        }

        protected void OnPlaced(PlaceEventArgs e)
        {
            this.RaiseEventCommon(this.Placed, e);
        }

        protected void RaiseEventCommon(Delegate handler, EventArgs e)
        {
            if (handler != null)
            {
                handler.DynamicInvoke(this, e);
            }
        }
    }

    public struct PlaceResult
    {
        public static readonly PlaceResult Success = new PlaceResult(TransactionError.OK);

        private TransactionError _error;
        private string _errorDetail;

        public PlaceResult(TransactionError error)
            : this(error, string.Empty) { }

        public PlaceResult(TransactionError error, string errorDetail)
        {
            _error = error;
            _errorDetail = errorDetail;
        }

        public TransactionError Error
        {
            get
            {
                return _error;
            }
        }
        public string Detail
        {
            get
            {
                return _errorDetail;
            }
        }
        public bool IsSuccess
        {
            get
            {
                return this.Error == TransactionError.OK;
            }
        }

    }

    public sealed class OrderExecuteEventArgs : EventArgs
    {
        public OrderExecuteEventArgs(ExecuteContext context)
        {
            this.Context = context;
        }

        public ExecuteContext Context { get; private set; }
    }


    public sealed class ExecuteContext
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExecuteContext));
        public static readonly ExecuteContext Empty = new ExecuteContext(Guid.Empty, Guid.Empty, null, false, true, ExecuteStatus.None, null);

        private ExecuteContext(Guid account, Guid tran, ExecuteStatus status)
            : this(account, tran, status, null)
        {
            this.ShouldExecuteDirectly = true;
        }

        public ExecuteContext(Guid account, Guid tran, Guid? executeOrderId, ExecuteStatus status, List<OrderPriceInfo> orderInfos)
            : this(account, tran, executeOrderId, false, true, status, orderInfos) { }

        public ExecuteContext(Guid account, Guid tran, ExecuteStatus status, List<OrderPriceInfo> orderInfos)
            : this(account, tran, null, false, true, status, orderInfos) { }


        public ExecuteContext(Guid account, Guid tran, Guid? executeOrderId, bool isFreeValidation, bool checkMaxPhysicalValue, ExecuteStatus status, List<OrderPriceInfo> orderInfos)
        {
            this.AccountId = account;
            this.TranId = tran;
            this.ShouldExecuteDirectly = false;
            this.IsFreeValidation = isFreeValidation;
            this.CheckMaxPhysicalValue = checkMaxPhysicalValue;
            this.ExecuteOrderId = executeOrderId;
            this.Status = status;
            this.OrderInfos = orderInfos ?? new List<OrderPriceInfo>();
        }

        public static ExecuteContext CreateExecuteDirectly(Guid accountId, Guid tranId, ExecuteStatus status)
        {
            return new ExecuteContext(accountId, tranId, status);
        }


        public Guid AccountId { get; private set; }
        public Guid TranId { get; private set; }
        public bool ShouldExecuteDirectly { get; private set; }
        public bool IsFreeValidation { get; set; }
        public bool CheckMaxPhysicalValue { get; private set; }
        public Guid? ExecuteOrderId { get; private set; }
        public decimal? ExecutingLot { get; private set; }
        public ExecuteStatus Status { get; private set; }
        public BookInfo? BookInfo { get; set; }

        public bool IsBook
        {
            get { return this.BookInfo != null; }
        }

        public bool? CheckMargin
        {
            get { return this.BookInfo != null ? this.BookInfo.Value.CheckMargin : (bool?)null; }
        }


        public bool ShouldUseHistorySettings
        {
            get
            {
                return this.IsBook;
            }
        }

        public DateTime? ExecuteTime
        {
            get
            {
                return this.BookInfo != null ? this.BookInfo.Value.ExecuteTime : (DateTime?)null;
            }
        }

        public DateTime? TradeDay
        {
            get
            {
                return this.BookInfo != null ? this.BookInfo.Value.TradeDay : (DateTime?)null;
            }
        }

        internal Account Account
        {
            get
            {
                return TradingSetting.Default.GetAccount(this.AccountId);
            }
        }
        internal Transaction Tran
        {
            get
            {
                return this.Account.GetTran(this.TranId);
            }
        }

        public List<OrderPriceInfo> OrderInfos { get; private set; }

        public bool IsFreeFee { get; set; }
        public bool ShouldCancelExecute { get; set; }

        public ExecutedInfo ExecutedInfo { get; set; }

        internal ExecuteContext ReplaceTran(Transaction tran, Guid executeOrderId, bool isCheckMaxPhysicalValue)
        {
            var result = new ExecuteContext(tran.Owner.Id, tran.Id, executeOrderId, this.IsFreeValidation, CheckMaxPhysicalValue, this.Status, this.OrderInfos);
            result.IsFreeFee = this.IsFreeFee;
            result.ShouldCancelExecute = this.ShouldCancelExecute;
            return result;
        }

    }


    public struct BookInfo
    {
        private DateTime _executeTime;
        private DateTime _tradeDay;
        private bool _checkMargin;

        public BookInfo(DateTime executeTime, DateTime tradeDay, bool checkMargin)
        {
            _executeTime = executeTime;
            _tradeDay = tradeDay;
            _checkMargin = checkMargin;
        }

        public bool CheckMargin
        {
            get
            {
                return _checkMargin;
            }
        }

        public DateTime ExecuteTime
        {
            get { return _executeTime; }
        }

        public DateTime TradeDay
        {
            get { return _tradeDay; }
        }
    }

    public sealed class ExecutedInfo
    {
        public ExecutedInfo(decimal balance, decimal necessary, decimal equity)
        {
            this.Balance = balance;
            this.Necessary = necessary;
            this.Equity = equity;
        }

        public decimal Balance { get; private set; }
        public decimal Necessary { get; private set; }
        public decimal Equity { get; private set; }
    }



    public sealed class OrderPriceInfo
    {
        public OrderPriceInfo(Guid orderId, Price buyPrice, Price sellPrice)
        {
            this.OrderId = orderId;
            this.BuyPrice = buyPrice;
            this.SellPrice = sellPrice;
        }

        public Guid OrderId { get; private set; }
        public Price BuyPrice { get; private set; }
        public Price SellPrice { get; private set; }

        public override string ToString()
        {
            return string.Format("orderId={0}, buyPrice={1}, sellPrice={2}", this.OrderId, this.BuyPrice, this.SellPrice);
        }
    }


    public enum ExecuteStatus
    {
        None,
        Filled,
        Canceled
    }


    public enum PlaceStatus
    {
        None,
        Accepted,
        Rejected
    }

    public enum CancelStatus
    {
        None,
        Accepted,
        Rejected
    }



    public sealed class PlaceEventArgs : EventArgs
    {
        public PlaceEventArgs(Guid accountId, Guid tranId, PlaceStatus status)
            : this(accountId, tranId, status, TransactionError.OK, string.Empty)
        {

        }

        public PlaceEventArgs(Guid accountId, Guid tranId, PlaceStatus status, TransactionError error, string errorDetail)
        {
            this.AccountId = accountId;
            this.TransactionId = tranId;
            this.Status = status;
            this.Error = error;
            this.ErrorDetail = string.Format("ErrorCode = {0}, detail = {1}", error, errorDetail);
        }

        public Guid AccountId { get; private set; }
        public Guid TransactionId { get; private set; }
        public TransactionError Error { get; private set; }
        public string ErrorDetail { get; private set; }
        public PlaceStatus Status { get; private set; }
    }


    public sealed class CancelEventArgs : EventArgs
    {
        public CancelEventArgs(Transaction tran, CancelStatus status, CancelReason cancelReason)
        {
            this.Account = tran.Owner;
            this.Tran = tran;
            this.Status = status;
            this.CancelReason = cancelReason;
        }

        public Account Account { get; private set; }
        public Transaction Tran { get; private set; }
        public CancelStatus Status { get; private set; }
        public CancelReason CancelReason { get; private set; }
    }

    public sealed class InstrumentStatusChangedEventArgs : EventArgs
    {
        public InstrumentStatusChangedEventArgs(Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>> status)
        {
            this.Status = status;
        }

        public Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>> Status { get; private set; }
    }

}
