using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL
{
    internal sealed class TradePolicyDetailNotFountException : Exception
    {
        internal TradePolicyDetailNotFountException(Guid instrumentId, Guid tradePolicyId)
            : base(string.Format("instrumentId = {0}, tradePolicyId = {1}", instrumentId, tradePolicyId))
        {
            this.InstrumentId = instrumentId;
            this.TradePolicyId = tradePolicyId;
        }

        internal Guid InstrumentId { get; private set; }
        internal Guid TradePolicyId { get; private set; }
    }

    internal sealed class InitializeEntityFromDBException : Exception
    {
        internal InitializeEntityFromDBException(string entityName, string columnName)
        {
            this.EntityName = entityName;
            this.ColumnName = columnName;
        }

        internal string EntityName { get; private set; }
        internal string ColumnName { get; private set; }

        public override string ToString()
        {
            return string.Format("EntityName = {0}, ColumnName = {1}", this.EntityName, this.ColumnName);
        }
    }

    internal sealed class OpenOrderNotFoundException : Exception
    {
        internal OpenOrderNotFoundException(Guid openOrderId, Guid closeOrderId)
        {
            this.OpenOrderId = openOrderId;
            this.CloseOrderId = closeOrderId;
        }

        internal Guid OpenOrderId { get; private set; }
        internal Guid CloseOrderId { get; private set; }

        public override string ToString()
        {
            return string.Format("openOrderID = {0}, closeOrderId = {1}", this.OpenOrderId, this.CloseOrderId);
        }
    }

}
