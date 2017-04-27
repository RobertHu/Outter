using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.UnitTest.BunessDataTest
{
    [TestFixture]
    internal sealed class TransactionBusinessItemTest
    {
        private sealed class Fund: BusinessRecord
        {
            private BusinessItem<decimal> _balance;

            internal Fund()
                :base("Fund", 2)
            {
                _balance = BusinessItemFactory.Create("Balance", 97103540.86m, PermissionFeature.Sound, this);
            }

            internal decimal Balance
            {
                get { return _balance.Value; }
                set
                {
                    _balance.SetValue(value);
                }
            }


        }

        [Test]
        public void AddTest()
        {
            Fund fund = new Fund();
            Assert.AreEqual(97103540.86, fund.Balance);
            fund.Balance += 100;
            Assert.AreEqual(97103640.86, fund.Balance);

        }

    }
}
