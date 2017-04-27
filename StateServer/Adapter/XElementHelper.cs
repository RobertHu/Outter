using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Reflection;

namespace iExchange.StateServer.Adapter
{
    internal class XmlFillable
    {
        private static Dictionary<Type, MethodInfo> parsers = new Dictionary<Type, MethodInfo>();

        protected XElement _currentFillElement = null;

        static XmlFillable()
        {
            Type type = typeof(decimal);
            parsers.Add(type, type.GetMethod("Parse", new Type[] { typeof(string) }));
            Type nullableType = typeof(decimal?);
            parsers.Add(nullableType, type.GetMethod("Parse", new Type[] { typeof(string) }));

            type = typeof(int);
            parsers.Add(type, type.GetMethod("Parse", new Type[] { typeof(string) }));
            nullableType = typeof(int?);
            parsers.Add(nullableType, type.GetMethod("Parse", new Type[] { typeof(string) }));

            type = typeof(DateTime);
            parsers.Add(type, type.GetMethod("Parse", new Type[] { typeof(string) }));
            nullableType = typeof(DateTime?);
            parsers.Add(nullableType, type.GetMethod("Parse", new Type[] { typeof(string) }));

            type = typeof(bool);
            parsers.Add(type, type.GetMethod("Parse", new Type[] { typeof(string) }));
            nullableType = typeof(bool?);
            parsers.Add(nullableType, type.GetMethod("Parse", new Type[] { typeof(string) }));

            type = typeof(Guid);
            parsers.Add(type, type.GetMethod("Parse", new Type[] { typeof(string) }));
            nullableType = typeof(Guid?);
            parsers.Add(nullableType, type.GetMethod("Parse", new Type[] { typeof(string) }));
        }

        protected void SetCurrentXElement(XElement element)
        {
            this._currentFillElement = element;
        }

        protected void FillProperty<TValue>(string attributeName, string propertyName = null)
        {
            XAttribute attribute = this._currentFillElement.Attribute(attributeName);
            if (attribute != null)
            {
                Type valueType = typeof(TValue);

                object value = default(TValue);
                if (valueType == typeof(string))
                {
                    value = attribute.Value;
                }
                else if (valueType.IsEnum)
                {
                    value = Enum.ToObject(typeof(TValue), int.Parse(attribute.Value));
                }
                else
                {
                    value = parsers[valueType].Invoke(null, new object[] { attribute.Value });
                }

                if (propertyName == null) propertyName = attributeName;
                this.GetType().GetProperty(propertyName, BindingFlags.Instance| BindingFlags.NonPublic).GetSetMethod(true).Invoke(this, new object[] { value });
            }
        }
    }
}