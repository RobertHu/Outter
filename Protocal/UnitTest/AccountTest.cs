#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using iExchange.Common;
using System.Diagnostics;

namespace Protocal.UnitTest
{
    [TestFixture]
    public class AccountTest
    {
        private Commands.Account _account;

        [TestFixtureSetUp]
        public void Start()
        {
            Debug.WriteLine("start");
            string fileContent = File.ReadAllText(@"UnitTest\account.xml");
            XElement accountsNode = XElement.Parse(fileContent);
            var accountNode = accountsNode.Element("Account");
            Guid id = Guid.Parse(accountNode.Attribute("ID").Value);
            _account = new Commands.Account(id);
            _account.Initialize(accountNode);
        }


        [Test]
        public void InitializeTest()
        {
            Assert.IsNotNull(_account);
            Assert.AreEqual(AlertLevel.Normal, _account.AlertLevel);
            Assert.AreEqual(0, _account.TotalDeposit);
            Assert.AreEqual(0, _account.MinUpkeepEquity);
            Assert.AreEqual(false, _account.IsMultiCurrency);
        }

        [Test]
        public void TestCustomerNameAndCurrency()
        {
            Assert.AreEqual("NZD098", _account.Code);
            Assert.AreEqual("USD", _account.CurrencyCode);
            Assert.AreEqual("NZD098", _account.CustomerName);
        }

        [Test]
        public void TestAccountTypeAndEquity()
        {
            Assert.AreEqual(AccountType.Company, _account.Type);
            Assert.AreEqual(67772.57, _account.Equity);
        }

        [Test]
        public void TestTransactionCount()
        {
            Assert.AreEqual(2, _account.TransactionCount);
        }

        [Test]
        public void TestTransactionProperties()
        {
            var tran = _account.GetTran(Guid.Parse("1da1c67e-ef0d-45ea-9350-ecf4cf03c9eb"));
            Assert.IsNotNull(tran);
            Assert.AreEqual("MHL160520SP00007", tran.Code);
            Assert.AreEqual(TransactionType.OneCancelOther, tran.Type);
            Assert.AreEqual(TransactionSubType.Amend, tran.SubType);
            Assert.AreEqual(TransactionPhase.Executed, tran.Phase);
            Assert.AreEqual(Guid.Parse("c2675f7a-d05e-4359-90e3-36597c464104"), tran.AccountId);
            Assert.AreEqual(Guid.Parse("cb4d7381-0b5c-40cf-80f2-46934bc84da4"), tran.InstrumentId);
            Assert.AreEqual(OrderType.MarketOnOpen, tran.OrderType);
            Assert.AreEqual(InstrumentCategory.Physical, tran.InstrumentCategory);
            Assert.AreEqual(50, tran.ContractSize);
            Assert.AreEqual(DateTime.Parse("2016-05-20 13:58:57"), tran.BeginTime);
            Assert.AreEqual(DateTime.Parse("2016-05-20 14:18:57"), tran.EndTime);
            Assert.AreEqual(DateTime.Parse("2016-05-20 13:58:58"), tran.ExecuteTime);
            Assert.AreEqual(DateTime.Parse("2016-05-20 13:58:57"), tran.SubmitTime);
            Assert.AreEqual(Guid.Parse("d238dfab-3668-4a8c-ab33-50cf4d3d5804"), tran.SubmitorId);
            Assert.IsNull(tran.ApproverId);
        }

        [Test]
        public void TestOrderProperties()
        {
            var tran = _account.GetTran(Guid.Parse("1da1c67e-ef0d-45ea-9350-ecf4cf03c9eb"));
            Assert.IsNotNull(tran);
            Assert.AreEqual(1, tran.Orders.Count);
            var order = tran.Orders[0];
            Assert.IsNotNull(order);
            Assert.AreEqual("0.6764", order.LivePrice);
            Assert.IsNull(order.AutoLimitPrice);
            Assert.AreEqual("MHL2016052000008", order.Code);
            Assert.IsNull(order.OriginalCode);
            Assert.IsTrue(order.IsOpen);
            Assert.IsTrue(order.IsBuy);
            Assert.AreEqual(3, order.OriginalLot);
            Assert.AreEqual(3, order.Lot);
            Assert.AreEqual(3, order.LotBalance);
            Assert.AreEqual(1.2, order.InterestPerLot);
            Assert.AreEqual(0.5, order.StoragePerLot);
            Assert.AreEqual(2, order.MinLot);
            Assert.IsNull(order.MaxShow);
            Assert.IsNull(order.BlotterCode);
            Assert.AreEqual("0.6746", order.ExecutePrice);
            Assert.AreEqual("0.6736", order.SetPrice);
            Assert.IsNull(order.AutoLimitPrice);
            Assert.IsNull(order.AutoStopPrice);
            Assert.IsNull(order.JudgePrice);
            Assert.IsNull(order.JudgePriceTimestamp);
            Assert.AreEqual(DateTime.Parse("2016-05-20 00:00:00"), order.InterestValueDate);
            Assert.AreEqual(2, order.SetPriceMaxMovePips);
            Assert.AreEqual(4, order.DQMaxMove);
            Assert.IsFalse(order.PlacedByRiskMonitor);
            Assert.AreEqual(OrderPhase.Executed, order.Phase);
            Assert.AreEqual(TradeOption.Better, order.TradeOption);
            Assert.AreEqual(0.03, order.InterestPLFloat);
            Assert.AreEqual(0.02, order.StoragePLFloat);
            Assert.AreEqual(-0.59, order.TradePLFloat);

            Assert.AreEqual(-2226.84, order.CommissionSum);
            Assert.AreEqual(-30000.0, order.LevySum);
            Assert.AreEqual(0, order.OtherFeeSum);
        }

        [Test]
        public void TestOrderRelation()
        {
            var tran = _account.GetTran(Guid.Parse("86ac2fe0-fd66-43a9-97c8-c14e36a0f5d7"));
            Assert.IsNotNull(tran);
            Assert.AreEqual(1, tran.Orders.Count);
            var order = tran.Orders[0];
            Assert.IsNotNull(order);
            Assert.IsFalse(order.IsOpen);
            Assert.IsNotNull(order.OrderRelations);
            Assert.AreEqual(1, order.OrderRelations.Count);
            var orderRelation = order.OrderRelations[0];
            Assert.IsNotNull(orderRelation);
            Assert.AreEqual(Guid.Parse("dd4f5eed-e4fc-458c-bd36-2ae6e1a2f058"), orderRelation.OpenOrderId);
            Assert.AreEqual(Guid.Parse("922d6df9-172f-450e-b721-b6240e14964f"), orderRelation.Owner.Id);
            Assert.AreEqual(1, orderRelation.ClosedLot);
            Assert.AreEqual(DateTime.Parse("2016-05-24 09:01:14"), orderRelation.CloseTime);
            Assert.AreEqual(DateTime.Parse("2016-05-24 09:01:14"), orderRelation.ValueTime);
            Assert.AreEqual(2, orderRelation.Decimals);
            Assert.AreEqual(1, orderRelation.RateIn);
            Assert.AreEqual(1, orderRelation.RateOut);
            Assert.AreEqual(200, orderRelation.Commission);
            Assert.AreEqual(30, orderRelation.Levy);
            Assert.AreEqual(0.5, orderRelation.OtherFee);
            Assert.AreEqual(0.2, orderRelation.InterestPL);
            Assert.AreEqual(0.3, orderRelation.StoragePL);
            Assert.AreEqual(-0.6, orderRelation.TradePL);
        }

    }
}
#else
#endif
