using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    public class DateTimeTest
    {

        [Test]
        public void AddTest()
        {
            DateTime dt1 = new DateTime(2016, 6, 16);
            DateTime dt2 = DateTime.Now;
            DateTime dt3 = dt1.AddHours(dt2.Hour).AddMinutes(dt2.Minute).AddSeconds(dt2.Second);
            Assert.AreEqual(dt2.Hour, dt3.Hour);
            Assert.AreEqual(dt2.Minute, dt3.Minute);
            Assert.AreEqual(dt2.Second, dt3.Second);
            Assert.AreEqual(2016, dt1.Year);
            Assert.AreEqual(6, dt1.Month);
            Assert.AreEqual(16, dt1.Day);
        }

    }
}
