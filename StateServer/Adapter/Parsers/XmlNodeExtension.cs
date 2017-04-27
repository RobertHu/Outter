using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace iExchange.StateServer.Adapter.Parsers
{
    internal static class XmlNodeExtension
    {
        internal static object GetAttrValue(this XmlNode node, string name, Type type)
        {
            if (type == typeof(Int32))
            {
                return node.GetInt32AttrValue(name);
            }
            if (type == typeof(Int64))
            {
                return node.GetLongAttrValue(name);
            }
            else if (type == typeof(DateTime))
            {
                return node.GetDateTimeAttrValue(name);
            }
            else if (type == typeof(Guid))
            {
                return node.GetGuidAttrValue(name);
            }
            else if (type == typeof(bool))
            {
                return node.GetBoolAttrValue(name);
            }
            else if (type == typeof(Guid?))
            {
                return node.GetNullableGuidValue(name);
            }
            else if (type == typeof(decimal))
            {
                return node.GetDecimalAttrValue(name);
            }
            else if (type.IsEnum)
            {
                return Enum.ToObject(type, node.GetInt32AttrValue(name));
            }
            else
            {
                throw new ArgumentException(string.Format("name = {0}, type = {1}", name, type));
            }
        }


        internal static Int32 GetInt32AttrValue(this XmlNode node, string attrName)
        {
            return XmlConvert.ToInt32(node.GetAttrValue(attrName));
        }

        internal static DateTime GetDateTimeAttrValue(this XmlNode node, string attrName)
        {
            return DateTime.Parse(node.GetAttrValue(attrName));
        }

        internal static Guid GetGuidAttrValue(this XmlNode node, string attrName)
        {
            return XmlConvert.ToGuid(node.GetAttrValue(attrName));
        }

        internal static bool GetBoolAttrValue(this XmlNode node, string attrName)
        {
            return XmlConvert.ToBoolean(node.GetAttrValue(attrName));
        }

        internal static long GetLongAttrValue(this XmlNode node, string attrName)
        {
            return XmlConvert.ToInt64(node.GetAttrValue(attrName));
        }

        internal static decimal GetDecimalAttrValue(this XmlNode node, string attrName)
        {
            return XmlConvert.ToDecimal(node.GetAttrValue(attrName));
        }

        internal static Guid? GetNullableGuidValue(this XmlNode node, string attrName)
        {
            return node.HasAttribute(attrName) ? node.GetGuidAttrValue(attrName) : (Guid?)null;
        }


        internal static string GetAttrValue(this XmlNode node, string attrName)
        {
            return node.Attributes[attrName].Value;
        }

        internal static bool HasAttribute(this XmlNode node, string attrName)
        {
            return node.Attributes[attrName] != null;
        }

    }
}