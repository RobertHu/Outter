using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Protocal.Commands;
using System.Xml.Linq;

namespace Protocal.UnitTest
{
    [TestFixture]
    public class OCOTEST
    {

        [TestFixtureSetUp]
        public void StartUp()
        {
            TradingCommandManager.Default.FillAccountInitData(XElement.Parse(this.GetInitXml()));
        }

        [Test]
        public void P001_PlacingTest()
        {
            var orderChanged = TradingCommandManager.Default.ProcessTest(this.GetPlacingXML());
            Assert.IsNotNull(orderChanged);
            Assert.AreEqual(2, orderChanged.Count);
            Assert.AreEqual(OrderChangeType.Placing, orderChanged[0].ChangeType);
            Assert.AreEqual(OrderChangeType.Placing, orderChanged[1].ChangeType);
        }

        [Test]
        public void P002_PlacedTest()
        {
            var orderChanged = TradingCommandManager.Default.ProcessTest(this.GetPlacedXml());
            Assert.IsNotNull(orderChanged);
            Assert.AreEqual(2, orderChanged.Count);
            Assert.AreEqual(OrderChangeType.Placed, orderChanged[0].ChangeType);
            Assert.AreEqual(OrderChangeType.Placed, orderChanged[1].ChangeType);
        }

        [Test]
        public void P003_ExecuteTest()
        {
            var orderChanged = TradingCommandManager.Default.ProcessTest(this.GetExecutedXml());
            Assert.IsNotNull(orderChanged);
            Assert.AreEqual(2, orderChanged.Count);
            var executedOrder = orderChanged.Where(m => m.Source.Id == Guid.Parse("f76218b7-4a6d-411d-a2ef-dfa0484ac842")).Single();
            var canceledOrder = orderChanged.Where(m => m.Source.Id == Guid.Parse("d1a2d96c-8ec2-42b0-bea2-6f95405ea588")).Single();
            Assert.AreEqual(OrderChangeType.Executed, executedOrder.ChangeType);
            Assert.AreEqual(OrderChangeType.Canceled, canceledOrder.ChangeType);
        }

        private string GetExecutedXml()
        {
            return @"<Account ID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" IsMultiCurrency=""False"" Necessary=""53314.80"">
  <Transactions>
    <Transaction ID=""a40530b2-10ec-4b5b-8f72-d978e515e31a"" Phase=""2"" ContractSize=""100.0000"" ExecuteTime=""2016-10-13 15:12:13"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"">
      <Orders>
        <Order ID=""f76218b7-4a6d-411d-a2ef-dfa0484ac842"" ExecutePrice=""1261.4"" JudgePrice=""1261.4"" JudgePriceTimestamp=""2016-10-13 15:12:13"" InterestValueDate=""2016-10-13 00:00:00"" Phase=""2"">
          <Bills>
            <Bill IsDeleted=""False"" ID=""5d2f23f3-a8ec-471c-90c9-4f1663496156"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 15:12:13"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            <Bill IsDeleted=""False"" ID=""4bcb3069-fc79-4430-b02e-321ce814cc6f"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 15:12:13"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            <Bill IsDeleted=""False"" ID=""8d7c570e-dd6e-45de-b5e0-d51173838a6c"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 15:12:13"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
          </Bills>
        </Order>
        <Order ID=""d1a2d96c-8ec2-42b0-bea2-6f95405ea588"" Phase=""1"" CancelType=""27"" />
      </Orders>
    </Transaction>
  </Transactions>
  <Bills>
    <Bill IsDeleted=""False"" ID=""e9460cef-8fee-4e50-a51f-71b50ef6152d"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 15:12:13"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
  </Bills>
</Account>";
        }


        private string GetPlacedXml()
        {
            return @"<Account ID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" IsMultiCurrency=""False"">
  <Transactions>
    <Transaction ID=""a40530b2-10ec-4b5b-8f72-d978e515e31a"" Phase=""0"" PlacePhase=""8"" UpdateTime=""2016-10-13 15:08:04"">
      <Orders>
        <Order ID=""f76218b7-4a6d-411d-a2ef-dfa0484ac842"" Phase=""0"" />
        <Order ID=""d1a2d96c-8ec2-42b0-bea2-6f95405ea588"" Phase=""0"" />
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
        }

        private string GetInitXml()
        {
            return @"<Accounts>
  <Account IsDeleted=""False"" AlertLevel=""0"" CutAlertLevel=""0"" ID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" CustomerName=""周先生(EE)"" IsMultiCurrency=""False"" MinUpkeepEquity=""0"" Type=""0"" CurrencyCode=""HKA"" Code=""060000"" TradeDay=""2016-10-13 00:00:00"" CreditAmount=""3600000.0000"" TotalDeposit=""0"" Balance=""21441955.93"" FrozenFund=""0"" Necessary=""0"" NetNecessary=""0"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""0"" MinEquityAvoidRiskLevel2=""0"" MinEquityAvoidRiskLevel3=""0"" NecessaryFillingOpenOrder=""0"" NecessaryFillingCloseOrder=""0"" TradePLFloat=""-2434.43"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""0"" RiskCredit=""0.00"" PartialPaymentPhysicalNecessary=""0"" TotalPaidAmount=""0"" Equity=""21439521.50"" LastResetDay=""2016-10-12 00:00:00"">
    <Funds>
      <Fund IsDeleted=""False"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" CurrencyCode=""HKA"" TotalDeposit=""0"" Balance=""21441955.9300"" FrozenFund=""0.0000000000000"" Necessary=""0"" NetNecessary=""0"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""0"" MinEquityAvoidRiskLevel2=""0"" MinEquityAvoidRiskLevel3=""0"" NecessaryFillingOpenOrder=""0"" NecessaryFillingCloseOrder=""0"" TradePLFloat=""0"" InterestPLFloat=""0"" StoragePLFloat=""0"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""0"" RiskCredit=""0"" PartialPaymentPhysicalNecessary=""0"" TotalPaidAmount=""0"" />
    </Funds>
    <Transactions>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""00670c58-c7bd-0141-af8a-636d7853a622"" Code=""WFB151110RS00011"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"" SubType=""0"" Phase=""2"" OrderType=""6"" ContractSize=""2500.0000"" BeginTime=""2015-11-10 13:50:43"" EndTime=""2015-11-10 13:53:13"" ExpireType=""3"" SubmitTime=""2015-11-10 13:51:43"" SubmitorID=""cbc4455b-7837-40c7-92b9-e7129bd49634"" ExecuteTime=""2015-11-10 13:51:19"" ApproverID=""cbc4455b-7837-40c7-92b9-e7129bd49634"" InstrumentCategory=""10"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 11:48:27"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""3e472f10-b9cd-aa4c-a21d-388fb2f8ee18"" IsOpen=""True"" IsBuy=""True"" Lot=""20.0000"" OriginalLot=""20.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""WFB2015111000066"" BlotterCode=""Q"" ExecutePrice=""14.875"" InterestValueDate=""2015-11-10 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills />
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""008afe9d-2885-4bae-9dd0-8dbe3e205970"" Code=""WFB161013SP00005"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""100.0000"" BeginTime=""2016-10-13 13:50:01"" EndTime=""2016-10-13 13:50:30"" ExpireType=""3"" SubmitTime=""2016-10-13 13:50:01"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:50:02"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:50:01"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""909b1114-80d2-4278-aa2c-a2c82f27b9aa"" IsOpen=""True"" IsBuy=""True"" Lot=""2.00000000"" OriginalLot=""2.00000000"" LotBalance=""2.00000000"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300007"" BlotterCode=""Q"" ExecutePrice=""1261.8"" SetPrice=""1261.8"" JudgePrice=""1261.6"" JudgePriceTimestamp=""2016-10-13 13:50:01"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""True"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""-4032.18"" LivePrice=""1259.2"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""25eebc58-3674-4fdc-83ec-73fadffdffa3"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:02"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""7ebe8f3c-836b-47eb-b9be-28586d68deb8"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:02"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""99a2e1c9-1269-4f8f-bcd5-dfac91689a00"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:02"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""ffb5781f-da33-498c-8d95-dc7030172666"" Code=""WFB161013SP00006"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""100.0000"" BeginTime=""2016-10-13 13:50:08"" EndTime=""2016-10-13 13:50:38"" ExpireType=""3"" SubmitTime=""2016-10-13 13:50:08"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:50:09"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:50:08"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""e3005c6a-9065-4415-a830-2d58101635d3"" IsOpen=""True"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300008"" BlotterCode=""Q"" ExecutePrice=""1261.1"" SetPrice=""1261.1"" JudgePrice=""1261.0"" JudgePriceTimestamp=""2016-10-13 13:50:08"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""True"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""1163.13"" LivePrice=""1259.6"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""5571bfa5-03c6-4c64-83e4-bb4b1e488e81"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:09"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""a41282d3-6dbe-4bf0-8ed3-ae00095b345e"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:09"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""23c0c862-e98f-4e4a-b304-137721a602c8"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:09"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""e3a212f6-6cec-4465-aee4-2e7cf0e12a9d"" Code=""WFB161013SP00007"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""89611ae3-8f72-4a93-b038-b1fe7aa8fb98"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""32.1507"" BeginTime=""2016-10-13 13:50:18"" EndTime=""2016-10-13 13:50:47"" ExpireType=""3"" SubmitTime=""2016-10-13 13:50:18"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:50:36"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:50:18"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""319e40e3-5f21-4c8f-a23d-cd77ebb47f69"" IsOpen=""True"" IsBuy=""True"" Lot=""3.00000000"" OriginalLot=""3.00000000"" LotBalance=""2.00000000"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300009"" BlotterCode=""Q"" ExecutePrice=""1261.5"" SetPrice=""1261.5"" JudgePrice=""1261.6"" JudgePriceTimestamp=""2016-10-13 13:50:26"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""-1146.77"" LivePrice=""1259.2"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""f619be17-b5e9-47a8-906d-f704c04166f5"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:36"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""06957666-235c-473b-bdcb-b61c679a45fc"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:36"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""0c3c0ae0-8253-45fd-8048-b0c05f83559a"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:36"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""03e2075f-366c-4d0a-b1ca-70524534bbec"" Code=""WFB161013SP00008"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""2500.0000"" BeginTime=""2016-10-13 13:50:44"" EndTime=""2016-10-13 13:51:13"" ExpireType=""3"" SubmitTime=""2016-10-13 13:50:44"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:50:44"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:50:44"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""c21ab861-f0cf-41d3-8583-59a63cf17e01"" IsOpen=""True"" IsBuy=""False"" Lot=""2.00000000"" OriginalLot=""2.00000000"" LotBalance=""2.00000000"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300010"" BlotterCode=""Q"" ExecutePrice=""17.635"" SetPrice=""17.635"" JudgePrice=""17.635"" JudgePriceTimestamp=""2016-10-13 13:50:28"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""True"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""969.28"" LivePrice=""17.610"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""3d2709d7-9901-42b6-b536-70d1f43e67e2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:44"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""0e22dc75-057b-46e6-a4e5-b7166174ef7c"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:44"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""0b5925c6-ef7a-4021-9f96-86ebc7a7919f"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:50:44"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""d5d684de-5081-444e-bdc0-489eaf0d3de9"" Code=""WFB161013LM00002"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"" SubType=""0"" Phase=""2"" OrderType=""1"" ContractSize=""2500.0000"" BeginTime=""2016-10-13 13:50:51"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 13:50:51"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:52:56"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:50:52"">
        <Orders>
          <Order IsDeleted=""False"" BestTime=""2016-10-13 13:52:56"" HitCount=""1"" BestPrice=""17.485"" HitStatus=""2"" ID=""d893242d-b161-42fd-a49a-04891f884673"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300011"" BlotterCode=""Q"" ExecutePrice=""17.475"" SetPrice=""17.475"" JudgePrice=""17.485"" JudgePriceTimestamp=""2016-10-13 13:52:56"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""1841.62"" LivePrice=""17.570"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""a0d1832c-25ff-40b4-9795-3941f9693aaf"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:56"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""a5a777fb-41e4-467e-88d8-c78d4e4d4909"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:56"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""72a9b0e7-22c7-47c1-96f0-eb2c04b6daa7"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:56"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""2a25d9b5-1b79-4381-9e8b-6e5871f189fe"" Code=""WFB161013LM00003"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""cc5f2be9-8746-4b10-b20c-7e0b8691de22"" SubType=""0"" Phase=""0"" OrderType=""1"" ContractSize=""0"" BeginTime=""2016-10-13 13:51:02"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 13:51:02"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:51:02"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""732aa226-6e0b-41b8-92d2-2ec674ea1a00"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300013"" BlotterCode=""Q"" SetPrice=""940.3"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""0"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills />
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""b2431694-5e80-48be-a901-15462a31c3d0"" Code=""WFB161013LM00004"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""cc5f2be9-8746-4b10-b20c-7e0b8691de22"" SubType=""2"" Phase=""0"" OrderType=""1"" ContractSize=""0"" BeginTime=""2016-10-13 13:51:12"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 13:51:12"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:51:12"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""02337e28-19f7-4df9-88f7-33d1b453aa80"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300014"" BlotterCode=""Q"" SetPrice=""940.3"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""0"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills />
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""1a3ee73a-11dd-418d-a314-7193eadff83d"" Code=""WFB161013LM00005"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""cc5f2be9-8746-4b10-b20c-7e0b8691de22"" SubType=""2"" Phase=""255"" OrderType=""1"" ContractSize=""25.0000"" BeginTime=""2016-10-13 13:51:12"" EndTime=""2016-10-14 05:14:59"" ExpireType=""0"" SubmitTime=""2016-10-13 13:51:12"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" AssigningOrderID=""02337e28-19f7-4df9-88f7-33d1b453aa80"" InstrumentCategory=""10"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:51:12"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""70623828-4efc-47d6-b601-3408ed392157"" IsOpen=""False"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""0"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300015"" SetPrice=""950.3"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills />
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""2fa5a20c-9c28-4225-a7d0-7e41897bda6d"" OpenOrderID=""02337e28-19f7-4df9-88f7-33d1b453aa80"" CloseOrderID=""70623828-4efc-47d6-b601-3408ed392157"" ClosedLot=""1"" TargetDecimals=""0"" RateIn=""0"" RateOut=""0"" Commission=""0"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""0"" OpenOrderExecuteTime=""0001-01-01 00:00:00"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""22a7573a-4d4b-4e3c-9cba-c3b422fe7768"" IsOpen=""False"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""0"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300016"" SetPrice=""930.3"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""1"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills />
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""30314eca-e5f0-4594-a14f-23d397598ecb"" OpenOrderID=""02337e28-19f7-4df9-88f7-33d1b453aa80"" CloseOrderID=""22a7573a-4d4b-4e3c-9cba-c3b422fe7768"" ClosedLot=""1"" TargetDecimals=""0"" RateIn=""0"" RateOut=""0"" Commission=""0"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""0"" OpenOrderExecuteTime=""0001-01-01 00:00:00"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""c27a8c4c-4510-4234-9c7d-5cd853709d3e"" Code=""WFB161013LM00006"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"" SubType=""2"" Phase=""2"" OrderType=""1"" ContractSize=""2500.0000"" BeginTime=""2016-10-13 13:51:31"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 13:51:31"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:54:06"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:51:31"">
        <Orders>
          <Order IsDeleted=""False"" BestTime=""2016-10-13 13:54:06"" HitCount=""1"" BestPrice=""17.840"" HitStatus=""2"" ID=""2f914fdf-6b44-4bf6-9d3d-87bfd5caaa94"" IsOpen=""True"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""0"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300017"" BlotterCode=""Q"" ExecutePrice=""17.840"" SetPrice=""17.840"" JudgePrice=""17.840"" JudgePriceTimestamp=""2016-10-13 13:54:06"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.00"" LivePrice=""17.675"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""8f307621-d831-491a-8786-7f7dbfbaa455"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""f5bbd056-ada8-4a92-88e6-377fb48c4976"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""3b4fe533-fc06-492e-8c02-b697693b554b"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""e0385c4e-1197-4b6d-be10-c9dfc79ef8dd"" Code=""WFB161013LM00007"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"" SubType=""0"" Phase=""2"" OrderType=""1"" ContractSize=""2500.0000"" BeginTime=""2016-10-13 13:51:31"" EndTime=""2016-10-14 05:14:59"" ExpireType=""0"" SubmitTime=""2016-10-13 13:51:31"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:54:06"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" AssigningOrderID=""2f914fdf-6b44-4bf6-9d3d-87bfd5caaa94"" InstrumentCategory=""10"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:54:06"">
        <Orders>
          <Order IsDeleted=""False"" BestTime=""2016-10-13 13:54:06"" HitCount=""1"" BestPrice=""17.675"" HitStatus=""2"" ID=""9ad14bba-4f7e-471e-8696-f79833ad8add"" IsOpen=""False"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""0"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300019"" ExecutePrice=""17.640"" SetPrice=""17.640"" JudgePrice=""17.675"" JudgePriceTimestamp=""2016-10-13 13:54:06"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""9af3d567-7446-4570-b39d-0ffac1b7eb0f"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""a7815321-7988-4100-9483-a0aaa9f860f0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""fb4a1a28-ee21-4b74-9068-b72b396f4da1"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""e6967e5f-b763-4eea-afdb-32453d07e0ac"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""3877.10"" Type=""23"" OwnerType=""1"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" ValueTime=""2016-10-13 13:54:06"" IsValued=""True"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""29d22213-2328-4d51-8eed-efc2e9d0969a"" OpenOrderID=""2f914fdf-6b44-4bf6-9d3d-87bfd5caaa94"" CloseOrderID=""9ad14bba-4f7e-471e-8696-f79833ad8add"" ClosedLot=""1"" CloseTime=""2016-10-13 13:54:06"" ValueTime=""2016-10-13 13:54:06"" TargetDecimals=""2"" RateIn=""7.7542"" RateOut=""7.7542"" Commission=""0"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""500.00"" OpenOrderExecuteTime=""0001-01-01 00:00:00"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""eb0c2285-8d6f-4f43-ba6b-0f328f18c7e0"" Code=""WFB161013SP00009"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""89611ae3-8f72-4a93-b038-b1fe7aa8fb98"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""32.1507"" BeginTime=""2016-10-13 13:51:40"" EndTime=""2016-10-13 13:52:09"" ExpireType=""3"" SubmitTime=""2016-10-13 13:51:40"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:51:46"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:51:40"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""36f297e8-81cc-4c8e-8b3a-50ffc3e1edef"" IsOpen=""True"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300023"" BlotterCode=""Q"" ExecutePrice=""1261.2"" SetPrice=""1261.2"" JudgePrice=""1261.2"" JudgePriceTimestamp=""2016-10-13 13:51:08"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""398.88"" LivePrice=""1259.6"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""2c2e45c1-881b-4c19-b76b-b38741ba3c53"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:51:46"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""6b5b9d3f-6c98-4029-8e57-ffa34c6a3081"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:51:46"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""9c9e4edf-79e9-479f-a7a9-9b0aae0831a8"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:51:46"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""2cc93120-aa29-45b0-90a9-c44275c13abe"" Code=""WFB161013SP00010"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""89611ae3-8f72-4a93-b038-b1fe7aa8fb98"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""32.1507"" BeginTime=""2016-10-13 13:52:05"" EndTime=""2016-10-13 13:52:34"" ExpireType=""3"" SubmitTime=""2016-10-13 13:52:05"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 13:52:10"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 13:52:05"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""ada25619-1bff-440d-b60c-456552ae37de"" IsOpen=""False"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""0"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300024"" BlotterCode=""Q"" ExecutePrice=""1261.1"" SetPrice=""1261.1"" JudgePrice=""1261.1"" JudgePriceTimestamp=""2016-10-13 13:51:47"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""8157a076-9531-4f6d-a619-0c294a70adb5"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:10"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""4a451d14-d607-410c-bd08-98c9a134bc44"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:10"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""fe35dfba-ed5f-4004-b3f6-ab2b2d8ee924"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:10"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""982508d5-ee0f-41f3-befe-288a1b8c4833"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""-99.72"" Type=""23"" OwnerType=""1"" UpdateTime=""2016-10-13 13:52:10"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" ValueTime=""2016-10-13 13:52:10"" IsValued=""True"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""5618e2e5-2478-4653-89f0-25b87389f91b"" OpenOrderID=""319e40e3-5f21-4c8f-a23d-cd77ebb47f69"" CloseOrderID=""ada25619-1bff-440d-b60c-456552ae37de"" ClosedLot=""1"" CloseTime=""2016-10-13 13:52:10"" ValueTime=""2016-10-13 13:52:10"" TargetDecimals=""2"" RateIn=""7.7542"" RateOut=""7.7542"" Commission=""0"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""-12.86"" OpenOrderExecuteTime=""0001-01-01 00:00:00"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""bda3365c-3244-4a9c-8d71-9edcc4340b57"" Code=""WFB161013LM00009"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""2"" OrderType=""1"" ContractSize=""100.0000"" BeginTime=""2016-10-13 14:08:10"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 14:08:10"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 14:17:21"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 14:08:10"">
        <Orders>
          <Order IsDeleted=""False"" BestTime=""2016-10-13 14:17:21"" HitCount=""1"" BestPrice=""1258.7"" HitStatus=""2"" ID=""f344b7fc-a623-407d-a63c-3a01f713ab6d"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300025"" BlotterCode=""Q"" ExecutePrice=""1258.5"" SetPrice=""1258.5"" JudgePrice=""1258.7"" JudgePriceTimestamp=""2016-10-13 14:17:21"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""542.79"" LivePrice=""1259.2"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""76195bfc-4e53-41a9-9276-0645ab3a1fab"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 14:17:21"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""1da280d2-ba9a-4603-9d29-489a82e352ad"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 14:17:21"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""ae2e4e9e-cae4-4947-b465-eb86cf441eef"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 14:17:21"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""5e274b71-9f1c-4c6c-a657-0a4ce02812ba"" Code=""WFB161013SP00047"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""100.0000"" BeginTime=""2016-10-13 14:38:25"" EndTime=""2016-10-13 14:38:54"" ExpireType=""3"" SubmitTime=""2016-10-13 14:38:25"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 14:38:26"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 14:38:25"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""57e713ce-097e-4e9b-9b28-f467ab4092fc"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300091"" BlotterCode=""Q"" ExecutePrice=""1260.6"" SetPrice=""1260.6"" JudgePrice=""1260.6"" JudgePriceTimestamp=""2016-10-13 14:38:22"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""True"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""-1085.59"" LivePrice=""1259.2"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""b5cd76bc-b2c6-4cae-8074-c3f772238163"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 14:38:26"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""8954651d-d6d6-45fb-84a9-d15a6d48d6de"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 14:38:26"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""eb3dfe3d-3c3c-41fb-b117-3fb6861418a2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 14:38:26"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""90e14f93-3884-4bae-8b9a-08585b091fa5"" Code=""WFB161013SP00048"" Type=""0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""100.0000"" BeginTime=""2016-10-13 14:39:36"" EndTime=""2016-10-13 14:40:06"" ExpireType=""3"" SubmitTime=""2016-10-13 14:39:36"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" ExecuteTime=""2016-10-13 14:39:37"" ApproverID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""8"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 14:39:36"">
        <Orders>
          <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""595bccf4-b0d7-4e70-a079-2fdbadc4eef9"" IsOpen=""True"" IsBuy=""True"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300092"" BlotterCode=""Q"" ExecutePrice=""1260.6"" SetPrice=""1260.6"" JudgePrice=""1260.6"" JudgePriceTimestamp=""2016-10-13 14:39:34"" InterestValueDate=""2016-10-13 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" CancelType=""0"" IsAutoFill=""True"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""-1085.59"" LivePrice=""1259.2"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""286c3b58-213b-4b3f-b62d-5397a8f0fd57"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-10-13 14:39:37"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""da47c891-c130-4f15-a523-d87e2f117ced"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-10-13 14:39:37"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
              <Bill IsDeleted=""False"" ID=""3b329942-c3d9-4668-83e6-6b16432547ba"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-10-13 14:39:37"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
    </Transactions>
    <DeliveryRequests />
    <Instruments>
      <Instrument IsDeleted=""False"" LastResetDay=""2016-10-12 00:00:00"" ID=""4a5c67c9-8468-4cd1-aa18-f985f6c7a00a"">
        <ResetItemHistory />
      </Instrument>
      <Instrument IsDeleted=""False"" ID=""b5c8eefb-66f2-423b-b287-428b655392d3"">
        <ResetItemHistory />
      </Instrument>
      <Instrument IsDeleted=""False"" ID=""89611ae3-8f72-4a93-b038-b1fe7aa8fb98"">
        <ResetItemHistory />
      </Instrument>
      <Instrument IsDeleted=""False"" ID=""cc5f2be9-8746-4b10-b20c-7e0b8691de22"">
        <ResetItemHistory />
      </Instrument>
    </Instruments>
    <ResetOrders />
    <Balances />
    <Bills>
      <Bill IsDeleted=""False"" ID=""e125bbfb-3d52-42bc-bd51-ecd94e848fac"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.0000000000000"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-12 05:14:59"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""9b75a99a-2ed0-4952-8d1c-fbb1372fe568"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.0000000000000"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 05:14:59"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""ce59afdc-a635-4106-b7f2-71fbc21e416f"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:50:02"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""9b759b59-0c4e-45d0-8913-0191cecd11bc"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:50:09"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""b64f0386-cd7c-435d-b6f1-38283c94bd4a"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:50:36"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""a4531642-683a-443c-9376-de441430d7f1"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:50:44"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""df961241-a063-4246-8420-e5e91c03cce5"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:51:46"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""c03dcf9d-ab26-49ab-bf11-8aa1694137a6"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""-99.72"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:52:10"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""28f53c83-f66d-4f70-979a-37a10deea3f3"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:52:56"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""3e4d417f-89c0-4f42-8b2d-71e777d7bc9d"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""70b9e1b3-63aa-44fb-afe4-479238b184c0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""3877.10"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 13:54:06"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""21807f0e-fc48-4a01-838f-9464a176b881"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 14:17:21"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""4f0f4cfb-f202-484e-a7af-ffb3c3519506"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 14:38:26"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
      <Bill IsDeleted=""False"" ID=""ed6ed9f5-0c9e-45cd-a79b-2d4854d43fa0"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" Value=""0.00"" Type=""44"" OwnerType=""3"" UpdateTime=""2016-10-13 14:39:37"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" />
    </Bills>
  </Account>
</Accounts>";
        }

        private string GetPlacingXML()
        {
            return @"<Account ID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" CurrencyID=""3a3eab93-b73a-4366-a8b8-172f708c197e"" IsMultiCurrency=""False"">
  <Transactions>
    <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""a40530b2-10ec-4b5b-8f72-d978e515e31a"" Code=""WFB161013LM00032"" Type=""2"" AccountID=""a995354e-ed6f-4bc9-966d-c5b8fda91dec"" InstrumentID=""b5c8eefb-66f2-423b-b287-428b655392d3"" SubType=""0"" Phase=""255"" OrderType=""1"" ContractSize=""0"" BeginTime=""2016-10-13 15:08:04"" EndTime=""2016-10-14 05:14:59"" ExpireType=""3"" SubmitTime=""2016-10-13 15:08:04"" SubmitorID=""8cda653d-c92d-42a8-8850-f5f0d542e55a"" InstrumentCategory=""10"" PlacePhase=""1"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-10-13 15:08:04"">
      <Orders>
        <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""f76218b7-4a6d-411d-a2ef-dfa0484ac842"" IsOpen=""True"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300125"" BlotterCode=""Q"" SetPrice=""1261.4"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""2"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
          <Bills />
          <OrderRelations />
        </Order>
        <Order IsDeleted=""False"" HitCount=""0"" HitStatus=""0"" ID=""d1a2d96c-8ec2-42b0-bea2-6f95405ea588"" IsOpen=""True"" IsBuy=""False"" Lot=""1"" OriginalLot=""1"" LotBalance=""1"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""WFB2016101300126"" BlotterCode=""Q"" SetPrice=""1257.2"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""1"" CancelType=""0"" IsAutoFill=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
          <Bills />
          <OrderRelations />
        </Order>
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
        }

    }
}
