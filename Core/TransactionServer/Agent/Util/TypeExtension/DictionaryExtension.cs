using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Util.TypeExtension
{
    internal static class DictionaryExtension
    {
        internal static void AddDecimalValue<T>(this Dictionary<T, decimal> dict, T key, decimal value)
        {
            decimal lastValue;
            if (!dict.TryGetValue(key, out lastValue))
            {
                dict.Add(key, value);
            }
            else
            {
                dict[key] = lastValue + value;
            }
        }
    }
}
