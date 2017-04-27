using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness
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
        public bool PlacedByRiskMonitor { get; set; }
        public DateTime? PriceTimestamp { get; set; }
        public bool DisableAcceptLmtVariation { get; set; }
        public OperationType OperationType { get; set; }
        public DateTime? InterestValueDate { get; set; }
        public decimal? MinLot { get; set; }
        public decimal? MaxShow { get; set; }
        public Price JudgePrice { get; set; }
        public DateTime JudgePriceTimeStamp { get; set; }

        protected virtual OrderConstructParams Create()
        {
            return new OrderConstructParams();
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
            result.PlacedByRiskMonitor = this.PlacedByRiskMonitor;
            result.PriceTimestamp = this.PriceTimestamp;
            result.DisableAcceptLmtVariation = this.DisableAcceptLmtVariation;
            result.OperationType = this.OperationType;
            result.InterestValueDate = this.InterestValueDate;
            return result;
        }

    }
}
