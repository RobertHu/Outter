using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Settings
{
    public struct TradingTime
    {
        private DateTime dayOpenTime;
        private DateTime dayCloseTime;
        private TimeSpan acceptTimeSpan;

        public TradingTime(DateTime dayOpenTime, DateTime dayCloseTime, TimeSpan acceptTimeSpan)
        {
            this.dayOpenTime = dayOpenTime;
            this.dayCloseTime = dayCloseTime;
            this.acceptTimeSpan = acceptTimeSpan;
        }

        public DateTime DayOpenTime
        {
            get { return this.dayOpenTime; }
        }

        public DateTime DayCloseTime
        {
            get { return this.dayCloseTime; }
        }

        public TimeSpan AcceptTimeSpan
        {
            get { return this.acceptTimeSpan; }
        }
    }
}