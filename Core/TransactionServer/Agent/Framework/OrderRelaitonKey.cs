using Core.TransactionServer.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Framework
{
    internal struct OrderRelationKey : IEquatable<OrderRelationKey>
    {
        private Guid _openOrderId;
        private Guid _closeOrderId;

        private int? _hashCode;

        internal OrderRelationKey(Guid openOrderId, Guid closeOrderId)
        {
            _openOrderId = openOrderId;
            _closeOrderId = closeOrderId;
            _hashCode = null;
        }

        internal Guid OpenOrderId { get { return _openOrderId; } }

        internal Guid CloseOrderId { get { return _closeOrderId; } }

        public bool Equals(OrderRelationKey other)
        {
            return this.OpenOrderId.Equals(other.OpenOrderId) && this.CloseOrderId.Equals(other.CloseOrderId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals((OrderRelationKey)obj);
        }

        public override int GetHashCode()
        {
            if (_hashCode == null)
            {
                _hashCode = HashCodeGenerator.Calculate(this.OpenOrderId.GetHashCode(), this.CloseOrderId.GetHashCode());
            }
            return _hashCode.Value;
        }


    }
}
