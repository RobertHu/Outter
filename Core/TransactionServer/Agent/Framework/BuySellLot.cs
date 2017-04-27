using Core.TransactionServer.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Framework
{
    public struct BuySellLot : IEquatable<BuySellLot>
    {
        public static readonly BuySellLot Empty = new BuySellLot(0m, 0m);
        private decimal _buyLot;
        private decimal _sellLot;
        public BuySellLot(decimal buyLot, decimal sellLot)
        {
            _buyLot = buyLot;
            _sellLot = sellLot;
        }

        public decimal BuyLot
        {
            get
            {
                return _buyLot;
            }
        }

        public decimal SellLot
        {
            get
            {
                return _sellLot;
            }
        }

        public override string ToString()
        {
            return string.Format("BuyLot = {0}, SellLot = {1}", this.BuyLot, this.SellLot);
        }

        public decimal NetPosition { get { return this.BuyLot - this.SellLot; } }

        internal bool IsNetPosWithDiffDirection(BuySellLot other)
        {
            return this.NetPosition * other.NetPosition < 0;
        }

        internal bool IsAbsNetPosLessThan(BuySellLot other)
        {
            return Math.Abs(this.NetPosition) < Math.Abs(other.NetPosition);
        }

        internal bool IsAbsNetPosGreateThan(BuySellLot other)
        {
            return Math.Abs(this.NetPosition) > Math.Abs(other.NetPosition);
        }

        internal bool IsAbsNetPosLessEqual(BuySellLot other)
        {
            return Math.Abs(this.NetPosition) <= Math.Abs(other.NetPosition);
        }

        public static BuySellLot operator +(BuySellLot left, BuySellLot right)
        {
            return new BuySellLot(left.BuyLot + right.BuyLot, left.SellLot + right.SellLot);
        }

        public static bool operator ==(BuySellLot left, BuySellLot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BuySellLot left, BuySellLot right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return this.Equals((BuySellLot)obj);
        }

        public override int GetHashCode()
        {
            return HashCodeGenerator.Calculate(_buyLot.GetHashCode(), _sellLot.GetHashCode());
        }

        public bool Equals(BuySellLot other)
        {
            return this.BuyLot == other.BuyLot && this.SellLot == other.SellLot;
        }

    }

    public struct BuySellPair : IEquatable<BuySellPair>
    {
        public static readonly BuySellPair Empty = new BuySellPair(0m, 0m);

        public BuySellPair(decimal buy, decimal sell)
            : this()
        {
            this.Buy = buy;
            this.Sell = sell;
        }

        public decimal Buy { get; private set; }

        public decimal Sell { get; private set; }


        public bool Equals(BuySellPair other)
        {
            return this.Buy == other.Buy && this.Sell == other.Sell;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((BuySellPair)obj);
        }

        public override string ToString()
        {
            return string.Format("buy={0}, sell={1}", this.Buy, this.Sell);
        }


        public override int GetHashCode()
        {
            return HashCodeGenerator.Calculate(this.Buy.GetHashCode(), this.Sell.GetHashCode());
        }

        public BuySellPair AddBuy(decimal buy)
        {
            return new BuySellPair(this.Buy + buy, this.Sell);
        }

        public BuySellPair AddBuy(BuySellPair other)
        {
            return new BuySellPair(this.Buy + other.Buy, this.Sell);
        }


        public BuySellPair AddSell(decimal sell)
        {
            return new BuySellPair(this.Buy, this.Sell + sell);
        }

        public BuySellPair AddSell(BuySellPair other)
        {
            return new BuySellPair(this.Buy, this.Sell + other.Sell);
        }

        public static bool operator ==(BuySellPair left, BuySellPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BuySellPair left, BuySellPair right)
        {
            return !(left == right);
        }

        public static BuySellPair operator +(BuySellPair left, BuySellPair right)
        {
            return new BuySellPair(left.Buy + right.Buy, left.Sell + right.Sell);
        }
    }

}
