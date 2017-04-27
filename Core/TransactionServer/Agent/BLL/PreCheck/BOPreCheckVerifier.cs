using Core.TransactionServer.Agent.BinaryOption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.PreCheck
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
