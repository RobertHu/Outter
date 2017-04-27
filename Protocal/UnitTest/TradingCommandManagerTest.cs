#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Protocal.Commands;
using System.Xml.Linq;
using Protocal.TypeExtensions;

namespace Protocal.UnitTest
{
    [TestFixture]
    public class TradingCommandManagerTest
    {
        private bool _isOrderPlacing;
        private bool _isOrderPlaced;
        private bool _isOrderCanceled;
        private bool _isOrderExecuted;

        [TestFixtureSetUp]
        public void StartUp()
        {
            string fileContent = File.ReadAllText(@"UnitTest\accountInitData.xml");
            TradingCommandManager.Default.FillAccountInitData(XElement.Parse(fileContent));
            TradingCommandManager.Default.OrderPlacing += e => _isOrderPlacing = true;
            TradingCommandManager.Default.OrderPlaced += e => _isOrderPlaced = true;
            TradingCommandManager.Default.OrderCanceled += e => _isOrderCanceled = true;
            TradingCommandManager.Default.OrderExecuted += e => _isOrderExecuted = true;
        }

        [Test]
        public void P001_PlacingEventTest()
        {
            string xml = @"<Account ID=""c39cb359-1918-4c99-a3bf-91d1293df949"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""True"">
  <Transactions>
    <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""2f8b608b-6fe7-417e-b3a3-94808f58ad22"" Code=""DEM160615SP00105"" Type=""0"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""255"" OrderType=""0"" ContractSize=""0"" BeginTime=""2016-06-15 15:17:47"" EndTime=""2016-06-15 15:18:17"" ExpireType=""3"" SubmitTime=""2016-06-15 15:17:47"" SubmitorID=""9f695137-9a93-4f6c-8c1b-61f42b603eca"" InstrumentCategory=""20"" PlacePhase=""1"" PlaceDetail="""" AppType=""18"" UpdateTime=""2016-06-15 15:17:47"">
      <Orders>
        <Order IsDeleted=""False"" ID=""bbda8264-0935-4f96-8294-6b281a196c98"" IsOpen=""True"" IsBuy=""True"" Lot=""0.01"" OriginalLot=""0.01"" LotBalance=""0.01"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""DEM2016061500153"" SetPrice=""1249.1"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""0"" PhysicalOriginValueBalance=""0"" PaidPledgeBalance=""0"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
          <Bills />
          <InstalmentDetails />
          <OrderRelations />
        </Order>
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            XElement accountE = XElement.Parse(xml);
            Guid accountId = accountE.AttrToGuid("ID");
            var account = AccountRepository.Default.Get(accountId);
            Assert.IsNotNull(account);
            Assert.IsNotNull(account.Fund);
            TradingCommandManager.Default.Add(new TradingCommand { Content = xml });
            Assert.IsTrue(_isOrderPlacing);
        }


        [Test]
        public void P002_PlacedEventTest()
        {
            string xml = @"<Account ID=""c39cb359-1918-4c99-a3bf-91d1293df949"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""True"">
  <Transactions>
    <Transaction ID=""2f8b608b-6fe7-417e-b3a3-94808f58ad22"" Phase=""0"" PlacePhase=""8"" UpdateTime=""2016-06-15 15:17:47"">
      <Orders>
        <Order ID=""bbda8264-0935-4f96-8294-6b281a196c98"" Phase=""0"" />
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            XElement accountE = XElement.Parse(xml);
            Guid accountId = accountE.AttrToGuid("ID");
            var account = AccountRepository.Default.Get(accountId);
            Assert.IsNotNull(account);
            Assert.IsNotNull(account.Fund);
            TradingCommandManager.Default.Add(new TradingCommand { Content = xml });
            Assert.IsTrue(_isOrderPlaced);
        }


        [Test]
        public void P003_ExecutedEventTest()
        {
            string xml = @"<Account ID=""c39cb359-1918-4c99-a3bf-91d1293df949"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""True"" Balance=""997259.43"" Necessary=""2501.00"" PartialPaymentPhysicalNecessary=""2501.00"">
  <Funds>
    <Fund CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" Balance=""997259.4300"" Necessary=""2501.00"" PartialPaymentPhysicalNecessary=""2501.0000000000"" />
  </Funds>
  <Transactions>
    <Transaction ID=""2f8b608b-6fe7-417e-b3a3-94808f58ad22"" Phase=""2"" ContractSize=""1.0000"" ExecuteTime=""2016-06-15 15:17:47"" ApproverID=""00000000-0000-0000-0000-000000000000"">
      <Orders>
        <Order ID=""bbda8264-0935-4f96-8294-6b281a196c98"" ExecutePrice=""1250.2"" JudgePrice=""1249.1"" JudgePriceTimestamp=""2016-06-15 15:16:11"" InterestValueDate=""2016-06-17 00:00:00"" Phase=""2"" PhysicalOriginValue=""12.50"" PhysicalOriginValueBalance=""12.50"" PaidPledgeBalance=""-5.30"" PaidPledge=""-5.30"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""-0.01"" LivePrice=""1249.1"" MarketValue=""0"" ValueAsMargin=""0"">
          <Bills>
            <Bill IsDeleted=""False"" ID=""a769fc29-07ea-4845-a261-1aebcb7c06ed"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""3.00"" Type=""37"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
            <Bill IsDeleted=""False"" ID=""b3a38434-55af-45bd-b804-f81bbd08a6ff"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""-5.30"" Type=""6"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
            <Bill IsDeleted=""False"" ID=""12c536d6-37c3-4538-9103-ef726b3fcae2"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""-250.04"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
            <Bill IsDeleted=""False"" ID=""40d5b8fd-73f2-4002-8047-2e7a196efc78"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""0"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
            <Bill IsDeleted=""False"" ID=""fd963ab1-0f69-45af-b82b-420ddeb1446a"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
            <Bill IsDeleted=""False"" ID=""c17b3fca-3f9f-483b-96ee-5bf9fe2c0c31"" AccountID=""c39cb359-1918-4c99-a3bf-91d1293df949"" Value=""-0.10"" Type=""11"" OwnerType=""1"" UpdateTime=""2016-06-15 15:17:47"" />
          </Bills>
          <InstalmentDetails>
            <InstalmentDetail IsDeleted=""False"" OrderId=""bbda8264-0935-4f96-8294-6b281a196c98"" Period=""1"" Principal=""7.20"" Interest=""0"" DebitInterest=""0"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-15 15:17:47"" InterestRate=""0.0000000000000"" LotBalance=""0.01"" />
          </InstalmentDetails>
        </Order>
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            XElement accountE = XElement.Parse(xml);
            Guid accountId = accountE.AttrToGuid("ID");
            var account = AccountRepository.Default.Get(accountId);
            Assert.IsNotNull(account);
            Assert.IsNotNull(account.Fund);
            TradingCommandManager.Default.Add(new TradingCommand { Content = xml });
            Assert.IsTrue(_isOrderExecuted);
        }

    }
}
#else
#endif
