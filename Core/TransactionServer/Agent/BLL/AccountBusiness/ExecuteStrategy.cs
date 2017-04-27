using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal abstract class AccountExecuteStrategy
    {
        internal void Execute(Transaction tran, Guid? executeOrderId, Price buyPrice, Price sellPrice, bool isFreeValidation, bool checkMaxPhysicalValue)
        {
            this.PreExecuteCheck(tran, checkMaxPhysicalValue);
            this.ExecuteTran(tran, executeOrderId, buyPrice, sellPrice, isFreeValidation);
        }


        internal virtual void PreExecuteCheck(Transaction tran, bool checkMaxPhysicalValue)
        {
        }

        internal abstract void ExecuteTran(Transaction tran, Guid? executeOrderId, Price buyPrice, Price sellPrice, bool isFreeValidation);
    }

    internal sealed class GeneralAccountExecuteStrategy : AccountExecuteStrategy
    {
        internal override void ExecuteTran(Transaction tran, Guid? executeOrderId, Price buyPrice, Price sellPrice, bool isFreeValidation)
        {
            Trace.WriteLine("GeneralExecuteStrategy , ExecuteTran");
            tran.Execute(executeOrderId, buyPrice, sellPrice, isFreeValidation);
        }
    }

    internal sealed class BookAccountExecuteStrategy : AccountExecuteStrategy
    {
        internal override void ExecuteTran(Transaction tran, Guid? executeOrderId, Price buyPrice, Price sellPrice, bool isFreeValidation)
        {
            tran.Book(executeOrderId, buyPrice, sellPrice);
        }
    }
}
