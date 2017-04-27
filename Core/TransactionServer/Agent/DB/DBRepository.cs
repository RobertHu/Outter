using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util.Code;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Data.SqlClient;
using Dapper;
using Core.TransactionServer.Agent.Reset;
using Protocal.TypeExtensions;
using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.DB.DBMapping;

namespace Core.TransactionServer.Agent.DB
{
    public sealed class DBRepository
    {
        internal static readonly DBRepository Default = new DBRepository();

        static DBRepository() { }
        private DBRepository() { }

        internal DataSet GetInitData(string transactionServerName)
        {
            return ResetAndInitDataDBHelper.GetInitData(transactionServerName, this.ConnectionString);
        }

        internal SqlDataReader GetInitDataByReader(string transactionServerName)
        {
            return ResetAndInitDataDBHelper.GetInitDataByReader(transactionServerName, this.ConnectionString);
        }

        internal string ConnectionString
        {
            get
            {
                return ExternalSettings.Default.DBConnectionString;
            }
        }

        internal DataSet GetHistorySettings(DateTime tradeDay)
        {
            string[] tableNames = {
                                      "Currency",
                                      "CurrencyRate",
                                      "QuotePolicyDetail",
                                      "TradePolicy",
                                      "TradePolicyDetail",
                                      "DealingPolicy",
                                      "DealingPolicyDetail",
                                      "VolumeNecessary",
                                      "VolumeNecessaryDetail",
                                      "InstalmentPolicy",
                                      "InstalmentPolicyDetail",
                                      "Customer",
                                      "Account",
                                      "Instrument",
                                      "TradeDay",
                                      "PhysicalPaymentDiscount",
                                      "PhysicalPaymentDiscountDetail"
                                 };
            return ResetAndInitDataDBHelper.GetHistorySettings(tradeDay, this.ConnectionString, tableNames);
        }

        internal SqlDataReader GetHistorySettingsByReader(DateTime tradeDay)
        {
            return ResetAndInitDataDBHelper.GetHistorySettingsByReader(tradeDay, this.ConnectionString);
        }

        internal SqlDataReader GetInstrumentTradeDaySettingData(Guid instrumentId, DateTime tradeDay)
        {
            return ResetAndInitDataDBHelper.GetInstrumentHistorySettings(instrumentId, tradeDay, this.ConnectionString);
        }

        internal DataSet GetCompletedOrderForDelete(Guid orderId)
        {
            return ResetAndInitDataDBHelper.GetCompletedOrderForDelete(orderId, this.ConnectionString);
        }


        internal DataRow GetInstrumentDailyClosePrice(Guid instrumentId, Guid accountId, DateTime tradeDay)
        {
            var data = ResetAndInitDataDBHelper.GetDailyClosePrice(instrumentId, accountId, tradeDay, this.ConnectionString);
            return data.Tables[0].Rows[0];
        }

        internal DataRow GetRefPriceForUsableMargin(Guid instrumentId, Guid accountId, DateTime tradeDay)
        {
            var data = ResetAndInitDataDBHelper.GetRefPriceForUsableMargin(instrumentId, accountId, tradeDay, this.ConnectionString);
            return data.Tables[0].Rows[0];
        }

        internal List<DB.DBMapping.InstrumentDayClosePrice> GenerateDailyClosePrice(DateTime tradeDay)
        {
            using (SqlConnection conn = new SqlConnection(this.ConnectionString))
            {
                conn.Open();
                conn.Query<DB.DBMapping.InstrumentDayClosePrice>("Trading.[UpdateDailyQuotation]", new { tradeDay = tradeDay }, commandType: CommandType.StoredProcedure, commandTimeout: (int)((new TimeSpan(0, 10, 0)).Milliseconds));
                return conn.Query<DB.DBMapping.InstrumentDayClosePrice>("Trading.[GetLastDailyQuotation]", new { tradeDay = tradeDay }, commandType: CommandType.StoredProcedure).ToList();
            }
        }

        internal DataSet GetOpenOrderIdsExceedOpenDays(XElement instrumentIds, DateTime tradeDay)
        {
            return ResetAndInitDataDBHelper.GetOpenOrderIdsExceedOpenDays(instrumentIds, tradeDay, this.ConnectionString);
        }

        internal SqlDataReader GetOrderDayHistorys(XElement orderXml)
        {
            return DataBaseHelper.GetReader("Trading.[GetOrderDayHistory]", this.ConnectionString, (List<DBParameter>)null);
        }

        internal IEnumerable<InstrumentResetResult> GetAccountInstrumentResetHistory(Guid accountId, Guid instrumentId, DateTime tradeDay)
        {
            string sql = "SELECT AccountID, InstrumentID , TradeDay, ResetBalance, FloatingPL,InterestPLNotValued, StoragePLNotValued, TradePLNotValued,Necessary FROM Trading.InstrumentResetResult WHERE AccountID = @accountID AND InstrumentID = @instrumentID AND TradeDay = @tradeDay";
            return DBHelper.LoadDBRecords<InstrumentResetResult>(sql, new { accountID = accountId, instrumentID = instrumentId, tradeDay = tradeDay }, CommandType.Text);
        }

        internal List<OrderQueryData> QueryOrders(string language, Guid customerId, int lastDays, Guid? accountId, Guid? instrumentId, int? queryType)
        {
            List<OrderQueryData> result = new List<OrderQueryData>();
            Dictionary<string, object> sqlParams = new Dictionary<string, object>
            {
                {"@language", language},
                {"@customerId",customerId},
                {"@lastDays", lastDays},
                {"@accountId", accountId},
                {"@instrumentId", instrumentId},
                {"@queryType", queryType}
            };
            var ds = Protocal.DataBaseHelper.GetData("Trading.[OrderQuery]", this.ConnectionString, null, sqlParams);
            if (ds.Tables[0].Rows.Count == 0) return result;
            foreach (DataRow eachDataRow in ds.Tables[0].Rows)
            {
                OrderQueryData model = new OrderQueryData
                {
                    ID = eachDataRow.GetColumn<Guid>("ID"),
                    Code = eachDataRow.GetColumn<string>("Code"),
                    AccountCode = eachDataRow.GetColumn<string>("AccountCode"),
                    BeginTime = eachDataRow.GetColumn<DateTime>("BeginTime"),
                    EndTime = eachDataRow.GetColumn<DateTime>("EndTime"),
                    ExecuteTime = eachDataRow.GetColumn<DateTime?>("ExecuteTime"),
                    InstrumentCode = eachDataRow.GetColumn<string>("InstrumentCode"),
                    OrderType = eachDataRow.GetColumn<int>("OrderTypeID"),
                    Lot = eachDataRow.GetColumn<decimal>("Lot"),
                    Price = eachDataRow.GetColumn<string>("Price"),
                    IsOpen = eachDataRow.GetColumn<bool>("IsOpen"),
                    IsBuy = eachDataRow.GetColumn<bool>("IsBuy"),
                    Phase = eachDataRow.GetColumn<byte>("Phase"),
                    Remarks = eachDataRow.GetColumn<string>("Remarks"),
                    TradeOption = eachDataRow.GetColumn<byte>("TradeOption"),
                    TransactionType = eachDataRow.GetColumn<byte>("TransactionType"),
                    TransactionSubType = eachDataRow.GetColumn<byte>("TransactionSubType"),
                    ExternalExchangeCode = eachDataRow.GetColumn<string>("ExternalExchangeCode"),
                    InstrumentCategory = eachDataRow.GetColumn<int>("InstrumentCategory"),
                    PhysicalRequestId = eachDataRow.GetColumn<Guid?>("PhysicalRequestId"),
                    PhysicalPaidAmount = eachDataRow.GetColumn<decimal>("PhysicalPaidAmount"),
                    PhysicalTradeSide = eachDataRow.GetColumn<int>("PhysicalTradeSide"),
                    PhysicalInstalmentType = eachDataRow.GetColumn<int?>("PhysicalInstalmentType"),
                    PhysicalOriginValue = eachDataRow.GetColumn<decimal>("PhysicalOriginValue"),
                    RecalculateRateType = eachDataRow.GetColumn<int?>("RecalculateRateType"),
                    InterestValueDate = eachDataRow.GetColumn<DateTime?>("InterestValueDate"),
                    TradePL = eachDataRow.GetColumn<decimal?>("TradePL"),
                    Decimals = eachDataRow.GetColumn<int?>("Decimals"),
                    CurrencyID = eachDataRow.GetColumn<Guid>("CurrencyID"),
                    CurrencyCode = eachDataRow.GetColumn<string>("CurrencyCode"),
                    CurrencyName = eachDataRow.GetColumn<string>("CurrencyName")
                };
                result.Add(model);
            }
            return result;
        }

        internal int GetCodeSequence(CodeType codeType, string codePrefix)
        {
            List<DBParameter> dbParameters = new List<DBParameter>
            {
                new DBParameter("@codeType", (int)codeType, ParameterDirection.Input),
                new DBParameter("@prefix", codePrefix, ParameterDirection.Input),
                new DBParameter("@sequenceNo", null, ParameterDirection.Output)
            };
            DataBaseHelper.GetData("Trading.[GetLastCode]", this.ConnectionString, null, dbParameters);
            var item = dbParameters.Single(m => m.Direction == ParameterDirection.Output).OutPutValue;
            return (int)(long)item;
        }

        public DateTime GetTradeDay(DateTime executeTime)
        {
            return DB.DBHelper.LoadDBRecords<DateTime>("SELECT dbo.FV_GetTradeDay(@dateTime) as TradeDay", new { dateTime = executeTime }, CommandType.Text).Single();
        }

        internal bool SaveLeverage(LeverageParameters leverageParams)
        {
            List<DBParameter> dbParameters = new List<DBParameter>
            {
                new DBParameter("@accountId", leverageParams.AccountId),
                new DBParameter("@leverage", leverageParams.Leverage),
                new DBParameter("@rateMarginO", leverageParams.RateMarginO),
                new DBParameter("@rateMarginD", leverageParams.RateMarginD),
                new DBParameter("@rateMarginLockO", leverageParams.RateMarginLockO),
                new DBParameter("@rateMarginLockD", leverageParams.RateMarginLockD)
            };
            return DataBaseHelper.ExecuteNonQuery("P_SaveLeverage", this.ConnectionString, dbParameters);
        }


        internal DataRow GetPhysicalCode(int codeType, DateTime tradeDay)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>
            {
                {"@codeType",codeType},
                {"@tradeDay", tradeDay}
            };
            DataSet ds = DataBaseHelper.GetData("Trading.[GetPhysicalCode]", this.ConnectionString, null, sqlParams);
            return ds.Tables[0].Rows[0];
        }

        internal SqlDataReader LoadInstrumentDayOpenCloseHistory()
        {
            return DataBaseHelper.GetReader("Trading.[GetInstrumentDayOpenCloseHistory]", this.ConnectionString, (Dictionary<string, object>)null);
        }


        internal DB.DBMapping.InstalmentPolicyDetail GetLastInstalmentPolicyDetail(DateTime endTime)
        {
            string sql = "SELECT [InstalmentPolicyId], IsActive, [Period],[MinDownPayment],[MaxDownPayment],[InterestRate],[AdministrationFeeBase], [AdministrationFee],[UpdatePersonId],[UpdateTime],[Frequence],[DebitInterestType],[DebitInterestRatio], [DebitFreeDays],[ContractTerminateType],[ContractTerminateFee],[DownPaymentBasis],[IsActive], [LatePaymentAutoCutDay],[AutoCutPenaltyBase],[AutoCutPenaltyValue],[ClosePenaltyBase],[ClosePenaltyValue] FROM [dbo].[InstalmentPolicyDetail] ipd WITH(NOLOCK) WHERE UpdateTime=(SELECT MAX(UpdateTime) FROM dbo.[InstalmentPolicyDetail] ipd2	 WITH(NOLOCK) WHERE ipd2.InstalmentPolicyId=ipd.InstalmentPolicyId AND ipd2.Period=ipd.Period AND ipd2.Frequence = ipd.Frequence AND ipd2.UpdateTime<=@endTime)";
            return DBHelper.LoadDBRecords<DB.DBMapping.InstalmentPolicyDetail>(sql, new { endTime = endTime }, CommandType.Text).FirstOrDefault();
        }

        internal IEnumerable<DB.DBMapping.Organization> LoadOrganizations()
        {
            return DBHelper.LoadDBRecords<DB.DBMapping.Organization>("SELECT ID, Code FROM dbo.Organization", null, CommandType.Text);
        }


        internal IEnumerable<DB.DBMapping.AccountBalanceDayHistory> GetLastAccountBalanceDayHistory(Guid accountId)
        {
            return DBResetRepository.GetLastAccountBalanceDayHistory(accountId, this.ConnectionString);
        }

        internal Dictionary<DateTime, decimal> GetAccountBalanceDayHistory(Guid accountId, Guid currencyId, DateTime beginTradeDay, DateTime endTradeDay)
        {
            return DBResetRepository.GetAccountBalanceDayHistory(accountId, currencyId, beginTradeDay, endTradeDay, this.ConnectionString);
        }

        internal IEnumerable<DB.DBMapping.AccountVersion> LoadAccountVersions()
        {
            return DBHelper.LoadDBRecords<DB.DBMapping.AccountVersion>("SELECT  AccountID, Version FROM Trading.AccountVersion", null, CommandType.Text);
        }

        internal DataRow LoadMissedTradePolicyDetail(Guid orderId)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>
            {
                {"@orderID",orderId},
            };
            DataSet ds = DataBaseHelper.GetData("dbo.P_GetOrderTradePolicyDetail", this.ConnectionString, null, sqlParams);
            return ds.Tables[0].Rows[0];
        }


    }

    internal static class DBHelper
    {
        internal static IEnumerable<T> LoadDBRecords<T>(string sql, object parameter, CommandType commandType)
        {
            using (SqlConnection conn = new SqlConnection(ExternalSettings.Default.DBConnectionString))
            {
                conn.Open();
                return conn.Query<T>(sql, parameter, commandType: commandType, commandTimeout: (int)((new TimeSpan(0, 5, 0)).Milliseconds));
            }
        }

    }

}
