using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using iExchange.Common;
using System.Xml;
using System.Text;

namespace iExchange.StateServer.Adapter
{
    internal sealed class InstrumentNotFoundException : Exception
    {
        internal InstrumentNotFoundException(Guid instrumentId)
            : base(string.Format("instrumentId = {0}", instrumentId))
        {
            this.Id = instrumentId;
        }
        internal Guid Id { get; private set; }
    }


    internal sealed class Transaction : Protocal.Commands.Transaction
    {
        public static IComparer<Transaction> SubmitTimeComparer = new InternalSubmitTimeComparer();
        private class InternalSubmitTimeComparer : IComparer<Transaction>
        {
            int IComparer<Transaction>.Compare(Transaction x, Transaction y)
            {
                return x.SubmitTime.CompareTo(y.SubmitTime);
            }
        }

        internal Transaction(Account owner)
            : base(owner)
        {
        }

        internal Guid CurrencyId
        {
            get
            {
                if (this.Owner.IsMultiCurrency)
                {
                    var instrument = InstrumentManager.Default.Get(this.InstrumentId);
                    if (instrument == null) throw new InstrumentNotFoundException(this.InstrumentId);
                    return instrument.CurrencyId;
                }
                else
                {
                    return this.Owner.Fund.CurrencyId;
                }
            }
        }

        internal Instrument Instrument
        {
            get
            {
                return InstrumentManager.Default.Get(this.InstrumentId);
            }
        }

        protected override Protocal.Commands.Order CreateOrder(Protocal.Commands.Transaction tran)
        {
            return new Order((Transaction)tran);
        }

        internal XElement ToXml()
        {
            return this.ToXml(false, false);
        }

        internal XElement ToXml(bool isForGetInitData, bool isForReport)
        {
            XElement result = new XElement("Transaction");
            this.FillProperties(result);
            if (isForReport)
            {
                this.FillProtertiesForReport(result);
            }
            this.FillOrders(result, isForGetInitData, isForReport);
            return result;
        }

        internal void FillOrders(XElement tranNode, bool isForGetInitData, bool isForReport)
        {
            foreach (Order eachOrder in this._orders)
            {
                this.FillSingleOrder(eachOrder, tranNode, isForGetInitData, isForReport);
            }
        }

        private void FillSingleOrder(Order order, XElement tranNode, bool isForGetInitData, bool isForReport)
        {
            if (order.Owner.Phase == TransactionPhase.Executed && order.Owner.Type == TransactionType.OneCancelOther && order.Phase == OrderPhase.Canceled) return;
            var tran = order.Owner;
            var orderNode = order.ToXml(isForGetInitData, isForReport);
            if (tran.Phase == TransactionPhase.Placed && tran.SubType == TransactionSubType.IfDone)
            {
                var doneTran = this.GetDoneTran(order.Id);
                if (doneTran != null)
                {
                    orderNode.SetAttributeValue("Extension", doneTran.ToXml(isForGetInitData, isForReport).ToString());
                }
            }
            tranNode.Add(orderNode);
        }

        private Transaction GetDoneTran(Guid orderId)
        {
            foreach (var eachTran in this.Owner.Transactions)
            {
                if (eachTran.SourceOrderId == orderId)
                {
                    return (Transaction)eachTran;
                }
            }
            return null;
        }


        private void FillProtertiesForReport(XElement node)
        {
            Instrument instrument = InstrumentManager.Default.Get(this.InstrumentId);
            node.SetAttributeValue("NumeratorUnit", Convert.ToString(instrument.NumeratorUnit));
            node.SetAttributeValue("Denominator", Convert.ToString(instrument.Denominator));
        }

        private void FillProperties(XElement node)
        {
            node.SetAttributeValue("ID", XmlConvert.ToString(this.Id));
            if (!string.IsNullOrEmpty(this.Code)) node.SetAttributeValue("Code", this.Code);
            node.SetAttributeValue("Type", XmlConvert.ToString((int)this.Type));
            node.SetAttributeValue("SubType", XmlConvert.ToString((int)this.SubType));
            node.SetAttributeValue("Phase", XmlConvert.ToString((int)this.Phase));
            node.SetAttributeValue("OrderType", XmlConvert.ToString((int)this.OrderType));
            node.SetAttributeValue("InstrumentCategory", XmlConvert.ToString((int)this.InstrumentCategory));
            node.SetAttributeValue("ContractSize", XmlConvert.ToString(this.ContractSize));
            node.SetAttributeValue("AccountID", XmlConvert.ToString(this.Owner.Id));
            node.SetAttributeValue("InstrumentID", XmlConvert.ToString(this.InstrumentId));
            node.SetAttributeValue("BeginTime", XmlConvert.ToString(this.BeginTime, DateTimeFormat.Xml));
            node.SetAttributeValue("EndTime", XmlConvert.ToString(this.EndTime, DateTimeFormat.Xml));
            node.SetAttributeValue("ExpireType", XmlConvert.ToString((int)this.ExpireType));
            node.SetAttributeValue("SubmitTime", XmlConvert.ToString(this.SubmitTime, DateTimeFormat.Xml));
            if (this.ExecuteTime != DateTime.MinValue)
                node.SetAttributeValue("ExecuteTime", XmlConvert.ToString(this.ExecuteTime, DateTimeFormat.Xml));
            node.SetAttributeValue("SubmitorID", XmlConvert.ToString(this.SubmitorId));
            node.SetAttributeValue("ApproverID", XmlConvert.ToString(this.ApproverId ?? Guid.Empty));
            if (this.SourceOrderId != Guid.Empty)
                node.SetAttributeValue("AssigningOrderID", XmlConvert.ToString(this.SourceOrderId));
            if (this.OrderBatchInstructionId != null)
                node.SetAttributeValue("OrderBatchInstructionID", XmlConvert.ToString(this.OrderBatchInstructionId.Value));
        }


        public XmlElement GetExecuteXmlElement()
        {
            return this.GetExecuteXmlElement(null);
        }

        public XmlElement GetExecuteXmlElement(DateTime? executeTradeDay)
        {
            XmlDocument document = new XmlDocument();
            if (executeTradeDay == null) executeTradeDay = TradeDayManager.Default.GetTradeDay();
            document.LoadXml(this.GetExecuteXmlString(executeTradeDay.Value));
            return (XmlElement)document.GetElementsByTagName("Transaction")[0];
        }

        public string GetExecuteXmlString()
        {
            return this.GetExecuteXmlString(TradeDayManager.Default.GetTradeDay());
        }


        public string GetExecuteXmlString(DateTime executeTradeDay)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<Transaction ID='{0}' Code='{1}'", XmlConvert.ToString(this.Id), this.Code);
            stringBuilder.AppendFormat(" Type='{0}' SubType='{1}'", XmlConvert.ToString((int)this.Type), XmlConvert.ToString((int)this.SubType));
            stringBuilder.AppendFormat(" Phase='{0}' OrderType='{1}'", XmlConvert.ToString((int)this.Phase), XmlConvert.ToString((int)this.OrderType));
            stringBuilder.AppendFormat(" InstrumentCategory='{0}'", XmlConvert.ToString((int)this.InstrumentCategory));
            stringBuilder.AppendFormat(" ContractSize='{0}' AccountID='{1}'", XmlConvert.ToString(this.ContractSize), XmlConvert.ToString(this.Owner.Id));
            stringBuilder.AppendFormat(" InstrumentID='{0}' BeginTime='{1}'", XmlConvert.ToString(this.InstrumentId), XmlConvert.ToString(this.BeginTime, DateTimeFormat.Xml));
            stringBuilder.AppendFormat(" EndTime='{0}' ExpireType='{1}'", XmlConvert.ToString(this.EndTime, DateTimeFormat.Xml), XmlConvert.ToString((int)this.ExpireType));
            DateTime executeTime = this.ExecuteTime == DateTime.MinValue ? executeTradeDay : this.ExecuteTime;
            stringBuilder.AppendFormat(" SubmitTime='{0}' ExecuteTime='{1}'", XmlConvert.ToString(this.SubmitTime, DateTimeFormat.Xml), XmlConvert.ToString(executeTime, DateTimeFormat.Xml));
            stringBuilder.AppendFormat(" SubmitorID='{0}' ApproverID='{1}'", XmlConvert.ToString(this.SubmitorId), XmlConvert.ToString(this.ApproverId ?? Guid.Empty));
            if (this.SourceOrderId != Guid.Empty)
            {
                stringBuilder.AppendFormat(" AssigningOrderID='{0}'", XmlConvert.ToString(this.SourceOrderId));
            }

            if (this.OrderBatchInstructionId != null)
            {
                stringBuilder.AppendFormat(" OrderBatchInstructionID='{0}'", XmlConvert.ToString(this.OrderBatchInstructionId.Value));
            }
            stringBuilder.AppendFormat(">");
            foreach (Order order in this.Orders)
            {
                if (order.Phase == OrderPhase.Canceled) continue;

                stringBuilder.AppendFormat("<Order ID='{0}'", XmlConvert.ToString(order.Id));
                if (order.Code != null) stringBuilder.AppendFormat(" Code='{0}'", order.Code);
                stringBuilder.AppendFormat(" TradeOption='{0}' IsOpen='{1}' IsBuy='{2}'", XmlConvert.ToString((int)order.TradeOption), XmlConvert.ToString(order.IsOpen), XmlConvert.ToString(order.IsBuy));
                stringBuilder.AppendFormat(" PhysicalTradeSide='{0}'", XmlConvert.ToString((int)order.PhysicalTradeSide));
                if (order.PhysicalRequestId != null) stringBuilder.AppendFormat(" PhysicalRequestId='{0}'", XmlConvert.ToString(order.PhysicalRequestId.Value));
                if (order.PhysicalValueMatureDay != 0) stringBuilder.AppendFormat(" PhysicalValueMatureDay='{0}'", XmlConvert.ToString(order.PhysicalValueMatureDay));
                stringBuilder.AppendFormat(" PhysicalOriginValue='{0}'", XmlConvert.ToString(order.PhysicalOriginValue));
                stringBuilder.AppendFormat(" PhysicalOriginValueBalance='{0}'", XmlConvert.ToString(order.PhysicalOriginValueBalance));
                stringBuilder.AppendFormat(" PhysicalPaymentDiscount='{0}'", XmlConvert.ToString(order.PhysicalPaymentDiscount));
                stringBuilder.AppendFormat(" PhysicalPaidAmount='{0}'", XmlConvert.ToString(order.PhysicalPaidAmount));
                stringBuilder.AppendFormat(" ValueAsMargin='{0}'", XmlConvert.ToString(order.ValueAsMargin));
                stringBuilder.AppendFormat(" InterestValueDate='{0}'", XmlConvert.ToString(order.InterestValueDate, DateTimeFormat.Xml));
                stringBuilder.AppendFormat(" PaidPledge='{0}'", XmlConvert.ToString(order.PaidPledge));
                stringBuilder.AppendFormat(" PaidPledgeBalance='{0}'", XmlConvert.ToString(order.PaidPledgeBalance));

                stringBuilder.AppendFormat(" TotalDeposit='{0}'", XmlConvert.ToString(this.Owner.TotalDeposit));
                stringBuilder.AppendFormat(" Equity='{0}'", XmlConvert.ToString(this.Owner.Equity));

                if (order.InstalmentPolicyId != null)
                {
                    stringBuilder.AppendFormat(" InstalmentPolicyId='{0}'", XmlConvert.ToString(order.InstalmentPolicyId.Value));
                    stringBuilder.AppendFormat(" PhysicalInstalmentType='{0}'", XmlConvert.ToString((int)order.InstalmentType));
                    stringBuilder.AppendFormat(" Period='{0}'", XmlConvert.ToString(order.Period));
                    stringBuilder.AppendFormat(" InstalmentFrequence='{0}'", XmlConvert.ToString((int)order.InstalmentFrequence));
                    stringBuilder.AppendFormat(" DownPayment='{0}'", XmlConvert.ToString(order.DownPayment));
                    stringBuilder.AppendFormat(" DownPaymentBasis='{0}'", XmlConvert.ToString((int)order.DownPaymentBasis));
                    stringBuilder.AppendFormat(" RecalculateRateType='{0}'", XmlConvert.ToString((int)order.RecalculateRateType));
                    stringBuilder.AppendFormat(" InstalmentAdministrationFee='{0}'", XmlConvert.ToString(order.InstalmentAdministrationFee));
                }

                if (order.BinaryOptionBetTypeId != null)
                {
                    stringBuilder.AppendFormat(" BOBetTypeID='{0}'", XmlConvert.ToString(order.BinaryOptionBetTypeId.Value));
                    stringBuilder.AppendFormat(" BOFrequency='{0}'", XmlConvert.ToString(order.BinaryOptionFrequency));
                    stringBuilder.AppendFormat(" BOOdds='{0}'", XmlConvert.ToString(order.BinaryOptionOdds));
                    stringBuilder.AppendFormat(" BOBetOption='{0}'", XmlConvert.ToString(order.BinaryOptionBetOption));
                }

                if (order.SetPrice != null) stringBuilder.AppendFormat(" SetPrice='{0}'", (string)order.SetPrice);
                if (order.SetPrice2 != null) stringBuilder.AppendFormat(" SetPrice2='{0}'", (string)order.SetPrice2);
                if (order.JudgePrice != null)
                {
                    stringBuilder.AppendFormat(" JudgePrice='{0}'", (string)order.JudgePrice);
                    stringBuilder.AppendFormat(" JudgePriceTimestamp='{0}'", order.JudgePriceTimestamp.Value.ToString(DateTimeFormat.Xml));
                }
                if (order.SetPriceMaxMovePips != 0) stringBuilder.AppendFormat(" SetPriceMaxMovePips='{0}'", XmlConvert.ToString(order.SetPriceMaxMovePips));
                if (order.DQMaxMove != 0) stringBuilder.AppendFormat(" DQMaxMove='{0}'", XmlConvert.ToString(order.DQMaxMove));
                stringBuilder.AppendFormat(" ExecutePrice='{0}'", (string)order.ExecutePrice);
                stringBuilder.AppendFormat(" ExecuteTradeDay='{0}'", XmlConvert.ToString(executeTradeDay, DateTimeFormat.Xml));
                if (order.AutoLimitPrice != null) stringBuilder.AppendFormat(" AutoLimitPrice='{0}'", (string)order.AutoLimitPrice);
                if (order.AutoStopPrice != null) stringBuilder.AppendFormat(" AutoStopPrice='{0}'", (string)order.AutoStopPrice);
                stringBuilder.AppendFormat(" OriginalLot='{0}'", XmlConvert.ToString(order.OriginalLot));
                stringBuilder.AppendFormat(" Lot='{0}' LotBalance='{1}'", XmlConvert.ToString(order.Lot), XmlConvert.ToString(order.LotBalance));
                stringBuilder.AppendFormat(" CommissionSum='{0}' LevySum='{1}' OtherFeeSum='{2}'", XmlConvert.ToString(order.CommissionSum), XmlConvert.ToString(order.LevySum), XmlConvert.ToString(order.OtherFeeSum));
                stringBuilder.AppendFormat(" LivePrice='{0}' InterestPerLot='{1}'", (string)order.LivePrice, XmlConvert.ToString(order.InterestPerLot));
                stringBuilder.AppendFormat(" StoragePerLot='{0}' InterestPLFloat='{1}'", XmlConvert.ToString(order.StoragePLFloat), XmlConvert.ToString(order.InterestPLFloat));
                stringBuilder.AppendFormat(" StoragePLFloat='{0}' TradePLFloat='{1}'", XmlConvert.ToString(order.StoragePLFloat), XmlConvert.ToString(order.TradePLFloat));
                if (order.DayInterestPLNotValued != null)
                {
                    stringBuilder.AppendFormat(" DayInterestNotValued='{0}' DayStorageNotValued='{1}'", iExchange.Common.Extensions.ToString(order.DayInterestPLNotValued, '|'), iExchange.Common.Extensions.ToString(order.DayStoragePLNotValued, '|'));
                }
                stringBuilder.AppendFormat(" PlacedByRiskMonitor='{0}'", XmlConvert.ToString(order.PlacedByRiskMonitor));
                stringBuilder.Append(">");

                if (!order.IsOpen)
                {
                    foreach (OrderRelation orderRelation in order.OrderRelations)
                    {
                        stringBuilder.AppendFormat("<OrderRelation OpenOrderID='{0}' ClosedLot='{1}'", XmlConvert.ToString(orderRelation.OpenOrderId), XmlConvert.ToString(orderRelation.ClosedLot));
                        if (orderRelation.CloseTime != default(DateTime))
                        {
                            stringBuilder.AppendFormat(" CloseTime='{0}'", XmlConvert.ToString(orderRelation.CloseTime, DateTimeFormat.Xml));
                        }
                        stringBuilder.AppendFormat(" Commission='{0}'", XmlConvert.ToString(orderRelation.Commission));
                        stringBuilder.AppendFormat(" OtherFee='{0}'", XmlConvert.ToString(orderRelation.OtherFee));
                        stringBuilder.AppendFormat(" Levy='{0}' InterestPL='{1}'", XmlConvert.ToString(orderRelation.Levy), XmlConvert.ToString(orderRelation.InterestPL));
                        stringBuilder.AppendFormat(" StoragePL='{0}' TradePL='{1}'", XmlConvert.ToString(orderRelation.StoragePL), XmlConvert.ToString(orderRelation.TradePL));
                        stringBuilder.AppendFormat(" PhysicalTradePL='{0}'", XmlConvert.ToString(orderRelation.PhysicalTradePL));
                        stringBuilder.AppendFormat(" PhysicalValue='{0}'", XmlConvert.ToString(orderRelation.PhysicalValue));
                        stringBuilder.AppendFormat(" ClosedPhysicalValue='{0}'", XmlConvert.ToString(orderRelation.ClosedPhysicalValue));
                        stringBuilder.AppendFormat(" PayBackPledge='{0}'", XmlConvert.ToString(orderRelation.PayBackPledgeOfOpenOrder));
                        stringBuilder.AppendFormat(" OverdueCutPenalty='{0}'", XmlConvert.ToString(orderRelation.OverdueCutPenalty));
                        stringBuilder.AppendFormat(" ClosePenalty='{0}'", XmlConvert.ToString(orderRelation.ClosePenalty));
                        if (orderRelation.PhysicalValueMatureDay != null) stringBuilder.AppendFormat(" PhysicalValueMatureDate='{0}'", XmlConvert.ToString(orderRelation.PhysicalValueMatureDay.Value, DateTimeFormat.Xml));

                        if (orderRelation.ValueTime != default(DateTime))
                        {
                            stringBuilder.AppendFormat(" ValueTime='{0}' RateIn='{1}'", XmlConvert.ToString(orderRelation.ValueTime, DateTimeFormat.Xml), Convert.ToString(orderRelation.RateIn));
                            stringBuilder.AppendFormat(" RateOut='{0}' Decimals='{1}'", Convert.ToString(orderRelation.RateOut), Convert.ToString(orderRelation.Decimals));
                        }
                        stringBuilder.Append(" />");
                    }
                }
                stringBuilder.Append("</Order>");
            }
            stringBuilder.Append("</Transaction>");

            return stringBuilder.ToString();
        }

        public XmlElement GetOpenInterestSummaryOrderList(string[] blotterCodeSelecteds)
        {
            XmlDocument xmlTran = new XmlDocument();

            XmlElement tranNode = xmlTran.CreateElement("Transaction");
            xmlTran.AppendChild(tranNode);

            tranNode.SetAttribute("ID", XmlConvert.ToString(this.Id));
            tranNode.SetAttribute("ContractSize", XmlConvert.ToString(this.ContractSize));
            tranNode.SetAttribute("InstrumentID", XmlConvert.ToString(this.InstrumentId));
            tranNode.SetAttribute("ExecuteTime", XmlConvert.ToString(this.ExecuteTime, DateTimeFormat.Xml));

            bool isExistsOpenOrder = false;
            foreach (Order order in this.Orders)
            {
                if (this.IsMatchBlotterCode(order.BlotterCode, blotterCodeSelecteds)
                    && order.IsOpen && order.LotBalanceReal != decimal.Zero
                    && (order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed))
                {
                    isExistsOpenOrder = true;
                    XmlElement orderNode = xmlTran.CreateElement("Order");
                    tranNode.AppendChild(orderNode);
                    orderNode.SetAttribute("ID", XmlConvert.ToString(order.Id));
                    //orderNode.SetAttribute("Code", order.Code);
                    orderNode.SetAttribute("IsBuy", XmlConvert.ToString(order.IsBuy));
                    orderNode.SetAttribute("LotBalance", XmlConvert.ToString(order.LotBalanceReal));
                    orderNode.SetAttribute("ExecutePrice", (string)order.ExecutePrice);
                }
            }
            return isExistsOpenOrder ? tranNode : null;
        }



        public void OpenInterestSummary(string[] blotterCodeSelecteds, out bool isExistsOpenOrder, out decimal buyLot, out decimal sellLot, out decimal buySumEL, out decimal sellSumEL, out decimal buyContractSize, out decimal sellContractSize)
        {
            buyLot = decimal.Zero;
            sellLot = decimal.Zero;
            buySumEL = decimal.Zero;
            sellSumEL = decimal.Zero;
            buyContractSize = decimal.Zero;
            sellContractSize = decimal.Zero;

            isExistsOpenOrder = false;
            foreach (Order order in this.Orders)
            {
                if (this.IsMatchBlotterCode(order.BlotterCode, blotterCodeSelecteds)
                    && order.IsOpen && order.LotBalanceReal != decimal.Zero
                    && (order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed))
                {
                    isExistsOpenOrder = true;
                    Price executePrice = this.CreatePrice(order.ExecutePrice, order.Owner.InstrumentId);
                    if (order.IsBuy)
                    {
                        buyLot += order.LotBalanceReal;
                        buySumEL += (decimal)executePrice * order.LotBalanceReal;
                        buyContractSize += order.LotBalance * this.ContractSize;
                    }
                    else
                    {
                        sellLot += order.LotBalanceReal;
                        sellSumEL += (decimal)executePrice * order.LotBalanceReal;
                        sellContractSize += order.LotBalanceReal * this.ContractSize;
                    }
                }
            }
        }

        private bool IsMatchBlotterCode(string blotterCode, string[] blotterCodeSelecteds)
        {
            if (blotterCodeSelecteds == null) return true;
            foreach (string blotterCode2 in blotterCodeSelecteds)
            {
                if (blotterCode == null && blotterCode2 == null) return true;
                if (!((blotterCode != null && blotterCode2 == null)
                    || (blotterCode == null && blotterCode2 != null)))
                {
                    if (blotterCode.Equals(blotterCode2, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void GroupNetPositionForManager(string[] blotterCodeSelecteds, out bool isExistsOpenOrder, out decimal buyQuantity, out decimal buyMultiplyValue, out decimal sellQuantity, out decimal sellMultiplyValue, out decimal buyLot, out decimal sellLot, out decimal buySumEL, out decimal sellSumEL)
        {
            isExistsOpenOrder = false;
            buyQuantity = decimal.Zero;
            buyMultiplyValue = decimal.Zero;
            sellQuantity = decimal.Zero;
            sellMultiplyValue = decimal.Zero;
            buyLot = decimal.Zero;
            sellLot = decimal.Zero;
            buySumEL = decimal.Zero;
            sellSumEL = decimal.Zero;

            foreach (Order order in this.Orders)
            {
                if (this.IsMatchBlotterCode(order.BlotterCode, blotterCodeSelecteds)
                    && order.IsOpen && order.LotBalanceReal != decimal.Zero
                    && (order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed))
                {
                    isExistsOpenOrder = true;
                    Price executePrice = this.CreatePrice(order.ExecutePrice, order.Owner.InstrumentId);
                    if (order.IsBuy)
                    {
                        //quantity = order.LotBalance * this.ContractSize ;
                        decimal quantity = order.LotBalanceReal * this.ContractSize;
                        buyLot += order.LotBalanceReal;
                        buyQuantity += quantity;
                        buySumEL += order.LotBalanceReal * (decimal)executePrice;
                        buyMultiplyValue += quantity * (decimal)executePrice;
                    }
                    else
                    {
                        // quantity = order.LotBalance * this.ContractSize;
                        decimal quantity = order.LotBalanceReal * this.ContractSize;
                        sellLot += order.LotBalanceReal;
                        sellQuantity += quantity;
                        sellSumEL += order.LotBalanceReal * (decimal)executePrice;
                        sellMultiplyValue += quantity * (decimal)executePrice;
                    }
                }
            }
        }

        private Price CreatePrice(string priceString, Guid instrumentId)
        {
            var instrument = InstrumentManager.Default.Get(instrumentId);
            return Price.CreateInstance(priceString, instrument.NumeratorUnit, instrument.Denominator);
        }

        protected override List<Protocal.Commands.OrderPhaseChange> UpdateOrders(XElement tranElement)
        {
            var result = base.UpdateOrders(tranElement);
            if (this.Type == TransactionType.OneCancelOther)
            {
                this.FilterChanges(result, Protocal.Commands.OrderChangeType.Executed);
                this.FilterChanges(result, Protocal.Commands.OrderChangeType.Placed);
            }
            return result;
        }

        private void FilterChanges(List<Protocal.Commands.OrderPhaseChange> changes, Protocal.Commands.OrderChangeType changeType)
        {
            Dictionary<Guid, Protocal.Commands.OrderPhaseChange> result = new Dictionary<Guid, Protocal.Commands.OrderPhaseChange>(changes.Count);
            foreach (var eachChange in changes)
            {
                if (eachChange.ChangeType == changeType)
                {
                    if (!result.ContainsKey(eachChange.Tran.Id))
                    {
                        result.Add(eachChange.Tran.Id, eachChange);
                    }
                }
            }

            if (result.Count > 0)
            {
                changes.Clear();
                foreach (var eachItem in result.Values)
                {
                    changes.Add(eachItem);
                }
            }
        }

    }
}