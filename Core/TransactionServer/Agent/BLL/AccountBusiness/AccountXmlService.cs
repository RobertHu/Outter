using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class AccountXmlService
    {
        public static XmlElement ToXmlNode(Account account,Guid currencyID)
        {
            string url = System.AppDomain.CurrentDomain.BaseDirectory + "Stylesheet/AccountCurrency.xslt";
            XsltArgumentList xsltArgList = new XsltArgumentList();
            xsltArgList.AddParam("currencyID", "", currencyID);
            return (XmlElement)XmlTransform.Transform(ToXmlNode(account), url, xsltArgList);
        }

        public static XmlElement ToXmlNode(Account account,bool includeTransactions = false)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement accountNode = xmlDoc.CreateElement("Account");
            xmlDoc.AppendChild(accountNode);

            var riskRawData = account.SumFund.RiskRawData;
            accountNode.SetAttribute("ID", XmlConvert.ToString(account.Id));
            accountNode.SetAttribute("Balance", XmlConvert.ToString(account.SumFund.Balance));
            accountNode.SetAttribute("Necessary", XmlConvert.ToString(riskRawData.Necessary));
            accountNode.SetAttribute("Necessary0", XmlConvert.ToString(riskRawData.Necessary));
            accountNode.SetAttribute("Necessary1", XmlConvert.ToString(riskRawData.MinEquityAvoidRiskLevel1));
            accountNode.SetAttribute("Necessary2", XmlConvert.ToString(riskRawData.MinEquityAvoidRiskLevel2));
            accountNode.SetAttribute("Necessary3", XmlConvert.ToString(riskRawData.MinEquityAvoidRiskLevel3));
            accountNode.SetAttribute("Equity", XmlConvert.ToString(account.SumFund.Equity));
            accountNode.SetAttribute("MinUpkeepEquity", XmlConvert.ToString(account.MinUpkeepEquity));
            accountNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(riskRawData.InterestPLNotValued));
            accountNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(riskRawData.StoragePLNotValued));
            accountNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(riskRawData.TradePLNotValued));
            accountNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(riskRawData.InterestPLFloat));
            accountNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(riskRawData.StoragePLFloat));
            accountNode.SetAttribute("TradePLFloat", XmlConvert.ToString(riskRawData.TradePLFloat));
            accountNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(riskRawData.ValueAsMargin));
            accountNode.SetAttribute("FrozenFund", XmlConvert.ToString(account.SumFund.FrozenFund));
            //accountNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this.partialPaymentPhysicalNecessary)); need to be to
            accountNode.SetAttribute("TotalPaidAmount", XmlConvert.ToString(account.SumFund.TotalPaidAmount));
            accountNode.SetAttribute("AlertLevel", XmlConvert.ToString((int)account.AlertLevel));

            if (account.AlertTime != null)
            {
                accountNode.SetAttribute("AlertTime", XmlConvert.ToString(account.AlertTime.Value, DateTimeFormat.Xml));
            }

            foreach (var fund in account.Funds)
            {
                XmlElement currencyNode = xmlDoc.CreateElement("Currency");
                accountNode.AppendChild(currencyNode);
                currencyNode.SetAttribute("ID", XmlConvert.ToString(fund.CurrencyId));
                currencyNode.SetAttribute("Balance", XmlConvert.ToString(fund.BalanceWhole.Balance));
                currencyNode.SetAttribute("Necessary", XmlConvert.ToString(fund.RiskData.Necessary));
                currencyNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(fund.RiskData.InterestPLNotValued));
                currencyNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(fund.RiskData.StoragePLNotValued));
                currencyNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(fund.RiskData.TradePLNotValued));
                currencyNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(fund.RiskData.InterestPLFloat));
                currencyNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(fund.RiskData.StoragePLFloat));
                currencyNode.SetAttribute("TradePLFloat", XmlConvert.ToString(fund.RiskData.TradePLFloat));
                currencyNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(fund.RiskData.ValueAsMargin));
                currencyNode.SetAttribute("FrozenFund", XmlConvert.ToString(fund.BalanceWhole.FrozenFund));
                //currencyNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(accountCurrency.PartialPaymentPhysicalNecessary));
                currencyNode.SetAttribute("TotalPaidAmount", XmlConvert.ToString(fund.TotalPaidAmount));

                if (includeTransactions)
                {
                    XmlElement transNode = xmlDoc.CreateElement("Transactions");
                    foreach (var tran in account.Transactions)
                    {
                        if (tran.CurrencyId == fund.CurrencyId)
                        {
                            transNode.AppendChild(xmlDoc.ImportNode(tran.ToXmlNode(true), true));
                        }
                    }
                    if (transNode.HasChildNodes) currencyNode.AppendChild(transNode);
                }
            }
            return accountNode;
        }
    }
}
