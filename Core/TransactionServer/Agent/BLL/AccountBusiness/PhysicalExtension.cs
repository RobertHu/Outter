using Core.TransactionServer.Agent.Reset.Exceptions;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class PhysicalExtension
    {
        internal static List<Physical.PhysicalOrder> GetInstalmentOrders(this Account account)
        {
            var result = new List<Physical.PhysicalOrder>(4);
            foreach (var eachTran in account.Transactions)
            {
                if (!eachTran.IsPhysical) continue;
                foreach (var eachOrder in eachTran.Orders)
                {
                    Physical.PhysicalOrder physicalOrder = eachOrder as Physical.PhysicalOrder;
                    if (physicalOrder == null)
                    {
                        throw new OrderConvertException(account.Id, eachOrder.Instrument().Id, eachOrder.Id, string.Format("tranId={0}, tran.instrumentId={1}", eachTran.Id, eachTran.InstrumentId));
                    }
                    if (physicalOrder.Instalment != null && physicalOrder.Instalment.InstalmentType != InstalmentType.FullAmount && Math.Abs(physicalOrder.PaidAmount) < physicalOrder.PhysicalOriginValue)
                    {
                        result.Add(physicalOrder);
                    }
                }
            }
            return result;
        }
    }
}
