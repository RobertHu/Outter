using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Util
{
    internal static class HashCodeGenerator
    {
        internal static int Calculate(int[] fieldHashCodes)
        {
            int result = 17;
            foreach (var eachHashCode in fieldHashCodes)
            {
                result = 31 * result + eachHashCode;
            }
            return result;
        }

        internal static int Calculate(int hashCode1, int hashCode2)
        {
            int result = 17;
            result = 31 * result + hashCode1;
            result = 31 * result + hashCode2;
            return result;
        }

        internal static int Calculate(int hashCode1, int hashCode2, int hashCode3)
        {
            int result = 17;
            result = 31 * result + hashCode1;
            result = 31 * result + hashCode2;
            result = 31 * result + hashCode3;
            return result;
        }


    }
}
