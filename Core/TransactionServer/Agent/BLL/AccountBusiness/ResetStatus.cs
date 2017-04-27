using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal enum AccountStatus
    {
        None,
        ReadyForReset,
        InReset
    }

    internal sealed class ResetStatus
    {
        private Dictionary<Guid, DateTime> _instrumentLastResetDayDict = new Dictionary<Guid, DateTime>();

        internal AccountStatus AccountStatus { get; private set; }

        internal Dictionary<Guid, DateTime> InstrumentLastResetDayDict
        {
            get { return _instrumentLastResetDayDict; }
        }

        internal void UpdateStatus(AccountStatus status)
        {
            this.AccountStatus = status;
        }
    }


    internal sealed class AccountPLCalculator
    {

        internal void CalculateValuedPL(DateTime tradeDay)
        {
        }

        internal void CalculateNotValuedPL(DateTime tradeDay)
        {

        }
    }


}
