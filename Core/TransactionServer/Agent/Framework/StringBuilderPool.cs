using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Framework
{
    internal sealed class StringBuilderPool : Protocal.PoolBase<StringBuilder>
    {
        internal static readonly StringBuilderPool Default = new StringBuilderPool();

        internal StringBuilder Get()
        {
            return this.Get(() => new StringBuilder(1000), m => m.Clear());
        }

    }
}
