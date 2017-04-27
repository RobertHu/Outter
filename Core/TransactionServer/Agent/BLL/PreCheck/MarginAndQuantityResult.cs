using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.BLL.PreCheck
{
    internal struct MarginAndQunatity
    {
        internal static readonly MarginAndQunatity Empty = new MarginAndQunatity(BuySellPair.Empty, BuySellPair.Empty);

        internal MarginAndQunatity(BuySellPair margin, BuySellPair quantity)
            : this()
        {
            this.Margin = margin;
            this.Quantity = quantity;
        }

        internal BuySellPair Margin { get; private set; }
        internal BuySellPair Quantity { get; private set; }

        internal MarginAndQunatity Add(bool isBuy, MarginAndQunatity other)
        {
            return isBuy ? new MarginAndQunatity(this.Margin.AddBuy(other.Margin), this.Quantity.AddBuy(other.Quantity)) :
                 new MarginAndQunatity(this.Margin.AddSell(other.Margin), this.Quantity.AddSell(other.Quantity));
        }

        internal MarginAndQunatity Add(bool isBuy, decimal margin, decimal quantity)
        {
            return isBuy ? new MarginAndQunatity(this.Margin.AddBuy(margin), this.Quantity.AddBuy(quantity)) :
                new MarginAndQunatity(this.Margin.AddSell(margin), this.Quantity.AddSell(quantity));
        }

        internal MarginAndQunatity Add(bool isBuy, BuySellPair margin, BuySellPair quantity)
        {
            return isBuy ? new MarginAndQunatity(this.Margin.AddBuy(margin), this.Quantity.AddBuy(quantity)) :
                   new MarginAndQunatity(this.Margin.AddSell(margin), this.Quantity.AddSell(quantity));
        }


        internal MarginAndQunatity AddBuy(MarginAndQunatity other)
        {
            return new MarginAndQunatity(this.Margin.AddBuy(other.Margin), this.Quantity.AddBuy(other.Quantity));
        }

        internal MarginAndQunatity AddSell(MarginAndQunatity other)
        {
            return new MarginAndQunatity(this.Margin.AddSell(other.Margin), this.Quantity.AddSell(other.Quantity));
        }

        public static MarginAndQunatity operator +(MarginAndQunatity left, MarginAndQunatity right)
        {
            return new MarginAndQunatity(left.Margin + right.Margin, left.Quantity + right.Quantity);
        }

        public override string ToString()
        {
            return string.Format("Margin = {0};  Quantity = {1}", this.Margin, this.Quantity);
        }

    }


    internal sealed class MarginAndQuantityResult
    {
        internal MarginAndQuantityResult()
        {
            this.Normal = MarginAndQunatity.Empty;
            this.Partial = MarginAndQunatity.Empty;
        }

        internal MarginAndQunatity Normal { get; private set; }
        internal MarginAndQunatity Partial { get; private set; }

        internal BuySellPair Margin { get { return this.Normal.Margin; } }
        internal BuySellPair Quantity { get { return this.Normal.Quantity; } }
        internal BuySellPair PartialMargin { get { return this.Partial.Margin; } }
        internal BuySellPair PartialQuantity { get { return this.Partial.Quantity; } }


        internal void Add(bool isBuy, MarginAndQuantityResult unfilledResult, MarginAndQuantityResult filledResult)
        {
            if (isBuy)
            {
                var normalMargin = new BuySellPair(unfilledResult.Normal.Margin.Buy, filledResult.Normal.Margin.Sell);
                var normalQuantity = new BuySellPair(unfilledResult.Normal.Quantity.Buy, filledResult.Normal.Quantity.Sell);
                var partialMargin = new BuySellPair(unfilledResult.Partial.Margin.Buy, filledResult.Partial.Margin.Sell);
                var partialQuantity = new BuySellPair(unfilledResult.Partial.Quantity.Buy, filledResult.Partial.Quantity.Sell);
                this.Normal += new MarginAndQunatity(normalMargin, normalQuantity);
                this.Partial += new MarginAndQunatity(partialMargin, partialQuantity);
            }
            else
            {
                var margin = new BuySellPair(filledResult.Normal.Margin.Buy, unfilledResult.Normal.Margin.Sell);
                var quantity = new BuySellPair(filledResult.Normal.Quantity.Buy, unfilledResult.Normal.Margin.Sell);
                var partialMargin = new BuySellPair(filledResult.Partial.Margin.Buy, unfilledResult.Partial.Margin.Sell);
                var partialQuantity = new BuySellPair(filledResult.Partial.Quantity.Buy, unfilledResult.Partial.Quantity.Sell);
                this.Normal += new MarginAndQunatity(margin, quantity);
                this.Partial += new MarginAndQunatity(partialMargin, partialQuantity);
            }
        }

        internal void AddMarginAndQuantity(bool isBuy, bool isPartialPaymentPhysicalOrder, decimal margin, decimal quantity)
        {
            if (isPartialPaymentPhysicalOrder)
            {
                this.Partial = this.Partial.Add(isBuy, margin, quantity);

            }
            else
            {
                this.Normal = this.Normal.Add(isBuy, margin, quantity);
            }
        }

        internal void AddMarginAndQuantity(bool isBuy, decimal margin, decimal quantity)
        {
            this.Normal = this.Normal.Add(isBuy, margin, quantity);
        }


        internal void Add(bool isBuy, BuySellPair margin, BuySellPair quantity, BuySellPair partialMargin, BuySellPair partialQuantity)
        {
            this.Normal = this.Normal.Add(isBuy, margin, quantity);
            this.Partial = this.Partial.Add(isBuy, partialMargin, partialQuantity);
        }

        internal void Add(bool isBuy, MarginAndQuantityResult other)
        {
            this.Normal = this.Normal.Add(isBuy, other.Normal);
            this.Partial = this.Partial.Add(isBuy, other.Partial);
        }

        internal void Add(MarginAndQuantityResult other)
        {
            this.Normal += other.Normal;
            this.Partial += other.Partial;
        }

        public static MarginAndQuantityResult operator +(MarginAndQuantityResult left, MarginAndQuantityResult right)
        {
            left.Add(right);
            return left;
        }

        public override string ToString()
        {
            return string.Format("Normal = {0} . Partail = {1}", this.Normal, this.Partial);
        }

    }
}
