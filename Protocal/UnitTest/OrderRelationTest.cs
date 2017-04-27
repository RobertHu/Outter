#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml.Linq;
using Protocal.Commands;

namespace Protocal.UnitTest
{
    [TestFixture]
    public class OrderRelationTest
    {
        [TestFixtureSetUp]
        public void Init()
        {
            string initData = @"<Accounts>
  <Account IsDeleted=""False"" AlertLevel=""0"" CutAlertLevel=""0"" ID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" CustomerName=""phh041"" IsMultiCurrency=""False"" MinUpkeepEquity=""4896.90"" Type=""0"" CurrencyCode=""USD"" Code=""phh041"" TradeDay=""2016-06-27 00:00:00"" CreditAmount=""0.0000"" TotalPaidAmount=""1568.88"" TotalDeposit=""0"" Balance=""77067.49"" FrozenFund=""0"" Necessary=""32646.00"" NetNecessary=""32646.00"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""24484.50"" MinEquityAvoidRiskLevel2=""16323.00"" MinEquityAvoidRiskLevel3=""4896.90"" NecessaryFillingOpenOrder=""32646.00"" NecessaryFillingCloseOrder=""32646.00"" TradePLFloat=""4.52"" InterestPLFloat=""0.00"" StoragePLFloat=""-0.08"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""3903.15"" RiskCredit=""0.00"" PartialPaymentPhysicalNecessary=""32646.00"" Equity=""78640.81"">
    <Funds>
      <Fund IsDeleted=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" CurrencyCode=""USD"" TotalPaidAmount=""522.9600000000000"" TotalDeposit=""0"" Balance=""77067.4905000000000"" FrozenFund=""0.0000000000000"" Necessary=""32646.00"" NetNecessary=""32646.0000000000"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""24484.50"" MinEquityAvoidRiskLevel2=""16323.00"" MinEquityAvoidRiskLevel3=""4896.90"" NecessaryFillingOpenOrder=""32646.00"" NecessaryFillingCloseOrder=""32646.00"" TradePLFloat=""4.52"" InterestPLFloat=""0.00"" StoragePLFloat=""-0.08"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""3903.1460000000000000"" RiskCredit=""0.00000000"" PartialPaymentPhysicalNecessary=""32646.0000000000"" />
    </Funds>
    <Transactions>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""ff63b612-1a6f-487c-9364-0c8aaeae1d28"" Code=""DEM160607SP00142"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:19:26"" EndTime=""2016-06-07 15:19:56"" ExpireType=""3"" SubmitTime=""2016-06-07 15:19:26"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:19:26"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:42"">
        <Orders>
          <Order IsDeleted=""False"" ID=""de3aff0b-2924-4ada-b625-0cd2856d6be9"" IsOpen=""True"" IsBuy=""True"" Lot=""3.0000"" OriginalLot=""3.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700286"" BlotterCode="""" ExecutePrice=""1522.00"" SetPrice=""1522.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""1"" PhysicalOriginValue=""45660.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""2"" Frequence=""3"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""1"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""53cc21ba-6568-4ec3-ba2e-2f7a3689b711"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-300.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""738e589d-dd35-4a40-beed-583b23209b54"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""bad69fdf-159e-44fc-a0d1-761f306627c6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-300.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""8648e9c2-c966-4ebb-af71-7c59d3d5a0f3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-13698.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7d498ad3-80af-4639-8c77-f7b112f21ee4"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""de3aff0b-2924-4ada-b625-0cd2856d6be9"" Period=""1"" Principal=""15981.0000000000000"" Interest=""122.9300000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2017-06-07 15:19:26"" UpdateTime=""2016-06-07 15:19:26"" InterestRate=""0.1000000000000"" />
              <InstalmentDetail IsDeleted=""False"" OrderId=""de3aff0b-2924-4ada-b625-0cd2856d6be9"" Period=""2"" Principal=""15981.0000000000000"" Interest=""61.4700000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2018-06-07 15:19:26"" UpdateTime=""2016-06-07 15:19:26"" InterestRate=""0.1000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""523fd9a0-6ca1-48a4-9dd7-12c9b9308bed"" Code=""DEM160607SP00140"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:18:37"" EndTime=""2016-06-07 15:19:07"" ExpireType=""3"" SubmitTime=""2016-06-07 15:18:37"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:18:37"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:42"">
        <Orders>
          <Order IsDeleted=""False"" ID=""f3307243-e886-421a-bcc9-4f614ba85823"" IsOpen=""True"" IsBuy=""True"" Lot=""1.0000"" OriginalLot=""1.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700283"" BlotterCode="""" ExecutePrice=""1522.00"" SetPrice=""1522.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""15220.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""dc2bd899-5d07-4ae2-9b69-2141f29dcb10"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2d54e830-e983-44b9-87d5-b89458cd5044"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-100.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""91b34432-0c29-4baf-8548-cc5ee4c15fca"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-100.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""884daf19-a61a-4905-9e75-de222bf546b4"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-15220.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""73ea8402-0c71-4e8a-9c81-196398c010b4"" Code=""DEM160610SP00100"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-10 11:40:52"" EndTime=""2016-06-10 11:41:22"" ExpireType=""3"" SubmitTime=""2016-06-10 11:40:52"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-10 11:40:52"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:42"">
        <Orders>
          <Order IsDeleted=""False"" ID=""d4c6b4ff-7232-46de-9e95-34f45b4ff679"" IsOpen=""True"" IsBuy=""True"" Lot=""0.5000"" OriginalLot=""0.5000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016061000103"" BlotterCode="""" ExecutePrice=""1243.2"" SetPrice=""1242.1"" InterestValueDate=""2016-06-10 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""1"" PhysicalOriginValue=""621.6000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""2"" Frequence=""0"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""0.6000"" PhysicalInstalmentType=""1"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""0be321db-8567-45a0-af07-3485cd06b6b3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b62fd4a2-571f-439d-aedf-556bb6b41b87"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-5.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""978e8528-3471-4d81-a34c-699e0af0eee0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-522.9600000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""90f38ae9-fa68-4cae-aab8-854e8a53a915"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a546b5e7-d8e6-4690-aae8-88bc98828375"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-12432.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""413a906f-4201-4bd9-93fc-9b5bd1b9e8a2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""522.9600000000000"" Type=""28"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""9d81fb30-d262-40ef-9ef6-b637692cc8ca"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""150.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""0fc8f3fe-b45c-4ad4-9ca8-1ae090cfa088"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-6.75"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""d4c6b4ff-7232-46de-9e95-34f45b4ff679"" Period=""1"" Principal=""49.3200000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2016-07-11 11:40:52"" PaidDateTime=""2016-06-27 08:39:26"" UpdateTime=""2016-06-27 08:39:26"" InterestRate=""0.0000000000000"" />
              <InstalmentDetail IsDeleted=""False"" OrderId=""d4c6b4ff-7232-46de-9e95-34f45b4ff679"" Period=""2"" Principal=""49.3200000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2016-08-10 11:40:52"" PaidDateTime=""2016-06-27 08:39:26"" UpdateTime=""2016-06-27 08:39:26"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""d20c14e5-4722-4470-9a91-1b60a395f5be"" Code=""DEM160627SP00005"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""33f7ade0-d4d7-4cec-8a0d-63f3d5b95ae8"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""50.0000"" BeginTime=""2016-06-27 08:40:36"" EndTime=""2016-06-27 08:41:06"" ExpireType=""3"" SubmitTime=""2016-06-27 08:40:36"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-27 08:40:36"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:43"">
        <Orders>
          <Order IsDeleted=""False"" ID=""cab9df0a-09a8-47f6-acbe-119809b7e04e"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0100"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062700005"" ExecutePrice=""59.305"" SetPrice=""59.305"" InterestValueDate=""2016-06-27 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""9.8800000000000"" PhysicalOriginValueBalance=""9.8800000000000"" PaidPledgeBalance=""-2.3000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""96368bbb-ac9d-4a06-8539-4a9b990fce26"" InterestPLFloat=""0"" StoragePLFloat=""0"" TradePLFloat=""0"" LivePrice=""59.305"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""ea209fcf-70e4-42a3-8182-2bfb98f4a474"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-2.3000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""74755747-8301-4851-9fa6-57e12c0fcef4"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""bf3371e3-c4f3-4214-83c2-6f75680c3a21"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""1536a0b8-26e5-4f71-939b-c0bdcbc4bd96"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.1000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a65c0c4b-3fb5-49d5-8206-cef7cae14fa5"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-197.6800000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""cab9df0a-09a8-47f6-acbe-119809b7e04e"" Period=""1"" Principal=""7.5800000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-27 08:40:36"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""337f0afe-27ff-4325-9776-1faccf873038"" Code=""DEM160623SP00042"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""33f7ade0-d4d7-4cec-8a0d-63f3d5b95ae8"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""50.0000"" BeginTime=""2016-06-23 13:46:07"" EndTime=""2016-06-23 13:46:37"" ExpireType=""3"" SubmitTime=""2016-06-23 13:46:07"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:46:07"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:43"">
        <Orders>
          <Order IsDeleted=""False"" ID=""528c3512-e277-463a-b61b-df9b69190f4a"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0100"" InterestPerLot=""0.0000000000000"" StoragePerLot=""-12.0000000000000"" Code=""DEM2016062300061"" ExecutePrice=""59.306"" SetPrice=""59.306"" InterestValueDate=""2016-06-23 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""9.8800000000000"" PhysicalOriginValueBalance=""9.8800000000000"" PaidPledgeBalance=""-2.3000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""96368bbb-ac9d-4a06-8539-4a9b990fce26"" InterestPLFloat=""0"" StoragePLFloat=""-0.04"" TradePLFloat=""0"" LivePrice=""59.305"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""aed9aee0-45a0-4ef9-a815-0537eaa8e403"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-2.3000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""6f476eb4-ff7f-489e-ad83-916f1d28a61b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b143debc-7707-4ef0-ad97-a49003f8a36a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.1000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7b3772e5-4959-4574-955e-ef3d4a88bf8b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b17bf850-01da-4c0c-8300-f1a8583f5adc"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-197.6900000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""528c3512-e277-463a-b61b-df9b69190f4a"" Period=""1"" Principal=""7.5800000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:46:07"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""9a0ff91b-5d07-4087-afa1-5c4e44e94e17"" Code=""DEM160610SP00096"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-10 11:36:36"" EndTime=""2016-06-10 11:37:06"" ExpireType=""3"" SubmitTime=""2016-06-10 11:36:36"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-10 11:36:36"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:45"">
        <Orders>
          <Order IsDeleted=""False"" ID=""7f99395b-b2f0-47c0-a394-19782bc0c241"" IsOpen=""False"" IsBuy=""False"" Lot=""1.0200"" OriginalLot=""1.0200"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016061000099"" BlotterCode="""" ExecutePrice=""1524.00"" SetPrice=""1524.00"" InterestValueDate=""2016-06-12 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""15544.8000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""4bc194b6-fa95-4426-8f5a-2b1740e2d3d5"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""8"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""ee10291d-3d25-443b-9f90-4bc894d2dfd3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""45.6000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b74315a0-9f15-456f-a901-4c687a5f40f4"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""45.6900000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""d5f0af6b-b859-4ce2-8efd-67457b340f4e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""10"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""3036a6bc-5837-42fb-bbf3-72f3a8ce3702"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-15.3000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2231b5b9-35f0-4dbd-9b08-7f4fdbb65f79"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7675ae68-ec0e-4ab6-bda7-8283ba4f6aec"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""9"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7a054851-ba26-42fd-b94a-96927ea3458d"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""15220.0000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b916c205-7adc-46cb-b1f9-b29fb19cf01d"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1.0200000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""391dde94-70e7-4371-8d3f-d0391a5b14be"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c78d1ae0-f5eb-4e6f-bdf5-f7ef78922c88"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""20.5000000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" ValueTime=""2016-06-10 11:36:36"" IsValued=""True"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""c184a9c6-b3e0-4157-9a91-5312cdcbfde2"" OpenOrderID=""f3307243-e886-421a-bcc9-4f614ba85823"" CloseOrderID=""7f99395b-b2f0-47c0-a394-19782bc0c241"" ClosedLot=""1.0000"" CloseTime=""2016-06-10 11:36:36"" ValueTime=""2016-06-10 11:36:36"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""15.0000000000000"" Levy=""1.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""20.0000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""15220.0000000000000"" ClosedPhysicalValue=""15220.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
              <OrderRelation IsDeleted=""False"" ID=""d2ae4aa4-20b5-48dd-aa74-66611f9f7bfd"" OpenOrderID=""9f2a45a3-b69b-46c0-9266-30674c7a2a02"" CloseOrderID=""7f99395b-b2f0-47c0-a394-19782bc0c241"" ClosedLot=""0.0100"" CloseTime=""2016-06-10 11:36:36"" ValueTime=""2016-06-10 11:36:36"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""0.1500000000000"" Levy=""0.0100000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""0.3900000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""45.6000000000000"" ClosedPhysicalValue=""152.0100000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
              <OrderRelation IsDeleted=""False"" ID=""1f8bf16c-b625-4e0c-8273-0032c7a16376"" OpenOrderID=""ac0c0480-c9a9-4119-b575-5487b4066dbd"" CloseOrderID=""7f99395b-b2f0-47c0-a394-19782bc0c241"" ClosedLot=""0.0100"" CloseTime=""2016-06-10 11:36:36"" ValueTime=""2016-06-10 11:36:36"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""0.1500000000000"" Levy=""0.0100000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""0.1100000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""45.6900000000000"" ClosedPhysicalValue=""152.2900000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""a3a7b5c0-bbd3-4bfa-8fe5-5d3b74527364"" Code=""DEM160610SP00099"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-10 11:40:11"" EndTime=""2016-06-10 11:40:41"" ExpireType=""3"" SubmitTime=""2016-06-10 11:40:11"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-10 11:40:11"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:45"">
        <Orders>
          <Order IsDeleted=""False"" ID=""7e451cbe-25aa-43a0-bbe9-9715ed930886"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0200"" OriginalLot=""0.0200"" LotBalance=""0.0200"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016061000102"" BlotterCode="""" ExecutePrice=""1243.2"" SetPrice=""1242.1"" InterestValueDate=""2016-06-10 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""24.8600000000000"" PhysicalOriginValueBalance=""24.8600000000000"" PaidPledgeBalance=""-10.6000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.60"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""765b15fb-5a1c-416b-8e2d-471ff4875fb2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""6.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""3f3aa9be-079c-4442-a616-5484963426e8"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-10.6000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""ece5fd38-51ac-457e-b80a-a05866973a96"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-497.2800000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c0c03f01-dd20-4631-a731-e0835d9a12b1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c26643e9-d297-4611-b6c6-e55c1d07680e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""33839388-e84f-4a67-90b7-fbd3e9a3f2c4"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.2000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""17f12cf8-c749-4f8b-be48-e31d71201fe1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.27"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""7e451cbe-25aa-43a0-bbe9-9715ed930886"" Period=""1"" Principal=""14.2600000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-10 11:40:11"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""f630c0fc-249a-4c75-9ecc-65c36a03e11a"" Code=""DEM160607SP00150"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:32:58"" EndTime=""2016-06-07 15:33:28"" ExpireType=""3"" SubmitTime=""2016-06-07 15:32:58"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:32:58"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:45"">
        <Orders>
          <Order IsDeleted=""False"" ID=""9896f767-6ed4-4f9f-8dc5-43f462492533"" IsOpen=""True"" IsBuy=""True"" Lot=""2.0000"" OriginalLot=""2.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700296"" BlotterCode="""" ExecutePrice=""1520.00"" SetPrice=""1520.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""30400.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""b1e35dda-786a-4553-9cc0-10eed5ec1f86"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7fb5cc1c-846b-4c27-b4bc-66c2fcdf9bec"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-9120.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""4897e5d0-c10c-42cf-803a-b0e1efae56d6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-20.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""434ed14e-711c-4331-8c6b-be63965859e9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2e8b2434-2987-4a58-be23-c1bcb6082fea"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""9896f767-6ed4-4f9f-8dc5-43f462492533"" Period=""1"" Principal=""21280.0000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-07 15:32:58"" InterestRate=""0.5000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""3f457147-aac7-4ebc-8456-6d312db3042f"" Code=""DEM160623SP00042"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-23 13:55:51"" EndTime=""2016-06-23 13:56:21"" ExpireType=""3"" SubmitTime=""2016-06-23 13:55:51"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:55:51"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:45"">
        <Orders>
          <Order IsDeleted=""False"" ID=""6dbf9904-1e11-4f46-ab8f-eae975de4a79"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0600"" OriginalLot=""0.0600"" LotBalance=""0.0600"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062300064"" ExecutePrice=""1256.7"" SetPrice=""1257.8"" InterestValueDate=""2016-06-25 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""75.4000000000000"" PhysicalOriginValueBalance=""75.4000000000000"" PaidPledgeBalance=""-31.8000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.98"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""a84e42a4-9756-4342-9ca8-1b72a0a05445"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.6000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""15fceb04-0b81-4ea6-8612-2cd0db5c8bcd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-754.0200000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""00fa8bc6-6164-45c5-820d-2d8ae39747cd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""84e40267-670c-4dde-9253-9dfe19532716"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""18.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""304bd470-4f01-4cfb-8539-be947e0e6703"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-31.8000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""22a311ed-312a-4017-b230-dd79239a640e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c4443eb9-c2be-476e-9dd5-eff5eb9a66c9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.45"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""6dbf9904-1e11-4f46-ab8f-eae975de4a79"" Period=""1"" Principal=""43.6000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:55:51"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""802d7b57-c06d-49b3-a0bc-79a7801960bd"" Code=""DEM160607LM00025"" Type=""2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""1"" ContractSize=""10.0000"" BeginTime=""2016-06-07 16:49:09"" EndTime=""2016-06-08 03:03:00"" ExpireType=""0"" SubmitTime=""2016-06-07 16:49:09"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 16:49:20"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:46"">
        <Orders>
          <Order IsDeleted=""False"" ID=""ac0c0480-c9a9-4119-b575-5487b4066dbd"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700368"" BlotterCode="""" ExecutePrice=""1522.90"" SetPrice=""1522.90"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""2"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""152.2900000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""a1901980-bafe-4260-89ca-37a26d6c0c1a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-45.6900000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c106ea1a-e10a-4d5b-84e1-3b1e8d179257"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f1fc331d-4c94-464c-9d19-a277aeba645b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""d87d2207-c6c7-467e-a0a2-a90bd7a09ff3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.1000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a5969165-6e46-4bf8-8163-cec5f9e71b17"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.0100000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""ac0c0480-c9a9-4119-b575-5487b4066dbd"" Period=""1"" Principal=""106.6000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-07 16:49:20"" InterestRate=""0.5000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""114e2fc7-3520-46ba-9623-7ab28f2bd39b"" Code=""DEM160607SP00145"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:26:38"" EndTime=""2016-06-07 15:27:08"" ExpireType=""3"" SubmitTime=""2016-06-07 15:26:38"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:26:38"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:46"">
        <Orders>
          <Order IsDeleted=""False"" ID=""3ab243b7-e66e-4c8e-8654-070954f45d9f"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700290"" BlotterCode="""" ExecutePrice=""1520.00"" SetPrice=""1520.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""1"" PhysicalOriginValue=""152.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""2"" Frequence=""3"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3300"" PhysicalInstalmentType=""1"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""81ba47c4-0ea7-4553-92f9-0065f77b8971"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-50.1600000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""8845ada9-ec58-467c-a99d-2c81798c1377"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""dc976929-4f7b-4d14-a238-a90e2cf9dba3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""95694cf4-2fd6-45c8-9578-b913409a76b1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""24c00473-ba35-426b-b262-f6f3ab017079"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""3ab243b7-e66e-4c8e-8654-070954f45d9f"" Period=""1"" Principal=""50.9200000000000"" Interest=""0.3900000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2017-06-07 15:26:38"" UpdateTime=""2016-06-07 15:26:38"" InterestRate=""0.1000000000000"" />
              <InstalmentDetail IsDeleted=""False"" OrderId=""3ab243b7-e66e-4c8e-8654-070954f45d9f"" Period=""2"" Principal=""50.9200000000000"" Interest=""0.2000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""2018-06-07 15:26:38"" UpdateTime=""2016-06-07 15:26:38"" InterestRate=""0.1000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""3050e9ad-9368-4f44-97c7-8100eac9780a"" Code=""DEM160607SP00141"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:18:50"" EndTime=""2016-06-07 15:19:20"" ExpireType=""3"" SubmitTime=""2016-06-07 15:18:50"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:18:50"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:46"">
        <Orders>
          <Order IsDeleted=""False"" ID=""8cfe4725-b9af-449e-b195-8080e57585ce"" IsOpen=""True"" IsBuy=""True"" Lot=""2.0000"" OriginalLot=""2.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700285"" BlotterCode="""" ExecutePrice=""1522.00"" SetPrice=""1522.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""30440.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""67a39af8-7a96-4033-b24c-2b59d7007b45"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a4275cff-88c4-49bc-b910-69cc314c6309"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-9132.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""3c9f3793-9a6c-4b8b-9843-9100cf02b501"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-20.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""1f4e6d0e-2e0e-4dad-9df3-b7a066c64ba9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f3f20c24-c821-4d59-a477-e91d3c4b509f"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""8cfe4725-b9af-449e-b195-8080e57585ce"" Period=""1"" Principal=""21308.0000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-07 15:18:50"" InterestRate=""0.5000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""c5080ab3-34e7-4ab5-8ec5-8e88785784d2"" Code=""DEM160623SP00037"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-23 13:48:32"" EndTime=""2016-06-23 13:49:02"" ExpireType=""3"" SubmitTime=""2016-06-23 13:48:32"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:48:32"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:46"">
        <Orders>
          <Order IsDeleted=""False"" ID=""b96b049a-fc65-4fd0-841a-8440929517cd"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0600"" OriginalLot=""0.0600"" LotBalance=""0.0600"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062300057"" ExecutePrice=""1256.7"" SetPrice=""1257.8"" InterestValueDate=""2016-06-25 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""75.4000000000000"" PhysicalOriginValueBalance=""75.4000000000000"" PaidPledgeBalance=""-31.8000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.98"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""e8267931-87eb-42e2-9339-0d8a61d0b539"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-754.0200000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""994c3fca-c63d-41d7-aac1-925c6d65c320"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""cecf69ea-b5f4-4bee-8d48-9355a9d395e7"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.6000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""dd9ff1ac-eec4-4d2a-a808-ba02a828927d"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-31.8000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""8f722fec-3179-4e50-9fdd-f1bfec942d3a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""18.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""4a20c624-a1a3-4e8b-a834-fe48cb370b07"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""34d57807-ffdb-4557-8f1e-c642ac9e0c86"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.45"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""b96b049a-fc65-4fd0-841a-8440929517cd"" Period=""1"" Principal=""43.6000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:48:32"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""e47292b7-1ed7-49b3-801d-91ab2e5a38ec"" Code=""DEM160607SP00143"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:24:49"" EndTime=""2016-06-07 15:25:19"" ExpireType=""3"" SubmitTime=""2016-06-07 15:24:49"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 15:24:49"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:46"">
        <Orders>
          <Order IsDeleted=""False"" ID=""18f2b6c7-c476-4e40-9c95-5bc42fb44985"" IsOpen=""True"" IsBuy=""True"" Lot=""2.0000"" OriginalLot=""2.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700288"" BlotterCode="""" ExecutePrice=""1520.00"" SetPrice=""1520.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""30400.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""55edb6f9-2d11-4919-88e2-4636903312f1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-20.0000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2d8b5a03-2b64-4d83-a765-468f062735b5"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-9120.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""25abf3c9-60d3-48e7-97e0-5e860c77bdf2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""d20986e7-8b50-41f4-85ab-a76a6892575b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-200.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""3e47177d-83b5-48ec-a3bf-e196b25f5c34"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""18f2b6c7-c476-4e40-9c95-5bc42fb44985"" Period=""1"" Principal=""21280.0000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-07 15:24:49"" InterestRate=""0.5000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""a826ce34-19b6-4554-af14-991facbfe788"" Code=""DEM160627SP00003"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-27 08:39:31"" EndTime=""2016-06-27 08:40:01"" ExpireType=""3"" SubmitTime=""2016-06-27 08:39:31"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-27 08:39:31"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:47"">
        <Orders>
          <Order IsDeleted=""False"" ID=""ffa8747f-ebfc-4587-bfeb-b3f07ba65480"" IsOpen=""False"" IsBuy=""False"" Lot=""0.5000"" OriginalLot=""0.5000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062700003"" ExecutePrice=""1259.3"" SetPrice=""1258.2"" InterestValueDate=""2016-06-30 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""629.6500000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""4a15b5c8-90cf-44dc-94fd-1125222d7b0a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""8"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b8a6851e-3921-42aa-9073-1786af89d491"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""9643440f-4d7b-4cb7-a219-20865cfc6f74"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""ef2c9cf7-0098-4afd-818f-265f910233db"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""10"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""655fb4d6-7bd1-4185-b5fe-47905043af6e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b9cb5f2d-b1e9-4145-9679-ac8b0f70c0c9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""8.0500000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" ValueTime=""2016-06-27 08:39:31"" IsValued=""True"" />
              <Bill IsDeleted=""False"" ID=""eadc1a5e-a680-4963-a2b9-b66edd7b4448"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""9"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""16fec384-3f3e-470a-ba12-c0b22f3010fb"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""621.6000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""358b2212-18a1-4023-8541-e8dfa3907d0e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-12593.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""3493d127-3634-4b2c-a5d5-848337d62de2"" OpenOrderID=""d4c6b4ff-7232-46de-9e95-34f45b4ff679"" CloseOrderID=""ffa8747f-ebfc-4587-bfeb-b3f07ba65480"" ClosedLot=""0.5000"" CloseTime=""2016-06-27 08:39:31"" ValueTime=""2016-06-27 08:39:31"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""12593.0000000000000"" Levy=""0.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""8.0500000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""621.6000000000000"" ClosedPhysicalValue=""621.6000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""f6015e6b-2b45-4908-bcd1-a267f619a647"" Code=""DEM160607RS00126"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""6"" ContractSize=""10.0000"" BeginTime=""2016-06-07 15:21:29"" EndTime=""2016-06-07 15:36:29"" ExpireType=""0"" SubmitTime=""2016-06-07 15:21:29"" SubmitorID=""00000000-0000-0000-0000-000000000000"" ExecuteTime=""2016-06-07 15:21:29"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:47"">
        <Orders>
          <Order IsDeleted=""False"" ID=""5ee1e4a8-5577-4a74-8500-55d6c0dab7de"" IsOpen=""False"" IsBuy=""False"" Lot=""5.0000"" OriginalLot=""5.0000"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700287"" ExecutePrice=""1520.00"" SetPrice=""1520.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""76000.0000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""b52ed1fb-4843-4b1c-be64-2a9b96028ecf"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-100.0000000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" ValueTime=""2016-06-07 15:21:29"" IsValued=""True"" />
              <Bill IsDeleted=""False"" ID=""6befeadc-694c-41ab-be7d-33ae4f1c2158"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-60.0000000000000"" Type=""9"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""d1bc5d09-496e-4c9d-b8a2-4d60db671a8c"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2df2f94b-1019-4aff-9010-5476c1b7fa92"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""8"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""021d9652-2547-4de5-88d4-63263728e8fe"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""834ce8f4-41a3-4653-a851-772b41ca7935"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""10"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""cb416cb2-32e3-4508-9516-96b7d12b8146"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""9132.0000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""514eecb9-22c6-4cac-afdd-a4e8439b9acd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""13698.0000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""42c3dc68-a1d8-40b4-b3af-bfd57ab69cf9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-5.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""75c12a85-da78-4683-8411-f0c9c75cf23a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-50.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""b7ca44c4-116d-4307-8cef-69744234ccd0"" OpenOrderID=""de3aff0b-2924-4ada-b625-0cd2856d6be9"" CloseOrderID=""5ee1e4a8-5577-4a74-8500-55d6c0dab7de"" ClosedLot=""3.0000"" CloseTime=""2016-06-07 15:21:29"" ValueTime=""2016-06-07 15:21:29"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""30.0000000000000"" Levy=""3.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""-60.0000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""-60.0000000000000"" PayBackPledge=""13698.0000000000000"" ClosedPhysicalValue=""45660.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
              <OrderRelation IsDeleted=""False"" ID=""a6267f1f-b88a-46b8-82f3-7affc490250d"" OpenOrderID=""8cfe4725-b9af-449e-b195-8080e57585ce"" CloseOrderID=""5ee1e4a8-5577-4a74-8500-55d6c0dab7de"" ClosedLot=""2.0000"" CloseTime=""2016-06-07 15:21:29"" ValueTime=""2016-06-07 15:21:29"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""20.0000000000000"" Levy=""2.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""-40.0000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""9132.0000000000000"" ClosedPhysicalValue=""30440.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""b368a6ae-daa3-432d-8644-a79ecdb49ddc"" Code=""DEM160623SP00041"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-23 13:45:59"" EndTime=""2016-06-23 13:46:29"" ExpireType=""3"" SubmitTime=""2016-06-23 13:45:59"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:46:00"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:47"">
        <Orders>
          <Order IsDeleted=""False"" ID=""1e41cfda-3a23-4876-a17a-6516a5e74ce4"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0600"" OriginalLot=""0.0600"" LotBalance=""0.0600"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062300060"" ExecutePrice=""1256.7"" SetPrice=""1257.8"" InterestValueDate=""2016-06-25 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""75.4000000000000"" PhysicalOriginValueBalance=""75.4000000000000"" PaidPledgeBalance=""-31.8000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.98"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""185d6578-1a68-4324-870d-3e42dfbdac25"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.6000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""d8a11999-3c39-4f70-a430-56eba317efcd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2468c75d-3ad6-4567-8a97-5a933fc04caf"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-754.0200000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a75ea956-dd38-4b1d-b98b-8ecb7291d0b3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""18.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""1b4a0273-7dca-47ff-9a14-b54101e7acf0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""9e400db3-aae2-4757-bc99-c003a6dedf6a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-31.8000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""ac8cb5db-b3e7-4616-bdf0-c8ed0b98ded7"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.45"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""1e41cfda-3a23-4876-a17a-6516a5e74ce4"" Period=""1"" Principal=""43.6000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:46:00"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""c775fe31-aaf6-437e-8936-b64364e082b3"" Code=""DEM160607LM00024"" Type=""2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""1"" ContractSize=""10.0000"" BeginTime=""2016-06-07 16:42:20"" EndTime=""2016-06-08 03:03:00"" ExpireType=""0"" SubmitTime=""2016-06-07 16:42:20"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-07 16:48:51"" ApproverID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:47"">
        <Orders>
          <Order IsDeleted=""False"" ID=""9f2a45a3-b69b-46c0-9266-30674c7a2a02"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700333"" BlotterCode="""" ExecutePrice=""1520.10"" SetPrice=""1520.10"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""1"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""152.0100000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""9bc94775-7490-49ff-b5ae-818e8edf69c4"" DownPayment=""0.3000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""0"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""6b4a3d2a-3d8d-44d5-a234-017f09c81692"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""477ea239-5e90-4317-bc9e-5af4561c22dc"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-45.6000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a9d05c9c-7ebc-4b18-a19e-b0995fcbb24a"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.0100000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""cb830914-ad54-4bd3-875f-c26a0f0ae4f6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.1000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""5d4960f1-1e52-4b5f-a9db-cded1b20cdd5"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""9f2a45a3-b69b-46c0-9266-30674c7a2a02"" Period=""1"" Principal=""106.4100000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-07 16:48:51"" InterestRate=""0.5000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""a6feb362-4af8-4e35-b2a7-d83fd8646a2b"" Code=""DEM160623SP00038"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""33f7ade0-d4d7-4cec-8a0d-63f3d5b95ae8"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""50.0000"" BeginTime=""2016-06-23 13:48:39"" EndTime=""2016-06-23 13:49:09"" ExpireType=""3"" SubmitTime=""2016-06-23 13:48:39"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:48:39"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:48"">
        <Orders>
          <Order IsDeleted=""False"" ID=""5adc116b-364f-4a27-81c9-021c9da3849a"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0100"" OriginalLot=""0.0100"" LotBalance=""0.0100"" InterestPerLot=""0.0000000000000"" StoragePerLot=""-12.0000000000000"" Code=""DEM2016062300058"" ExecutePrice=""59.306"" SetPrice=""59.306"" InterestValueDate=""2016-06-23 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""9.8800000000000"" PhysicalOriginValueBalance=""9.8800000000000"" PaidPledgeBalance=""-2.3000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""96368bbb-ac9d-4a06-8539-4a9b990fce26"" InterestPLFloat=""0"" StoragePLFloat=""-0.04"" TradePLFloat=""0"" LivePrice=""59.305"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""a66a586d-b79c-48d6-b115-0abe18477388"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.1000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""ac530b92-e52d-42e9-a1e9-1a3ce98984f6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-197.6900000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""7e3ace37-3912-41b9-8314-2ef5e94b8ef1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""5f3baf00-df93-4645-8eff-3e58996de6ca"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-2.3000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""06be9764-0938-4f45-8dcb-726885ed6b38"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""5adc116b-364f-4a27-81c9-021c9da3849a"" Period=""1"" Principal=""7.5800000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:48:39"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""7b1a4446-c6b9-435b-b9cf-e52fb094e9a3"" Code=""DEM160624SP00038"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-24 14:22:23"" EndTime=""2016-06-24 14:22:53"" ExpireType=""3"" SubmitTime=""2016-06-24 14:22:23"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-24 14:22:23"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:49"">
        <Orders>
          <Order IsDeleted=""False"" ID=""3b5105e2-9ed3-40a1-b026-5fbb57025a85"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0600"" OriginalLot=""0.0600"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062400107"" ExecutePrice=""1256.5"" SetPrice=""1257.6"" InterestValueDate=""2016-06-26 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""75.3900000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.99"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""eb0b0471-58cf-4e03-a6a5-28f2dcfd7b00"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f5bcc7c0-11b4-423e-a86c-4a6ea3be31f0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.6000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""01ab0522-f427-42d0-a35a-aa49d063af60"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""467ccdf5-2e9b-40db-9c29-bf03573629c3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-753.9000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""4dcf9e3a-ca37-437f-86f0-d8b6cc118bfb"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""18.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""e2a41c00-c74c-47d1-bcff-fcd5266100ee"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-31.8000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""3b5105e2-9ed3-40a1-b026-5fbb57025a85"" Period=""1"" Principal=""43.5900000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-24 14:22:23"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""28105bc7-a025-43a9-829a-e60071333f2c"" Code=""DEM160607RS00128"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"" SubType=""0"" Phase=""2"" OrderType=""6"" ContractSize=""10.0000"" BeginTime=""2016-06-07 16:00:08"" EndTime=""2016-06-07 16:15:08"" ExpireType=""0"" SubmitTime=""2016-06-07 16:00:08"" SubmitorID=""00000000-0000-0000-0000-000000000000"" ExecuteTime=""2016-06-07 16:00:08"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:49"">
        <Orders>
          <Order IsDeleted=""False"" ID=""b3abbc6a-ce3c-462a-acab-e3ba73e43716"" IsOpen=""False"" IsBuy=""False"" Lot=""4.0100"" OriginalLot=""4.0100"" LotBalance=""0.0000"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016060700299"" ExecutePrice=""1522.00"" SetPrice=""1522.00"" InterestValueDate=""2016-06-07 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""61032.2000000000000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""69a56d1c-fe5a-4efb-aadc-00ee62b1f66c"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f4e43f19-b996-4aaa-ad04-0c440e21fc68"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""10"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f35c1e58-4d68-47c9-a96f-2f299c66d4fd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""9120.0000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""a641aca7-45ab-4eab-b5f1-42a36ffc3fa3"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.2000000000000"" Type=""9"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""8578d85e-245e-42bb-89ef-5c4bea589a63"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""50.1600000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""2de7a7cd-b1dd-4f19-97af-6893c8a019f8"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""80.2000000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" ValueTime=""2016-06-07 16:00:08"" IsValued=""True"" />
              <Bill IsDeleted=""False"" ID=""1010466d-b9c9-444b-b1e1-8e1170cacc90"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-4.0100000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""893da6a3-d923-4985-85b3-91a86517a39e"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""9120.0000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""6c265944-c6a5-40db-9681-c0bd59486e75"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""8"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""b6b56266-e75e-4d04-9567-e4baaadf264c"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-40.1000000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""0bce8e2d-07b0-4eab-b481-eec083f99e98"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""faf1273e-1d4e-477b-80d0-040179e4a327"" OpenOrderID=""18f2b6c7-c476-4e40-9c95-5bc42fb44985"" CloseOrderID=""b3abbc6a-ce3c-462a-acab-e3ba73e43716"" ClosedLot=""2.0000"" CloseTime=""2016-06-07 16:00:08"" ValueTime=""2016-06-07 16:00:08"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""20.0000000000000"" Levy=""2.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""40.0000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""9120.0000000000000"" ClosedPhysicalValue=""30400.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
              <OrderRelation IsDeleted=""False"" ID=""52e14661-0e69-455e-8141-09eed2b787fc"" OpenOrderID=""9896f767-6ed4-4f9f-8dc5-43f462492533"" CloseOrderID=""b3abbc6a-ce3c-462a-acab-e3ba73e43716"" ClosedLot=""2.0000"" CloseTime=""2016-06-07 16:00:08"" ValueTime=""2016-06-07 16:00:08"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""20.0000000000000"" Levy=""2.0000000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""40.0000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""0.0000000000000"" PayBackPledge=""9120.0000000000000"" ClosedPhysicalValue=""30400.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
              <OrderRelation IsDeleted=""False"" ID=""7b9db1fb-e9d1-4c7e-a6cb-33fb08c08a8a"" OpenOrderID=""3ab243b7-e66e-4c8e-8654-070954f45d9f"" CloseOrderID=""b3abbc6a-ce3c-462a-acab-e3ba73e43716"" ClosedLot=""0.0100"" CloseTime=""2016-06-07 16:00:08"" ValueTime=""2016-06-07 16:00:08"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""0.1000000000000"" Levy=""0.0100000000000"" OtherFee=""0.0000000000000"" InterestPL=""0.0000000000000"" StoragePL=""0.0000000000000"" TradePL=""0.2000000000000"" OverdueCutPenalty=""0.0000000000000"" ClosePenalty=""-0.2000000000000"" PayBackPledge=""50.1600000000000"" ClosedPhysicalValue=""152.0000000000000"" physicalValue=""0.0000000000000"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""307ed375-f0fc-4dc7-a3e9-f2101c6d91cf"" Code=""DEM160623SP00040"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-23 13:52:04"" EndTime=""2016-06-23 13:52:34"" ExpireType=""3"" SubmitTime=""2016-06-23 13:52:04"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-23 13:52:04"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""0"" PlaceDetail="""" AppType=""0"" UpdateTime=""2016-06-27 14:59:49"">
        <Orders>
          <Order IsDeleted=""False"" ID=""c44f26a7-9fa4-4354-9808-a987119ac15d"" IsOpen=""True"" IsBuy=""True"" Lot=""0.0600"" OriginalLot=""0.0600"" LotBalance=""0.0600"" InterestPerLot=""0.0000000000000"" StoragePerLot=""0.0000000000000"" Code=""DEM2016062300061"" ExecutePrice=""1256.7"" SetPrice=""1257.8"" InterestValueDate=""2016-06-25 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""1"" PhysicalValueMatureDay=""0"" PhysicalType=""2"" PhysicalOriginValue=""75.4000000000000"" PhysicalOriginValueBalance=""75.4000000000000"" PaidPledgeBalance=""-31.8000000000000"" PaidPledge=""0"" DeliveryLockLot=""0"" Period=""1"" Frequence=""-1"" InstalmentPolicyId=""c3528fdd-8625-4ff0-b060-b30c6c6e343f"" DownPayment=""230.0000"" PhysicalInstalmentType=""2"" RecalculateRateType=""1"" DownPaymentBasis=""1"" IsInstalmentOverdue=""False"" InstalmentOverdueDay=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPLFloat=""0.00"" StoragePLFloat=""0.00"" TradePLFloat=""0.98"" LivePrice=""1273.0"" MarketValue=""0"" ValueAsMargin=""0"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""eb35768e-dd8c-4ae7-ab73-093f1e862727"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-754.0200000000000"" Type=""1"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""0a8031f5-ccdf-451e-b531-4550c833b636"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-31.8000000000000"" Type=""6"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""fdad7c46-a896-47fb-9830-715018a60872"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""2"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""f4eca1cb-e4b4-45ef-a66e-d21b2f52c688"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0.0000000000000"" Type=""40"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""91113060-582d-4228-9c7e-dc15397d664b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.6000000000000"" Type=""11"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""c90c7245-69b1-4017-b691-e404324217e6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""18.0000000000000"" Type=""37"" OwnerType=""1"" UpdateTime=""0001-01-01 00:00:00"" />
              <Bill IsDeleted=""False"" ID=""de1a01f8-770f-4ab0-b761-2e4b19e40f31"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-0.45"" Type=""22"" OwnerType=""1"" UpdateTime=""2016-06-27 14:59:59"" IsValued=""False"" />
            </Bills>
            <InstalmentDetails>
              <InstalmentDetail IsDeleted=""False"" OrderId=""c44f26a7-9fa4-4354-9808-a987119ac15d"" Period=""1"" Principal=""43.6000000000000"" Interest=""0.0000000000000"" DebitInterest=""0.0000000000000"" PaymentDateTimeOnPlan=""9999-12-31 00:00:00"" UpdateTime=""2016-06-23 13:52:04"" InterestRate=""0.0000000000000"" />
            </InstalmentDetails>
            <OrderRelations />
          </Order>
        </Orders>
      </Transaction>
      <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""4f419be9-89e5-4150-bd7b-670be6afc23a"" Code=""DEM160627SP00011"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""2"" OrderType=""0"" ContractSize=""1.0000"" BeginTime=""2016-06-27 15:02:49"" EndTime=""2016-06-27 15:03:19"" ExpireType=""3"" SubmitTime=""2016-06-27 15:02:49"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" ExecuteTime=""2016-06-27 15:02:50"" ApproverID=""00000000-0000-0000-0000-000000000000"" InstrumentCategory=""20"" PlacePhase=""8"" PlaceDetail="""" AppType=""18"" UpdateTime=""2016-06-27 15:02:50"">
        <Orders>
          <Order IsDeleted=""False"" ID=""8f554b3e-dbe8-48b5-9434-c8d90542fa9e"" IsOpen=""False"" IsBuy=""False"" Lot=""0.06"" OriginalLot=""0.06"" LotBalance=""0.00"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""DEM2016062700021"" ExecutePrice=""1274.1"" SetPrice=""1273.0"" JudgePrice=""1273.0"" JudgePriceTimestamp=""2016-06-27 10:14:53"" InterestValueDate=""2016-06-30 00:00:00"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""2"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""76.45"" PhysicalOriginValueBalance=""0"" PaidPledgeBalance=""0"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
            <Bills>
              <Bill IsDeleted=""False"" ID=""29756c50-6f64-4c5b-8423-0c8706e65547"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""10"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""d8cfa3f8-988d-46c7-a767-56859e8bcfcc"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""6"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""247d7205-b91f-4d8b-8686-0d0abe84e5b0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""9"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""436c37e3-1cfe-45dd-a4e0-3e39ad2adaa1"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""8"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""e531b81a-1b46-4ad6-8c99-b9aa8b8886d2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""31.8000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""b350b75c-f9f6-4c3e-b3ff-6c4022c0336d"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1528.92"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""22e9bf50-6b91-48af-a25f-d647816540b9"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""6b7ad168-475d-4479-8469-0a198ec89dd8"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" />
              <Bill IsDeleted=""False"" ID=""1e95b6cd-c5e8-4f83-a127-fb1fb6185ecd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""1.0600000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""2016-06-27 15:02:50"" ValueTime=""2016-06-27 15:02:50"" IsValued=""True"" />
            </Bills>
            <OrderRelations>
              <OrderRelation IsDeleted=""False"" ID=""83794ce2-8cd3-4f0a-8042-fa95d2eb8a1c"" OpenOrderID=""3b5105e2-9ed3-40a1-b026-5fbb57025a85"" CloseOrderID=""8f554b3e-dbe8-48b5-9434-c8d90542fa9e"" ClosedLot=""0.06"" CloseTime=""2016-06-27 15:02:50"" ValueTime=""2016-06-27 15:02:50"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""1528.92"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""1.0600000000000"" OverdueCutPenalty=""0"" ClosePenalty=""0"" PayBackPledge=""31.8000000000000"" ClosedPhysicalValue=""75.3900000000000"" physicalValue=""76.45"">
                <Bills />
              </OrderRelation>
            </OrderRelations>
          </Order>
        </Orders>
      </Transaction>
    </Transactions>
    <DeliveryRequests />
    <Instruments>
      <Instrument IsDeleted=""False"" LastResetDay=""2016-06-26 00:00:00"" ID=""7113a39a-2ff8-4f6c-8a53-ef5e84ab58c0"">
        <ResetItemHistory />
      </Instrument>
      <Instrument IsDeleted=""False"" LastResetDay=""2016-06-26 00:00:00"" ID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"">
        <ResetItemHistory />
      </Instrument>
      <Instrument IsDeleted=""False"" LastResetDay=""2016-06-26 00:00:00"" ID=""33f7ade0-d4d7-4cec-8a0d-63f3d5b95ae8"">
        <ResetItemHistory />
      </Instrument>
    </Instruments>
    <ResetOrders />
    <Balances />
  </Account>
</Accounts>";
            Commands.TradingCommandManager.Default.FillAccountInitData(XElement.Parse(initData));
        }


        [Test]
        public void UpdateTest()
        {
            Guid accountId = Guid.Parse("AAADC4A8-7EE1-4D62-A355-7A36518A2099");
            var account = Commands.AccountRepository.Default.Get(accountId);
            string placeData = @"<Account ID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""False"">
  <Transactions>
    <Transaction IsDeleted=""False"" PlacedByRiskMonitor=""False"" FreePlacingPreCheck=""False"" FreeLmtVariationCheck=""False"" ID=""f02b6e9d-47f4-46f4-a088-a2f9d00edadd"" Code=""DEM160627SP00012"" Type=""0"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" InstrumentID=""0ac2acae-66d4-4d6f-9e75-494324e0fb8a"" SubType=""0"" Phase=""255"" OrderType=""0"" ContractSize=""0"" BeginTime=""2016-06-27 15:31:47"" EndTime=""2016-06-27 15:32:17"" ExpireType=""3"" SubmitTime=""2016-06-27 15:31:47"" SubmitorID=""2c0d78f9-4820-49fb-bd82-e3b5b2d1967a"" InstrumentCategory=""20"" PlacePhase=""1"" PlaceDetail="""" AppType=""18"" UpdateTime=""2016-06-27 15:31:48"">
      <Orders>
        <Order IsDeleted=""False"" ID=""3e87f01c-4d40-42b6-9a94-14da9046924f"" IsOpen=""False"" IsBuy=""False"" Lot=""0.06"" OriginalLot=""0.06"" LotBalance=""0.00"" InterestPerLot=""0"" StoragePerLot=""0"" Code=""DEM2016062700022"" SetPrice=""1273.0"" SetPriceMaxMovePips=""0"" DQMaxMove=""0"" PlacedByRiskMonitor=""False"" Phase=""255"" TradeOption=""0"" PhysicalTradeSide=""2"" PhysicalValueMatureDay=""0"" PhysicalType=""0"" PhysicalOriginValue=""0"" PhysicalOriginValueBalance=""0"" PaidPledgeBalance=""0"" PaidPledge=""0"" DeliveryLockLot=""0"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"">
          <Bills />
          <OrderRelations>
            <OrderRelation IsDeleted=""False"" ID=""5f99d49b-b539-47f2-856f-eda781d304b2"" OpenOrderID=""1e41cfda-3a23-4876-a17a-6516a5e74ce4"" CloseOrderID=""3e87f01c-4d40-42b6-9a94-14da9046924f"" ClosedLot=""0.06"" TargetDecimals=""0"" RateIn=""0"" RateOut=""0"" Commission=""0"" Levy=""0"" OtherFee=""0"" InterestPL=""0"" StoragePL=""0"" TradePL=""0"" OverdueCutPenalty=""0"" ClosePenalty=""0"" PayBackPledge=""0"" ClosedPhysicalValue=""0"" physicalValue=""0"">
              <Bills />
            </OrderRelation>
          </OrderRelations>
        </Order>
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            ChangedFund fund;
            account.Update(XElement.Parse(placeData), out fund);
            Guid orderId = Guid.Parse("3e87f01c-4d40-42b6-9a94-14da9046924f");
            var order = account.GetOrder(orderId);
            Assert.IsNotNull(order.OrderRelations);
            Assert.AreEqual(1, order.OrderRelations.Count);
            string placedXml = @"<Account ID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""False"">
  <Transactions>
    <Transaction ID=""f02b6e9d-47f4-46f4-a088-a2f9d00edadd"" Phase=""0"" PlacePhase=""8"" UpdateTime=""2016-06-27 15:31:48"">
      <Orders>
        <Order ID=""3e87f01c-4d40-42b6-9a94-14da9046924f"" Phase=""0"" />
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            account.Update(XElement.Parse(placedXml), out fund);
            Assert.AreEqual(1, order.OrderRelations.Count);
            string executedXml = @"<Account ID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" IsMultiCurrency=""False"" Balance=""75571.42"" Necessary=""25106.00"" PartialPaymentPhysicalNecessary=""25106.00"">
  <Funds>
    <Fund CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" Balance=""75571.4205000000000"" Necessary=""25106.00"" PartialPaymentPhysicalNecessary=""25106.0000000000"" />
  </Funds>
  <Transactions>
    <Transaction ID=""b368a6ae-daa3-432d-8644-a79ecdb49ddc"">
      <Orders>
        <Order ID=""1e41cfda-3a23-4876-a17a-6516a5e74ce4"" LotBalance=""0.0000"" PhysicalOriginValueBalance=""0.0000000000000"" PaidPledgeBalance=""0.0000000000000"" />
      </Orders>
    </Transaction>
    <Transaction ID=""f02b6e9d-47f4-46f4-a088-a2f9d00edadd"" Phase=""2"" ContractSize=""1.0000"" ExecuteTime=""2016-06-27 15:31:48"" ApproverID=""00000000-0000-0000-0000-000000000000"">
      <Orders>
        <Order ID=""3e87f01c-4d40-42b6-9a94-14da9046924f"" ExecutePrice=""1274.1"" JudgePrice=""1273.0"" JudgePriceTimestamp=""2016-06-27 10:14:53"" InterestValueDate=""2016-06-30 00:00:00"" Phase=""2"" PhysicalOriginValue=""76.45"">
          <Bills>
            <Bill IsDeleted=""False"" ID=""0f981422-8c66-4318-bdc6-26215fe19618"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""10"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""38cd5dbd-bf98-4d0b-8e59-22207ea6dd2f"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""6"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""0415e6f8-8525-4a86-9375-bd837b385620"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""9"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""a7e84471-39c6-42be-9f1d-8f174205debd"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""8"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""60efc299-176e-497b-80ab-d9383c4ca8ba"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""31.8000000000000"" Type=""7"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""909d80a6-a370-4c51-bfc6-8f790837f47d"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""-1528.92"" Type=""1"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""974890d8-b4cb-497a-a660-cc44cb0bbbd2"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""2"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""596b7f30-2ecb-4797-916a-8e2a2a211ab6"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""0"" Type=""40"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" />
            <Bill IsDeleted=""False"" ID=""1ab8c9f7-6484-4141-83de-d87e999d053b"" AccountID=""aaadc4a8-7ee1-4d62-a355-7a36518a2099"" Value=""1.0500000000000"" Type=""23"" OwnerType=""1"" UpdateTime=""2016-06-27 15:31:48"" ValueTime=""2016-06-27 15:31:48"" IsValued=""True"" />
          </Bills>
          <OrderRelations>
            <OrderRelation ID=""5f99d49b-b539-47f2-856f-eda781d304b2"" OpenOrderID=""1e41cfda-3a23-4876-a17a-6516a5e74ce4"" CloseOrderID=""3e87f01c-4d40-42b6-9a94-14da9046924f"" CloseTime=""2016-06-27 15:31:48"" ValueTime=""2016-06-27 15:31:48"" TargetDecimals=""2"" RateIn=""1"" RateOut=""1"" Commission=""1528.92"" TradePL=""1.0500000000000"" PayBackPledge=""31.8000000000000"" ClosedPhysicalValue=""75.4000000000000"" physicalValue=""76.45"" />
          </OrderRelations>
        </Order>
      </Orders>
    </Transaction>
  </Transactions>
</Account>";
            account.Update(XElement.Parse(executedXml), out fund);
            Assert.AreEqual(1, order.OrderRelations.Count);
            var closeorder = account.GetOrder(Guid.Parse("3e87f01c-4d40-42b6-9a94-14da9046924f"));
            Assert.IsNotNull(closeorder);
            var orderRelation = closeorder.OrderRelations[0];
            Assert.IsNotNull(orderRelation);

            Assert.AreEqual(1.05, orderRelation.TradePL);
        }


        private void ParseXml()
        {

        }

    }
}
#else
#endif
