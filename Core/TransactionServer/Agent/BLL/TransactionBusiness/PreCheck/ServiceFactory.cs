using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck
{
    internal static class ServiceFactory
    {
        internal static Verifier CreateVerifier(Transaction tran)
        {
            if (tran.OrderType == iExchange.Common.OrderType.BinaryOption)
            {
                return new BOPreCheckVerifier((BOTransaction)tran);
            }
            else if (tran.IsPhysical)
            {
                return new PhysicalVerifier((PhysicalTransaction)tran);
            }
            else
            {
                return new Verifier(tran);
            }
        }
    }
}
