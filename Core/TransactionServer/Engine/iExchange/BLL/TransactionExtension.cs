using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer;
using iExchange.Common;
using log4net;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Settings;
using Protocal;

namespace Core.TransactionServer.Engine.iExchange.BLL
{
    internal static class TransactionExtension
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionExtension));
        public static void GetTryExecutePrice(this Transaction tran, out Price buy, out Price sell)
        {
            buy = sell = null;
            if (tran.OrderType == OrderType.SpotTrade)
            {
                foreach (Order order in tran.Orders)
                {
                    if (order.IsBuy)
                    {
                        buy = order.SetPrice;
                    }
                    else
                    {
                        sell = order.SetPrice;
                    }
                }
            }
            else if (tran.OrderType == OrderType.Market)
            {
                var account = tran.Owner;
                Instrument instrument = tran.SettingInstrument();
                var quotation = tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
                if (quotation == null)
                {
                    throw new TransactionServerException(TransactionError.HasNoQuotationExists);
                }
                buy = quotation.BuyPrice;
                sell = quotation.SellPrice;
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("not supported orderType={0}", tran.OrderType.ToString()));
            }
        }
    }
}
