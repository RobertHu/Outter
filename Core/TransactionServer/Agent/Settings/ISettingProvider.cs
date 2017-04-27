using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Settings
{
    internal interface ISettingProvider
    {
        Setting Setting { get; }
    }
}
