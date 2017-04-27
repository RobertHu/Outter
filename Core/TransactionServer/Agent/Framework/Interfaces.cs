using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.TransactionServer.Agent.Settings;

namespace Core.TransactionServer.Agent.Framework
{
    public interface IQuotePolicyProvider
    {
        Guid PublicQuotePolicyId { get; }
        Guid? PrivateQuotePolicyId { get; }
    }

    public interface IPriceParameterProvider
    {
        int NumeratorUnit { get; }
        int Denominator { get; }
    }

    internal delegate bool QuotePolicyHandle<T>(Guid quotePolicyId, out T result);


    internal static class QuotePolicyPrividerHelper
    {
        public static T Get<T>(this IQuotePolicyProvider provider, QuotePolicyHandle<T> handler)
        {
            T result;
            if (provider.PrivateQuotePolicyId == null)
            {
                handler(provider.PublicQuotePolicyId, out result);
            }
            else
            {
                if (!handler(provider.PrivateQuotePolicyId.Value, out result))
                {
                    handler(provider.PublicQuotePolicyId, out result);
                }
            }
            return result;
        }
    }

}