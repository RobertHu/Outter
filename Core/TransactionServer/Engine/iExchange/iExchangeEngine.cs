using Core.TransactionServer;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.Concurrent;
using log4net;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Core.TransactionServer.Engine.iExchange.BLL;
using Core.TransactionServer.Agent;
using System.Diagnostics;
using Core.TransactionServer.Engine.iExchange.BLL.OrderBLL;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Protocal;

namespace Core.TransactionServer.Engine.iExchange
{
    internal sealed class iExchangeEngine : TradingEngine
    {
        public static readonly iExchangeEngine Default = new iExchangeEngine();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(iExchangeEngine));
        private PlaceService _placeService;
        private ConcurrentDictionary<Guid, ActionBlock<OrderExecuteEventArgs>> _executeOrderDict;

        private ConcurrentDictionary<Guid, ActionBlock<Transaction>> _placeTranPerAccountDict;

        private ActionBlock<Tuple<Transaction, CancelReason>> _cancelBlock;

        private ActionBlock<Transaction> _acceptPlaceBlock;

        private ActionBlock<Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>>> _instrumentTradingStatusBlock;

        static iExchangeEngine() { }

        private iExchangeEngine()
        {
            _placeService = new PlaceService();
            _executeOrderDict = new ConcurrentDictionary<Guid, ActionBlock<OrderExecuteEventArgs>>();
            _placeTranPerAccountDict = new ConcurrentDictionary<Guid, ActionBlock<Transaction>>();
            _instrumentTradingStatusBlock = new ActionBlock<Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>>>(status => this.OnInstrumentStatusChanged(new InstrumentStatusChangedEventArgs(status)));
            _acceptPlaceBlock = new ActionBlock<Transaction>(tran => _placeService.AcceptPlace(tran));
            this.InitializeCancelBlock();
        }

        private void InitializeCancelBlock()
        {
            _cancelBlock = new ActionBlock<Tuple<Transaction, CancelReason>>(item =>
                {
                    var tran = item.Item1;
                    var cancelReason = item.Item2;
                    Logger.InfoFormat("in cancel block, tran={0}", tran);
                    this.OnCanceled(new CancelEventArgs(tran, CancelStatus.Accepted, cancelReason));
                });
        }


        public override void Place(Transaction tran)
        {
            ActionBlock<Transaction> block;
            if (!_placeTranPerAccountDict.TryGetValue(tran.Owner.Id, out block))
            {
                block = new ActionBlock<Transaction>(m => _placeService.Place(m));
                _placeTranPerAccountDict.TryAdd(tran.Owner.Id, block);
            }
            block.Post(tran);
        }

        public override TransactionError Cancel(Transaction tran, CancelReason reason)
        {
            TransactionError error = TransactionError.OK;
            if (!tran.CancelService.CanCancel)
            {
                return TransactionError.TransactionCannotBeCanceled;
            }
            Logger.InfoFormat("begin in engine cancel tran={0}", tran);
            _cancelBlock.Post(Tuple.Create(tran, reason));
            return error;
        }

        public void Execute(OrderExecuteEventArgs e)
        {
            ActionBlock<OrderExecuteEventArgs> block = this.GetBlock(e.Context.AccountId);
            block.Post(e);
        }

        private ActionBlock<OrderExecuteEventArgs> GetBlock(Guid accountId)
        {
            ActionBlock<OrderExecuteEventArgs> result;
            if (!_executeOrderDict.TryGetValue(accountId, out result))
            {
                result = new ActionBlock<OrderExecuteEventArgs>(request =>
                {
                    try
                    {
                        this.OnExecuted(request);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                });
                _executeOrderDict.TryAdd(accountId, result);
            }
            return result;
        }


        internal void AcceptPlace(Guid accountId, Guid tranId)
        {
            this.OnPlaced(new PlaceEventArgs(accountId, tranId, PlaceStatus.Accepted));
        }

        internal void RejectPlace(Guid accountId, Guid tranId, TransactionError error, string errorDetail)
        {
            this.OnPlaced(new PlaceEventArgs(accountId, tranId, PlaceStatus.Rejected, error, errorDetail));
        }

        internal void UpdateInstrumentStatus(Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>> status)
        {
            _instrumentTradingStatusBlock.Post(status);
        }

        public override void AcceptPlace(Transaction tran)
        {
            _acceptPlaceBlock.Post(tran);
        }
    }
}
