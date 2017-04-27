using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Protocal.TypeExtensions
{
    public static class DataRowExtension
    {
        private static class Cache<T>
        {
            public static Func<object, T> Get;
        }

        static DataRowExtension()
        {
            Cache<Guid?>.Get = m =>
            {
                if (m == DBNull.Value)
                {
                    return new Nullable<Guid>();
                }
                return new Nullable<Guid>((Guid)m);
            };

            Cache<decimal>.Get = m => m == DBNull.Value ? 0m : (decimal)m;
        }

        public static T GetColumn<T>(this DataRow row, string columnName)
        {
            try
            {
                var value = row[columnName];
                if (value == DBNull.Value) return default(T);
                return (T)value;
            }
            catch
            {
                throw;
            }
        }

        public static bool ExistsColumn(this IDBRow row, string columnName)
        {
            return row.Contains(columnName);
        }

        public static T GetColumn<T>(this IDBRow row, string columnName)
        {
            try
            {
                var value = row[columnName];
                if (value == DBNull.Value) return default(T);
                return (T)value;
            }
            catch
            {
                throw;
            }
        }

        public static bool ExistsColumn(this DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName);
        }



    }

}
