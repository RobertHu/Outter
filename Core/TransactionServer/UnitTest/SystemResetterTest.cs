using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    internal class SystemResetterTest
    {
        [Test]
        public void GetLastAccountBalanceDayHistoryTest()
        {
            string connectionString = "data source=ws0301;initial catalog=iExchange_V3;user id=sa;password=Omni1234;Connect Timeout=60";

            var model = Core.TransactionServer.Agent.DB.DBResetRepository.GetLastAccountBalanceDayHistory(Guid.Parse("C7E5BE51-DF2C-4318-8A18-6368EE77E149"),connectionString).SingleOrDefault();
            Assert.IsNotNull(model);
            Assert.AreEqual(DateTime.Parse("2016-06-29 00:00:00.000"), model.TradeDay);
            Assert.AreEqual(Guid.Parse("C7E5BE51-DF2C-4318-8A18-6368EE77E149"), model.AccountID);
            Assert.AreEqual(Guid.Parse("767515E7-CFCC-4BA1-9684-659588C02E3E"), model.CurrencyID);
            Assert.AreEqual(93087.22, model.Balance);
        }
    }
}
