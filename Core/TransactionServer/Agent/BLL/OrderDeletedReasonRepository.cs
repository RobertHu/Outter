using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iExchange.Common;
using System.Data;
using System.Diagnostics;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.BLL
{
    internal sealed class OrderDeletedReasonRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrderDeletedReasonRepository));

        internal static readonly OrderDeletedReasonRepository Default = new OrderDeletedReasonRepository();

        private Dictionary<CancelReason, DB.DBMapping.OrderDeletedReason> _reasons;

        private OrderDeletedReasonRepository()
        {
            _reasons = new Dictionary<CancelReason, DB.DBMapping.OrderDeletedReason>(50);
        }


        internal void InitializeOrderDeletedReason(IDBRow dr)
        {
            var orderDeletedReason = new DB.DBMapping.OrderDeletedReason(dr);
            _reasons.Add((CancelReason)Enum.Parse(typeof(CancelReason), orderDeletedReason.Code), orderDeletedReason);
        }


        internal DB.DBMapping.OrderDeletedReason GetReasonModel(CancelReason reason)
        {
            DB.DBMapping.OrderDeletedReason result = null;
            _reasons.TryGetValue(reason, out result);
            return result;
        }

    }
}
