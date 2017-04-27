using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal sealed class IfDoneTransactionParser
    {
        public static readonly IfDoneTransactionParser Default = new IfDoneTransactionParser();
        private const int MAX_DONE_TRANSACTION_PER_IF_TRANSACTION = 2;
        private IfDoneTransactionParser() { }
        static IfDoneTransactionParser() { }

        public List<Transaction> FillDoneTrans(Transaction tran, Protocal.TransactionData tranData)
        {
            List<Transaction> result = new List<Transaction>();
            foreach (var eachOrderData in tranData.Orders)
            {
                if (eachOrderData.IfDoneOrderSetting == null) continue;
                Transaction doneTran = this.FillIndividualDoneTran(tran, eachOrderData.IfDoneOrderSetting.LimitPrice, eachOrderData.IfDoneOrderSetting.StopPrice, eachOrderData.Id);
                if (doneTran != null)
                {
                    result.Add(doneTran);
                }
            }

            if (result.Count > MAX_DONE_TRANSACTION_PER_IF_TRANSACTION)
            {
                throw new ArgumentOutOfRangeException(string.Format("Done Transaction Count {0}, if transaction Id= {1}", result.Count, tran.Id));
            }
            return result;
        }

        internal Transaction FillDoneTran(Transaction tran, Guid sourceOrderId, List<Order> orders)
        {
            Order originDoneOrder1 = orders[0];
            Order originDoneOrder2 = orders.Count == 2 ? orders[1] : null;
            Price limitPrice = originDoneOrder1.TradeOption == TradeOption.Better ? originDoneOrder1.SetPrice : (originDoneOrder2 == null ? null : originDoneOrder2.SetPrice);
            Price stopPrice = originDoneOrder1.TradeOption == TradeOption.Stop ? originDoneOrder1.SetPrice : (originDoneOrder2 == null ? null : originDoneOrder2.SetPrice);
            return this.CreateDoneTransactionAndOrderRelation(tran, sourceOrderId, limitPrice, stopPrice);
        }


        private Transaction FillIndividualDoneTran(Transaction tran, string doneLimitPrice, string doneStopPrice, Guid sourceOrderId)
        {
            if (string.IsNullOrEmpty(doneLimitPrice) && string.IsNullOrEmpty(doneStopPrice)) return null;
            var instrument = tran.SettingInstrument();
            var limitPrice = string.IsNullOrEmpty(doneLimitPrice) ? null : Price.CreateInstance(doneLimitPrice, instrument.NumeratorUnit, instrument.Denominator);
            var stopPrice = string.IsNullOrEmpty(doneStopPrice) ? null : Price.CreateInstance(doneStopPrice, instrument.NumeratorUnit, instrument.Denominator);
            return this.CreateDoneTransactionAndOrderRelation(tran, sourceOrderId, limitPrice, stopPrice);
        }

        private Transaction CreateDoneTransactionAndOrderRelation(Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
        {
            var factory = TransactionFacade.CreateAddTranCommandFactory(ifTran.OrderType, ifTran.InstrumentCategory);
            var command = factory.CreateDoneTransaction(ifTran.Owner, ifTran, sourceOrderId, limitPrice, stopPrice);
            command.Execute();
            return command.Result;
        }
    }
}
