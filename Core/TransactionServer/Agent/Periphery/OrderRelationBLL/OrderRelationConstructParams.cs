using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderRelationBLL
{
    public class OrderRelationConstructParams
    {
        internal OrderRelationConstructParams()
        {
            this.Id = Guid.NewGuid();
        }

        internal Guid Id { get; set; }
        internal Order CloseOrder { get; set; }
        internal Order OpenOrder { get; set; }
        internal decimal ClosedLot { get; set; }

        internal DateTime? OpenOrderExecuteTime { get; set; }
        internal string OpenOrderExecutePrice { get; set; }

        internal DateTime? CloseTime { get; set; }
        internal decimal Commission { get; set; }
        internal decimal Levy { get; set; }
        internal decimal OtherFee { get; set; }
        internal decimal InterestPL { get; set; }
        internal decimal StoragePL { get; set; }
        internal decimal TradePL { get; set; }
        internal DateTime? ValueTime { get; set; }
        internal int Decimals { get; set; }
        internal decimal RateIn { get; set; }
        internal decimal RateOut { get; set; }
        internal OperationType OperationType { get; set; }

        internal decimal EstimateCloseCommissionOfOpenOrder { get; set; }
        internal decimal EstimateCloseLevyOfOpenOrder { get; set; }


        internal virtual void Fill(Account account, Protocal.OrderRelationData orderRelationData)
        {
            this.OperationType = Framework.OperationType.AsNewRecord;
            this.CloseOrder = account.GetOrder(orderRelationData.CloseOrderId);
            this.OpenOrder = account.GetOrder(orderRelationData.OpenOrderId);
            this.ClosedLot = orderRelationData.ClosedLot;
            this.OpenOrderExecuteTime = orderRelationData.OpenOrderExecuteTime;
            this.OpenOrderExecutePrice = orderRelationData.OpenOrderExecutePrice;
            var bookData = orderRelationData as Protocal.OrderRelationBookData;
            if (bookData != null)
            {
                this.FillBookData(bookData);
            }
        }

        protected virtual void FillBookData(Protocal.OrderRelationBookData bookData)
        {
            this.CloseTime = bookData.CloseTime;
            this.Commission = bookData.Commission;
            this.Levy = bookData.Levy;
            this.OtherFee = bookData.OtherFee;
            this.InterestPL = bookData.InterestPL;
            this.StoragePL = bookData.StoragePL;
            this.TradePL = bookData.TradePL;

            if (bookData.ValuedInfo != null)
            {
                this.ValueTime = bookData.ValuedInfo.ValueTime;
                this.Decimals = bookData.ValuedInfo.Decimals;
                this.RateIn = bookData.ValuedInfo.RateIn;
                this.RateOut = bookData.ValuedInfo.RateOut;
            }
        }

    }

    public sealed class PhysicalOrderRelationConstructParams : OrderRelationConstructParams
    {
        internal decimal PhysicalValue { get; set; }
        internal decimal OverdueCutPenalty { get; set; }
        internal decimal ClosePenalty { get; set; }
        internal decimal PayBackPledge { get; set; }
        internal decimal ClosedPhysicalValue { get; set; }

        internal DateTime? PhysicalValueMatureDay { get; set; }
        internal DateTime? RealPhysicalValueMatureDate { get; set; }
    }


    internal sealed class OrderRelationRecord
    {
        internal OrderRelationRecord(Order openOrder, decimal closedLot)
        {
            this.OpenOrder = openOrder;
            this.ClosedLot = closedLot;
        }

        internal Order OpenOrder { get; private set; }
        internal decimal ClosedLot { get; private set; }
    }

}
