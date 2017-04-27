using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace Protocal.TypeExtensions
{
    public static class XmlStringExtension
    {
        private sealed class Cache<T>
        {
            internal static Func<string, T> Get;
        }

        static XmlStringExtension()
        {
            Cache<Int32>.Get = s => XmlConvert.ToInt32(s);
            Cache<Int64>.Get = s => XmlConvert.ToInt64(s);
            Cache<bool>.Get = s => XmlConvert.ToBoolean(s);
            Cache<Guid>.Get = s => XmlConvert.ToGuid(s);
            Cache<decimal>.Get = s => XmlConvert.ToDecimal(s);
            Cache<DateTime>.Get = s => Convert.ToDateTime(s);
        }

        public static bool XmlToBoolean(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return XmlConvert.ToBoolean(value.ToLower());
        }

        public static int XmlToInt32(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return XmlConvert.ToInt32(value);
        }

        public static Int64 XmlToInt64(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return XmlConvert.ToInt64(value);
        }

        public static Guid XmlToGuid(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return XmlConvert.ToGuid(value);
        }

        public static decimal XmlToDecimal(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return XmlConvert.ToDecimal(value);
        }

        public static DateTime ToDateTime(this string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));
            return Convert.ToDateTime(value);
        }

        public static T Get<T>(this string value)
        {
            if (string.IsNullOrEmpty(value.Trim())) return default(T);
            string inputValue = value.Trim();
            return Cache<T>.Get(inputValue);
        }
    }
}
