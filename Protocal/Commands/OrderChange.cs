using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal.Commands
{
    public enum OrderChangeType
    {
        None,
        Placing,
        Placed,
        Canceled,
        Deleted,
        Executed,
        Cut,
        Hit,
        Changed
    }



    public class OrderPhaseChange : IEquatable<OrderPhaseChange>
    {
        public OrderPhaseChange(OrderChangeType changeType, Order source)
        {
            this.ChangeType = changeType;
            this.Source = source;
        }

        public OrderChangeType ChangeType { get; private set; }
        public Order Source { get; private set; }

        public Transaction Tran { get { return this.Source.Owner; } }


        public bool Equals(OrderPhaseChange other)
        {
            return this.Source.Id == other.Source.Id;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((OrderPhaseChange)obj);
        }

        public override int GetHashCode()
        {
            return this.Source.Id.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("OrderId = {0}, changeType = {1}", this.Source.Id, this.ChangeType);
        }

    }
}
