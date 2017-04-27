using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using iExchange.Common;
using System.Diagnostics;
using log4net;
using System.Xml.Linq;
using Protocal;

namespace Core.TransactionServer.Agent.DB
{
    public static class ResetAndInitDataDBHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ResetAndInitDataDBHelper));

        public static DataSet GetInitData(string transactionServerID, string connectionString, DateTime? tradeDay = null)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@transactionServerID", transactionServerID},
                {"@tradeDay", tradeDay==null? null : (object)tradeDay.Value}
            };
            return DataBaseHelper.GetData("Trading.GetInitData", connectionString, GetTableNames(), sqlParams);
        }

        internal static SqlDataReader GetInitDataByReader(string transactionServerID, string connectionString, DateTime? tradeDay = null)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@transactionServerID", transactionServerID},
                {"@tradeDay", tradeDay==null? null : (object)tradeDay.Value}
            };
            return DataBaseHelper.GetReader("Trading.GetInitData", connectionString, sqlParams);
        }


        internal static DataSet GetHistorySettings(DateTime tradeDay, string connectionString, string[] tableNames)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetData("Trading.[GetSettingsHistory]", connectionString, tableNames, sqlParams);
        }

        internal static SqlDataReader GetHistorySettingsByReader(DateTime tradeDay, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetReader("Trading.[GetSettingsHistory]", connectionString, sqlParams);
        }

        internal static SqlDataReader GetInstrumentHistorySettings(Guid instrumentId, DateTime tradeDay, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@instrumentId", instrumentId},
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetReader("Trading.[GetInstrumentSettingsHistory]", connectionString, sqlParams);
        }

        internal static DataSet GetCompletedOrderForDelete(Guid orderId, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@deletedOrderId", orderId}
            };
            return DataBaseHelper.GetData("Trading.[GetCompletedOrdersForDelete]", connectionString, new string[] { "Transaction", "Order", "OrderRelation", "Bill", "OrderDayHistory" }, sqlParams);
        }

        internal static DataSet GetDailyClosePrice(Guid instrumentId, Guid accountId, DateTime tradeDay, string connectionString)
        {
            return GetInstrumentSettingAndDailyClosePriceCommon("Trading.[GetDailyClosePrice]", instrumentId, accountId, tradeDay, connectionString);
        }

        internal static DataSet GetRefPriceForUsableMargin(Guid instrumentId, Guid accountId, DateTime tradeDay, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@accountID", accountId},
                {"@instrumentID", instrumentId},
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetData("Trading.[GetPriceForUsableMargin]", connectionString, null, sqlParams);
        }

        private static DataSet GetInstrumentSettingAndDailyClosePriceCommon(string storeProcedureName, Guid instrumentId, Guid accountId, DateTime tradeDay, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@accountId", accountId},
                {"@instrumentId", instrumentId},
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetData(storeProcedureName, connectionString, null, sqlParams);
        }


        internal static DataSet GetOpenOrderIdsExceedOpenDays(XElement instrumentIds, DateTime tradeDay, string connectionString)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>()
            {
                {"@instrumentIds", instrumentIds.ToString()},
                {"@tradeDay", tradeDay}
            };
            return DataBaseHelper.GetData("Trading.[Order_GetOpenOrderIdsExceedOpenDays]", connectionString, null, sqlParams);
        }


        private static string[] GetTableNames()
        {
            return new string[]{
													 "TradeDay",
													   "SystemParameter",
													   "Currency",
                                                       "CurrencyRate",
													   "Instrument",
                                                       "QuotePolicyDetail",
													   "TradePolicy",
													   "TradePolicyDetail",
                                                       "InstalmentPolicy",
                                                       "InstalmentPolicyDetail",
                                                       "SpecialTradePolicy",
					                                   "SpecialTradePolicyDetail",
                                                       "VolumeNecessary",
                                                       "VolumeNecessaryDetail",
                                                       "PhysicalPaymentDiscount",
                                                       "PhysicalPaymentDiscountDetail",
                                                       "DealingPolicy",
                                                       "DealingPolicyDetail",
													   "Customer",
													   "Account",
                                                       "UnclearDeposit",
                                                       "DayQuotation",
													   "OverridedQuotation",
													   "AccountEx",
													   "AccountBalance",
                                                       "SettlementPrice",
                                                       "DeliveryRequest",
                                                       "DeliveryRequestOrderRelation",
                                                       "OrderInstalment",
                                                       "Transaction",
                                                       "Order",
                                                       "OrderRelation",
                                                       "Bill",
                                                       "Organization",
                                                       "OrderType",
                                                       "InstrumentResetStatus",
                                                       "AccountResetStatus",
                                                       "InterestPolicy",
                                                       "Blotter",
                                                       "PriceAlert",
                                                       "OrderPLNotValued",
                                                       "OrderDeletedReason"
												   };
        }

    }


}
