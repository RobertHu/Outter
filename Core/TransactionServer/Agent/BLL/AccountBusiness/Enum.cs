using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal enum AccountState
    {
        None,
        ReadyToReset,
        InReset,
        Initialize
    }
}
