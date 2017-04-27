using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Engine.iExchange.Common;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange.Util
{
    //public static class BroadcastHelper
    //{
    //    private static readonly ILog Logger = LogManager.GetLogger(typeof(BroadcastHelper));

    //    public static void NotifyCancel(IBroadcaster broadcaster, CancelReason reason, params Transaction[] trans)
    //    {
    //        if (trans.Length == 0) return;
    //        Command[] cancelCommands = new Command[trans.Length];
    //        int index = 0;
    //        foreach (Transaction tran in trans)
    //        {
    //            CancelCommand cancelCommand = new CancelCommand((int)CommandSequenceManager.Default.Get().Value);
    //            cancelCommand.InstrumentID = tran.InstrumentId;
    //            cancelCommand.AccountID = tran.Owner.Setting.Id;
    //            cancelCommand.TransactionID = tran.Id;
    //            cancelCommand.CancelReason = reason;
    //            cancelCommand.ErrorCode = TransactionError.OK;
    //            cancelCommands[index++] = cancelCommand;
    //        }
    //        Logger.Info("NotifyCancel1");
    //       // broadcaster.Add(new CommandMessage(cancelCommands));
    //    }
    //}
}
