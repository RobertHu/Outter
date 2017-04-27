using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.InstrumentBusiness
{
    internal sealed class PhysicalInstrumentOrderCollector : OrderCollector
    {
        internal PhysicalInstrumentOrderCollector(PhysicalInstrument owner)
            : base(owner)
        {
        }
    }
}
