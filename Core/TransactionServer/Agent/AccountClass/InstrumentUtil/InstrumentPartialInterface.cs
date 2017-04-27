using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.AccountClass
{
    public partial class Instrument : IEquatable<Instrument>
    {
        internal void CalculateNecessaryHelper(decimal buyNecessarySum, decimal sellNecessarySum,
               decimal buyQuantitySum, decimal sellQuantitySum, ref decimal partialPhysicalNecessarySum,
               out decimal netNecessary, out decimal hedgeNecessary)
        {
            this.Calculator.CalculateNecessary(buyNecessarySum, sellNecessarySum, buyQuantitySum, sellQuantitySum, ref partialPhysicalNecessarySum, out netNecessary, out hedgeNecessary);
        }

        public bool Equals(Instrument other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Instrument;
            return this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public static bool operator ==(Instrument left, Instrument right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Instrument left, Instrument right)
        {
            return !(left == right);
        }
    }
}
