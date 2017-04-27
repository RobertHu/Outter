using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.DB.DBMapping
{
    public sealed class OrderDayHistory
    {
        internal OrderDayHistory() { }

        internal OrderDayHistory(IDBRow  dr)
        {
            this.TradeDay = (DateTime)dr["TradeDay"];
            this.OrderID = (Guid)dr["OrderID"];
            this.InstrumentID = (Guid)dr["InstrumentID"];
            this.AccountID = (Guid)dr["AccountID"];
            this.CurrencyID = (Guid)dr["CurrencyID"];
            this.DayInterestPLNotValued = dr.GetColumn<decimal>("DayInterestPLNotValued");
            this.DayStoragePLNotValued = dr.GetColumn<decimal>("DayStoragePLNotValued");

            this.InterestPLValued = dr.GetColumn<decimal>("InterestPLValued");
            this.StoragePLValued = dr.GetColumn<decimal>("StoragePLValued");
            this.TradePLValued = dr.GetColumn<decimal>("TradePLValued");

            this.InterestPLFloat = dr.GetColumn<decimal>("InterestPLFloat");
            this.StoragePLFloat = dr.GetColumn<decimal>("StoragePLFloat");
            this.TradePLFloat = dr.GetColumn<decimal>("TradePLFloat");

            this.LotBalance = dr.GetColumn<decimal>("LotBalance");
            this.StoragePerLot = dr.GetColumn<decimal>("StoragePerLot");
            this.InterestPerLot = dr.GetColumn<decimal>("InterestPerLot");
        }

        public DateTime TradeDay { get; set; }
        public Guid OrderID { get; set; }
        internal Guid InstrumentID { get; set; }
        internal Guid AccountID { get; set; }
        internal Guid CurrencyID { get; set; }
        internal decimal DayInterestPLNotValued { get; set; }
        internal decimal DayStoragePLNotValued { get; set; }

        internal decimal InterestPLValued { get; set; }
        internal decimal StoragePLValued { get; set; }
        internal decimal TradePLValued { get; set; }

        internal decimal InterestPLFloat { get; set; }
        internal decimal StoragePLFloat { get; set; }
        internal decimal TradePLFloat { get; set; }

        internal decimal LotBalance { get; set; }
        internal decimal StoragePerLot { get; set; }
        internal decimal InterestPerLot { get; set; }

        public override string ToString()
        {
            return string.Format("orderId={0}, TradeDay={1}, instrumentId={2}, accountId={3}", this.OrderID, this.TradeDay, this.InstrumentID, this.AccountID);
        }

    }
}
