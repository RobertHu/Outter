using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.DB;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    internal class InstrumentDayClosePriceTest
    {
        [Test]
        public void GetTest()
        {
            Guid instrumentId = Guid.Parse("2B3A1A49-37B8-4449-91D1-623CF01F4CA7");
            DateTime tradeDay = new DateTime(2016, 9, 5);
            ExternalSettings.Default.DBConnectionString = "data source=ws3190;initial catalog=iExchange_V3;user id=sa;password=Omni1234;Connect Timeout=60";
            var result = DBRepository.Default.GenerateDailyClosePrice(tradeDay);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
        }

    }
}
