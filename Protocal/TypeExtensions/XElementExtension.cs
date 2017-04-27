using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Protocal.TypeExtensions
{
    public static class XElementExtension
    {
        private sealed class Cache<T>
        {
            internal static Func<string, T> Get;
        }

        static XElementExtension()
        {
            Cache<int>.Get = s => s.XmlToInt32();
            Cache<int?>.Get = s => string.IsNullOrEmpty(s) ? new Nullable<Int32>() : s.XmlToInt32();
            Cache<Int64>.Get = s => s.XmlToInt64();
            Cache<decimal>.Get = s => s.XmlToDecimal();
            Cache<Guid>.Get = s => s.XmlToGuid();
            Cache<DateTime>.Get = s => s.ToDateTime();
        }

        public static XmlNode ToXmlNode(this XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlReader);
                return xmlDoc.FirstChild;
            }
        }

        public static bool HasAttribute(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName) != null;
        }

        public static bool DBAttToBoolean(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value == "1" ? true : false;
        }

        public static bool AttrToBoolean(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.XmlToBoolean();
        }

        public static Int32 AttrToInt32(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.XmlToInt32();
        }

        public static Int64 AttrToInt64(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.XmlToInt64();
        }
        public static decimal AttrToDecimal(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.XmlToDecimal();
        }

        public static Guid AttrToGuid(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.XmlToGuid();
        }

        public static DateTime AttrToDateTime(this XElement xElement, string attrName)
        {
            return xElement.Attribute(attrName).Value.ToDateTime();
        }

        public static T Get<T>(this XElement xElement, string attrName)
        {
            if (!xElement.HasAttribute(attrName)) return default(T);
            string value = xElement.Attribute(attrName).Value;
            if (string.IsNullOrEmpty(value)) return default(T);
            return Cache<T>.Get(value);
        }

        //public static T ConvertTo<T>(this XElement xElement, string attrName)
        //{
        //    string value = xElement.Attribute(attrName).Value;
        //    if (string.IsNullOrEmpty(value)) return default(T);
        //    Type type = typeof(T);
        //    if (type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition())
        //    {
        //        type = Nullable.GetUnderlyingType(type);
        //    }
        //    return (T)(type.IsEnum ? Enum.ToObject(type, Convert.ToInt32(value)) : Convert.ChangeType(value, type));
        //}

    }
}
