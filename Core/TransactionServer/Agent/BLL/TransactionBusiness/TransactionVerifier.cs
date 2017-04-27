using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iExchange.Common;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal static class TransactionVerifier
    {
        internal static void VerifyForPlacing(Transaction tran, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            var placeVerifier = OrderFacade.Default.GetPlaceVerifier(tran);
            foreach (Order order in tran.Orders)
            {
                placeVerifier.Verify(order, isPlaceByRiskMonitor, appType, context);
            }
        }

        internal static void VerifyForExecuting(this Transaction tran, bool isPlaceByRiskMonitor, AppType appType, PlaceContext context)
        {
            var executeVerifier = OrderFacade.Default.GetExecuteVerifier(tran);
            foreach (Order order in tran.Orders)
            {
                executeVerifier.Verify(order, isPlaceByRiskMonitor, appType, context);
            }
        }
    }
}