using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Settings
{
    public struct QuotationParameter : IEquatable<QuotationParameter>
    {
        private bool isNormal;
        private int numerator;
        private int denominator;

        public static readonly QuotationParameter Invalid = new QuotationParameter(false, 0, 0);

        public QuotationParameter(bool isNormal, int numerator, int denominator)
        {
            this.isNormal = isNormal;
            this.numerator = numerator;
            this.denominator = denominator;
        }

        public bool IsNormal
        {
            get { return this.isNormal; }
        }

        public int Numerator
        {
            get { return this.numerator; }
        }

        public int Denominator
        {
            get { return this.denominator; }
        }

        public override int GetHashCode()
        {
            return this.IsNormal.GetHashCode() ^ this.Numerator.GetHashCode() ^ this.Denominator.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals((QuotationParameter)obj);
        }

        public bool Equals(QuotationParameter other)
        {
            return this.isNormal == other.isNormal && this.Numerator == other.Numerator && this.Denominator == other.Denominator;
        }

        public static bool operator ==(QuotationParameter qp1, QuotationParameter qp2)
        {
            return qp1.Equals(qp2);
        }

        public static bool operator !=(QuotationParameter qp1, QuotationParameter qp2)
        {
            return !(qp1 == qp2);
        }
    }
}