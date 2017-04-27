using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    public sealed class StateServerWebService : Protocal.IStateServerWebService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StateServerWebService));
        public void NotifyManagerStarted(string managerAddress, string exchangeCode)
        {
            Logger.Info(string.Format("managerAddress = {0}, exchangeCode = {1}", managerAddress, exchangeCode));
            ManagerServiceProxy.Default.Initialize(managerAddress, exchangeCode);
        }
    }
}
