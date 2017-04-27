using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    internal class GetResetTimeTest
    {
        [Test]
        public void ResetTimeTest()
        {
            string connectionString = "data source=ws0308;initial catalog=iExchange_V3;user id=sa;password=Omni1234;Connect Timeout=60";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var result = conn.Query<DateTime>("select Trading.GetResetTime(@tradeDay, @instrumentId)", new { tradeDay = DateTime.Parse("2016-11-02 00:00:00.000"), instrumentId = Guid.Parse("736AD70A-3A2D-43A1-BAF8-04E1544DEB9E") }).First();
                Debug.WriteLine(result);
            }
        }
    }
}
