using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class AccountExecutor
    {
        internal static void Execute(ExecuteContext context)
        {
            if (context.Tran.IsPhysical)
            {
                VerifyIsExceedMaxPhysicalValue(context.Tran, context.CheckMaxPhysicalValue);
            }
            context.Tran.Execute(context);
        }

        private static void VerifyIsExceedMaxPhysicalValue(Transaction tran, bool checkMaxPhysicalValue)
        {
            string errorDetail = string.Empty;
            if (checkMaxPhysicalValue && PhysicalValueChecker.IsExceedMaxPhysicalValue((PhysicalTransaction)tran, out errorDetail))
            {
                throw new TransactionServerException(TransactionError.ExceedMaxPhysicalValue, errorDetail);
            }
        }

    }

}
