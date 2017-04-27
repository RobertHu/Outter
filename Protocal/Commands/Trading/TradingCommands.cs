using iExchange.Common;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Commands
{
    [ProtoContract]
    public sealed class TradingUpdateBalanceCommand : TradingCommand
    {
        [ProtoMember(6)]
        public ModifyType ModifyType { get; set; }
        [ProtoMember(7)]
        public Guid CurrencyId { get; set; }
        [ProtoMember(8)]
        public decimal Balance { get; set; }
    }


    [ProtoContract]
    public sealed class TradingPrePaymentCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid CurrencyId { get; set; }
        [ProtoMember(7)]
        public decimal Balance { get; set; }
        [ProtoMember(8)]
        public decimal TotalPaidAmount { get; set; }
    }



    [ProtoContract]
    public sealed class TradingExecuteCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid InstrumentId { get; set; }

        [ProtoMember(7)]
        public Guid TransactionId { get; set; }
    }

    [ProtoContract]
    public sealed class TradingHitCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid OrderId { get; set; }
    }

    [ProtoContract]
    public sealed class TradingResetAlertLevelCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid UserId { get; set; }
    }


    [ProtoContract]
    public sealed class TradingTransferCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid TransferId { get; set; }
        [ProtoMember(7)]
        public Guid RemitterId { get; set; }
        [ProtoMember(8)]
        public Guid PayeeId { get; set; }
        [ProtoMember(9)]
        public TransferAction Action { get; set; }
    }


    [ProtoContract]
    public sealed class TradingAcceptPlaceCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid TransactionId { get; set; }

        [ProtoMember(7)]
        public Guid InstrumentId { get; set; }
    }

    [ProtoContract]
    public sealed class TradingCancelByManagerCommand : TradingCommand
    {
        [ProtoMember(6)]
        public Guid TransactionId { get; set; }
        [ProtoMember(7)]
        public Guid AccountId { get; set; }
        [ProtoMember(8)]
        public Guid InstrumentId { get; set; }
        [ProtoMember(9)]
        public TransactionError ErrorCode { get; set; }
        [ProtoMember(10)]
        public CancelReason Reason { get; set; }
    }



    [ProtoContract]
    public sealed class TradingPriceAlertCommand : TradingCommand
    {
        [ProtoMember(6)]
        public List<UserPriceAlertData> UserPriceAlerts { get; set; }

        [ProtoMember(7)]
        public AlertType Type { get; set; }
    }

    public enum AlertType
    {
        None,
        Hit = 1,
        Expired = 2
    }


    [ProtoContract]
    public sealed class UserPriceAlertData
    {
        [ProtoMember(1)]
        public Guid UserId { get; set; }

        [ProtoMember(2)]
        public List<PriceAlertData> PriceAlerts { get; set; }
    }


    [ProtoContract]
    public sealed class PriceAlertData
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public AlertState State { get; set; }
        [ProtoMember(3)]
        public string HitPrice { get; set; }
        [ProtoMember(4)]
        public DateTime? HitTime { get; set; }
    }

}
