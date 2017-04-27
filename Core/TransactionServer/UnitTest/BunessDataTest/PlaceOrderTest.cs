using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.UnitTest.BunessDataTest
{
    [TestFixture]
    internal sealed class PlaceOrderTest
    {
        [Test]
        public void PlaceTest()
        {
            FakeAccount account = new FakeAccount(Guid.NewGuid(), "Acc1");
            Guid tran1_id = Guid.NewGuid();
            FakeTransaction tran1 = new FakeTransaction(tran1_id, "tran1", account, OperationType.AsNewRecord);
            Assert.AreEqual(ChangeStatus.Modified, account.Status);
            Assert.AreEqual(ChangeStatus.Added, tran1.Status);
            Debug.WriteLine(account.SaveChanges());
            Assert.AreEqual(1, account.TransactionCount);
            Assert.AreEqual(ChangeStatus.None, account.Status);
            Assert.AreEqual(ChangeStatus.None, tran1.Status);
        }

        [Test]
        public void DeleteOrderTest()
        {
            FakeAccount account = new FakeAccount(Guid.NewGuid(), "Acc2");
            FakeTransaction tran1 = new FakeTransaction(Guid.NewGuid(), "tran1", account);
            FakeOrder order1 = new FakeOrder(tran1, Guid.NewGuid(), "order1", 2, 2);

            FakeTransaction tran2 = new FakeTransaction(Guid.NewGuid(), "tran2", account);
            FakeOrder order2 = new FakeOrder(tran2, Guid.NewGuid(), "order2", 3, 3);
            Assert.AreEqual(2, account.TransactionCount);
            Assert.AreEqual(1, tran2.OrderCount);
            order1.LotBalance = 1;
            tran2.RemoveOrder(order2);
            Debug.WriteLine(account.SaveChanges());
            Assert.AreEqual(0, tran2.OrderCount);

            account.RemoveTran(tran2);
            Assert.AreEqual(2, account.TransactionCount);

            Debug.WriteLine(account.SaveChanges());
            Assert.AreEqual(1, account.TransactionCount);
        }

        [Test]
        public void RejectTest()
        {
            FakeAccount account = new FakeAccount(Guid.NewGuid(), "Acc3");
            Debug.WriteLine(account.SaveChanges());
            Assert.AreEqual(0, account.TransactionCount);
            FakeTransaction tran1 = new FakeTransaction(Guid.NewGuid(), "tran1", account);
            FakeOrder order1 = new FakeOrder(tran1, Guid.NewGuid(), "order1", 2, 2);
            Assert.AreEqual(1, account.TransactionCount);
            account.RejectChanges();
            Assert.AreEqual(1, account.TransactionCount);
        }

        [Test]
        public void RejectTestWithAsNewRecord()
        {
            FakeAccount account = new FakeAccount(Guid.NewGuid(), "Acc4");
            Debug.WriteLine(account.SaveChanges());
            Assert.AreEqual(0, account.TransactionCount);
            FakeTransaction tran1 = new FakeTransaction(Guid.NewGuid(), "tran1", account,  OperationType.AsNewRecord);
            FakeOrder order1 = new FakeOrder(tran1, Guid.NewGuid(), "order1", 2, 2);
            account.RejectChanges();
            Assert.AreEqual(0, account.TransactionCount);
        }

    }
}
