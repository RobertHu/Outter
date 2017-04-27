using Core.TransactionServer.Agent.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL
{
    internal sealed class MissedTradePolicyDetailManager
    {
        internal static TradePolicyDetail Get(AccountClass.Instrument instrument)
        {
            var result = instrument.TradePolicyDetail();
            if (result != null) return result;
            var trans = instrument.GetTransactions();
            if (trans.Count == 0)
            {
                throw new NullReferenceException(string.Format("tradePolicyDetail not found, because instrument's tranCount = 0, instrumentId = {0}, accountId = {1}", instrument.Id, instrument.Owner.Id));
            }
            var order = trans[0].FirstOrder;
            return GetCommon(order.Id, instrument.Owner);
        }

        internal static TradePolicyDetail Get(Order order)
        {
            var result = order.Owner.TradePolicyDetail();
            if (result != null) return result;
            return GetCommon(order.Id, order.Account);
        }

        private static TradePolicyDetail GetCommon(Guid orderId, Account account)
        {
            DataRow dr = DB.DBRepository.Default.LoadMissedTradePolicyDetail(orderId);
            if (dr == null)
            {
                throw new NullReferenceException(string.Format("can not load missed tradePolicyDetail from db , orderId = {0}, accountId = {1}", orderId, account.Id));
            }
            return new TradePolicyDetail(new Protocal.CommonSetting.DBRow(dr), account.Setting().TradePolicy());
        }

    }
}
