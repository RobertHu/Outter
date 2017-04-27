using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

namespace iExchange.StateServer.Adapter.CommonTypes
{
    public class TradeDay
    {
        private DateTime day;
        private DateTime beginTime;
        private DateTime endTime;
        private bool isTrading;

        #region Common public properties definition
        public DateTime Day
        {
            get { return this.day; }
        }
        public DateTime BeginTime
        {
            get { return this.beginTime; }
        }
        public DateTime EndTime
        {
            get { return this.endTime; }
        }
        public bool IsTrading
        {
            get { return this.isTrading; }
        }
        #endregion Common public properties definition

        public TradeDay(DataRow tradeDayRow)
        {
            this.day = (DateTime)tradeDayRow["TradeDay"];
            this.beginTime = (DateTime)tradeDayRow["BeginTime"];
            this.endTime = (DateTime)tradeDayRow["EndTime"];
            this.isTrading = (bool)tradeDayRow["IsTrading"];
        }
    }
}