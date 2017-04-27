using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Protocal.Physical;
using System.Diagnostics;

namespace Protocal.UnitTest
{
    [TestFixture]
    internal class InstalmentDataTest
    {
        [Test]
        public void ToStringTest()
        {
            InstalmentData data = new InstalmentData();
            data.OrderID = Guid.NewGuid();
            data.PaidPledge = 100;
            Debug.WriteLine(data.ToString());
        }
    }
}
