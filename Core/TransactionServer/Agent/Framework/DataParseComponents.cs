using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Framework
{
    //internal sealed class XmlNodeHelper
    //{
    //    internal static string ParseValue(string attrName, XmlNode node)
    //    {
    //        if (node.Attributes[attrName] == null) return null;
    //        return node.Attributes[attrName].Value;
    //    }
    //}

    //internal sealed class DataRowHelper
    //{
    //    internal static string ParseValue(string colName, DataRow dr)
    //    {
    //        var value = Parse(colName, dr);
    //        return value == null ? null : (string)value;
    //    }

    //    internal static object Parse(string colName, DataRow dr)
    //    {
    //        if (dr[colName] == DBNull.Value) return null;
    //        return dr[colName];
    //    }
    //}


    //public abstract class ValueProvider
    //{
    //    #region Cache Definition
    //    protected static class Cache<T>
    //    {
    //        internal static Func<string, T> GetFromString;
    //        internal static Func<object, T> GetFromObj;
    //    }

    //    static ValueProvider()
    //    {
    //        Cache<string>.GetFromString = s => s;
    //        Cache<decimal?>.GetFromString = s =>
    //        {
    //            if (string.IsNullOrEmpty(s)) return null;
    //            return XmlConvert.ToDecimal(s);
    //        };
    //        Cache<decimal>.GetFromString = s =>
    //            {
    //                if (string.IsNullOrEmpty(s)) return 0;
    //                return XmlConvert.ToDecimal(s);
    //            };
    //        Cache<TradeOption>.GetFromString = s =>
    //            {
    //                int value = XmlConvert.ToInt32(s);
    //                return (TradeOption)value;
    //            };


    //        Cache<string>.GetFromObj = obj =>
    //        {
    //            if (obj == null) return null;
    //            return (string)obj;
    //        };
    //        Cache<decimal?>.GetFromObj = obj =>
    //        {
    //            if (obj == null) return null;
    //            return (decimal)obj;
    //        };
    //        Cache<decimal>.GetFromObj = obj =>
    //        {
    //            if (obj == null) return 0;
    //            return (decimal)obj;
    //        };
    //        Cache<TradeOption>.GetFromObj = obj => (TradeOption)obj;
    //        Cache<OrderPhase?>.GetFromObj = obj => (OrderPhase)obj;

    //    }
    //    #endregion
    //    private Dictionary<string, string> _attrMappingDict;

    //    protected ValueProvider(Dictionary<string, string> attrMappingDict = null)
    //    {
    //        _attrMappingDict = attrMappingDict;
    //    }

    //    internal T GetValue<T>(string attrName)
    //    {
    //        string mappingAttrName = GetMappingAttrName(attrName);
    //        return this.InnerGetValue<T>(mappingAttrName);
    //    }

    //    protected abstract T InnerGetValue<T>(string attrName);

    //    private string GetMappingAttrName(string originAttrName)
    //    {
    //        if (_attrMappingDict == null) return originAttrName;
    //        if (!_attrMappingDict.ContainsKey(originAttrName))
    //        {
    //            return originAttrName;
    //        }
    //        return _attrMappingDict[originAttrName];
    //    }
    //}


    //internal sealed class XmlValueProvider : ValueProvider
    //{
    //    private XmlNode _source;
    //    internal XmlValueProvider(XmlNode source, Dictionary<string, string> attrMappingDict = null) :
    //        base(attrMappingDict)
    //    {
    //        _source = source;
    //    }

    //    protected override T InnerGetValue<T>(string attrName)
    //    {
    //        string value = XmlNodeHelper.ParseValue(attrName, _source);
    //        return Cache<T>.GetFromString(value);
    //    }
    //}


    //internal sealed class DataRowValueProvider : ValueProvider
    //{
    //    private DataRow _source;
    //    internal DataRowValueProvider(DataRow source, Dictionary<string, string> attrMappingDict = null) :
    //        base(attrMappingDict)
    //    {
    //        _source = source;
    //    }

    //    protected override T InnerGetValue<T>(string attrName)
    //    {
    //        var obj = DataRowHelper.Parse(attrName, _source);
    //        return Cache<T>.GetFromObj(obj);
    //    }
    //}

    //internal sealed class DictonaryValueProvider : ValueProvider
    //{
    //    private Dictionary<string, object> _source;
    //    internal DictonaryValueProvider(Dictionary<string, object> source, Dictionary<string, string> attrMappingDict = null)
    //        : base(attrMappingDict)
    //    {
    //        _source = source;
    //    }

    //    protected override T InnerGetValue<T>(string attrName)
    //    {
    //        object obj = null;
    //        if (_source.TryGetValue(attrName, out obj))
    //        {
    //            return (T)obj;
    //        }
    //        return default(T);
    //    }
    //}

}
