using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Interact
{
    public sealed class InteractFacade
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InteractFacade));
        static InteractFacade() { }
        public static readonly InteractFacade Default = new InteractFacade();
        private TradingEngine _tradingEngine;
        private object _mutex = new object();

        private InteractFacade() { }

        public void Initialize(TradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
            _tradingEngine.Canceled += TradingEngine_Canceled;
            _tradingEngine.OrderExecuted += TradingEngine_Executed;

            _tradingEngine.InstrumentStatusChanged += TradingEngine_InstrumentStatusChanged;

            _tradingEngine.Placed += this.TradingEnginePlaced;
        }


        internal TradingEngine TradingEngine
        {
            get
            {
                return _tradingEngine;
            }
        }

        private void TradingEngine_Executed(object sender, OrderExecuteEventArgs e)
        {
            try
            {
                if (e.Context.Status == ExecuteStatus.Filled)
                {
                    this.ProcessForTranExecuted(e.Context);
                }
                else
                {
                    e.Context.Account.CancelExecute(e.Context.Tran, CancelReason.OtherReason);
                }
                TransactionExpireChecker.Default.Remove(e.Context.TranId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ProcessForTranExecuted(ExecuteContext context)
        {
            if (context.ShouldExecuteDirectly)
            {
                context.Account.ExecuteDirectly(context);
            }
            else
            {
                context.Account.Execute(context);
            }
        }

        private void TradingEngine_Canceled(object sender, CancelEventArgs e)
        {
            try
            {
                Logger.InfoFormat("TradingEngine_Canceled, tran = {0}", e.Tran);
                if (e.Account.GetTran(e.Tran.Id) == null)
                {
                    Logger.ErrorFormat("can't find tran = {0} in account = {1}", e.Tran.Id, e.Account.Id);
                }
                e.Account.OnTransactionCanceled(e.Tran, e.Status, e.CancelReason);
                TransactionExpireChecker.Default.Remove(e.Tran.Id);
            }
            catch (Exception ex)
            {
                e.Account.RejectChanges();
                Logger.Error(ex);
            }
        }

        private void TradingEnginePlaced(object sender, PlaceEventArgs e)
        {
            try
            {
                var account = TradingSetting.Default.GetAccount(e.AccountId);
                account.OnPlaced(e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void TradingEngine_InstrumentStatusChanged(object sender, InstrumentStatusChangedEventArgs e)
        {
            try
            {
                ServerFacade.Default.Server.UpdateInstrumentsTradingStatus(e.Status);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
