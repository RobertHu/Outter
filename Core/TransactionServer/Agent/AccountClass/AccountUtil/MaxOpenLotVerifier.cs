using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Xml;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine.iExchange.Common;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.AccountClass.AccountUtil
{
    internal static class MaxOpenLotVerifier
    {
        private static ILog _Logger = LogManager.GetLogger(typeof(MaxOpenLotVerifier));

        internal static bool IsExceedMaxOpenLot(Transaction tran, PlaceContext context)
        {
            if (!tran.ShouldCheckIsExceedMaxOpenLot) return false;

            TradePolicyDetail tradePolicyDetail = tran.TradePolicyDetail(context.TradeDay);
            var settingAccount = tran.Owner.Setting(context.TradeDay);
            if (settingAccount.MaxOpenLot == null && tradePolicyDetail.AccountMaxOpenLot == null) return false;
            if (!tran.ExistsOpenOrder()) return false;
            bool isBuy = tran.FirstOrder.IsBuy;
            var placingLotsPerInstrument = CalculateBuySellInfoPerInstrument(tran.Owner.Transactions);
            decimal totalPlacingOpenLot = 0, totalPlacingOpenLotOfTheInstrument = 0;
            foreach (Guid instrumentId in placingLotsPerInstrument.Keys)
            {
                BuySellLot info = placingLotsPerInstrument[instrumentId];
                if (instrumentId == tran.InstrumentId)
                {
                    totalPlacingOpenLotOfTheInstrument = isBuy ? info.BuyLot : info.SellLot;
                    totalPlacingOpenLot += (isBuy ? info.BuyLot : info.SellLot);
                }
                else
                {
                    totalPlacingOpenLot += Math.Max(info.BuyLot, info.SellLot);
                }
            }
            decimal? accountMaxOpenLot = settingAccount.MaxOpenLot;
            if ((accountMaxOpenLot != null && totalPlacingOpenLot > accountMaxOpenLot)
                || (tradePolicyDetail.AccountMaxOpenLot != null && totalPlacingOpenLotOfTheInstrument > tradePolicyDetail.AccountMaxOpenLot))
            {

                string info = string.Format("Placing {0}, totalPlacingOpenLot={1}, totalPlacingOpenLotOfTheInstrument={2}, account.MaxOpenLot={3}, tradePolicyDetail.AccountMaxOpenLot={4}{5}{6}",
                    TransactionError.ExceedMaxOpenLot, totalPlacingOpenLot, totalPlacingOpenLotOfTheInstrument, accountMaxOpenLot, tradePolicyDetail.AccountMaxOpenLot, Environment.NewLine, string.Empty);
                _Logger.Warn(info);
                return true;
            }
            return false;
        }

        private static Dictionary<Guid, BuySellLot> CalculateBuySellInfoPerInstrument(IEnumerable<Transaction> trans)
        {
            Dictionary<Guid, BuySellLot> lotsPerInstrumentDict = new Dictionary<Guid, BuySellLot>(17);
            foreach (Transaction tran in trans)
            {
                bool isHandledOCOTran = false;
                foreach (Order order in tran.Orders)
                {
                    if (order.Phase == OrderPhase.Canceled || order.Phase == OrderPhase.Deleted
                        || !order.IsOpen || order.LotBalance <= 0 || isHandledOCOTran)
                    {
                        continue;
                    }
                    isHandledOCOTran = tran.Type == TransactionType.OneCancelOther;
                    decimal buyLot = order.IsBuy ? order.LotBalance : 0;
                    decimal sellLot = !order.IsBuy ? order.LotBalance : 0;
                    BuySellLot oldLots;
                    if (!lotsPerInstrumentDict.TryGetValue(tran.InstrumentId, out oldLots))
                    {
                        lotsPerInstrumentDict.Add(tran.InstrumentId, new BuySellLot(buyLot, sellLot));
                    }
                    else
                    {
                        lotsPerInstrumentDict[tran.InstrumentId] = oldLots + new BuySellLot(buyLot, sellLot);
                    }
                }
            }
            return lotsPerInstrumentDict;
        }
    }
}
