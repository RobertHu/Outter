using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL
{
    internal static class TransactionErrorExtension
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionErrorExtension));

        internal static Protocal.CancelType ToCancelType(this TransactionError error)
        {
            Protocal.CancelType result = Protocal.CancelType.None;
            switch (error)
            {
                case TransactionError.AccountIsInReset:
                    result = Protocal.CancelType.AccountIsInReset;
                    break;
                case TransactionError.AccountIsIntializing:
                    result = Protocal.CancelType.AccountIsInIntialized;
                    break;
                case TransactionError.InstrumentIsNotAccepting:
                    result = Protocal.CancelType.InstrumentIsNotAccepting;
                    break;
                case TransactionError.HasNoQuotationExists:
                    result = Protocal.CancelType.InstrumentNoQuotation;
                    break;
                case TransactionError.InvalidPrice:
                    result = Protocal.CancelType.InvalidPrice;
                    break;
                case TransactionError.MarginIsNotEnough:
                    result = Protocal.CancelType.MarginIsNotEnough;
                    break;
                case TransactionError.OpenOrderNotExists:
                    result = Protocal.CancelType.OpenOrderNotExists;
                    break;
                case TransactionError.OrderLotExceedMaxLot:
                    result = Protocal.CancelType.OrderLotExceedMaxLot;
                    break;
                case TransactionError.OrderTypeIsNotAcceptable:
                    result = Protocal.CancelType.OrderTypeIsNotAcceptable;
                    break;
                case TransactionError.OutOfAcceptDQVariation:
                    result = Protocal.CancelType.OutOfAcceptDQVariation;
                    break;
                case TransactionError.PrepaymentIsNotAllowed:
                    result = Protocal.CancelType.PrepaymentIsNotAllowed;
                    break;
                case TransactionError.PriceIsDisabled:
                    result = Protocal.CancelType.PriceIsDisabled;
                    break;
                case TransactionError.PriceIsOutOfDate:
                    result = Protocal.CancelType.PriceIsOutOfDate;
                    break;
                case TransactionError.RuntimeError:
                    result = Protocal.CancelType.RuntimeError;
                    break;
                case TransactionError.ShortSellNotAllowed:
                    result = Protocal.CancelType.ShortSellNotAllowed;
                    break;
                case TransactionError.SetPriceTooCloseToMarket:
                    result = Protocal.CancelType.SetPriceTooCloseToMarket;
                    break;
                case TransactionError.TimingIsNotAcceptable:
                    result = Protocal.CancelType.TimingIsNotAcceptable;
                    break;
                case TransactionError.TransactionAlreadyExists:
                    result = Protocal.CancelType.TransactionAlreadyExists;
                    break;
                case TransactionError.ExceedOpenLotBalance:
                    result = Protocal.CancelType.ExceedOpenLotBalance;
                    break;
                case TransactionError.ExceedMaxPhysicalValue:
                    result = Protocal.CancelType.ExceedMaxPhysicalValue;
                    break;
                default:
                    Logger.WarnFormat("unrecognize error = {0}", error);
                    break;
            }
            return result;
        }

        internal static Protocal.CancelType ToCancelType(this CancelReason reason)
        {
            Protocal.CancelType result = Protocal.CancelType.None;
            switch (reason)
            {
                case CancelReason.CustomerCanceled:
                    result = Protocal.CancelType.CustomerCanceled;
                    break;
                case CancelReason.DealerCanceled:
                    result = Protocal.CancelType.DealerCanceled;
                    break;
                case CancelReason.RiskMonitorCanceled:
                    result = Protocal.CancelType.RiskMonitorCanceled;
                    break;
                default:
                    Logger.WarnFormat("not recognize reason = {0}", reason);
                    break;
            }
            return result;
        }
    }

}
