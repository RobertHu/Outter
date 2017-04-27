using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Agent.Util.TypeExtension
{
    public static class ToXmlStringExtension
    {
        private static Type[] _supportedTypes = {typeof(int), typeof(decimal),
                                                typeof(Guid), typeof(DateTime)};
        private class Cache<T>
        {
            public static Func<T, string> Get;
        }
        static ToXmlStringExtension()
        {
            Cache<Guid>.Get = s => XmlConvert.ToString(s);
            Cache<int>.Get = s => XmlConvert.ToString(s);
            Cache<decimal>.Get = s => XmlConvert.ToString(s);
            Cache<DateTime>.Get = s => s.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public static string ToXmlString<T>(this T source) where T : struct
        {
            Type targetType = typeof(T);
            if(!_supportedTypes.Contains(targetType, EqualityComparer<Type>.Default))
            {
                throw new ArgumentOutOfRangeException(string.Format("type: {0} to xml string is not supported",targetType.ToString()));
            }
            return Cache<T>.Get(source);
        }

    }

}
