using Core.TransactionServer;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Interact
{
    public interface IExecuteService
    {
        bool Execute(OrderExecuteEventArgs e);
    }

    public sealed class ExecuteService : IExecuteService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExecuteService));
        private ExecuteStrategy _executeStrategy;
        internal ExecuteService(ExecuteStrategy executeStrategy)
        {
            _executeStrategy = executeStrategy;
        }

        public bool Execute(OrderExecuteEventArgs e)
        {
            var error = this.InnerExecute(e);
            Trace.WriteLine(string.Format("Execute result , error code = {0}", error));
            return error == TransactionError.OK ? true : false;
        }

        private TransactionError InnerExecute(OrderExecuteEventArgs e)
        {
            TransactionError error = TransactionError.OK;
            try
            {
                var account = TradingSetting.Default.GetAccount(e.AccountId);
                var tran = account.GetTran(e.TranId);
                if (e.TranId == null) return TransactionError.TransactionNotExists;
                if (e.ExecutingLot != null)
                {
                    this.PartialExecute(e.ExecutingLot.Value, tran);
                }

                if (!tran.CanExecute)
                {
                    if (tran.IsExecuted) return TransactionError.OK;
                    else return TransactionError.TransactionCannotBeExecuted;
                }
                _executeStrategy.Execute(e);
                //if (!SettingFacade.Default.SettingManager.SystemParameter.NeedsFillCheck(e.Transaction.OrderType))
                //{
                //    Trace.WriteLine("ready to book");
                //    error = this.Book(e);
                //}
                //else
                //{
                //    Trace.WriteLine("executeStrategy.Execute");
                //    _executeStrategy.Execute(e);
                //}
            }
            catch (TransactionException tranEx)
            {
                error = tranEx.ErrorCode;
                Logger.Warn(tranEx.Message);
                Trace.WriteLine(tranEx.Message);
            }
            catch (Exception ex)
            {
                error = TransactionError.RuntimeError;
                Logger.Error(ex);
                Trace.WriteLine(ex.ToString());
            }
            return error;
        }

        private bool IsTranAlreadyExecuted(Transaction tran)
        {
            if (!tran.CanExecute)
            {
                if (tran.IsExecuted)
                {
                    return true;
                }
                throw new TransactionException(TransactionError.TransactionCannotBeExecuted);
            }
            return false;
        }

        private void PartialExecute(decimal lot, Transaction tran)
        {
            XmlNode toExecuteTranNode;
            tran.PartialExecute(lot, true, out toExecuteTranNode);
        }
    }

    public sealed class BookService : IExecuteService
    {
        private ExecuteStrategy _executeStrategy;

        internal BookService(ExecuteStrategy executeStrategy)
        {
            _executeStrategy = executeStrategy;
        }

        public bool Execute(OrderExecuteEventArgs e)
        {
            _executeStrategy.Execute(e);
            return true;
        }
    }

}

