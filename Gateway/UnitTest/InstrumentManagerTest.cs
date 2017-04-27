using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using NUnit.Framework;

namespace SystemController.UnitTest
{
    //[TestFixture]
    //internal sealed class InstrumentManagerTest
    //{
    //    [Test]
    //    public void AddInstrumentTest()
    //    {
    //        string addXml = @"<Add><Instrument ID=""a38c1d5d-3fc4-4911-808b-78832ab3d59d"" Code=""XY1"" OriginCode=""NZDUSD"" Description=""XY1"" MappingCode=""XY1"" ContractMonth=""999912"" Category=""10"" IsActive=""true"" BeginTime=""2016-09-12 15:09:51.000"" EndTime=""9999-12-31 23:59:59.000"" DayPolicyID=""59c2e8a6-4b23-4f29-b367-ca803712c99c"" WeekPolicyID=""e25b12fc-ef46-40aa-a8af-f2c61e79b877"" NumeratorUnit=""1"" Denominator=""10000"" IsSinglePrice=""false"" IsNormal=""true"" OriginType=""1"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" InterestPolicyID=""00823257-5646-46d5-8caa-bb5096b43bc7"" CommissionFormula=""0"" LevyFormula=""0"" OtherFeeFormula=""5"" MarginFormula=""0"" TradePLFormula=""0"" InterestFormula=""0"" InterestYearDays=""360"" PLValueDay=""0"" PriceValidTime=""15"" DailyMaxMove=""0"" AlertVariation=""20"" NormalWaitTime=""0"" AlertWaitTime=""10"" OriginInactiveTime=""300"" OrderTypeMask=""195"" MaxDQLot=""50.0000"" MaxOtherLot=""50.0000"" DQQuoteMinLot=""1.0000"" AutoDQMaxLot=""1.0000"" AutoLmtMktMaxLot=""1.0000"" AcceptDQVariation=""10"" AcceptLmtVariation=""10"" AcceptCloseLmtVariation=""10"" CancelLmtVariation=""10"" AcceptIfDoneVariation=""10"" LastAcceptTimeSpan=""0"" MaxMinAdjust=""0"" MOCSession=""1"" IsBetterPrice=""false"" AutoAcceptMaxLot=""9999.0000"" AutoCancelMaxLot=""9999.0000"" HitTimes=""1"" PenetrationPoint=""0"" UseSettlementPriceForInterest=""false"" PriceConvertFomulaType=""0"" MIT=""false"" CanPlacePendingOrderAtAnyTime=""false"" AllowedSpotTradeOrderSides=""3"" UpdatePersonID=""525bbbc6-0e94-4991-bac1-0cf1d31bbc17"" UpdateTime=""2016-09-12 15:10:22.487"" AutoDQDelay=""0"" Sequence=""0"" SummaryUnit=""1"" SummaryQuantity=""1.0000"" AllowedNewTradeSides=""3"" HolidayAlertDayPolicyID=""00000000-0000-0000-0000-000000000001"" HitPriceVariationForSTP=""9999"" PhysicalLotDecimal=""2"" DeliveryTimeBeginDay=""1"" DeliveryTimeEndDay=""0"" LimitRangeForOrder=""0.000000000"" FirstOrderTime=""0"" PlaceSptMktTimeSpan=""0"" IsAutoEnablePrice=""true"" IsPriceEnabled=""true"" /></Add>";
    //        SettingManager.Default.DBConnectionString = "data source=ws3190;initial catalog=iExchange_V3;user id=sa;password=Omni1234;Connect Timeout=60";
    //        InstrumentBLL.InstrumentManager.Default.AddNewInstrument(addXml);
    //        var instrument = InstrumentBLL.InstrumentManager.Default.GetInstrument(Guid.Parse("a38c1d5d-3fc4-4911-808b-78832ab3d59d"));
    //        Assert.IsNotNull(instrument);
    //        Assert.AreEqual(DateTime.Now.Date, instrument.TradeDay);
    //    }
    //}
}
