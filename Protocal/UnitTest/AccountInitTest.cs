#if DEBUG 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml.Linq;

namespace Protocal.UnitTest
{

    [TestFixture]
    public class AccountInitTest
    {
        [Test]
        public void InitTest()
        {
            string xml = @"<Accounts>
  <Account IsDeleted=""False"" AlertLevel=""3"" CutAlertLevel=""3"" ID=""9fc2c2fa-cdea-4c70-a356-1168af9d0656"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" CustomerName=""phy01"" IsMultiCurrency=""False"" MinUpkeepEquity=""0"" Type=""0"" CurrencyCode=""USD"" Code=""15013675452&#xA;15013675452&#xA;pp01"" TradeDay=""2016-06-21 00:00:00"" TotalPaidAmount=""0"" TotalDeposit=""0"" Balance=""-0.01"" FrozenFund=""0"" Necessary=""0"" NetNecessary=""0"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""0"" MinEquityAvoidRiskLevel2=""0"" MinEquityAvoidRiskLevel3=""0"" NecessaryFillingOpenOrder=""0"" NecessaryFillingCloseOrder=""0"" TradePLFloat=""0"" InterestPLFloat=""0"" StoragePLFloat=""0"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""0"" RiskCredit=""0"" PartialPaymentPhysicalNecessary=""0"" Equity=""-0.01"">
    <Funds>
      <Fund IsDeleted=""False"" CurrencyID=""767515e7-cfcc-4ba1-9684-659588c02e3e"" CurrencyCode=""USD"" TotalPaidAmount=""0"" TotalDeposit=""0"" Balance=""-0.0100"" FrozenFund=""0.0000000000000"" Necessary=""0"" NetNecessary=""0"" HedgeNecessary=""0"" MinEquityAvoidRiskLevel1=""0"" MinEquityAvoidRiskLevel2=""0"" MinEquityAvoidRiskLevel3=""0"" NecessaryFillingOpenOrder=""0"" NecessaryFillingCloseOrder=""0"" TradePLFloat=""0"" InterestPLFloat=""0"" StoragePLFloat=""0"" ValueAsMargin=""0"" TradePLNotValued=""0"" InterestPLNotValued=""0"" StoragePLNotValued=""0"" LockOrderTradePLFloat=""0"" FeeForCutting=""0"" RiskCredit=""0"" PartialPaymentPhysicalNecessary=""0"" />
    </Funds>
    <Transactions />
    <DeliveryRequests />
    <Instruments />
    <ResetOrders />
    <Balances />
  </Account>
</Accounts>";
            Commands.TradingCommandManager.Default.FillAccountInitData(XElement.Parse(xml));
            Guid accountId = Guid.Parse("9fc2c2fa-cdea-4c70-a356-1168af9d0656");
            var account = Commands.AccountRepository.Default.Get(accountId);
            Assert.IsNotNull(account);

        }
    }
}
#else
#endif
