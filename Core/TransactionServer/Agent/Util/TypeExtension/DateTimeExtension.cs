using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Util.TypeExtension
{
    public static class DateTimeExtension
    {
        public static string ToYYYY_MM_DDHH_MM_SSFormat(this DateTime source)
        {
            return source.ToString("yyyy-MM-dd HH:mm:ss");
        }

        internal static int GetDateDiff(this DateTime dt1, DateTime dt2)
        {
            return dt1 > dt2 ? (dt1 - dt2).Days : (dt2 - dt1).Days;
        }

    }
}
