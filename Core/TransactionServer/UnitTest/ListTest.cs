using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Core.TransactionServer.UnitTest
{
    [TestFixture]
    internal sealed class ListTest
    {
        [Test]
        public void MinTest()
        {
            List<int> items = new List<int>();
            Assert.IsNull(items.Min());
            items.Add(4);
            items.Add(-1);
            items.Add(-2);
            items.Add(0);
            Assert.AreEqual(-2, items.Min());
        }
    }
}
