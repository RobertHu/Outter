using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Agent
{
    public partial class Transaction : IEquatable<Transaction>, IEqualityComparer<Transaction>
    {
        public XmlNode ToXmlNode(bool isForReport = false)
        {
            return TransactionXmlService.ToXmlNode(this, isForReport);
        }

        #region IEqualable and IEqualableComparer interface methods
        public bool Equals(Transaction other)
        {
            if (other == null) return false;
            return this.Id.Equals(other.Id);
        }

        public bool Equals(Transaction x, Transaction y)
        {
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(Transaction obj)
        {
            return this.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this.Equals((Transaction)obj);
        }

        public static bool operator ==(Transaction left, Transaction right)
        {
            if (object.ReferenceEquals(left, right)) return true;
            if ((object)left == null || (object)right == null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Transaction left, Transaction right)
        {
            return !(left == right);
        }
        #endregion
    }
}
