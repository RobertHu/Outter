using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Settings
{
    internal sealed class CurrencyRateParseException : Exception
    {
        internal CurrencyRateParseException(Guid currencyId, string msg)
            : base(msg)
        {
            this.CurrencyId = currencyId;
        }
        public Guid CurrencyId { get; private set; }
    }



    internal static class SettingInitializer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingInitializer));

        internal static TradeDay InitializeTradeDay(IDataReader dr)
        {
            return new TradeDay(new DBReader(dr));
        }

        internal static SystemParameter InitializeSystemParameter(IDataReader dr)
        {
            return new SystemParameter(new DBReader(dr));
        }


        internal static SystemParameter InitializeSystemParameter(DataSet ds)
        {
            SystemParameter parameter = null;
            if (ds.Tables.Contains("SystemParameter"))
            {
                var dataRows = ds.Tables["SystemParameter"].Rows;
                parameter = new SystemParameter(new DBRow(dataRows[0]));
            }
            return parameter;
        }




        internal static IEnumerable<Currency> InitializeCurrencies(DataSet ds)
        {
            return Initialize(ds, "Currency", dr => new Currency(new DBRow(dr)));
        }

        internal static IEnumerable<CurrencyRate> InitializeCurrencyRates(DataSet ds, Dictionary<Guid, Currency> currencyDict)
        {
            var result = Initialize(ds, "CurrencyRate", dr =>
            {
                try
                {
                    return ParseCurrencyRate(new DBRow(dr), currencyDict);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return null;
                }
            });
            return result;
        }

        internal static CurrencyRate InitializeCurrencyRate(IDataReader dr, Dictionary<Guid, Currency> currencyDict)
        {
            return ParseCurrencyRate(new DBReader(dr), currencyDict);
        }

        private static CurrencyRate ParseCurrencyRate(IDBRow dr, Dictionary<Guid, Currency> currencyDict)
        {
            Guid sourceCurrencyId = (Guid)dr["SourceCurrencyID"];
            if (!currencyDict.ContainsKey(sourceCurrencyId)) throw new CurrencyRateParseException(sourceCurrencyId, string.Format("source currency = {0} not found", sourceCurrencyId));
            Currency sourceCurrency = currencyDict[sourceCurrencyId];
            Guid targetCurrencyId = (Guid)dr["TargetCurrencyID"];
            if (!currencyDict.ContainsKey(targetCurrencyId)) throw new CurrencyRateParseException(targetCurrencyId, string.Format("target currency = {0} not found", targetCurrencyId));
            Currency targetCurrency = currencyDict[targetCurrencyId];
            return new CurrencyRate(dr, sourceCurrency, targetCurrency);
        }



        internal static IEnumerable<Instrument> InitializeInstruments(DataSet ds)
        {
            var result = Initialize(ds, "Instrument", dr =>
            {
                Instrument instrument = new Instrument(new DBRow(dr));
                Logger.InfoFormat("initialize instrument {0}", instrument);
                return instrument;
            });
            return result;
        }

        internal static IEnumerable<T> Initialize<T>(DataSet ds, string tableName, Func<DataRow, T> factory)
        {
            if (ds.Tables.Contains(tableName))
            {
                var dataRows = ds.Tables[tableName].Rows;
                foreach (DataRow dr in dataRows)
                {
                    yield return factory(dr);
                }
            }
        }

        internal static void Initialize(DataSet ds, string tableName, Action<DataRow> action)
        {
            if (ds.Tables.Contains(tableName))
            {
                var dataRows = ds.Tables[tableName].Rows;
                foreach (DataRow dr in dataRows)
                {
                    try
                    {
                        action(dr);
                    }
                    catch (InitializeSettingException isx)
                    {
                        Logger.Error(isx);
                    }
                    catch (InitializeEntityFromDBException initializeEntityFromDBException)
                    {
                        Logger.ErrorFormat("InitializeEntityFromDBException entityName={0}, columnName={1}", initializeEntityFromDBException.EntityName, initializeEntityFromDBException.ColumnName);
                    }
                    catch (OpenOrderNotFoundException openOrderNotFoundException)
                    {
                        Logger.ErrorFormat("openOrderNotFoundException, openOrderId={0}, closeOrderId={1}", openOrderNotFoundException.OpenOrderId, openOrderNotFoundException.CloseOrderId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }
        }

        internal static void InitializeSettlementPrices(DataSet ds, Dictionary<Guid, Instrument> instrumentDict)
        {
            Initialize(ds, "SettlementPrice", dr =>
            {
                InitializeSettlementPriceCommon(new DBRow(dr), instrumentDict);
            });
        }

        internal static void InitializeSettlementPrice(IDataReader dr, Dictionary<Guid, Instrument> instrumentDict)
        {
            InitializeSettlementPriceCommon(new DBReader(dr), instrumentDict);
        }

        internal static void InitializeSettlementPriceCommon(IDBRow dr, Dictionary<Guid, Instrument> instrumentDict)
        {
            Guid instrumentId = (Guid)dr["InstrumentId"];
            Instrument instrument = instrumentDict[instrumentId];
            string depositPrice = (string)dr["DepositPrice"];
            string deliveryPrice = (string)dr["DeliveryPrice"];
            instrument.UpdateSettlementPrice(depositPrice, deliveryPrice);
        }



        internal static void InitializeQuotePolicyDetails(DataSet ds, Dictionary<QuotePolicyInstrumentIdPair, QuotePolicyDetail> dict)
        {
            Initialize(ds, "QuotePolicyDetail", dr =>
            {
                QuotePolicyDetail quotePolicyDetail = new QuotePolicyDetail(new DBRow(dr));
                dict.Add(new QuotePolicyInstrumentIdPair(quotePolicyDetail.QuotePolicyId, quotePolicyDetail.InstrumentId), quotePolicyDetail);
            });
        }

        internal static void InitializeDayQuotation(DataSet ds, Dictionary<Guid, Instrument> dict)
        {
            Initialize(ds, "DayQuotation", dr =>
            {
                Instrument instrument = dict[(Guid)dr["InstrumentID"]];
                instrument.DayQuotation = new DayQuotation(new DBRow(dr), instrument);
            });

        }

        internal static void InitializeTradePolicy(DataSet ds, Dictionary<Guid, TradePolicy> dict)
        {
            Initialize(ds, "TradePolicy", dr =>
            {
                TradePolicy tradePolicy = new TradePolicy(new DBRow(dr));
                dict.Add(tradePolicy.ID, tradePolicy);
            });
        }

        internal static TradeDay InitializeTradeDay(DataSet ds)
        {
            var dataRows = ds.Tables["TradeDay"].Rows;
            return new TradeDay(new DBRow(dataRows[0]));
        }

    }
}
