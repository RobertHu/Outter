using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Core.TransactionServer.Agent.DB
{
    internal static class DBResetRepository
    {
        internal static IEnumerable<DBMapping.AccountBalanceDayHistory> GetLastAccountBalanceDayHistory(Guid accountId, string connectonString)
        {
            SqlConnection conn = new SqlConnection(connectonString);
            string sql = string.Format("select TradeDay, AccountID, CurrencyID, Balance  from dbo.AccountBalanceDayHistory abdh where abdh.AccountID = '{0}' and TradeDay = (select MAX(TradeDay)  from dbo.AccountBalanceDayHistory where AccountID = abdh.AccountID)", accountId);
            return conn.Query<DBMapping.AccountBalanceDayHistory>(sql);
        }

        internal static Dictionary<DateTime, decimal> GetAccountBalanceDayHistory(Guid accountId, Guid currencyId, DateTime beginTradeDay, DateTime endTradeDay, string connectonString)
        {
            SqlConnection conn = new SqlConnection(connectonString);
            string sql = "select TradeDay, AccountID, CurrencyID, Balance  from dbo.AccountBalanceDayHistory WHERE AccountID = @accountID AND CurrencyID = @currencyID AND TradeDay >= @beginTradeDay and TradeDay <= @endTradeDay";
            var data = conn.Query<DBMapping.AccountBalanceDayHistory>(sql, new { accountID = accountId, currencyID = currencyId, beginTradeDay = beginTradeDay, endTradeDay = endTradeDay });

            if (data == null || data.Count() == 0) return new Dictionary<DateTime, decimal>();
            var result = new Dictionary<DateTime, decimal>(data.Count());
            foreach (var eachBalance in data)
            {
                result.Add(eachBalance.TradeDay, eachBalance.Balance);
            }
            return result;
        }

        internal static Guid GetCurrencyId(Guid accountId, Guid instrumentId, DateTime lastTime)
        {
            string sql = @"DECLARE @currencyID UNIQUEIDENTIFIER
	DECLARE @isMultiCurrency BIT
	
	SELECT @isMultiCurrency=IsMultiCurrency
	FROM dbo.FT_GetLastAccount(@calcTime) a
	WHERE a.ID=@accountID

	IF @isMultiCurrency=1
	BEGIN
		SELECT @currencyID=CurrencyID
		FROM dbo.FT_GetLastInstrument(@calcTime) i 
		WHERE i.ID=@instrumentID
	END
	ELSE
	BEGIN
		SELECT @currencyID=CurrencyID
		FROM dbo.FT_GetLastAccount(@calcTime) a 
		WHERE a.ID=@accountID
	END
    SELECT @currencyID ";
            return DB.DBHelper.LoadDBRecords<Guid>(sql, new { accountID = accountId, instrumentID = instrumentId, calcTime = lastTime }, System.Data.CommandType.Text).Single();

        }
    }
}
