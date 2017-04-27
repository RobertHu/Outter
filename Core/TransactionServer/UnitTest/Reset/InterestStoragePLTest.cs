using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Core.TransactionServer.Agent.Reset;
using System.Diagnostics;

namespace Core.TransactionServer.UnitTest.Reset
{
    [TestFixture]
    internal class InterestStoragePLTest
    {
        [Test]
        public void GetTest()
        {
            Dictionary<Guid, InterestStorage> dict = new Dictionary<Guid, InterestStorage>();
            Guid id = Guid.NewGuid();
            dict.Add(id, new InterestStorage(2, 3));
            dict.Add(Guid.NewGuid(), new InterestStorage(3, 4));
            InterestStorage result = InterestStorage.Empty;
            dict.TryGetValue(id, out result);
            Assert.AreEqual(2, result.Interest);
            Assert.AreEqual(3, result.Storage);
        }

        [Test]
        public void AddTest()
        {
            OpenOrderPLOfCurrentDay result = new OpenOrderPLOfCurrentDay();
            result.DayNotValued += new InterestStorage(1, 1);
            result.Valued += new InterestStorage(2, 2);
            result.NotValued += new InterestStorage(3, 3);

            Assert.AreEqual(new InterestStorage(1, 1), result.DayNotValued);
            Assert.AreEqual(new InterestStorage(2, 2), result.Valued);
            Assert.AreEqual(new InterestStorage(3, 3), result.NotValued);

            result.DayNotValued += new InterestStorage(3, 3);
            Assert.AreEqual(new InterestStorage(4, 4), result.DayNotValued);
            Debug.WriteLine(result.DayNotValued);
        }

    }
}
