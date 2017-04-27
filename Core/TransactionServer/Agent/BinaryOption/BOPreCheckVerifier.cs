using Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOPreCheckVerifier : Verifier
    {
        internal BOPreCheckVerifier(BOTransaction tran)
            : base(tran) { }
        internal override bool IsFreeOfMarginCheck()
        {
            if (_tran.FirstOrder.IsOpen) return false;
            return base.IsFreeOfMarginCheck();
        }

    }
}
