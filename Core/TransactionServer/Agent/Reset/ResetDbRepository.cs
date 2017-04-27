using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset
{
    internal static class ResetDbRepository
    {
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
            return DBHelper.LoadDBRecords<Guid>(sql, new { accountID = accountId, instrumentID = instrumentId, calcTime = lastTime }).Single();

        }
    }
}
