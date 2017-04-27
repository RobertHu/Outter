using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Core.TransactionServer.Agent.Framework
{
    public delegate T Convert<T>(string value);

    public static class BusinessDataConverter
    {
        public static Guid? ToNullableGuidConverter(string value)
        {
            return new Nullable<Guid>(XmlConvert.ToGuid(value));
        }

        public static DateTime? ToNullableDateTimeConverter(string value)
        {
            return null;
        }
    }
}
