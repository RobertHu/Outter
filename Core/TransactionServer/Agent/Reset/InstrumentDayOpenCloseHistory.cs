using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.CommonSetting;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.Reset
{
    internal sealed class InstrumentDayOpenCloseHistory
    {
        internal InstrumentDayOpenCloseHistory() { }

        internal InstrumentDayOpenCloseHistory(IDBRow dr)
        {
            this.TradeDay = (DateTime)dr["TradeDay"];
            this.InstrumentID = (Guid)dr["InstrumentID"];
            this.DayOpenTime = dr.GetColumn<DateTime?>("DayOpenTime");
            this.DayCloseTime = dr.GetColumn<DateTime?>("DayCloseTime");
            this.ValueDate = dr.GetColumn<DateTime?>("ValueDate");
            this.RealValueDate = dr.GetColumn<DateTime?>("RealValueDate");
        }

        internal DateTime TradeDay { get; set; }
        internal Guid InstrumentID { get; set; }
        internal DateTime? DayOpenTime { get; set; }
        internal DateTime? DayCloseTime { get; set; }
        internal DateTime? ValueDate { get; set; }
        internal DateTime? RealValueDate { get; set; }
    }
}
