using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Core.TransactionServer.Agent
{
    public enum TradeDirection
    {
        Buy,
        Sell
    }

    internal static class TradeDirectionExtension
    {
        public static bool SameAs(this TradeDirection direction, bool isBuy)
        {
            return (isBuy && direction == TradeDirection.Buy) || (!isBuy && direction == TradeDirection.Sell);
        }
    }

    internal enum TradeAction
    {
        Buy,
        Sell,
        Deposit,
        Delivery,
        ShortSell
    }

    internal static class TradeActionExtension
    {
        public static TradeDirection GetDirection(this TradeAction action)
        {
            if (action == TradeAction.Buy || action == TradeAction.Deposit)
            {
                return TradeDirection.Buy;
            }
            else
            {
                return TradeDirection.Sell;
            }
        }        
    }

    internal enum QuotationTrend
    {
        Identical,
        Up,
        Down
    }

    internal enum PriceCompareResult
    {
        Fair,
        Better,
        Worse
    }

    public enum OrderHitStatus
    {
        None,
        Hit,
        ToAutoFill,
        ToAutoLimitClose,
        ToAutoStopClose,
        ToCancel
    }

    internal static class OrderHitStatusHelper
    {
        internal static bool IsPending(this OrderHitStatus status)
        {
            return status == OrderHitStatus.None || status == OrderHitStatus.Hit;
        }

        internal static bool IsFinal(this OrderHitStatus status)
        {
            return !status.IsPending();
        }
    }

    public enum MarginFormula
    {
        FixedAmount=0,
        CS=1,

        ///<summary>CS/Price</summary>
        CSiPrice = 2,
        ///<summary>CS*Price</summary>
        CSxPrice = 3,

        ///<summary>期货使用，目前没有用到</summary>
        FKLI = 4,
        ///<summary>期货使用，目前没有用到</summary>
        FCPO = 5,

        ///<summary>CS/MarketPrice</summary>
        CSiMarketPrice = 6,
        ///<summary>CS*MarketPrice</summary>
        CSxMarketPrice = 7
    }

    internal static class MarginFormulaHelper
    {
        internal static bool MarketPriceInvolved(this MarginFormula marginFormula)
        {
            return marginFormula == MarginFormula.CSiMarketPrice || marginFormula == MarginFormula.CSxMarketPrice;
        }
    }

    internal enum TradePLFormula
    {
        ///<summary>(S-B)*CS</summary>
        S_BxCS = 0,
        ///<summary>(S-B)*CS/L</summary>
        S_BxCSiL = 1,
        ///<summary>(1/S-1/B)*CS</summary>
        Si1_Bi1xCSiL = 2,
        ///<summary>(S-B)*CS/O</summary>
        S_BxCSiO = 3
    }

    internal enum InterestFormula
    {
        FixedAmount,
        CS,
        ///<summary>CS/Price</summary>
        CSiPrice,
        ///<summary>CS*Price</summary>
        CSxPrice,
    }

  
}