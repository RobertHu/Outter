using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.Util
{
    public static class PriceHelper
    {
        public static Tuple<Price, Price> ParseBuyAndSellPrice(DataRow dr, Guid instrumentId, DateTime? tradeDay,Settings.Setting setting)
        {
            var instrument = setting.GetInstrument(instrumentId,tradeDay);
            Price buyPrice = dr.GetColumn<string>("BuyPrice").CreatePrice(instrument);
            Price sellPrice = dr.GetColumn<string>("SellPrice").CreatePrice(instrument);
            return Tuple.Create(buyPrice, sellPrice);
        }


        public static Price CreatePrice(this string price, Guid instrumentId, DateTime? tradeDay)
        {
            if (string.IsNullOrEmpty(price)) return null;
            var instrument = Settings.Setting.Default.GetInstrument(instrumentId, tradeDay);
            return price.CreatePrice(instrument);
        }

        public static Price CreatePrice(this string price, Settings.Instrument instrument)
        {
            return Price.CreateInstance(price, instrument.NumeratorUnit, instrument.Denominator);
        }

    }
}
