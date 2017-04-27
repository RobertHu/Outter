using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml;

namespace iExchange.StateServer.Adapter
{
    internal sealed class Fund : Protocal.Commands.Fund
    {
        internal Fund(Account owner, Guid currencyID, string currencyCode)
            : base(owner, currencyID, currencyCode)
        {
        }

        internal IEnumerable<Transaction> Trans
        {
            get
            {
                foreach (Transaction eachTran in this.Owner.Transactions)
                {
                    if (eachTran.CurrencyId == this.CurrencyId)
                    {
                        yield return eachTran;
                    }
                }
            }
        }

        internal XElement ToXml(XmlParameter parameter)
        {
            var result = new XElement("Currency");
            this.FillXmlAttrs(result);
            if (parameter.IncludeTrans || parameter.IsForGetInitData)
            {
                result.Add(this.CreateTransXml(parameter));
            }
            return result;
        }

        private XElement CreateTransXml(XmlParameter parameter)
        {
            XElement result = new XElement("Transactions");
            foreach (Transaction eachTran in this.Trans)
            {
                result.Add(eachTran.ToXml(parameter.IsForGetInitData, parameter.IsForReport));
            }
            return result;
        }

        internal XmlNode GetMemoryBalanceNecessaryEquity(XmlDocument xmlDoc)
        {
            XmlElement accountCurrencyNode = xmlDoc.CreateElement("AccountCurrency");

            accountCurrencyNode.SetAttribute("CurrencyID", XmlConvert.ToString(this.CurrencyId));
            accountCurrencyNode.SetAttribute("Balance", XmlConvert.ToString(this.Balance));
            accountCurrencyNode.SetAttribute("TotalDeposit", XmlConvert.ToString(this.TotalDeposit));
            accountCurrencyNode.SetAttribute("Necessary", XmlConvert.ToString(this.Necessary));

            accountCurrencyNode.SetAttribute("InterestPLNotValued", XmlConvert.ToString(this.InterestPLNotValued));
            accountCurrencyNode.SetAttribute("StoragePLNotValued", XmlConvert.ToString(this.StoragePLNotValued));
            accountCurrencyNode.SetAttribute("TradePLNotValued", XmlConvert.ToString(this.TradePLNotValued));
            accountCurrencyNode.SetAttribute("InterestPLFloat", XmlConvert.ToString(this.InterestPLFloat));
            accountCurrencyNode.SetAttribute("StoragePLFloat", XmlConvert.ToString(this.StoragePLFloat));
            accountCurrencyNode.SetAttribute("TradePLFloat", XmlConvert.ToString(this.TradePLFloat));
            accountCurrencyNode.SetAttribute("ValueAsMargin", XmlConvert.ToString(this.ValueAsMargin));
            accountCurrencyNode.SetAttribute("TotalPaidAmount", XmlConvert.ToString(this.TotalPaidAmount));
            accountCurrencyNode.SetAttribute("FrozenFund", XmlConvert.ToString(this.FrozenFund));
            accountCurrencyNode.SetAttribute("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this.PartialPaymentPhysicalNecessary));
            return accountCurrencyNode;
        }


        internal void FillXmlAttrs(XElement fundNode)
        {
            fundNode.SetAttributeValue("ID", XmlConvert.ToString(this.CurrencyId));
            fundNode.SetAttributeValue("Balance", XmlConvert.ToString(this.Balance));
            fundNode.SetAttributeValue("Necessary", XmlConvert.ToString(this.Necessary));
            fundNode.SetAttributeValue("Equity", XmlConvert.ToString(this.Equity));
            fundNode.SetAttributeValue("InterestPLNotValued", XmlConvert.ToString(this.InterestPLNotValued));
            fundNode.SetAttributeValue("StoragePLNotValued", XmlConvert.ToString(this.StoragePLNotValued));
            fundNode.SetAttributeValue("TradePLNotValued", XmlConvert.ToString(this.TradePLNotValued));
            fundNode.SetAttributeValue("InterestPLFloat", XmlConvert.ToString(this.InterestPLFloat));
            fundNode.SetAttributeValue("StoragePLFloat", XmlConvert.ToString(this.StoragePLFloat));
            fundNode.SetAttributeValue("TradePLFloat", XmlConvert.ToString(this.TradePLFloat));
            fundNode.SetAttributeValue("ValueAsMargin", XmlConvert.ToString(this.ValueAsMargin));
            fundNode.SetAttributeValue("FrozenFund", XmlConvert.ToString(this.FrozenFund));
            fundNode.SetAttributeValue("PartialPaymentPhysicalNecessary", XmlConvert.ToString(this.PartialPaymentPhysicalNecessary));
            fundNode.SetAttributeValue("TotalPaidAmount", XmlConvert.ToString(this.TotalPaidAmount));
            var estimateFees = this.CalculateEstimateFee();
            fundNode.SetAttributeValue("EstimateCloseCommission", XmlConvert.ToString(estimateFees.Item1));
            fundNode.SetAttributeValue("EstimateCloseLevy", XmlConvert.ToString(estimateFees.Item2));
        }

        private Tuple<decimal, decimal> CalculateEstimateFee()
        {
            decimal commission = 0m;
            decimal levy = 0m;
            foreach (var eachTran in this.Trans)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsOpen && eachOrder.Phase == Common.OrderPhase.Executed && eachOrder.LotBalance > 0)
                    {
                        commission += eachOrder.EstimateCloseCommission;
                        levy += eachOrder.EstimateCloseLevy;
                    }
                }
            }
            return Tuple.Create(commission, levy);
        }


    }

}