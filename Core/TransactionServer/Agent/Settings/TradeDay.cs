using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    internal struct TimeRange
    {
        private DateTime _beginTime;
        private DateTime _endTime;

        internal TimeRange(DateTime beginTime, DateTime endTime)
        {
            _beginTime = beginTime;
            _endTime = endTime;
        }
        internal DateTime BeginTime
        {
            get
            {
                return _beginTime;
            }
        }

        internal DateTime EndTime
        {
            get
            {
                return _endTime;
            }
        }

        internal bool Include(DateTime dt)
        {
            return this.BeginTime <= dt && dt <= this.EndTime;
        }

        internal bool IncludeWithoutEndpoint(DateTime dt)
        {
            return this.BeginTime < dt && dt < this.EndTime;
        }

        internal bool EqualBeginTime(DateTime dt)
        {
            return this.BeginTime == dt;
        }

        internal bool EqualEndTime(DateTime dt)
        {
            return this.EndTime == dt;
        }

        internal bool IncludeInMarketOnClose(DateTime dt)
        {
            var durationMinutes = Settings.Setting.Default.SystemParameter.MooMocAcceptDuration;
            return this.BeginTime <= dt && dt <= this.EndTime.AddMinutes(-durationMinutes);
        }

    }

  



    public sealed class TradeDay
    {
        internal DateTime Day { get; private set; }

        internal DateTime BeginTime { get; private set; }

        internal DateTime EndTime { get; private set; }

        internal bool IsTrading { get; private set; }

        public TradeDay(DateTime tradeDate, DateTime beginTime, DateTime endTime, bool isTrading)
        {
            this.Day = tradeDate;
            this.BeginTime = beginTime;
            this.EndTime = endTime;
            this.IsTrading = isTrading;
        }

        internal TradeDay(IDBRow dr)
        {
            this.Day = (DateTime)dr["TradeDay"];
            this.BeginTime = (DateTime)dr["BeginTime"];
            this.EndTime = (DateTime)dr["EndTime"];
            this.IsTrading = (bool)dr["IsTrading"];
        }

        internal bool Include(DateTime dt)
        {
            return this.BeginTime <= dt && dt <= this.EndTime;
        }

        internal bool IncludeWithoutEndpoint(DateTime dt)
        {
            return this.BeginTime < dt && dt < this.EndTime;
        }

        internal bool EqualBeginTime(DateTime dt)
        {
            return this.BeginTime == dt;
        }

        internal bool EqualEndTime(DateTime dt)
        {
            return this.EndTime == dt;
        }

        internal bool IncludeInMarketOnClose(DateTime dt)
        {
            var durationMinutes = Settings.Setting.Default.SystemParameter.MooMocAcceptDuration;
            return this.BeginTime <= dt && dt <= this.EndTime.AddMinutes(-durationMinutes);
        }


    }
}