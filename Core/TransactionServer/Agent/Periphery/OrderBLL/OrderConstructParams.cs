using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Util;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL
{
    public class OrderConstructParams
    {
        public Guid Id { get; set; }
        public Guid CurrencyId { get; set; }
        public string Code { get; set; }
        public string OriginCode { get; set; }
        public string BlotterCode { get; set; }
        public OrderPhase Phase { get; set; }
        public bool IsOpen { get; set; }
        public bool IsBuy { get; set; }
        public Price SetPrice { get; set; }
        public Price SetPrice2 { get; set; }
        public Price ExecutePrice { get; set; }
        public decimal Lot { get; set; }
        public decimal OriginalLot { get; set; }
        public decimal LotBalance { get; set; }
        public TradeOption TradeOption { get; set; }
        public int HitCount { get; set; }
        public Price BestPrice { get; set; }
        public DateTime? BestTime { get; set; }
        public OrderHitStatus HitStatus { get; set; }
        public decimal InterestPerLot { get; set; }
        public decimal StoragePerLot { get; set; }
        public int SetPriceMaxMovePips { get; set; }
        public int DQMaxMove { get; set; }
        public DateTime? PriceTimestamp { get; set; }
        public OperationType OperationType { get; set; }
        public DateTime? InterestValueDate { get; set; }
        public decimal? MinLot { get; set; }
        public decimal? MaxShow { get; set; }
        public Guid? OrderBatchInstructionID { get; set; }
        public decimal EstimateCloseCommission { get; set; }
        public decimal EstimateCloseLevy { get; set; }


        protected virtual OrderConstructParams Create()
        {
            return new OrderConstructParams();
        }


        internal virtual void FillOrderCommonData(Protocal.OrderCommonData orderData, Guid instrumentId, DateTime? tradeDay = null)
        {
            this.Id = orderData.Id;
            this.TradeOption = orderData.TradeOption;
            this.IsOpen = orderData.IsOpen;
            this.IsBuy = orderData.IsBuy;
            this.SetPrice = PriceHelper.CreatePrice(orderData.SetPrice, instrumentId, tradeDay);
            this.SetPrice2 = PriceHelper.CreatePrice(orderData.SetPrice2, instrumentId, tradeDay);
            this.SetPriceMaxMovePips = orderData.SetPriceMaxMovePips;
            this.DQMaxMove = orderData.DQMaxMove;
            this.Lot = orderData.Lot;
            this.OriginalLot = orderData.OriginalLot != null ? orderData.OriginalLot.Value : orderData.Lot;
            this.LotBalance = this.Lot;
        }


        internal virtual OrderConstructParams Copy()
        {
            var result = this.Create();
            result.Id = Guid.NewGuid();
            result.Code = this.Code;
            result.OriginCode = this.OriginCode;
            result.BlotterCode = this.BlotterCode;
            result.Phase = this.Phase;
            result.IsOpen = this.IsOpen;
            result.IsBuy = this.IsBuy;
            result.SetPrice = this.SetPrice;
            result.SetPrice2 = this.SetPrice2;
            result.ExecutePrice = this.ExecutePrice;
            result.Lot = this.Lot;
            result.OriginalLot = this.OriginalLot;
            result.LotBalance = this.LotBalance;
            result.TradeOption = this.TradeOption;
            result.HitCount = this.HitCount;
            result.BestPrice = this.BestPrice;
            result.BestTime = this.BestTime;
            result.HitStatus = this.HitStatus;
            result.InterestPerLot = this.InterestPerLot;
            result.StoragePerLot = this.StoragePerLot;
            result.SetPriceMaxMovePips = this.SetPriceMaxMovePips;
            result.DQMaxMove = this.DQMaxMove;
            result.PriceTimestamp = this.PriceTimestamp;
            result.OperationType = this.OperationType;
            result.InterestValueDate = this.InterestValueDate;
            return result;
        }

    }

    internal sealed class PhysicalOrderConstructParams : OrderConstructParams
    {
        internal InstalmentConstructParams Instalment { get; set; }
        internal PhysicalConstructParams PhysicalSettings { get; set; }

        protected override OrderConstructParams Create()
        {
            return new PhysicalOrderConstructParams();
        }
        internal override OrderConstructParams Copy()
        {
            var result = (PhysicalOrderConstructParams)base.Copy();
            if (this.Instalment != null)
            {
                result.Instalment = this.Instalment.Copy();
            }
            result.PhysicalSettings = this.PhysicalSettings.Copy();
            return result;
        }

        internal override void FillOrderCommonData(OrderCommonData orderData, Guid instrumentId, DateTime? tradeDay = null)
        {
            base.FillOrderCommonData(orderData, instrumentId, tradeDay);
            this.PhysicalSettings = new PhysicalConstructParams();
            if (orderData is Protocal.Physical.PhysicalOrderData)
            {
                var physicalOrderData = (Protocal.Physical.PhysicalOrderData)orderData;
                this.PhysicalSettings.Fill(physicalOrderData);
                if (physicalOrderData.InstalmentPart != null && this.PhysicalSettings.PhysicalType != Protocal.Physical.PhysicalType.FullPayment)
                {
                    this.Instalment = new InstalmentConstructParams();
                    this.Instalment.FIll(physicalOrderData.InstalmentPart);
                }
            }
            else if (orderData is Protocal.Physical.PhysicalOrderBookData)
            {
                var bookData = (Protocal.Physical.PhysicalOrderBookData)orderData;
                this.PhysicalSettings.FillForBookData(bookData);
                if (bookData.InstalmentPart != null)
                {
                    this.Instalment = new InstalmentConstructParams();
                    this.Instalment.FIll(bookData.InstalmentPart);
                }
            }

        }


    }


    internal sealed class InstalmentConstructParams
    {
        internal Guid? InstalmentPolicyId { get; set; }
        internal decimal DownPayment { get; set; }
        internal InstalmentType InstalmentType { get; set; }
        internal RecalculateRateType RecalculateRateType { get; set; }
        internal int Period { get; set; }
        internal InstalmentFrequence Frequence { get; set; }
        internal bool IsInstalmentOverdue { get; set; }
        internal int InstalmentOverdueDay { get; set; }
        internal DownPaymentBasis DownPaymentBasis { get; set; }

        internal InstalmentConstructParams Copy()
        {
            return new InstalmentConstructParams
            {
                InstalmentPolicyId = this.InstalmentPolicyId,
                DownPayment = this.DownPayment,
                InstalmentType = this.InstalmentType,
                RecalculateRateType = this.RecalculateRateType,
                Period = this.Period,
                Frequence = this.Frequence,
                IsInstalmentOverdue = this.IsInstalmentOverdue,
                InstalmentOverdueDay = this.InstalmentOverdueDay,
                DownPaymentBasis = this.DownPaymentBasis
            };
        }

        internal void FIll(Protocal.Physical.InstalmentPart instalmentData)
        {
            this.InstalmentPolicyId = instalmentData.InstalmentPolicyId;
            this.DownPayment = instalmentData.DownPayment;
            this.InstalmentType = instalmentData.InstalmentType;
            this.RecalculateRateType = instalmentData.RecalculateRateType;
            this.Period = instalmentData.Period;
            this.Frequence = instalmentData.InstalmentFrequence;
        }


    }

    internal sealed class PhysicalConstructParams
    {
        internal PhysicalTradeSide PhysicalTradeSide { get; set; }
        internal decimal PhysicalOriginValue { get; set; }
        internal decimal PhysicalOriginValueBalance { get; set; }
        internal decimal PaidPledgeBalance { get; set; }
        internal decimal PaidPledge { get; set; }
        internal int PhysicalValueMatureDay { get; set; }
        internal Guid? PhysicalRequestId { get; set; }
        internal decimal AdvanceAmount { get; set; }
        internal Protocal.Physical.PhysicalType PhysicalType { get; set; }

        internal PhysicalConstructParams Copy()
        {
            return new PhysicalConstructParams
            {
                PhysicalTradeSide = this.PhysicalTradeSide,
                PhysicalOriginValue = this.PhysicalOriginValue,
                PhysicalOriginValueBalance = this.PhysicalOriginValueBalance,
                PaidPledgeBalance = this.PaidPledgeBalance,
                PhysicalValueMatureDay = this.PhysicalValueMatureDay,
                PhysicalRequestId = this.PhysicalRequestId,
                PhysicalType = this.PhysicalType,
                AdvanceAmount = this.AdvanceAmount
            };
        }

        internal void Fill(Protocal.Physical.PhysicalOrderData orderData)
        {
            this.PhysicalTradeSide = orderData.PhysicalTradeSide;
            this.PhysicalType = orderData.PhysicalType;
            this.AdvanceAmount = orderData.AdvanceAmount;
        }

        internal void FillForBookData(Protocal.Physical.PhysicalOrderBookData bookData)
        {
            this.PhysicalTradeSide = bookData.PhysicalTradeSide;
            this.PhysicalRequestId = bookData.PhysicalRequestId;
            this.PhysicalType = bookData.PhysicalType;
            if (bookData.PhysicalValueMatureDay != null)
            {
                this.PhysicalValueMatureDay = bookData.PhysicalValueMatureDay.Value;
            }
        }

    }

    internal sealed class BOOrderConstructParams : OrderConstructParams
    {
        internal Guid BetTypeId { get; set; }
        internal int Frequency { get; set; }
        internal long BetOption { get; set; }
        internal decimal Odds { get; set; }
        internal DateTime? SettleTime { get; set; }
        internal decimal PaidPledge { get; set; }
        internal decimal PaidPledgeBalance { get; set; }

        protected override OrderConstructParams Create()
        {
            return new BOOrderConstructParams();
        }

        internal override OrderConstructParams Copy()
        {
            var result =  (BOOrderConstructParams)base.Copy();
            result.BetTypeId = this.BetTypeId;
            result.Frequency = this.Frequency;
            result.BetOption = this.BetOption;
            result.Odds = this.Odds;
            result.SettleTime = this.SettleTime;
            result.PaidPledge = this.PaidPledge;
            result.PaidPledgeBalance = this.PaidPledgeBalance;
            return result;
        }


        internal override void FillOrderCommonData(OrderCommonData orderData, Guid instrumentId, DateTime? tradeDay = null)
        {
            base.FillOrderCommonData(orderData, instrumentId, tradeDay);
            var boOrderData = (BOOrderData)orderData;
            this.BetTypeId = boOrderData.BOBetTypeID;
            this.Frequency = boOrderData.BOFrequency;
            this.BetOption = boOrderData.BOBetOption;
            this.Odds = boOrderData.BOOdds;
        }

    }
}
