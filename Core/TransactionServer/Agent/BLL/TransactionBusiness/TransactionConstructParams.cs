using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    public sealed class TransactionConstructParams
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public Guid InstrumentId { get; set; }
        public TransactionType Type { get; set; }
        public TransactionSubType SubType { get; set; }
        public TransactionPhase Phase { get; set; }
        public OrderType OrderType { get; set; }
        public ExpireType ExpireType { get; set; }
        public decimal ConstractSize { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime SubmitTime { get; set; }
        public DateTime? ExecuteTime { get; set; }
        public Guid SubmitorId { get; set; }
        public Guid? ApproveId { get; set; }
        public Guid? SourceOrderId { get; set; }
        public DateTime? SetPriceTimestamp { get; set; }
        public OperationType OperationType { get; set; }
        public bool PlaceByRiskMonitor { get; set; }
        public bool FreePlacingPreCheck { get; set; }
        public bool DisableAcceptLmtVariation { get; set; }
        public AppType AppType { get; set; }

        internal void Fill(Protocal.TransactionCommonData tranData)
        {
            this.Id = tranData.Id;
            this.InstrumentId = tranData.InstrumentId;
            this.Type = tranData.Type;
            this.SubType = tranData.SubType;
            this.OrderType = tranData.OrderType;
            this.FreePlacingPreCheck = tranData.FreePlacingPreCheck;
            this.PlaceByRiskMonitor = tranData.PlaceByRiskMonitor;
            this.DisableAcceptLmtVariation = tranData.DisableLmtVariation;
            this.BeginTime = tranData.BeginTime;
            this.EndTime = tranData.EndTime;
            this.ExpireType = tranData.ExpireType;
            this.SubmitorId = tranData.SubmitorId;
            this.SubmitTime = tranData.SubmitTime;
            this.SourceOrderId = tranData.SourceOrderId;
            this.AppType = tranData.AppType;
        }

        public InstrumentCategory GetInstrumentCategory(Guid accountId)
        {
            var instrument = Settings.Setting.Default.GetInstrument(this.InstrumentId);
            return instrument.Category;
        }
    }
}
