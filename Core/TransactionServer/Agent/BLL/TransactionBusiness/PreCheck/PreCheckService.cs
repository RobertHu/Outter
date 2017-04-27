using Core.TransactionServer.Agent.Framework;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck
{
    internal class PreCheckService
    {
        private Transaction _tran;
        private Verifier _verifier;

        internal PreCheckService(Transaction tran)
        {
            _tran = tran;
            _verifier = ServiceFactory.CreateVerifier(tran);
        }

        internal bool IsFreeOfPlaceMarginCheck()
        {
            return _verifier.IsFreeOfMarginCheck();
        }

    }
}
