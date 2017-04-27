using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Core.TransactionServer.Agent.Reset
{
    internal abstract class OrderFactory
    {
        internal abstract Order Create(DataRow data);

        protected void Parse(DataRow data)
        {

        }

    }

    internal sealed class GeneralOrderFactory : OrderFactory
    {
        internal override Order Create(DataRow data)
        {
            throw new NotImplementedException();
        }
    }


}
